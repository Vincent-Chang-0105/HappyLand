using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using DG.Tweening;
using AudioSystem;

/// <summary>
/// Pot cooking station - specialized for boiling ingredients
/// Features gesture-based stirring where players must make circular motions 3 times during cooking
/// Supports two-phase cooking for sinigang dishes
/// </summary>
public class Pot : CookingStation
{
    [Header("Gesture-Based Stirring")]
    [SerializeField] private bool useGestureStirring = true;
    [SerializeField] private int requiredStirs = 3;
    [SerializeField] private float boilingDurationBetweenStirs = 2.5f;
    [SerializeField] private StirGestureDetector gestureDetector;
    [SerializeField] private FrameAnimatedPrompt stirPrompt;

    [Header("Boiling Motion")]
    [SerializeField] private float boilingSwirlSpeed = 40f;  // Degrees per second for passive swirl
    [SerializeField] private float stirringSwirlSpeed = 120f; // Faster when actively stirring
    [SerializeField] private float stirBurstSpeed = 600f;    // Burst speed right after completing a stir
    [SerializeField] private float bobAmount = 0.05f;        // Vertical bobbing amplitude
    [SerializeField] private float bobSpeed = 3f;            // Bobbing frequency
    [SerializeField] private float ingredientTiltAmount = 15f; // How much chickens tilt while swirling

    [Header("Wind Down")]
    [SerializeField] private float residualSwirlSpeed = 8f;
    [SerializeField] private float windDownRate = 2f;

    [Header("Per-Ingredient Variation")]
    [SerializeField] private float minOrbitRadius = 0.15f;
    [SerializeField] private float maxOrbitRadius = 1.0f;
    [SerializeField] private float minSpeedMultiplier = 0.7f;
    [SerializeField] private float maxSpeedMultiplier = 2.0f;

    [Header("Sounds")]
    [SerializeField] private SoundData stirStartedSound;
    [SerializeField] private SoundData stirCycleCompleteSound;
    [SerializeField] private SoundData allStirsCompleteSound;
    [SerializeField] private SoundData boilingLoopSound;
    [SerializeField] private SoundData ingredientAddSound;
    [SerializeField] private SoundData pourSound;

    [Header("Pour Settings")]
    [SerializeField] private float pourTiltAngle = 45f;
    [SerializeField] private float tiltSmoothing = 8f;
    [SerializeField] private float pourDetectRadius = 1.5f;
    [SerializeField] private float pourPickupRadius = 1.5f;

    // Stirring state
    private enum StirringState
    {
        NotStarted,
        WaitingForStir,
        Stirring,
        Boiling
    }

    // Cooking phase
    private enum PotCookingPhase
    {
        Initial,         // No cooking
        Boiling,         // Regular boiling
        BoilingComplete  // Boiling done, pot is pourable
    }

    // Per-ingredient orbit data for organic motion
    private class IngredientOrbitData
    {
        public float orbitRadius;
        public float speedMultiplier;
        public float phaseOffset;
        public float bobPhaseOffset;
    }

    private StirringState currentStirState = StirringState.NotStarted;
    private PotCookingPhase currentCookingPhase = PotCookingPhase.Initial;
    private int completedStirs = 0;
    private float boilingTimer = 0f;
    private float currentSwirlSpeed = 0f;
    private Vector3 containerBasePosition;
    private float orbitAngle = 0f;
    private Dictionary<Transform, IngredientOrbitData> ingredientOrbits = new Dictionary<Transform, IngredientOrbitData>();

    // Sound state
    private SoundEmitter boilingLoopEmitter;

    // Pour state
    private bool isPourable = false;
    private bool isPouring = false;
    private Vector3 potOriginalPosition;
    private Quaternion potOriginalRotation;
    private Vector3 dragOffset;
    private Camera mainCamera;
    private PourZone nearbyPourZone;
    private bool isDraggingPot = false;
    private bool isWindingDown = false;

    #region Unity Lifecycle

    protected override void Start()
    {
        base.Start();

        // Setup gesture detector if not assigned
        if (useGestureStirring && gestureDetector == null)
        {
            gestureDetector = gameObject.AddComponent<StirGestureDetector>();
        }

        if (gestureDetector != null)
        {
            gestureDetector.SetStirCenter(transform);
            gestureDetector.SetActive(false);
            gestureDetector.OnStirComplete += OnStirGestureComplete;
            gestureDetector.OnStirProgress += OnStirGestureProgress;
        }

        // Find or create stir prompt
        if (useGestureStirring && stirPrompt == null)
        {
            // Try to find existing stir prompt in children
            stirPrompt = GetComponentInChildren<FrameAnimatedPrompt>(true);

            if (stirPrompt == null)
            {
                Debug.LogWarning("Pot: No FrameAnimatedPrompt component found. Please add a FrameAnimatedPrompt UI to the pot.");
            }
        }
    }

    protected override void Update()
    {
        base.Update();

        // Continue boiling motion while winding down after cooking completes
        if (isWindingDown)
        {
            UpdateBoilingMotion();
        }

        // Handle pour input when pot is pourable
        if (isPourable && !isPouring)
        {
            HandlePourInput();
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        // Unsubscribe from gesture detector events
        if (gestureDetector != null)
        {
            gestureDetector.OnStirComplete -= OnStirGestureComplete;
            gestureDetector.OnStirProgress -= OnStirGestureProgress;
        }

        // Cleanup boiling loop sound
        if (boilingLoopEmitter != null)
        {
            boilingLoopEmitter.Stop();
            boilingLoopEmitter = null;
        }
    }

    #endregion

    #region Abstract Method Implementations

    protected override bool CanCookIngredient(GameObject ingredient)
    {
        IBoilable boilable = ingredient.GetComponent<IBoilable>();
        return boilable != null && boilable.CanBeBoiled();
    }

    protected override void StartCookingIngredient(GameObject ingredient)
    {
        IBoilable boilable = ingredient.GetComponent<IBoilable>();
        if (boilable != null)
        {
            boilable.StartBoiling();
        }
    }

    protected override void StopCookingIngredient(GameObject ingredient)
    {
        IBoilable boilable = ingredient.GetComponent<IBoilable>();
        if (boilable != null)
        {
            boilable.StopBoiling();
        }
    }

    protected override void CompleteCookingIngredient(GameObject ingredient)
    {
        IBoilable boilable = ingredient.GetComponent<IBoilable>();
        if (boilable != null)
        {
            boilable.CompleteBoiling();
        }
    }

    protected override bool AdditionalBowlAcceptanceCheck()
    {
        // Don't accept bowls while pot is pourable or pouring
        if (isPourable || isPouring) return false;
        return true;
    }

    protected override string GetCookingProcessName()
    {
        return "boiling";
    }

    #endregion

    #region Gesture-Based Stirring Logic

    protected override void StartCooking()
    {
        if (isCooking || ingredientsInStation.Count == 0) return;

        // If gesture stirring is disabled, use base behavior
        if (!useGestureStirring)
        {
            base.StartCooking();
            return;
        }

        // Initialize gesture-based cooking
        isCooking = true;
        completedStirs = 0;
        currentStirState = StirringState.WaitingForStir;
        currentCookingPhase = PotCookingPhase.Boiling;

        // Store container base position for bobbing
        if (ingredientContainer != null)
        {
            containerBasePosition = ingredientContainer.localPosition;
        }

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
            cookingEffect.Stop(); // Don't start yet, wait for first stir
        }

        // Show first stir prompt
        ShowStirPrompt();
    }

    protected override void UpdateCooking()
    {
        // If gesture stirring is disabled, use base timer behavior
        if (!useGestureStirring)
        {
            base.UpdateCooking();
            return;
        }

        int requiredStirCount = requiredStirs;
        float boilDuration = boilingDurationBetweenStirs;

        // Update based on current stirring state
        switch (currentStirState)
        {
            case StirringState.WaitingForStir:
                // Just waiting for player to stir, no timer
                break;

            case StirringState.Boiling:
                // Boiling between stirs - increment timer
                boilingTimer += Time.deltaTime;

                // Update progress bar to show boiling progress
                if (cookingProgressBar != null)
                {
                    float progress = boilingTimer / boilDuration;
                    cookingProgressBar.fillAmount = progress;
                    cookingProgressBar.color = Color.Lerp(Color.yellow, Color.red, progress);
                }

                // After boiling time is up, request next stir or complete
                if (boilingTimer >= boilDuration)
                {
                    if (completedStirs < requiredStirCount)
                    {
                        // Request another stir
                        currentStirState = StirringState.WaitingForStir;
                        boilingTimer = 0f;
                        ShowStirPrompt();
                    }
                    else
                    {
                        // All stirs complete, finish cooking
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

        // Swirl and bob the ingredients
        UpdateBoilingMotion();
    }

    private void UpdateBoilingMotion()
    {
        if (ingredientContainer == null) return;

        // Determine target swirl speed based on state
        float targetSpeed;
        float lerpRate;

        if (isWindingDown)
        {
            targetSpeed = residualSwirlSpeed;
            lerpRate = windDownRate;
        }
        else if (currentStirState == StirringState.WaitingForStir)
        {
            targetSpeed = boilingSwirlSpeed;
            lerpRate = 3f;
        }
        else
        {
            targetSpeed = stirringSwirlSpeed;
            lerpRate = 3f;
        }

        // Smooth the speed transition
        currentSwirlSpeed = Mathf.Lerp(currentSwirlSpeed, targetSpeed, Time.deltaTime * lerpRate);

        // Advance the base orbit angle
        orbitAngle += currentSwirlSpeed * Time.deltaTime;

        // Tilt amount based on speed
        float speedRatio = Mathf.Clamp01(currentSwirlSpeed / stirBurstSpeed);
        float tilt = ingredientTiltAmount * speedRatio;

        // Move each child individually with its own orbit parameters
        foreach (Transform child in ingredientContainer)
        {
            IngredientOrbitData data = GetOrCreateOrbitData(child);

            // Each chicken orbits at its own speed and radius
            float angle = (orbitAngle * data.speedMultiplier + data.phaseOffset) * Mathf.Deg2Rad;

            // Elliptical orbit (slightly squashed Y for top-down pot look)
            float x = Mathf.Cos(angle) * data.orbitRadius;
            float y = Mathf.Sin(angle) * data.orbitRadius * 0.5f;

            // Per-chicken bob with unique phase
            float bob = Mathf.Sin(Time.time * bobSpeed + data.bobPhaseOffset) * bobAmount;

            child.localPosition = new Vector3(x, y + bob, 0f);

            // Tilt tangent to orbit direction
            float tiltAngle = angle * Mathf.Rad2Deg;
            child.rotation = Quaternion.Euler(0, 0, tiltAngle - 90f + tilt);
        }

        // Gentle vertical bob for the whole container too
        Vector3 bobPosition = containerBasePosition;
        bobPosition.y += Mathf.Sin(Time.time * bobSpeed * 0.5f) * bobAmount * 0.5f;
        ingredientContainer.localPosition = bobPosition;
    }

    private IngredientOrbitData GetOrCreateOrbitData(Transform child)
    {
        if (!ingredientOrbits.TryGetValue(child, out IngredientOrbitData data))
        {
            data = new IngredientOrbitData
            {
                orbitRadius = Random.Range(minOrbitRadius, maxOrbitRadius),
                speedMultiplier = Random.Range(minSpeedMultiplier, maxSpeedMultiplier),
                phaseOffset = Random.Range(0f, 360f),
                bobPhaseOffset = Random.Range(0f, Mathf.PI * 2f)
            };
            ingredientOrbits[child] = data;
        }
        return data;
    }
    
    private void ShowStirPrompt()
    {
        if (stirPrompt != null)
        {
            stirPrompt.ShowPrompt(completedStirs, requiredStirs);
        }

        // Play stir started sound (spoon pickup)
        if (stirStartedSound != null && SoundManager.Instance != null)
            SoundManager.Instance.CreateSoundBuilder().Play(stirStartedSound);

        // Enable gesture detection
        if (gestureDetector != null)
        {
            gestureDetector.SetActive(true);
        }

        // Show UI
        if (cookingMeterUI != null)
        {
            cookingMeterUI.SetActive(true);
        }
    }

    private void HideStirPrompt()
    {
        if (stirPrompt != null)
        {
            stirPrompt.HidePrompt();
        }

        // Disable gesture detection
        if (gestureDetector != null)
        {
            gestureDetector.SetActive(false);
        }
    }

    private void OnStirGestureComplete()
    {
        // Ignore if not waiting for a stir
        if (currentStirState != StirringState.WaitingForStir)
            return;

        completedStirs++;

        //Debug.Log($"Stir {completedStirs}/{requiredStirs} completed!");

        // Tutorial event
        TutorialEvents.StirCompleted();

        // Play stir cycle complete sound
        if (stirCycleCompleteSound != null && SoundManager.Instance != null)
            SoundManager.Instance.CreateSoundBuilder().WithRandomPitch().Play(stirCycleCompleteSound);

        // Show success feedback, then hide after a short delay so VFX can play
        if (stirPrompt != null)
        {
            stirPrompt.ShowStirSuccess();
            DOVirtual.DelayedCall(0.5f, HideStirPrompt);
        }
        else
        {
            HideStirPrompt();
        }

        // Start boiling
        currentStirState = StirringState.Boiling;
        boilingTimer = 0f;

        // Start boiling loop sound (only once, not on every stir)
        if (boilingLoopSound != null && SoundManager.Instance != null && boilingLoopEmitter == null)
            boilingLoopEmitter = SoundManager.Instance.CreateSoundBuilder().Play(boilingLoopSound);

        // Burst the swirl speed for a satisfying "whoosh"
        currentSwirlSpeed = stirBurstSpeed;

        // Start/restart cooking effects
        if (cookingEffect != null)
        {
            cookingEffect.Play();
        }

        if (steamEffect != null)
        {
            steamEffect.Play();
        }

        // Play some visual feedback (ingredient shake or something)
        if (ingredientDropEffect != null)
        {
            ingredientDropEffect.Play();
        }
    }

    private void OnStirGestureProgress(float progress)
    {
        // Update the stir prompt with current gesture progress
        if (stirPrompt != null && currentStirState == StirringState.WaitingForStir)
        {
            stirPrompt.UpdateProgress(progress);
        }

        // Speed up swirl based on stir progress for responsive feedback
        if (progress > 0 && ingredientContainer != null)
        {
            currentSwirlSpeed = Mathf.Lerp(boilingSwirlSpeed, stirringSwirlSpeed, progress);
        }
    }

    #endregion

    #region Two-Phase Cooking (Sinigang)

    protected override void CompleteCooking()
    {
        if (currentCookingPhase == PotCookingPhase.Boiling)
        {
            CompleteBoilingPhase();
        }
        else
        {
            base.CompleteCooking();
        }
    }

    private void CompleteBoilingPhase()
    {
        // Complete boiling for all ingredients
        foreach (GameObject ingredient in ingredientsInStation)
        {
            if (ingredient != null)
            {
                CompleteCookingIngredient(ingredient); // Completes boiling
            }
        }

        // Tutorial event
        TutorialEvents.AllStirsCompleted();

        // Play completion fanfare + stop boiling loop
        if (allStirsCompleteSound != null && SoundManager.Instance != null)
            SoundManager.Instance.CreateSoundBuilder().Play(allStirsCompleteSound);

        if (boilingLoopEmitter != null)
        {
            boilingLoopEmitter.FadeOutAndStop(0.5f);
            boilingLoopEmitter = null;
        }

        //Debug.Log("Boiling complete! Pot is now pourable - drag to PourZone.");
        currentCookingPhase = PotCookingPhase.BoilingComplete;
        isCooking = false;
        isWindingDown = true;

        // Make pot pourable
        isPourable = true;
        potOriginalPosition = transform.position;
        potOriginalRotation = transform.rotation;

        // Hide UI temporarily
        if (cookingMeterUI != null)
            cookingMeterUI.SetActive(false);

        HideStirPrompt();

        // Stop effects
        if (cookingEffect != null)
            cookingEffect.Stop();

        // Bounce to hint "drag me"
        transform.DOPunchScale(Vector3.one * 0.1f, 0.5f, 4, 0.5f);
    }

    #endregion

    #region Seasoning & Liquid Management (for backward compatibility with DraggableIngredient)

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

        if (ingredientAddSound != null && SoundManager.Instance != null)
            SoundManager.Instance.CreateSoundBuilder().WithRandomPitch().Play(ingredientAddSound);

        // Try to use SeasoningManager if available
        SeasoningManager seasoningMgr = GetComponent<SeasoningManager>();
        if (seasoningMgr != null)
        {
            seasoningMgr.AddSeasoning(seasoning);
        }
        else
        {
            // Fallback - just log if no manager is attached
            //Debug.Log($"🧂 Added seasoning to pot: {seasoning.ingredientName} (no SeasoningManager attached)");
        }
    }

    /// <summary>
    /// Check if liquid can be added
    /// </summary>
    public bool CanAcceptLiquid()
    {
        return !isCooking;
    }

    /// <summary>
    /// Add a liquid ingredient - delegates to SeasoningManager if available
    /// </summary>
    public void AddLiquid(Ingredient liquid)
    {
        if (!CanAcceptLiquid())
        {
            Debug.LogWarning($"Cannot add liquid while cooking!");
            return;
        }

        if (ingredientAddSound != null && SoundManager.Instance != null)
            SoundManager.Instance.CreateSoundBuilder().WithRandomPitch().Play(ingredientAddSound);

        // Try to use SeasoningManager if available
        SeasoningManager seasoningMgr = GetComponent<SeasoningManager>();
        if (seasoningMgr != null)
        {
            seasoningMgr.AddLiquid(liquid);
        }
        else
        {
            // Fallback - just log if no manager is attached
            // Apply visual feedback
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(spriteRenderer.color, Color.cyan, 0.2f);
            }
            //Debug.Log($"💧 Added liquid to pot: {liquid.ingredientName} (no SeasoningManager attached)");
        }
    }

    #endregion

    #region Pour Mechanic

    public bool IsPourable() => isPourable;

    private void HandlePourInput()
    {
        // Start drag - check if mouse clicks near the pot
        if (Input.GetMouseButtonDown(0) && !isDraggingPot)
        {
            mainCamera = Camera.main;
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = transform.position.z;
            float distance = Vector2.Distance(mouseWorldPos, transform.position);

            if (distance <= pourPickupRadius)
            {
                isDraggingPot = true;
                dragOffset = transform.position - mouseWorldPos;
            }
        }

        // During drag - move pot and tilt
        if (isDraggingPot && Input.GetMouseButton(0))
        {
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = transform.position.z;
            transform.position = mouseWorldPos + dragOffset;

            // Tilt pot based on horizontal displacement from original position
            Vector3 delta = transform.position - potOriginalPosition;
            float tiltTarget = Mathf.Clamp(delta.x * -(pourTiltAngle / 3f), -pourTiltAngle, pourTiltAngle);
            float currentZ = transform.eulerAngles.z;
            if (currentZ > 180f) currentZ -= 360f;
            float newZ = Mathf.LerpAngle(currentZ, tiltTarget, Time.unscaledDeltaTime * tiltSmoothing);
            transform.rotation = Quaternion.Euler(0, 0, newZ);

            // Highlight nearby PourZone
            UpdatePourZoneHighlight();
        }

        // Release - check for PourZone or snap back
        if (isDraggingPot && Input.GetMouseButtonUp(0))
        {
            isDraggingPot = false;

            // Clear PourZone highlight
            if (nearbyPourZone != null)
            {
                nearbyPourZone.SetHighlight(false);
                nearbyPourZone = null;
            }

            // Check for PourZone nearby
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, pourDetectRadius);
            foreach (Collider2D col in colliders)
            {
                PourZone pourZone = col.GetComponent<PourZone>();
                if (pourZone != null)
                {
                    StartPour(pourZone);
                    return;
                }
            }

            // Not over a PourZone, snap back
            SnapBackToOriginal();
        }
    }

    private void UpdatePourZoneHighlight()
    {
        PourZone newNearby = null;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, pourDetectRadius);
        foreach (Collider2D col in colliders)
        {
            PourZone pz = col.GetComponent<PourZone>();
            if (pz != null)
            {
                newNearby = pz;
                break;
            }
        }

        if (newNearby != nearbyPourZone)
        {
            nearbyPourZone?.SetHighlight(false);
            nearbyPourZone = newNearby;
            nearbyPourZone?.SetHighlight(true);
        }
    }

    private void StartPour(PourZone pourZone)
    {
        isPouring = true;

        if (pourSound != null && SoundManager.Instance != null)
            SoundManager.Instance.CreateSoundBuilder().Play(pourSound);

        GameObject destination = GameObject.FindWithTag(pourZone.DestinationTag);
        if (destination == null)
        {
            Debug.LogWarning($"Pot: No destination found with tag '{pourZone.DestinationTag}'");
            isPouring = false;
            SnapBackToOriginal();
            return;
        }

        //Debug.Log($"Pot: Pouring {ingredientsInStation.Count} ingredients to {pourZone.DestinationTag}");

        // Transfer each ingredient out of the pot
        int count = 0;
        foreach (GameObject ingredient in ingredientsInStation)
        {
            if (ingredient == null) continue;

            // Unparent from pot
            ingredient.transform.SetParent(null);

            // Reset bowl state so chicken is draggable after landing
            var chicken = ingredient.GetComponent<Chicken>();
            if (chicken != null)
            {
                chicken.ExitBowl();

                // Fire tutorial event
                TutorialEvents.ChickenTransferred();
            }

            var drag = ingredient.GetComponent<ChickenDragBehavior>();

            // Keep drag and colliders disabled during the pour flight.
            // OnComplete will enter the nearest bowl and restore interactivity only if needed.

            // Animate to destination with stagger
            ingredient.transform.DOMove(destination.transform.position, pourZone.PourDuration)
                .SetDelay(count * 0.1f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    ChickenBowlInteraction bowlInteraction = ingredient.GetComponent<ChickenBowlInteraction>();
                    if (bowlInteraction != null)
                    {
                        bowlInteraction.TeleportToNearestBowl();

                        // If no bowl accepted it, re-enable drag/colliders for manual placement
                        if (!bowlInteraction.IsInBowl)
                        {
                            if (drag != null) drag.enabled = true;
                            foreach (Collider2D c in ingredient.GetComponents<Collider2D>())
                                c.enabled = true;
                        }
                    }
                    else
                    {
                        if (drag != null) drag.enabled = true;
                        foreach (Collider2D c in ingredient.GetComponents<Collider2D>())
                            c.enabled = true;
                    }
                });

            // Reset rotation
            ingredient.transform.DORotate(Vector3.zero, pourZone.PourDuration)
                .SetDelay(count * 0.1f);

            count++;
        }

        // After all ingredients transferred, return pot
        float totalDelay = count * 0.1f + pourZone.PourDuration;
        DOVirtual.DelayedCall(totalDelay, () => CompletePour());
    }

    private void CompletePour()
    {
        // Stop boiling loop if still running
        if (boilingLoopEmitter != null)
        {
            boilingLoopEmitter.FadeOutAndStop(0.3f);
            boilingLoopEmitter = null;
        }

        // Clear the pot state
        ingredientsInStation.Clear();
        isPourable = false;
        isPouring = false;
        isWindingDown = false;
        currentCookingPhase = PotCookingPhase.Initial;
        currentSwirlSpeed = 0f;
        orbitAngle = 0f;
        ingredientOrbits.Clear();

        // Return pot to original position
        transform.DOMove(potOriginalPosition, 0.4f).SetEase(Ease.OutQuad);
        transform.DORotateQuaternion(potOriginalRotation, 0.4f).SetEase(Ease.OutQuad);

        // Reset visual
        if (spriteRenderer != null)
            spriteRenderer.color = normalColor;

        // Reset ingredient container
        if (ingredientContainer != null)
        {
            ingredientContainer.localRotation = Quaternion.identity;
            ingredientContainer.localPosition = containerBasePosition;
        }

        HideStirPrompt();

        if (cookingMeterUI != null)
            cookingMeterUI.SetActive(false);

        //Debug.Log("Pot poured and cleared - ready for next use");
    }

    private void SnapBackToOriginal()
    {
        transform.DOMove(potOriginalPosition, 0.3f).SetEase(Ease.OutQuad);
        transform.DORotateQuaternion(potOriginalRotation, 0.3f).SetEase(Ease.OutQuad);
    }

    public void Reset()
    {
        StopAllCoroutines();
        DOTween.Kill(transform);
        isCooking = false;
        isPourable = false;
        isPouring = false;
        isWindingDown = false;
        currentCookingPhase = PotCookingPhase.Initial;
        currentSwirlSpeed = 0f;
        orbitAngle = 0f;
        ingredientOrbits.Clear();

        if (boilingLoopEmitter != null)
        {
            boilingLoopEmitter.FadeOutAndStop(0.3f);
            boilingLoopEmitter = null;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = normalColor;

        if (ingredientContainer != null)
        {
            ingredientContainer.localRotation = Quaternion.identity;
            ingredientContainer.localPosition = containerBasePosition;
        }

        HideStirPrompt();

        if (cookingMeterUI != null)
            cookingMeterUI.SetActive(false);

        List<GameObject> toDestroy = new List<GameObject>(ingredientsInStation);
        ingredientsInStation.Clear();
        foreach (GameObject ingredient in toDestroy)
        {
            if (ingredient != null)
                Destroy(ingredient);
        }
    }

    #endregion
}
