using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using DG.Tweening;

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
    [SerializeField] private StirPrompt stirPrompt;

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

    [Header("Pour Settings")]
    [SerializeField] private float pourTiltAngle = 45f;
    [SerializeField] private float tiltSmoothing = 8f;
    [SerializeField] private float pourDetectRadius = 1.5f;
    [SerializeField] private float pourPickupRadius = 1.5f;

    [Header("Sinigang Cooking")]
    [SerializeField] private int sinigangRequiredStirs = 3;
    [SerializeField] private float sinigangBoilingDurationBetweenStirs = 2.5f;
    [SerializeField] private Color sinigangBrothColor = new Color(0.8f, 1f, 0.8f);

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
        BoilingComplete, // Waiting for sinigang mix
        SinigangCooking  // Sinigang phase active
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
    private bool hasSinigangMix = false;
    private float currentSwirlSpeed = 0f;
    private Vector3 containerBasePosition;
    private float orbitAngle = 0f;
    private Dictionary<Transform, IngredientOrbitData> ingredientOrbits = new Dictionary<Transform, IngredientOrbitData>();

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
            stirPrompt = GetComponentInChildren<StirPrompt>(true);

            if (stirPrompt == null)
            {
                Debug.LogWarning("Pot: No StirPrompt component found. Please add a StirPrompt UI to the pot.");
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

        // Determine which stir counts to use based on cooking phase
        int requiredStirCount = (currentCookingPhase == PotCookingPhase.SinigangCooking)
            ? sinigangRequiredStirs
            : requiredStirs;

        float boilDuration = (currentCookingPhase == PotCookingPhase.SinigangCooking)
            ? sinigangBoilingDurationBetweenStirs
            : boilingDurationBetweenStirs;

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
                    cookingProgressBar.color = Color.Lerp(Color.yellow, Color.orange, progress);
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
        int requiredStirCount = (currentCookingPhase == PotCookingPhase.SinigangCooking)
            ? sinigangRequiredStirs
            : requiredStirs;

        if (stirPrompt != null)
        {
            stirPrompt.ShowPrompt(completedStirs, requiredStirCount);
        }

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

        Debug.Log($"Stir {completedStirs}/{requiredStirs} completed!");

        // Tutorial event
        TutorialEvents.StirCompleted();

        // Show success feedback
        if (stirPrompt != null)
        {
            stirPrompt.ShowStirSuccess();
        }

        // Hide prompt after a short delay
        HideStirPrompt();

        // Start boiling
        currentStirState = StirringState.Boiling;
        boilingTimer = 0f;

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
        // Boiling phase complete (handles both sinigang and non-sinigang paths)
        if (currentCookingPhase == PotCookingPhase.Boiling)
        {
            CompleteBoilingPhase();
        }
        else if (currentCookingPhase == PotCookingPhase.SinigangCooking)
        {
            // Complete sinigang cooking
            CompleteSinigangCooking();
        }
        else
        {
            // Regular completion (for non-sinigang ingredients)
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

        // If sinigang mix already added, start sinigang cooking phase
        if (hasSinigangMix)
        {
            Debug.Log("Boiling complete! Starting sinigang cooking phase...");
            currentCookingPhase = PotCookingPhase.SinigangCooking;
            StartSinigangCooking();
        }
        else
        {
            Debug.Log("Boiling complete! Pot is now pourable - drag to PourZone.");
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
    }

    private void CompleteSinigangCooking()
    {
        Debug.Log("Sinigang cooking complete! Pot is now pourable - drag to PourZone.");

        // Tutorial event
        TutorialEvents.SinigangCompleted();

        // Complete sinigang cooking for all ingredients
        foreach (GameObject ingredient in ingredientsInStation)
        {
            if (ingredient != null)
            {
                ISinigangable sinigang = ingredient.GetComponent<ISinigangable>();
                if (sinigang != null)
                {
                    sinigang.CompleteSiniganging();
                }
            }
        }

        // Register with plating manager
        if (PlatingManager.Instance != null)
        {
            foreach (GameObject ingredient in ingredientsInStation)
            {
                if (ingredient != null)
                {
                    PlatingManager.Instance.RegisterSinigangChicken(ingredient);
                }
            }
        }

        // Make pot pourable instead of clearing immediately
        isCooking = false;
        isWindingDown = true;
        isPourable = true;
        potOriginalPosition = transform.position;
        potOriginalRotation = transform.rotation;

        HideStirPrompt();

        if (cookingMeterUI != null)
            cookingMeterUI.SetActive(false);

        if (cookingEffect != null)
            cookingEffect.Stop();

        // Bounce to hint "drag me"
        transform.DOPunchScale(Vector3.one * 0.1f, 0.5f, 4, 0.5f);
    }

    private void ClearPot()
    {
        base.CompleteCooking(); // Calls base to clean up

        ingredientsInStation.Clear();
        hasSinigangMix = false;
        currentCookingPhase = PotCookingPhase.Initial;
        isCooking = false;
        isWindingDown = false;
        currentSwirlSpeed = 0f;
        orbitAngle = 0f;
        ingredientOrbits.Clear();

        // Reset ingredient container position
        if (ingredientContainer != null)
        {
            ingredientContainer.localRotation = Quaternion.identity;
            ingredientContainer.localPosition = containerBasePosition;
        }

        // Reset visual to normal
        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        HideStirPrompt();

        if (cookingMeterUI != null)
            cookingMeterUI.SetActive(false);

        Debug.Log("Pot cleared and ready for next use");
    }

    public bool CanAcceptSinigangMix()
    {
        // Can accept sinigang mix BEFORE cooking starts (to mark it as sinigang mode)
        // OR after boiling is complete
        return !hasSinigangMix;
    }

    public bool IsSinigangMode()
    {
        return hasSinigangMix;
    }

    public void AddSinigangMix(Ingredient mix)
    {
        if (!CanAcceptSinigangMix())
        {
            Debug.LogWarning("Pot already has sinigang mix!");
            return;
        }

        Debug.Log($"Adding sinigang mix: {mix.ingredientName}");

        hasSinigangMix = true;

        // Tutorial event
        TutorialEvents.SinigangMixAdded();

        // Change pot color to indicate sinigang broth
        if (spriteRenderer != null)
        {
            spriteRenderer.color = sinigangBrothColor;
        }

        // If we're already done with initial boiling, start sinigang cooking
        if (currentCookingPhase == PotCookingPhase.BoilingComplete)
        {
            currentCookingPhase = PotCookingPhase.SinigangCooking;
            StartSinigangCooking();
        }
        else
        {
            Debug.Log("Sinigang mix added - pot is now in sinigang mode!");
            // Pot will start in sinigang mode when ingredients are added
        }
    }

    private void StartSinigangCooking()
    {
        Debug.Log("Starting sinigang cooking phase...");

        isCooking = true;
        completedStirs = 0;
        currentStirState = StirringState.WaitingForStir;

        // Start sinigang cooking on ingredients
        foreach (GameObject ingredient in ingredientsInStation)
        {
            if (ingredient != null)
            {
                ISinigangable sinigang = ingredient.GetComponent<ISinigangable>();
                if (sinigang != null)
                {
                    sinigang.StartSiniganging();
                }
            }
        }

        // Show stir prompt
        ShowStirPrompt();
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

        // Try to use SeasoningManager if available
        SeasoningManager seasoningMgr = GetComponent<SeasoningManager>();
        if (seasoningMgr != null)
        {
            seasoningMgr.AddSeasoning(seasoning);
        }
        else
        {
            // Fallback - just log if no manager is attached
            Debug.Log($"🧂 Added seasoning to pot: {seasoning.ingredientName} (no SeasoningManager attached)");
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
            Debug.Log($"💧 Added liquid to pot: {liquid.ingredientName} (no SeasoningManager attached)");
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

        GameObject destination = GameObject.FindWithTag(pourZone.DestinationTag);
        if (destination == null)
        {
            Debug.LogWarning($"Pot: No destination found with tag '{pourZone.DestinationTag}'");
            isPouring = false;
            SnapBackToOriginal();
            return;
        }

        Debug.Log($"Pot: Pouring {ingredientsInStation.Count} ingredients to {pourZone.DestinationTag}");

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

            // Re-enable dragging
            var drag = ingredient.GetComponent<ChickenDragBehavior>();
            if (drag != null)
                drag.enabled = true;

            // Re-enable collider
            var col = ingredient.GetComponent<Collider2D>();
            if (col != null)
                col.enabled = true;

            // Animate to destination with stagger
            ingredient.transform.DOMove(destination.transform.position, pourZone.PourDuration)
                .SetDelay(count * 0.1f)
                .SetEase(Ease.OutQuad);

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
        // Clear the pot state
        ingredientsInStation.Clear();
        isPourable = false;
        isPouring = false;
        isWindingDown = false;
        hasSinigangMix = false;
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

        Debug.Log("Pot poured and cleared - ready for next use");
    }

    private void SnapBackToOriginal()
    {
        transform.DOMove(potOriginalPosition, 0.3f).SetEase(Ease.OutQuad);
        transform.DORotateQuaternion(potOriginalRotation, 0.3f).SetEase(Ease.OutQuad);
    }

    #endregion
}
