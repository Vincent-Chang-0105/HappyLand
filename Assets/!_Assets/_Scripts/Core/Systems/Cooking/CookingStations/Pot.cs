using UnityEngine;
using System.Linq;

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

    private StirringState currentStirState = StirringState.NotStarted;
    private PotCookingPhase currentCookingPhase = PotCookingPhase.Initial;
    private int completedStirs = 0;
    private float boilingTimer = 0f;
    private bool hasSinigangMix = false;

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
        // Pot has no additional requirements (unlike Pan which needs oil)
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
    }

    #endregion

    #region Two-Phase Cooking (Sinigang)

    protected override void CompleteCooking()
    {
        // If sinigang mix was added, we're in sinigang mode
        if (hasSinigangMix && currentCookingPhase == PotCookingPhase.Boiling)
        {
            // Just finished first boiling phase with sinigang mix already added
            // Need to continue with sinigang cooking
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
            Debug.Log("Boiling complete! Waiting for sinigang mix...");
            currentCookingPhase = PotCookingPhase.BoilingComplete;
            isCooking = false;

            // Hide UI temporarily
            if (cookingMeterUI != null)
                cookingMeterUI.SetActive(false);

            HideStirPrompt();

            // Stop effects
            if (cookingEffect != null)
                cookingEffect.Stop();
        }
    }

    private void CompleteSinigangCooking()
    {
        Debug.Log("Sinigang cooking complete!");

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

        // Clear pot for next use
        ClearPot();
    }

    private void ClearPot()
    {
        base.CompleteCooking(); // Calls base to clean up

        ingredientsInStation.Clear();
        hasSinigangMix = false;
        currentCookingPhase = PotCookingPhase.Initial;
        isCooking = false;

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
}
