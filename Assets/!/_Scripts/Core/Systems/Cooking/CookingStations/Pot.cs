using UnityEngine;

/// <summary>
/// Pot cooking station - specialized for boiling ingredients
/// Features gesture-based stirring where players must make circular motions 3 times during cooking
/// </summary>
public class Pot : CookingStation
{
    [Header("Gesture-Based Stirring")]
    [SerializeField] private bool useGestureStirring = true;
    [SerializeField] private int requiredStirs = 3;
    [SerializeField] private float boilingDurationBetweenStirs = 2.5f;
    [SerializeField] private GestureDetector gestureDetector;
    [SerializeField] private StirPrompt stirPrompt;

    // Stirring state
    private enum StirringState
    {
        NotStarted,
        WaitingForStir,
        Stirring,
        Boiling
    }

    private StirringState currentStirState = StirringState.NotStarted;
    private int completedStirs = 0;
    private float boilingTimer = 0f;

    #region Unity Lifecycle

    protected override void Start()
    {
        base.Start();

        // Setup gesture detector if not assigned
        if (useGestureStirring && gestureDetector == null)
        {
            gestureDetector = gameObject.AddComponent<GestureDetector>();
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
                    float progress = boilingTimer / boilingDurationBetweenStirs;
                    cookingProgressBar.fillAmount = progress;
                    cookingProgressBar.color = Color.Lerp(Color.yellow, Color.orange, progress);
                }

                // After boiling time is up, request next stir or complete
                if (boilingTimer >= boilingDurationBetweenStirs)
                {
                    if (completedStirs < requiredStirs)
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
        if (stirPrompt != null)
        {
            stirPrompt.ShowPrompt(completedStirs, requiredStirs);
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
