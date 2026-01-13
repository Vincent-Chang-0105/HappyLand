using UnityEngine;
using DG.Tweening;
using AudioSystem;

/// <summary>
/// Pan cooking station - specialized for frying ingredients
/// Features gesture-based tossing where players must make fast upward drag motions
/// Requires oil before accepting ingredients
/// </summary>
public class Pan : CookingStation
{
    [Header("Pan-Specific Settings")]
    [SerializeField] private bool hasOil = false;
    [SerializeField] private GameObject oilVisual;
    [SerializeField] private ParticleSystem oilEffect;

    [Header("Audio")]
    [SerializeField] private SoundData sizzleSound;

    [Header("Gesture-Based Tossing")]
    [SerializeField] private bool useGestureTossing = true;
    [SerializeField] private int requiredTosses = 3;
    [SerializeField] private float fryingDurationBetweenTosses = 2f;
    [SerializeField] private TossGestureDetector tossGestureDetector;
    [SerializeField] private TossPrompt tossPrompt;

    [Header("Toss Animation")]
    [SerializeField] private float tossHeight = 0.5f; // How high the pan moves during toss
    [SerializeField] private float tossDuration = 0.4f; // Duration of toss animation
    [SerializeField] private float tossRotation = 15f; // Slight rotation during toss

    // Tossing state
    private enum TossingState
    {
        NotStarted,
        WaitingForToss,
        TossAnimating,
        Frying
    }

    private TossingState currentTossState = TossingState.NotStarted;
    private int completedTosses = 0;
    private float fryingTimer = 0f;
    private Vector3 originalPanPosition;
    private Quaternion originalPanRotation;
    private SoundEmitter currentSizzleEmitter;

    #region Abstract Method Implementations

    protected override bool CanCookIngredient(GameObject ingredient)
    {
        IFryable fryable = ingredient.GetComponent<IFryable>();
        return fryable != null && fryable.CanBeFried();
    }

    protected override void StartCookingIngredient(GameObject ingredient)
    {
        IFryable fryable = ingredient.GetComponent<IFryable>();
        if (fryable != null)
        {
            fryable.StartFrying();
        }
    }

    protected override void StopCookingIngredient(GameObject ingredient)
    {
        IFryable fryable = ingredient.GetComponent<IFryable>();
        if (fryable != null)
        {
            fryable.StopFrying();
        }
    }

    protected override void CompleteCookingIngredient(GameObject ingredient)
    {
        IFryable fryable = ingredient.GetComponent<IFryable>();
        if (fryable != null)
        {
            fryable.CompleteFrying();
        }
    }

    protected override bool AdditionalBowlAcceptanceCheck()
    {
        // Pan requires oil before accepting ingredients
        if (!hasOil)
        {
            Debug.LogWarning("⚠️ Add oil to the pan first before frying!");
            return false;
        }
        return true;
    }

    protected override string GetCookingProcessName()
    {
        return "frying";
    }

    #endregion

    #region Unity Lifecycle

    protected override void Start()
    {
        base.Start();

        // Store original position and rotation for toss animation
        originalPanPosition = transform.localPosition;
        originalPanRotation = transform.localRotation;

        // Setup gesture detector if not assigned
        if (useGestureTossing && tossGestureDetector == null)
        {
            tossGestureDetector = gameObject.AddComponent<TossGestureDetector>();
        }

        if (tossGestureDetector != null)
        {
            tossGestureDetector.SetTossOrigin(transform);
            tossGestureDetector.SetActive(false);
            tossGestureDetector.OnTossComplete += OnTossGestureComplete;
            tossGestureDetector.OnTossProgress += OnTossGestureProgress;
        }

        // Find or create toss prompt
        if (useGestureTossing && tossPrompt == null)
        {
            // Try to find existing toss prompt in children
            tossPrompt = GetComponentInChildren<TossPrompt>(true);

            if (tossPrompt == null)
            {
                Debug.LogWarning("Pan: No TossPrompt component found. Please add a TossPrompt UI to the pan.");
            }
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        // Stop sizzle sound
        StopSizzleSound();

        // Unsubscribe from gesture detector events
        if (tossGestureDetector != null)
        {
            tossGestureDetector.OnTossComplete -= OnTossGestureComplete;
            tossGestureDetector.OnTossProgress -= OnTossGestureProgress;
        }
    }

    #endregion

    #region Gesture-Based Tossing Logic

    protected override void StartCooking()
    {
        if (isCooking || ingredientsInStation.Count == 0) return;

        // If gesture tossing is disabled, use base behavior
        if (!useGestureTossing)
        {
            base.StartCooking();
            return;
        }

        // Initialize gesture-based cooking
        isCooking = true;
        completedTosses = 0;
        currentTossState = TossingState.WaitingForToss;

        // Start cooking each ingredient
        foreach (GameObject ingredient in ingredientsInStation)
        {
            if (ingredient != null)
            {
                StartCookingIngredient(ingredient);
            }
        }

        // Start effects
        if (cookingEffect != null)
        {
            cookingEffect.Stop(); // Don't start yet, wait for first toss
        }

        // Show first toss prompt
        ShowTossPrompt();
    }

    protected override void UpdateCooking()
    {
        // If gesture tossing is disabled, use base timer behavior
        if (!useGestureTossing)
        {
            base.UpdateCooking();
            return;
        }

        // Update based on current tossing state
        switch (currentTossState)
        {
            case TossingState.WaitingForToss:
                // Just waiting for player to toss, no timer
                break;

            case TossingState.TossAnimating:
                // Animation is playing, no timer
                break;

            case TossingState.Frying:
                // Frying between tosses - increment timer
                fryingTimer += Time.deltaTime;

                // Update progress bar to show frying progress
                if (cookingProgressBar != null)
                {
                    float progress = fryingTimer / fryingDurationBetweenTosses;
                    cookingProgressBar.fillAmount = progress;
                    cookingProgressBar.color = Color.Lerp(Color.yellow, Color.orange, progress);
                }

                // After frying time is up, request next toss or complete
                if (fryingTimer >= fryingDurationBetweenTosses)
                {
                    if (completedTosses < requiredTosses)
                    {
                        // Request another toss
                        currentTossState = TossingState.WaitingForToss;
                        fryingTimer = 0f;
                        ShowTossPrompt();
                    }
                    else
                    {
                        // All tosses complete, finish cooking
                        CompleteCooking();
                    }
                }
                break;
        }

        // Update UI position (follow station)
        if (cookingMeterUI != null && cookingMeterUI.activeInHierarchy)
        {
            cookingMeterUI.transform.position = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        }
    }

    private void ShowTossPrompt()
    {
        // Stop sizzle sound when waiting for toss
        StopSizzleSound();

        if (tossPrompt != null)
        {
            tossPrompt.ShowPrompt(completedTosses, requiredTosses);
        }

        // Enable gesture detection
        if (tossGestureDetector != null)
        {
            tossGestureDetector.SetActive(true);
        }

        // Show UI
        if (cookingMeterUI != null)
        {
            cookingMeterUI.SetActive(true);
        }
    }

    private void HideTossPrompt()
    {
        if (tossPrompt != null)
        {
            tossPrompt.HidePrompt();
        }

        // Disable gesture detection
        if (tossGestureDetector != null)
        {
            tossGestureDetector.SetActive(false);
        }
    }

    private void OnTossGestureComplete()
    {
        // Ignore if not waiting for a toss
        if (currentTossState != TossingState.WaitingForToss)
            return;

        completedTosses++;

        Debug.Log($"Toss {completedTosses}/{requiredTosses} completed!");

        // Show success feedback
        if (tossPrompt != null)
        {
            tossPrompt.ShowTossSuccess();
        }

        // Hide prompt
        HideTossPrompt();

        // Play toss animation
        currentTossState = TossingState.TossAnimating;
        PlayTossAnimation();
    }

    private void OnTossGestureProgress(float velocityProgress)
    {
        // Update the toss prompt with current velocity
        if (tossPrompt != null && currentTossState == TossingState.WaitingForToss)
        {
            tossPrompt.UpdateVelocity(velocityProgress);
        }
    }

    private void PlayTossAnimation()
    {
        // Kill any existing tweens
        transform.DOKill();

        // Store original position
        Vector3 startPos = transform.localPosition;
        Quaternion startRot = transform.localRotation;

        // Create animation sequence
        Sequence tossSequence = DOTween.Sequence();

        // Move up and rotate slightly
        tossSequence.Append(transform.DOLocalMoveY(originalPanPosition.y + tossHeight, tossDuration * 0.5f).SetEase(Ease.OutQuad));
        tossSequence.Join(transform.DOLocalRotate(new Vector3(0, 0, tossRotation), tossDuration * 0.5f).SetEase(Ease.OutQuad));

        // Move back down and rotate back
        tossSequence.Append(transform.DOLocalMoveY(originalPanPosition.y, tossDuration * 0.5f).SetEase(Ease.InQuad));
        tossSequence.Join(transform.DOLocalRotate(originalPanRotation.eulerAngles, tossDuration * 0.5f).SetEase(Ease.InQuad));

        // When animation completes, start frying
        tossSequence.OnComplete(() =>
        {
            OnTossAnimationComplete();
        });
    }

    private void OnTossAnimationComplete()
    {
        // Start frying
        currentTossState = TossingState.Frying;
        fryingTimer = 0f;

        // Start/restart cooking effects
        if (cookingEffect != null)
        {
            cookingEffect.Play();
        }

        if (steamEffect != null)
        {
            steamEffect.Play();
        }

        // Play some visual feedback
        if (ingredientDropEffect != null)
        {
            ingredientDropEffect.Play();
        }

        // Play sizzle sound
        PlaySizzleSound();
    }

    protected override void CompleteCooking()
    {
        // Stop sizzle sound
        StopSizzleSound();

        // Call base implementation to complete frying for all ingredients
        base.CompleteCooking();

        // Notify plating manager about fried chickens
        if (PlatingManager.Instance != null)
        {
            foreach (GameObject ingredient in ingredientsInStation)
            {
                if (ingredient != null)
                {
                    PlatingManager.Instance.RegisterFriedChicken(ingredient);
                }
            }
        }
        else
        {
            Debug.LogWarning("Pan: PlatingManager not found! Cannot register fried chickens.");
        }

        // Remove oil after cooking is complete
        RemoveOil();
    }

    #endregion

    #region Audio Management

    private void PlaySizzleSound()
    {
        // Stop any existing sizzle sound first
        StopSizzleSound();

        // Play new sizzle sound if SoundManager is available
        if (SoundManager.Instance != null && sizzleSound != null)
        {
            currentSizzleEmitter = SoundManager.Instance
                .CreateSoundBuilder()
                .WithPosition(transform.position)
                .Play(sizzleSound);
        }
    }

    private void StopSizzleSound()
    {
        if (currentSizzleEmitter != null && currentSizzleEmitter.IsPlaying())
        {
            currentSizzleEmitter.Stop();
            currentSizzleEmitter = null;
        }
    }

    #endregion

    #region Oil Management

    /// <summary>
    /// Check if oil can be added to the pan
    /// </summary>
    public bool CanAcceptOil()
    {
        return !hasOil && !isCooking;
    }

    /// <summary>
    /// Add oil to the pan
    /// </summary>
    public void AddOil()
    {
        if (hasOil) return;

        hasOil = true;

        // Play oil effect
        if (oilEffect != null)
        {
            oilEffect.Play();
        }

        // Show oil visual
        if (oilVisual != null)
        {
            oilVisual.SetActive(true);
        }

        // Visual feedback - tint sprite yellow
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, Color.yellow, 0.3f);
        }

        Debug.Log("🛢️ Oil added to pan!");
    }

    /// <summary>
    /// Check if pan has oil
    /// </summary>
    public bool HasOil()
    {
        return hasOil;
    }

    /// <summary>
    /// Remove oil from the pan (called after cooking is complete)
    /// </summary>
    private void RemoveOil()
    {
        if (!hasOil) return;

        hasOil = false;

        // Hide oil visual
        if (oilVisual != null)
        {
            oilVisual.SetActive(false);
        }

        // Stop oil effect
        if (oilEffect != null)
        {
            oilEffect.Stop();
        }

        // Reset sprite color
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }

        Debug.Log("🛢️ Oil consumed! Add more oil to fry again.");
    }

    #endregion

    #region Seasoning Management (for backward compatibility with DraggableIngredient)

    /// <summary>
    /// Check if seasoning can be added
    /// </summary>
    public bool CanAcceptSeasoning()
    {
        return !isCooking;
    }

    /// <summary>
    /// Add a seasoning ingredient - delegates to SeasoningManager if available
    /// </summary>
    public void AddSeasoning(Ingredient seasoning)
    {
        if (!CanAcceptSeasoning())
        {
            Debug.LogWarning($"Cannot add seasoning while cooking!");
            return;
        }

        // Try to use SeasoningManager if available
        SeasoningManager seasoningMgr = GetComponent<SeasoningManager>();
        if (seasoningMgr != null)
        {
            seasoningMgr.AddSeasoning(seasoning);
        }
        else
        {
            // Fallback - just log if no manager is attached
            Debug.Log($"🧂 Added seasoning: {seasoning.ingredientName} (no SeasoningManager attached)");
        }
    }

    #endregion
}
