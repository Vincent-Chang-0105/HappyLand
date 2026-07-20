using UnityEngine;
using System;
using AudioSystem;
using DG.Tweening;

/// <summary>
/// Unified gesture state machine for cooking stations.
/// Replaces the separate TossingState and ShakingState machines that were embedded in Pan.cs.
/// Pan wires Begin()/Stop()/Reset() and listens to the three events.
/// </summary>
public class GestureStateMachine : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TossGestureDetector gestureDetector;
    [SerializeField] private TossPrompt prompt;

    [Header("Toss Animation")]
    [SerializeField] private float tossHeight = 0.5f;
    [SerializeField] private float tossDuration = 0.4f;
    [SerializeField] private float tossRotation = 15f;

    [Header("Ingredient Toss")]
    [SerializeField] private float ingredientTossHeight = 1.0f;
    [SerializeField] private float ingredientFlipRotation = 360f;
    [SerializeField] private float ingredientSpreadX = 0.3f;
    [SerializeField] private float landingPositionVariance = 0.25f;
    [SerializeField] private float landingRotationVariance = 30f;

    // Events
    public event Action OnGestureComplete;
    public event Action OnAllGesturesComplete;
    public event Action<float> OnGestureProgress;

    private enum State { Idle, WaitingForGesture, GestureAnimating, Cooking }
    private State currentState = State.Idle;

    private int completedGestures;
    private int requiredGestures;
    private float cookInterval;
    private float cookTimer;

    // Pan reference for animation (set by Pan on Begin)
    private Transform panTransform;
    private Vector3 originalPanPosition;
    private Quaternion originalPanRotation;
    private System.Collections.Generic.List<GameObject> ingredients;

    public int CompletedGestures => completedGestures;
    public bool IsActive => currentState != State.Idle;

    #region Lifecycle

    private void Awake()
    {
        if (gestureDetector != null)
        {
            gestureDetector.OnTossComplete  += HandleTossComplete;
            gestureDetector.OnTossProgress  += HandleTossProgress;
            gestureDetector.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (gestureDetector != null)
        {
            gestureDetector.OnTossComplete  -= HandleTossComplete;
            gestureDetector.OnTossProgress  -= HandleTossProgress;
        }
    }

    private void Update()
    {
        if (currentState == State.Cooking)
            UpdateCooking();
    }

    #endregion

    #region Public API

    /// <summary>Called by Pan when cooking starts.</summary>
    public void Begin(int required, float interval,
                      Transform pan, Vector3 panOrigPos, Quaternion panOrigRot,
                      System.Collections.Generic.List<GameObject> ingredientList)
    {
        requiredGestures   = required;
        cookInterval       = interval;
        completedGestures  = 0;
        cookTimer          = 0f;
        panTransform       = pan;
        originalPanPosition = panOrigPos;
        originalPanRotation = panOrigRot;
        ingredients        = ingredientList;

        ShowPrompt();
    }

    public void Stop()
    {
        currentState = State.Idle;
        HidePrompt();
        gestureDetector?.SetActive(false);
    }

    public void Reset()
    {
        Stop();
        completedGestures = 0;
        cookTimer = 0f;
    }

    #endregion

    #region Internal State

    private void ShowPrompt()
    {
        currentState = State.WaitingForGesture;
        prompt?.ShowPrompt(completedGestures, requiredGestures);
        gestureDetector?.SetActive(true);
    }

    private void HidePrompt()
    {
        prompt?.HidePrompt();
        gestureDetector?.SetActive(false);
    }

    private void HandleTossComplete()
    {
        if (currentState != State.WaitingForGesture) return;

        completedGestures++;
        TutorialEvents.TossCompleted();

        prompt?.ShowTossSuccess();
        HidePrompt();

        currentState = State.GestureAnimating;
        PlayTossAnimation();

        OnGestureComplete?.Invoke();
    }

    private void HandleTossProgress(float velocityProgress)
    {
        if (currentState == State.WaitingForGesture)
        {
            prompt?.UpdateVelocity(velocityProgress);
        }
    }

    private void OnTossAnimationComplete()
    {
        currentState = State.Cooking;
        cookTimer = 0f;
        UpdateProgressBar(0f);
    }

    private void UpdateCooking()
    {
        cookTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(cookTimer / cookInterval);
        UpdateProgressBar(progress);

        if (cookTimer < cookInterval) return;

        if (completedGestures < requiredGestures)
        {
            ShowPrompt();
        }
        else
        {
            currentState = State.Idle;
            OnAllGesturesComplete?.Invoke();
        }
    }

    private void UpdateProgressBar(float progress)
    {
        OnGestureProgress?.Invoke(progress);
    }

    #endregion

    #region Toss Animation

    private void PlayTossAnimation()
    {
        if (panTransform == null) return;

        PlayIngredientTossAnimation();

        panTransform.DOKill();

        var seq = DOTween.Sequence();
        seq.Append(panTransform.DOLocalMoveY(originalPanPosition.y + tossHeight, tossDuration * 0.5f)
            .SetEase(Ease.OutQuad));
        seq.Join(panTransform.DOLocalRotate(new Vector3(0, 0, tossRotation), tossDuration * 0.5f)
            .SetEase(Ease.OutQuad));
        seq.Append(panTransform.DOLocalMoveY(originalPanPosition.y, tossDuration * 0.5f)
            .SetEase(Ease.InQuad));
        seq.Join(panTransform.DOLocalRotate(originalPanRotation.eulerAngles, tossDuration * 0.5f)
            .SetEase(Ease.InQuad));
        seq.OnComplete(OnTossAnimationComplete);
    }

    private void PlayIngredientTossAnimation()
    {
        if (ingredients == null) return;

        for (int i = 0; i < ingredients.Count; i++)
        {
            var ingredient = ingredients[i];
            if (ingredient == null) continue;

            var t = ingredient.transform;
            t.DOKill();

            Vector3 startLocalPos = t.localPosition;
            float staggerDelay = i * 0.05f;

            Vector3 landingPos = new Vector3(
                UnityEngine.Random.Range(-landingPositionVariance, landingPositionVariance),
                startLocalPos.y,
                startLocalPos.z);
            float landingRotZ = UnityEngine.Random.Range(-landingRotationVariance, landingRotationVariance);

            var seq = DOTween.Sequence().SetDelay(staggerDelay);
            seq.Append(t.DOLocalMoveY(startLocalPos.y + ingredientTossHeight, tossDuration * 0.4f)
                .SetEase(Ease.OutQuad));
            seq.Join(t.DOLocalRotate(new Vector3(0, 0, ingredientFlipRotation), tossDuration * 0.4f,
                RotateMode.FastBeyond360).SetEase(Ease.Linear));
            seq.Append(t.DOLocalMove(landingPos, tossDuration * 0.6f)
                .SetEase(Ease.InBounce));
            seq.Join(t.DOLocalRotate(new Vector3(0, 0, landingRotZ), tossDuration * 0.6f)
                .SetEase(Ease.OutQuad));
        }
    }

    #endregion
}
