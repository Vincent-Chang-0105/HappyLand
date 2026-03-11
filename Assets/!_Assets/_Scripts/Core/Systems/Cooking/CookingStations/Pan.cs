using UnityEngine;
using System.Collections.Generic;
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
    [SerializeField] private SoundData panImpactSound;
    [SerializeField] private SoundData tossSuccessSound;
    [SerializeField] private SoundData allTossesCompleteSound;
    [SerializeField] private SoundData panBounceSound;

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

    [Header("Ingredient Toss Animation")]
    [SerializeField] private float ingredientTossHeight = 1.0f;
    [SerializeField] private float ingredientFlipRotation = 360f;
    [SerializeField] private float ingredientSpreadX = 0.3f;
    [SerializeField] private float landingPositionVariance = 0.25f;
    [SerializeField] private float landingRotationVariance = 30f;

    [Header("Pan Bob Animation")]
    [SerializeField] private float panBobHeightMin = 0.08f;
    [SerializeField] private float panBobHeightMax = 0.25f;
    [SerializeField] private float panBobDurationMin = 0.4f;
    [SerializeField] private float panBobDurationMax = 0.8f;

    [Header("Idle Sizzle Animation")]
    [SerializeField] private float sizzleShakeStrength = 0.03f;
    [SerializeField] private float sizzleShakeFrequency = 20f;

    [Header("Seasoning Requirement")]
    [SerializeField] private bool requiresSeasoning = true;

    [Header("Sinigang Mode")]
    [SerializeField] private bool hasWater = false;
    [SerializeField] private bool hasSinigangMix = false;
    [SerializeField] private GameObject waterVisual;
    [SerializeField] private Color sinigangBrothColor = new Color(0.8f, 1f, 0.8f);

    [Header("Noodle Mode")]
    [SerializeField] private bool hasNoodles = false;
    [SerializeField] private GameObject noodleObject = null;

    [Header("Mechado Mode")]
    [SerializeField] private bool hasCatsup = false;
    [SerializeField] private bool hasPowderedMilk = false;
    [SerializeField] private Color mechadoBrothColor = new Color(1f, 0.4f, 0.2f);

    [Header("Adobo Mode")]
    [SerializeField] private bool hasOnionGarlic = false;
    [SerializeField] private bool hasSoySauce = false;
    [SerializeField] private bool hasVinegar = false;
    [SerializeField] private bool hasSugar = false;
    [SerializeField] private bool hasLaurel = false;
    [SerializeField] private Color adoboBrothColor = new Color(0.35f, 0.2f, 0.08f);

    [Header("Pour Audio")]
    [SerializeField] private SoundData pourSound;
    [SerializeField] private SoundData ingredientAddSound;

    [Header("Gesture-Based Shaking (Sinigang)")]
    [SerializeField] private int requiredShakes = 3;
    [SerializeField] private float cookingDurationBetweenShakes = 2f;
    [SerializeField] private ShakeGestureDetector shakeGestureDetector;
    [SerializeField] private ShakePrompt shakePrompt;

    // Tossing state
    private enum TossingState
    {
        NotStarted,
        WaitingForSeasoning,
        WaitingForToss,
        TossAnimating,
        Frying
    }

    // Shaking state (sinigang)
    private enum ShakingState
    {
        NotStarted,
        WaitingForShake,
        ShakeAnimating,
        Cooking
    }

    private TossingState currentTossState = TossingState.NotStarted;
    private ShakingState currentShakeState = ShakingState.NotStarted;
    private int completedTosses = 0;
    private int completedShakes = 0;
    private float fryingTimer = 0f;
    private float shakeCookTimer = 0f;
    private Vector3 originalPanPosition;
    private Quaternion originalPanRotation;
    private SoundEmitter currentSizzleEmitter;
    private bool hasSeasoning = false;
    private Dictionary<GameObject, Vector3> ingredientBasePositions = new Dictionary<GameObject, Vector3>();
    private Sequence panBobSequence;

    // Pour state
    [Header("Pour Settings")]
    [SerializeField] private float pourTiltAngle = 45f;
    [SerializeField] private float pourTiltSmoothing = 8f;
    [SerializeField] private float pourDetectRadius = 1.5f;
    [SerializeField] private float pourPickupRadius = 1.5f;
    [SerializeField] private float pourDuration = 0.4f;

    private bool isPourable = false;
    private bool isPouring = false;
    private bool isDraggingPan = false;
    private Vector3 dragOffset;
    private Camera mainCamera;
    private PlatingStation nearbyPlatingStation;

    /// <summary>Whether the pan is in sinigang mode (water + sinigang mix added)</summary>
    public bool IsSinigangMode => hasWater && hasSinigangMix;

    /// <summary>Whether the pan is in noodle mode (water + noodles added)</summary>
    public bool IsNoodleMode => hasWater && hasNoodles;

    /// <summary>Whether the pan is in mechado mode (water + catsup + powdered milk added)</summary>
    public bool IsMechadoMode => hasWater && hasCatsup && hasPowderedMilk;

    /// <summary>Whether the pan is in adobo mode (oil + onion&garlic added)</summary>
    public bool IsAdoboMode => hasOil && hasOnionGarlic;

    /// <summary>Whether all adobo seasonings have been added</summary>
    public bool IsAdoboFullySeasoned => IsAdoboMode && hasSoySauce && hasVinegar && hasSugar && hasLaurel;

    #region Abstract Method Implementations

    protected override bool CanCookIngredient(GameObject ingredient)
    {
        if (IsAdoboMode)
        {
            IAdoboable adoboable = ingredient.GetComponent<IAdoboable>();
            return adoboable != null && adoboable.CanBeAdobo();
        }
        else if (IsMechadoMode)
        {
            IMechadoable mechadoable = ingredient.GetComponent<IMechadoable>();
            return mechadoable != null && mechadoable.CanBeMechado();
        }
        else if (IsNoodleMode)
        {
            INoodleable noodleable = ingredient.GetComponent<INoodleable>();
            return noodleable != null && noodleable.CanBeNoodled();
        }
        else if (IsSinigangMode)
        {
            ISinigangable sinigangable = ingredient.GetComponent<ISinigangable>();
            bool canCook = sinigangable != null && sinigangable.CanBeSiniganged();
            return canCook;
        }
        else
        {
            IFryable fryable = ingredient.GetComponent<IFryable>();
            bool canCook = fryable != null && fryable.CanBeFried();
            return canCook;
        }
    }

    protected override void StartCookingIngredient(GameObject ingredient)
    {
        if (IsAdoboMode)
        {
            ingredient.GetComponent<IAdoboable>()?.StartAdobo();
        }
        else if (IsMechadoMode)
        {
            ingredient.GetComponent<IMechadoable>()?.StartMechado();
        }
        else if (IsNoodleMode)
        {
            ingredient.GetComponent<INoodleable>()?.StartNoodling();
        }
        else if (IsSinigangMode)
        {
            ingredient.GetComponent<ISinigangable>()?.StartSiniganging();
        }
        else
        {
            ingredient.GetComponent<IFryable>()?.StartFrying();
        }
    }

    protected override void StopCookingIngredient(GameObject ingredient)
    {
        if (IsAdoboMode)
        {
            ingredient.GetComponent<IAdoboable>()?.StopAdobo();
        }
        else if (IsMechadoMode)
        {
            ingredient.GetComponent<IMechadoable>()?.StopMechado();
        }
        else if (IsNoodleMode)
        {
            ingredient.GetComponent<INoodleable>()?.StopNoodling();
        }
        else if (IsSinigangMode)
        {
            ingredient.GetComponent<ISinigangable>()?.StopSiniganging();
        }
        else
        {
            ingredient.GetComponent<IFryable>()?.StopFrying();
        }
    }

    protected override void CompleteCookingIngredient(GameObject ingredient)
    {
        if (IsAdoboMode)
        {
            ingredient.GetComponent<IAdoboable>()?.CompleteAdobo();
        }
        else if (IsMechadoMode)
        {
            ingredient.GetComponent<IMechadoable>()?.CompleteMechado();
        }
        else if (IsNoodleMode)
        {
            ingredient.GetComponent<INoodleable>()?.CompleteNoodling();
        }
        else if (IsSinigangMode)
        {
            ingredient.GetComponent<ISinigangable>()?.CompleteSiniganging();
        }
        else
        {
            ingredient.GetComponent<IFryable>()?.CompleteFrying();
        }
    }

    protected override bool AdditionalBowlAcceptanceCheck()
    {
        // Don't accept bowls while actively pouring
        if (isPouring) return false;

        // Don't accept new bowls while pan still has cooked food waiting to be poured
        if (isPourable) return false;

        // Adobo mode: requires oil + onion & garlic
        if (IsAdoboMode)
        {
            return true;
        }

        // Mechado mode: requires water + catsup + powdered milk
        if (IsMechadoMode)
        {
            return true;
        }

        // Noodle mode: requires water + noodles
        if (IsNoodleMode)
        {
            return true;
        }

        // Sinigang mode: requires water + sinigang mix
        if (IsSinigangMode)
        {
            return true;
        }

        // Frying mode: requires oil
        if (!hasOil)
        {
            Debug.LogWarning("Add oil to the pan first before frying!");
            return false;
        }

        return true;
    }

    protected override string GetCookingProcessName()
    {
        if (IsAdoboMode) return "adobo cooking";
        if (IsMechadoMode) return "mechado cooking";
        if (IsNoodleMode) return "noodle cooking";
        return IsSinigangMode ? "sinigang cooking" : "frying";
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

        // Setup shake gesture detector for sinigang mode
        if (shakeGestureDetector == null)
        {
            shakeGestureDetector = GetComponentInChildren<ShakeGestureDetector>(true);
        }

        if (shakeGestureDetector != null)
        {
            shakeGestureDetector.SetShakeOrigin(transform);
            shakeGestureDetector.SetActive(false);
            shakeGestureDetector.OnShakeComplete += OnShakeGestureComplete;
            shakeGestureDetector.OnShakeProgress += OnShakeGestureProgress;
            shakeGestureDetector.OnAllShakesComplete += OnAllShakeGesturesComplete;
        }

        // Find shake prompt
        if (shakePrompt == null)
        {
            shakePrompt = GetComponentInChildren<ShakePrompt>(true);
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

        if (shakeGestureDetector != null)
        {
            shakeGestureDetector.OnShakeComplete -= OnShakeGestureComplete;
            shakeGestureDetector.OnShakeProgress -= OnShakeGestureProgress;
            shakeGestureDetector.OnAllShakesComplete -= OnAllShakeGesturesComplete;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (isPourable && !isPouring)
        {
            HandlePourInput();
        }
    }

    #endregion

    #region Gesture-Based Tossing Logic

    protected override void StartCooking()
    {
        if (isCooking || ingredientsInStation.Count == 0) return;

        // Branch: adobo mode — needs all seasonings before tossing
        if (IsAdoboMode && !IsAdoboFullySeasoned)
        {
            currentTossState = TossingState.WaitingForSeasoning;
            NotificationManager.Instance.SetNewNotification("Add soy sauce, vinegar, sugar and laurel to the pan!");
            return;
        }

        // Branch: noodle mode uses shake gesture
        if (IsNoodleMode)
        {
            StartSinigangCooking();
            return;
        }

        // Branch: sinigang mode uses shake gesture
        if (IsSinigangMode)
        {
            StartSinigangCooking();
            return;
        }

        // If gesture tossing is disabled, use base behavior
        if (!useGestureTossing)
        {
            base.StartCooking();
            return;
        }

        // Check if seasoning is required before tossing
        if (requiresSeasoning && !hasSeasoning)
        {
            currentTossState = TossingState.WaitingForSeasoning;
            string seasoningMsg = IsMechadoMode ? "Add crackers to the pan!" : "Add seasoning to the pan before tossing!";
            // Note: adobo seasoning wait is handled above before this block
            NotificationManager.Instance.SetNewNotification(seasoningMsg);
            //ebug.Log("Add salt to the pan before tossing!");
            return;
        }

        // Initialize gesture-based cooking (frying)
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

        // Start sizzle immediately when ingredients hit the pan
        PlaySizzleSound();
        StartSizzleAnimation();

        if (cookingEffect != null)
            cookingEffect.Play();

        // Show first toss prompt
        ShowTossPrompt();
    }

    private void StartSinigangCooking()
    {
        isCooking = true;
        completedShakes = 0;
        currentShakeState = ShakingState.WaitingForShake;

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
            cookingEffect.Stop();
        }

        Debug.Log("Pan: Starting sinigang cooking - shake the pan!");
        ShowShakePrompt();
    }

    protected override void UpdateCooking()
    {
        // Branch: noodle mode (uses same shake gesture as sinigang)
        if (IsNoodleMode)
        {
            UpdateSinigangCooking();
            return;
        }

        // Branch: sinigang mode
        if (IsSinigangMode)
        {
            UpdateSinigangCooking();
            return;
        }

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

                // Update idle sizzle animation
                UpdateSizzleAnimation();

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

    private void UpdateSinigangCooking()
    {
        switch (currentShakeState)
        {
            case ShakingState.WaitingForShake:
                // Waiting for player shake gesture
                break;

            case ShakingState.Cooking:
                // Cooking between shakes
                shakeCookTimer += Time.deltaTime;

                UpdateSizzleAnimation();

                if (cookingProgressBar != null)
                {
                    float progress = shakeCookTimer / cookingDurationBetweenShakes;
                    cookingProgressBar.fillAmount = progress;
                    cookingProgressBar.color = Color.Lerp(Color.yellow, Color.green, progress);
                }

                if (shakeCookTimer >= cookingDurationBetweenShakes)
                {
                    if (completedShakes < requiredShakes)
                    {
                        currentShakeState = ShakingState.WaitingForShake;
                        shakeCookTimer = 0f;
                        ShowShakePrompt();
                    }
                    else
                    {
                        CompleteCooking();
                    }
                }
                break;
        }

        // Update UI position
        if (cookingMeterUI != null && cookingMeterUI.activeInHierarchy)
        {
            cookingMeterUI.transform.position = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        }
    }

    private void ShowTossPrompt()
    {
        // Stop pan bob, sizzle animation, and sound when waiting for toss
        StopPanBobAnimation();
        StopSizzleAnimation();
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

        // Tutorial event
        TutorialEvents.TossCompleted();

        // Play toss success sound
        if (tossSuccessSound != null && SoundManager.Instance != null)
            SoundManager.Instance.CreateSoundBuilder().WithRandomPitch().Play(tossSuccessSound);

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
        // Stop bob animation and kill any existing tweens
        StopPanBobAnimation();
        transform.DOKill();

        // Animate ingredients being tossed
        PlayIngredientTossAnimation();

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

    private void PlayIngredientTossAnimation()
    {
        // Stop sizzle animation before toss
        StopSizzleAnimation();

        for (int i = 0; i < ingredientsInStation.Count; i++)
        {
            GameObject ingredient = ingredientsInStation[i];
            if (ingredient == null) continue;

            Transform t = ingredient.transform;
            t.DOKill();

            Vector3 startLocalPos = t.localPosition;
            float staggerDelay = i * 0.05f;
            float spreadDir = (i % 2 == 0) ? 1f : -1f;

            // Randomize landing position and rotation so chickens shuffle around
            Vector3 landingPos = new Vector3(
                Random.Range(-landingPositionVariance, landingPositionVariance),
                startLocalPos.y,
                startLocalPos.z
            );
            float landingRotZ = Random.Range(-landingRotationVariance, landingRotationVariance);

            Sequence seq = DOTween.Sequence();

            // Fly up high (relative to pan), spread out, and flip
            seq.Append(t.DOLocalMoveY(startLocalPos.y + ingredientTossHeight, tossDuration * 0.45f)
                .SetEase(Ease.OutCubic));
            seq.Join(t.DOLocalMoveX(startLocalPos.x + ingredientSpreadX * spreadDir, tossDuration * 0.45f)
                .SetEase(Ease.OutSine));
            seq.Join(t.DOLocalRotate(new Vector3(0, 0, ingredientFlipRotation * spreadDir), tossDuration * 0.45f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear));

            // Fall to new randomized position
            seq.Append(t.DOLocalMoveY(landingPos.y, tossDuration * 0.35f)
                .SetEase(Ease.InCubic));
            seq.Join(t.DOLocalMoveX(landingPos.x, tossDuration * 0.35f)
                .SetEase(Ease.InSine));
            seq.Join(t.DOLocalRotate(new Vector3(0, 0, landingRotZ), tossDuration * 0.35f)
                .SetEase(Ease.OutQuad));

            // Small bounce on landing
            seq.Append(t.DOLocalMoveY(landingPos.y + ingredientTossHeight * 0.1f, tossDuration * 0.1f)
                .SetEase(Ease.OutQuad));
            seq.Append(t.DOLocalMoveY(landingPos.y, tossDuration * 0.1f)
                .SetEase(Ease.InQuad));

            // Set final position (for sizzle animation base positions)
            Vector3 finalPos = landingPos;
            float finalRotZ = landingRotZ;
            seq.OnComplete(() =>
            {
                t.localEulerAngles = new Vector3(0, 0, finalRotZ);
                t.localPosition = finalPos;
            });

            seq.SetDelay(staggerDelay);
        }
    }

    private void StartSizzleAnimation()
    {
        ingredientBasePositions.Clear();
        foreach (GameObject ingredient in ingredientsInStation)
        {
            if (ingredient == null) continue;
            ingredientBasePositions[ingredient] = ingredient.transform.localPosition;
        }
    }

    private void UpdateSizzleAnimation()
    {
        foreach (var kvp in ingredientBasePositions)
        {
            if (kvp.Key == null) continue;
            float id = kvp.Key.GetInstanceID() * 0.01f;
            float offsetX = (Mathf.PerlinNoise(Time.time * sizzleShakeFrequency, id) - 0.5f) * 2f * sizzleShakeStrength;
            float offsetY = (Mathf.PerlinNoise(id, Time.time * sizzleShakeFrequency) - 0.5f) * 2f * sizzleShakeStrength;
            kvp.Key.transform.localPosition = kvp.Value + new Vector3(offsetX, offsetY, 0);
        }
    }

    private void StopSizzleAnimation()
    {
        foreach (var kvp in ingredientBasePositions)
        {
            if (kvp.Key != null)
            {
                kvp.Key.transform.localPosition = kvp.Value;
            }
        }
        ingredientBasePositions.Clear();
    }

    private void StartPanBobAnimation()
    {
        StopPanBobAnimation();
        PlayNextBob();
    }

    private void PlayNextBob()
    {
        float randomHeight = Random.Range(panBobHeightMin, panBobHeightMax);
        float randomDuration = Random.Range(panBobDurationMin, panBobDurationMax);

        panBobSequence = DOTween.Sequence();
        panBobSequence.Append(transform.DOLocalMoveY(originalPanPosition.y + randomHeight, randomDuration * 0.5f)
            .SetEase(Ease.InOutSine));
        panBobSequence.Append(transform.DOLocalMoveY(originalPanPosition.y, randomDuration * 0.5f)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                if (panBounceSound != null && SoundManager.Instance != null)
                    SoundManager.Instance.CreateSoundBuilder().WithRandomPitch().Play(panBounceSound);
            }));
        panBobSequence.OnComplete(() => PlayNextBob());
    }

    private void StopPanBobAnimation()
    {
        if (panBobSequence != null && panBobSequence.IsActive())
        {
            panBobSequence.Kill();
            panBobSequence = null;
        }
        transform.localPosition = originalPanPosition;
        transform.localRotation = originalPanRotation;
    }

    private void OnTossAnimationComplete()
    {
        // Start frying
        currentTossState = TossingState.Frying;
        fryingTimer = 0f;

        // Start pan bob and idle sizzle animation on ingredients
        StartPanBobAnimation();
        StartSizzleAnimation();

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

        // Play pan impact sound (pan landing back down)
        if (panImpactSound != null && SoundManager.Instance != null)
            SoundManager.Instance.CreateSoundBuilder().WithRandomPitch().Play(panImpactSound);

        // Play sizzle sound
        PlaySizzleSound();
    }

    protected override void CompleteCooking()
    {
        // Stop pan bob, sizzle animation, and sound
        StopPanBobAnimation();
        StopSizzleAnimation();
        StopSizzleSound();

        // Tutorial event
        if (IsNoodleMode || IsSinigangMode)
        {
            TutorialEvents.SinigangCompleted();
        }
        else
        {
            TutorialEvents.AllTossesCompleted();

            // Play all tosses complete sound
            if (allTossesCompleteSound != null && SoundManager.Instance != null)
                SoundManager.Instance.CreateSoundBuilder().Play(allTossesCompleteSound);
        }

        // Call base implementation to complete cooking for all ingredients
        base.CompleteCooking();

        // Make pan pourable - player drags it over the PlatingStation
        // NOTE: do NOT reset hasOil/hasWater/hasOnionGarlic here — StartPourToPlate needs them to detect dish type
        Debug.Log("Pan: Cooking complete! Drag the pan over the plate to pour.");
        isCooking = false;
        isPourable = true;

        // Hide cooking UI
        if (cookingMeterUI != null)
            cookingMeterUI.SetActive(false);

        HideTossPrompt();
        HideShakePrompt();

        if (cookingEffect != null)
            cookingEffect.Stop();

        // Bounce to hint "drag me"
        transform.DOPunchScale(Vector3.one * 0.1f, 0.5f, 4, 0.5f);
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
        if (currentSizzleEmitter != null)
        {
            currentSizzleEmitter.Stop();
            currentSizzleEmitter = null;
        }
    }

    #endregion

    private void PlayIngredientAddSound()
    {
        if (ingredientAddSound != null && SoundManager.Instance != null)
            SoundManager.Instance.CreateSoundBuilder().WithRandomPitch().Play(ingredientAddSound);
    }

    #region Oil Management

    /// <summary>
    /// Check if oil can be added to the pan
    /// </summary>
    public bool CanAcceptOil()
    {
        return !hasOil && !isCooking && !isPourable && !isPouring;
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

        // Sizzle immediately when oil hits the pan
        PlaySizzleSound();

        Debug.Log("🛢️ Oil added to pan!");

        // Tutorial event
        TutorialEvents.OilAdded();
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

    #region Seasoning Management

    public bool CanAcceptSeasoning()
    {
        if (isPourable || isPouring) return false;
        if (currentTossState == TossingState.WaitingForSeasoning) return true;
        return !isCooking;
    }

    public void AddSeasoning(Ingredient seasoning)
    {
        if (!CanAcceptSeasoning())
        {
            Debug.LogWarning($"Cannot add seasoning while cooking!");
            return;
        }

        hasSeasoning = true;
        PlayIngredientAddSound();
        Debug.Log($"Added {seasoning.ingredientName} to pan!");

        if (seasoning.ingredientName.ToLower().Contains("salt"))
        {
            TutorialEvents.SaltAdded();
        }

        SeasoningManager seasoningMgr = GetComponent<SeasoningManager>();
        if (seasoningMgr != null)
        {
            seasoningMgr.AddSeasoning(seasoning);
        }

        if (currentTossState == TossingState.WaitingForSeasoning && ingredientsInStation.Count > 0)
        {
            StartCooking();
        }
    }

    #endregion

    #region Shake Gesture Handling (Sinigang)

    private void ShowShakePrompt()
    {
        StopPanBobAnimation();
        StopSizzleAnimation();
        StopSizzleSound();

        if (shakePrompt != null)
        {
            shakePrompt.ShowPrompt(completedShakes, requiredShakes);
        }

        if (shakeGestureDetector != null)
        {
            shakeGestureDetector.SetActive(true);
            shakeGestureDetector.StartTracking();
        }

        if (cookingMeterUI != null)
        {
            cookingMeterUI.SetActive(true);
        }
    }

    private void HideShakePrompt()
    {
        if (shakePrompt != null)
        {
            shakePrompt.HidePrompt();
        }

        if (shakeGestureDetector != null)
        {
            shakeGestureDetector.SetActive(false);
        }
    }

    private void OnShakeGestureComplete()
    {
        if (currentShakeState != ShakingState.WaitingForShake) return;

        completedShakes++;
        Debug.Log($"Shake {completedShakes}/{requiredShakes} completed!");

        if (shakePrompt != null)
        {
            shakePrompt.ShowShakeSuccess();
        }

        HideShakePrompt();

        // Start cooking phase between shakes
        currentShakeState = ShakingState.Cooking;
        shakeCookTimer = 0f;

        StartPanBobAnimation();
        StartSizzleAnimation();

        if (cookingEffect != null) cookingEffect.Play();
        if (steamEffect != null) steamEffect.Play();

        PlaySizzleSound();
    }

    private void OnShakeGestureProgress(float progress)
    {
        if (shakePrompt != null && currentShakeState == ShakingState.WaitingForShake)
        {
            shakePrompt.UpdateProgress(progress);
        }
    }

    private void OnAllShakeGesturesComplete()
    {
        // All shakes done in one continuous gesture - complete cooking
        Debug.Log("All shakes completed!");
        HideShakePrompt();
        CompleteCooking();
    }

    #endregion

    #region Water & Sinigang Mix Management

    public bool CanAcceptWater()
    {
        return !hasWater && !isCooking && !hasOil && !isPourable && !isPouring;
    }

    public void AddWater()
    {
        if (hasWater) return;

        hasWater = true;
        PlayIngredientAddSound();

        if (waterVisual != null)
        {
            waterVisual.SetActive(true);
            waterVisual.transform.localScale = Vector3.zero;
            waterVisual.transform.DOScale(0.85f, 0.4f).SetEase(Ease.OutBack);
        }

        Debug.Log("Water added to pan!");
    }

    public bool HasWater()
    {
        return hasWater;
    }

    private void RemoveWater()
    {
        if (!hasWater) return;

        hasWater = false;

        if (waterVisual != null)
        {
            waterVisual.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
            {
                waterVisual.SetActive(false);
                // Reset water color for next use
                SpriteRenderer waterSprite = waterVisual.GetComponent<SpriteRenderer>();
                if (waterSprite != null)
                    waterSprite.color = Color.white;
            });
        }
    }

    public bool CanAcceptSinigangMix()
    {
        return !hasSinigangMix && !isCooking && hasWater && !isPourable && !isPouring;
    }

    public void AddSinigangMix(Ingredient mix)
    {
        if (!CanAcceptSinigangMix())
        {
            Debug.LogWarning("Cannot add sinigang mix! Need water first, or already has mix.");
            return;
        }

        hasSinigangMix = true;
        PlayIngredientAddSound();

        TutorialEvents.SinigangMixAdded();

        // Change water color to sinigang broth color
        if (waterVisual != null)
        {
            SpriteRenderer waterSprite = waterVisual.GetComponent<SpriteRenderer>();
            if (waterSprite != null)
            {
                waterSprite.DOColor(sinigangBrothColor, 0.4f);
            }
        }

        Debug.Log($"Added {mix.ingredientName} to pan - sinigang mode activated!");
    }

    #endregion

    #region Mechado Mode Management (Catsup + Powdered Milk)

    public bool CanAcceptCatsup()
    {
        return !hasCatsup && !isCooking && hasWater && !hasOil && !hasSinigangMix && !hasNoodles && !isPourable && !isPouring;
    }

    public void AddCatsup()
    {
        if (!CanAcceptCatsup()) return;

        hasCatsup = true;
        PlayIngredientAddSound();

        // Tint the water visual to a tomato-red colour
        if (waterVisual != null)
        {
            SpriteRenderer waterSprite = waterVisual.GetComponent<SpriteRenderer>();
            if (waterSprite != null)
                waterSprite.DOColor(mechadoBrothColor, 0.4f);
        }

        Debug.Log("Catsup added to pan!");
    }

    public bool CanAcceptPowderedMilk()
    {
        return !hasPowderedMilk && !isCooking && hasCatsup && !isPourable && !isPouring;
    }

    public void AddPowderedMilk()
    {
        if (!CanAcceptPowderedMilk()) return;

        hasPowderedMilk = true;
        PlayIngredientAddSound();

        // Lighten the broth colour slightly to show milk mixing in
        if (waterVisual != null)
        {
            SpriteRenderer waterSprite = waterVisual.GetComponent<SpriteRenderer>();
            if (waterSprite != null)
                waterSprite.DOColor(Color.Lerp(mechadoBrothColor, Color.white, 0.25f), 0.4f);
        }

        Debug.Log("Powdered milk added to pan - mechado mode activated!");
    }

    #endregion

    #region Adobo Mode Management

    public bool CanAcceptOnionGarlic()
    {
        return !hasOnionGarlic && !isCooking && hasOil && !hasSinigangMix && !hasNoodles && !hasCatsup && !isPourable && !isPouring;
    }

    public void AddOnionGarlic()
    {
        if (!CanAcceptOnionGarlic()) return;

        hasOnionGarlic = true;
        PlayIngredientAddSound();

        // Tint the oil visual to show the adobo broth starting
        if (waterVisual != null)
        {
            waterVisual.SetActive(true);
            SpriteRenderer waterSprite = waterVisual.GetComponent<SpriteRenderer>();
            if (waterSprite != null)
                waterSprite.DOColor(adoboBrothColor, 0.4f);
        }
        else if (oilVisual != null)
        {
            SpriteRenderer oilSprite = oilVisual.GetComponent<SpriteRenderer>();
            if (oilSprite != null)
                oilSprite.DOColor(adoboBrothColor, 0.4f);
        }

        Debug.Log("Onion & Garlic added to pan — adobo mode activated!");
    }

    public bool CanAcceptSoySauce()
    {
        return IsAdoboMode && !hasSoySauce && !isPourable && !isPouring;
    }

    public void AddSoySauce()
    {
        if (!CanAcceptSoySauce()) return;
        hasSoySauce = true;
        PlayIngredientAddSound();
        Debug.Log("Soy sauce added to adobo pan!");
        CheckAdoboSeasoningComplete();
    }

    public bool CanAcceptVinegar()
    {
        return IsAdoboMode && !hasVinegar && !isPourable && !isPouring;
    }

    public void AddVinegar()
    {
        if (!CanAcceptVinegar()) return;
        hasVinegar = true;
        PlayIngredientAddSound();
        Debug.Log("Vinegar added to adobo pan!");
        CheckAdoboSeasoningComplete();
    }

    public bool CanAcceptSugar()
    {
        return IsAdoboMode && !hasSugar && !isPourable && !isPouring;
    }

    public void AddSugar()
    {
        if (!CanAcceptSugar()) return;
        hasSugar = true;
        PlayIngredientAddSound();
        Debug.Log("Sugar added to adobo pan!");
        CheckAdoboSeasoningComplete();
    }

    public bool CanAcceptLaurel()
    {
        return IsAdoboMode && !hasLaurel && !isPourable && !isPouring;
    }

    public void AddLaurel()
    {
        if (!CanAcceptLaurel()) return;
        hasLaurel = true;
        PlayIngredientAddSound();
        Debug.Log("Laurel added to adobo pan!");
        CheckAdoboSeasoningComplete();
    }

    private void CheckAdoboSeasoningComplete()
    {
        if (!IsAdoboFullySeasoned) return;

        // All seasonings added — satisfy the seasoning requirement and start tossing
        hasSeasoning = true;
        if (currentTossState == TossingState.WaitingForSeasoning && ingredientsInStation.Count > 0)
        {
            StartCooking();
        }
    }

    #endregion

    #region Noodle Mode Management

    public bool CanAcceptNoodles()
    {
        return !hasNoodles && !isCooking && hasWater && !hasOil && !hasSinigangMix && !isPourable && !isPouring;
    }

    public void AddNoodles(GameObject noodleObj)
    {
        if (!CanAcceptNoodles()) return;

        hasNoodles = true;
        noodleObject.SetActive(true);

        // Parent noodle into pan and animate it in
        Transform container = ingredientContainer != null ? ingredientContainer : transform;
        noodleObject.transform.SetParent(container);
        noodleObject.transform.DOLocalMove(new Vector3(0f, 0.1f, 0f), 0.35f).SetEase(Ease.OutBack);
        noodleObject.transform.DOScale(0.8f, 0.35f).SetEase(Ease.OutBack);

        // // Disable drag so player can't pick noodle back up out of the pan
        // DraggableIngredient drag = noodleObj.GetComponent<DraggableIngredient>();
        // if (drag != null) drag.enabled = false;
    }

    #endregion

    #region Pour Mechanic

    private void HandlePourInput()
    {
        // Start drag
        if (Input.GetMouseButtonDown(0) && !isDraggingPan)
        {
            mainCamera = Camera.main;
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = transform.position.z;
            float distance = Vector2.Distance(mouseWorldPos, transform.position);

            Debug.Log($"[Pan] MouseDown — pan pos: {transform.position}, mouse world pos: {mouseWorldPos}, distance: {distance:F2}, pickupRadius: {pourPickupRadius}");

            if (distance <= pourPickupRadius)
            {
                isDraggingPan = true;
                dragOffset = transform.position - mouseWorldPos;
                Debug.Log($"[Pan] Drag started (offset: {dragOffset})");
            }
            else
            {
                Debug.Log($"[Pan] MouseDown too far from pan — not starting drag");
            }
        }

        // During drag
        if (isDraggingPan && Input.GetMouseButton(0))
        {
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = transform.position.z;
            transform.position = mouseWorldPos + dragOffset;

            // Tilt pan based on horizontal displacement
            Vector3 delta = transform.position - originalPanPosition;
            float tiltTarget = Mathf.Clamp(delta.x * -(pourTiltAngle / 3f), -pourTiltAngle, pourTiltAngle);
            float currentZ = transform.eulerAngles.z;
            if (currentZ > 180f) currentZ -= 360f;
            float newZ = Mathf.LerpAngle(currentZ, tiltTarget, Time.unscaledDeltaTime * pourTiltSmoothing);
            transform.rotation = Quaternion.Euler(0, 0, newZ);

            // Highlight nearby PlatingStation
            UpdatePlatingStationHighlight();
        }

        // Release
        if (isDraggingPan && Input.GetMouseButtonUp(0))
        {
            isDraggingPan = false;
            Debug.Log($"[Pan] Mouse released — pan world pos: {transform.position}");

            if (nearbyPlatingStation != null)
            {
                nearbyPlatingStation.SetHighlight(false);
            }

            // Check for PlatingStation nearby
            Collider2D[] allHits = Physics2D.OverlapCircleAll(transform.position, pourDetectRadius);
            Debug.Log($"[Pan] OverlapCircle at {transform.position} radius {pourDetectRadius} — found {allHits.Length} collider(s)");
            foreach (Collider2D hit in allHits)
            {
                Debug.Log($"  [Pan] Hit: {hit.gameObject.name} (tag: {hit.tag}, layer: {LayerMask.LayerToName(hit.gameObject.layer)})");
            }

            PlatingStation station = FindNearbyPlatingStation();
            if (station != null)
            {
                Debug.Log($"[Pan] PlatingStation found: {station.gameObject.name} — starting pour");
                StartPourToPlate(station);
                return;
            }

            Debug.Log($"[Pan] No PlatingStation found within radius {pourDetectRadius} — snapping back");
            SnapBackToOriginal();
        }
    }

    private PlatingStation FindNearbyPlatingStation()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, pourDetectRadius);
        foreach (Collider2D col in colliders)
        {
            PlatingStation station = col.GetComponent<PlatingStation>();
            if (station != null) return station;
        }
        return null;
    }

    private void UpdatePlatingStationHighlight()
    {
        PlatingStation newNearby = FindNearbyPlatingStation();

        if (newNearby != nearbyPlatingStation)
        {
            if (nearbyPlatingStation != null)
                nearbyPlatingStation.SetHighlight(false);
            nearbyPlatingStation = newNearby;
            if (nearbyPlatingStation != null)
                nearbyPlatingStation.SetHighlight(true);
        }
    }

    private void StartPourToPlate(PlatingStation station)
    {
        isPouring = true;

        if (pourSound != null && SoundManager.Instance != null)
            SoundManager.Instance.CreateSoundBuilder().Play(pourSound);

        Debug.Log($"Pan: Pouring {ingredientsInStation.Count} ingredients onto plate!");

        // Build ingredient list: chickens + noodle (if noodle mode)
        List<GameObject> ingredientsCopy = new List<GameObject>(ingredientsInStation);
        if (IsNoodleMode && noodleObject != null)
            ingredientsCopy.Add(noodleObject);

        float totalDelay = ingredientsCopy.Count * 0.1f + pourDuration;

        // Determine dish type string
        string dishType = IsAdoboMode ? "Adobo"
                        : IsMechadoMode ? "Mechado"
                        : IsNoodleMode ? "NoodleChicken"
                        : IsSinigangMode ? "Sinigang"
                        : "FriedChicken";

        // Pour all ingredients to the plating station
        station.AcceptPour(ingredientsCopy, dishType);

        // After pour animation, reset pan
        DOVirtual.DelayedCall(totalDelay, CompletePanPour);
    }

    private void CompletePanPour()
    {
        Debug.Log($"Pan: CompletePanPour called! ingredientsInStation.Count={ingredientsInStation.Count}, isPourable={isPourable}, isPouring={isPouring}");

        // Clear all ingredients
        ingredientsInStation.Clear();

        // Reset all flags
        isCooking = false;
        isPourable = false;
        isPouring = false;
        isDraggingPan = false;

        // Reset cooking state
        RemoveOil();
        RemoveWater();

        // Force-hide waterVisual even when hasWater was false (e.g. adobo activates it without setting hasWater)
        if (waterVisual != null && waterVisual.activeSelf)
        {
            waterVisual.transform.DOKill();
            waterVisual.SetActive(false);
            if (waterVisual.TryGetComponent(out SpriteRenderer sp))
                sp.color = Color.white;
        }
        RemoveNoodles();
        hasOil = false;
        hasSeasoning = false;
        hasSinigangMix = false;
        hasWater = false;
        hasNoodles = false;
        hasCatsup = false;
        hasPowderedMilk = false;
        hasOnionGarlic = false;
        hasSoySauce = false;
        hasVinegar = false;
        hasSugar = false;
        hasLaurel = false;
        noodleObject = null; // PlatingStation now owns the noodle object

        // Reset gesture state
        currentTossState = TossingState.NotStarted;
        currentShakeState = ShakingState.NotStarted;
        completedTosses = 0;
        completedShakes = 0;
        fryingTimer = 0f;
        shakeCookTimer = 0f;
        cookTimer = 0f;

        // Hide prompts and UI
        HideTossPrompt();
        HideShakePrompt();
        if (cookingMeterUI != null)
            cookingMeterUI.SetActive(false);

        // Kill all tweens on pan before snapping back
        transform.DOKill();

        // Return pan to original position
        SnapBackToOriginal();

        // Reset sprite color
        if (spriteRenderer != null)
            spriteRenderer.color = normalColor;

        Debug.Log("Pan poured and reset - ready for next use");
    }

    private void RemoveNoodles()
    {
        if (noodleObject != null)
        {
            noodleObject.SetActive(false);
        }
    }

    private void SnapBackToOriginal()
    {
        transform.DOLocalMove(originalPanPosition, 0.3f).SetEase(Ease.OutQuad);
        transform.DOLocalRotateQuaternion(originalPanRotation, 0.3f).SetEase(Ease.OutQuad);
    }

    public void Reset()
    {
        List<GameObject> toDestroy = new List<GameObject>(ingredientsInStation);
        CompletePanPour();
        foreach (GameObject ingredient in toDestroy)
        {
            if (ingredient != null)
            {
                Destroy(ingredient);
            }
        }
    }

    #endregion
}
