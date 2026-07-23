using UnityEngine;
using System.Collections;

public class TutorialManager : ImpersistentSingleton<TutorialManager>
{
    [Header("Tutorial Data")]
    [SerializeField] private TutorialData tutorialData;

    [Header("UI References")]
    [SerializeField] private TutorialUIPanel uiPanel;
    [SerializeField] private TutorialArrowIndicator arrowIndicator;
    [SerializeField] private TutorialUIHighlighter uiHighlighter;

    [Header("VFX")]
    [SerializeField] private ParticleSystem taskCompletionVFX;

    [Header("Settings")]
    [SerializeField] private bool pauseGameDuringSteps = true;
    [SerializeField] private float pauseDelay = 0.1f;

    [Header("Navigation Control")]
    [SerializeField] private ScreenTransitionManager screenTransitionManager;
    [SerializeField] private bool controlNavigationDuringTutorial = true;

    // State
    private TutorialStep currentStep;
    private int currentStepIndex = -1;
    private bool isTutorialActive = false;
    private float previousTimeScale = 1f;

    // Counters for progress tracking
    private int chickensWashedCount = 0;


    // Events
    public event System.Action<TutorialStep> OnStepStarted;
    public event System.Action<TutorialStep> OnStepCompleted;
    public event System.Action OnTutorialCompleted;

    // Properties
    public bool IsTutorialActive => isTutorialActive;
    public TutorialStep CurrentStep => currentStep;

    /// <summary>
    /// Called by TutorialSceneSetup each time the Tutorial scene loads to refresh stale scene references.
    /// Required because TutorialManager persists across scenes as a PersistentSingleton.
    /// </summary>
    public void RefreshSceneReferences(TutorialUIPanel panel, TutorialArrowIndicator arrow, ScreenTransitionManager stm, ParticleSystem vfx = null)
    {
        uiPanel = panel;
        arrowIndicator = arrow;
        screenTransitionManager = stm;
        if (vfx != null) taskCompletionVFX = vfx;
    }

    protected override void Awake()
    {
        base.Awake();
        SubscribeToGameEvents();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        UnsubscribeFromGameEvents();
    }

    #region Tutorial Flow Control

    public void StartTutorial()
    {
        if (tutorialData == null)
        {
            Debug.LogError("TutorialManager: No tutorial data assigned!");
            return;
        }

        // Validate references
        if (uiPanel == null)
        {
            Debug.LogError("TutorialManager: uiPanel reference is not assigned! Drag your TutorialUIPanel here.");
        }

        if (arrowIndicator == null)
        {
            Debug.LogWarning("TutorialManager: arrowIndicator reference is not assigned. Arrows won't show.");
        }

        //Debug.Log($"TutorialManager: Found {tutorialData.steps.Length} steps");

        tutorialData.ResetAllSteps();
        isTutorialActive = true;

        // Enable tutorial mode in screen transition manager
        if (screenTransitionManager != null && controlNavigationDuringTutorial)
        {
            screenTransitionManager.EnableTutorialMode();
        }

        //Debug.Log("Tutorial started!");

        if (tutorialData.autoStartFirstStep && tutorialData.steps.Length > 0)
        {
            //Debug.Log($"Auto-starting first step: Name={tutorialData.steps[0].stepName}, CompletionType={tutorialData.steps[0].completionType}");
            ShowStep(0);
        }
    }

    public void StopTutorial()
    {
        isTutorialActive = false;
        HideCurrentStep();
        ResumeGame();

        // Disable tutorial mode in screen transition manager
        if (screenTransitionManager != null && controlNavigationDuringTutorial)
        {
            screenTransitionManager.DisableTutorialMode();
        }

        OnTutorialCompleted?.Invoke();
        //Debug.Log("Tutorial completed!");
    }

    private void ShowStep(int stepIndex)
    {
        TutorialStep step = tutorialData.GetStep(stepIndex);
        if (step == null)
        {
            StopTutorial();
            return;
        }

        currentStepIndex = stepIndex;
        currentStep = step;
        OnStepStarted?.Invoke(step);

        //Debug.Log($"Tutorial Step {stepIndex}: {step.stepName} (showUI={step.showUI})");

        // Control navigation buttons based on step requirements
        if (screenTransitionManager != null && controlNavigationDuringTutorial)
        {
            if (step.controlNavigation && step.allowedDirections != null && step.allowedDirections.Count > 0)
            {
                // Manual control: use specified allowed directions
                screenTransitionManager.SetAllowedNavigationButtons(step.allowedDirections);
            }
            else
            {
                // Auto control: detect required navigation from completion type
                System.Collections.Generic.List<DirectionButton> requiredNav = GetRequiredNavigationForStep(step);

                if (requiredNav.Count > 0)
                {
                    screenTransitionManager.SetAllowedNavigationButtons(requiredNav);
                }
                else
                {
                    // No navigation required for this step, enable all
                    screenTransitionManager.EnableAllNavigationButtons();
                }
            }
        }

        // Silent step - just wait for event, no UI/pause
        if (!step.showUI)
            return;

        // Show UI
        if (uiPanel != null)
        {
            uiPanel.ShowInstruction(step.instructionText);
        }

        // Show UI highlight if a tag is specified
        if (uiHighlighter != null)
        {
            if (!string.IsNullOrEmpty(step.highlightUITag))
            {
                GameObject highlightObj = GameObject.FindWithTag(step.highlightUITag);
                RectTransform highlightRect = highlightObj != null ? highlightObj.GetComponent<RectTransform>() : null;
                if (highlightRect != null)
                    uiHighlighter.Show(highlightRect);
                else
                    uiHighlighter.Hide();
            }
            else
            {
                uiHighlighter.Hide();
            }
        }

        // Show arrow pointing to target
        if (step.showArrow && arrowIndicator != null && step.targetType != TutorialTargetType.None)
        {
            GameObject target = FindTargetObject(step);
            if (target != null)
            {
                arrowIndicator.ShowArrow(target.transform, step.arrowOffset);
            }
        }
        else if (arrowIndicator != null)
        {
            arrowIndicator.HideArrow();
        }

        // Pause game if needed
        if (pauseGameDuringSteps)
        {
            StartCoroutine(PauseAfterDelay());
        }

        // Skip steps the player already completed out of order
        if (step.isCompleted)
        {
            ShowStep(stepIndex + 1);
            return;
        }

        // Handle completion type
        if (step.autoCompleteDelay > 0)
            StartCoroutine(AutoCompleteAfterDelay(step.autoCompleteDelay));
    }

    private IEnumerator PauseAfterDelay()
    {
        yield return new WaitForSecondsRealtime(pauseDelay);
        PauseGame();
    }

    private IEnumerator AutoCompleteAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        CompleteCurrentStep();
    }

    public void OnContinueButtonPressed()
    {
        if (currentStep == null) return;

        //Debug.Log($"OnContinueButtonPressed called. currentStep={currentStep?.stepName}, completionType={currentStep?.completionType}");

        // For ButtonPress completion, complete the step immediately
        if (currentStep.completionType == TutorialCompletionType.ButtonPress)
        {
            ResumeGame();
            CompleteCurrentStep();
        }
        else
        {
            // For event-based completion, just resume the game and hide UI
            // The step will complete when the actual event fires
            //Debug.Log("OnContinueButtonPressed: Resuming game, waiting for event to complete step");
            ResumeGame();
            if (uiPanel != null)
            {
                uiPanel.HideInstruction();
            }
        }
    }

    public void CompleteCurrentStep()
    {
        if (currentStep == null) return;

        currentStep.isCompleted = true;
        OnStepCompleted?.Invoke(currentStep);

        //Debug.Log($"Completed step {currentStepIndex}: {currentStep.stepName}");

        // Play completion VFX
        if (taskCompletionVFX != null)
        {
            // Ensure particle system uses unscaled time so it plays even when paused
            var main = taskCompletionVFX.main;
            main.useUnscaledTime = true;

            taskCompletionVFX.Stop();
            taskCompletionVFX.Play();
            //Debug.Log("TutorialManager: Playing completion VFX");
        }
        else
        {
            Debug.LogWarning("TutorialManager: taskCompletionVFX is not assigned!");
        }

        // Execute action on complete
        ExecuteStepAction(currentStep);

        HideCurrentStep();

        // Get next step
        TutorialStep nextStep = tutorialData.GetNextStep(currentStepIndex);
        int nextIndex = currentStepIndex + 1;

        if (nextStep != null)
        {
            //Debug.Log($"Moving to next step {nextIndex}: {nextStep.stepName}");

            // Check if next step has a show delay
            if (nextStep.showDelay > 0)
            {
                StartCoroutine(ShowStepAfterDelay(nextIndex, nextStep.showDelay));
            }
            else
            {
                ShowStep(nextIndex);
            }
        }
        else
        {
            //Debug.Log($"No next step found after step {currentStepIndex}. Tutorial ending.");
            StopTutorial();
        }
    }

    private IEnumerator ShowStepAfterDelay(int stepIndex, float delay)
    {
        //Debug.Log($"Waiting {delay}s before showing step {stepIndex}");
        yield return new WaitForSecondsRealtime(delay);
        ShowStep(stepIndex);
    }

    private void ExecuteStepAction(TutorialStep step)
    {
        if (step.actionOnComplete == TutorialActionType.None)
            return;

        //Debug.Log($"Executing action: {step.actionOnComplete} with parameter: {step.actionParameter}");

        switch (step.actionOnComplete)
        {
            case TutorialActionType.SpawnCustomerWithOrder:
                var customerGenerator = FindFirstObjectByType<CustomerGenerator>();
                if (customerGenerator != null)
                {
                    customerGenerator.SpawnCustomerWithSpecificOrder(step.actionParameter);
                }
                else
                {
                    Debug.LogError("TutorialManager: Could not find CustomerGenerator to spawn customer!");
                }
                break;

            case TutorialActionType.StopCustomerGeneration:
                var genStop = FindFirstObjectByType<CustomerGenerator>();
                if (genStop != null)
                {
                    genStop.StopCustomerGeneration();
                }
                break;

            case TutorialActionType.StartCustomerGeneration:
                var genStart = FindFirstObjectByType<CustomerGenerator>();
                if (genStart != null)
                {
                    genStart.StartCustomerGeneration();
                }
                break;

            case TutorialActionType.ResumeGameOnly:
                // Already handled by ResumeGame() in the calling code
                break;
        }
    }

    private void HideCurrentStep()
    {
        if (uiPanel != null)
            uiPanel.HideInstruction();

        if (arrowIndicator != null)
            arrowIndicator.HideArrow();

        if (uiHighlighter != null)
            uiHighlighter.Hide();
    }

    #endregion

    #region Pause/Resume

    private void PauseGame()
    {
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
    }

    private void ResumeGame()
    {
        Time.timeScale = previousTimeScale > 0 ? previousTimeScale : 1f;
    }

    #endregion

    #region Target Finding

    private GameObject FindTargetObject(TutorialStep step)
    {
        // First try by tag
        if (!string.IsNullOrEmpty(step.targetObjectTag))
        {
            GameObject obj = GameObject.FindGameObjectWithTag(step.targetObjectTag);
            if (obj != null) return obj;
        }

        // Then by name
        if (!string.IsNullOrEmpty(step.targetObjectName))
        {
            GameObject obj = GameObject.Find(step.targetObjectName);
            if (obj != null) return obj;
        }

        // Finally by type
        return FindTargetByType(step.targetType);
    }

    private GameObject FindTargetByType(TutorialTargetType type)
    {
        switch (type)
        {
            case TutorialTargetType.Chicken:
                var chicken = FindFirstObjectByType<Chicken>();
                return chicken?.gameObject;
            case TutorialTargetType.Faucet:
                var faucet = FindFirstObjectByType<Faucet>();
                return faucet?.gameObject;
            case TutorialTargetType.Pot:
                var pot = FindFirstObjectByType<Pot>();
                return pot?.gameObject;
            case TutorialTargetType.Pan:
                var pan = FindFirstObjectByType<Pan>();
                return pan?.gameObject;
            case TutorialTargetType.BoilBowl:
                return GameObject.FindGameObjectWithTag("BoilBowl");
            case TutorialTargetType.FryBowl:
                return GameObject.FindGameObjectWithTag("FryBowl");
            case TutorialTargetType.Customer:
                var customer = FindFirstObjectByType<Customer>();
                return customer?.gameObject;
            case TutorialTargetType.PlatedDish:
                var dish = FindFirstObjectByType<PlatedDish>();
                return dish?.gameObject;
            case TutorialTargetType.OrderSlot:
                return GameObject.FindGameObjectWithTag("OrderSlot");
            default:
                //Debug.Log("TutorialManager: Unknown target type or not implemented: " + type);
                return null;
        }
    }

    #endregion

    #region Navigation Control

    private System.Collections.Generic.List<DirectionButton> GetRequiredNavigationForStep(TutorialStep step)
    {
        System.Collections.Generic.List<DirectionButton> required = new System.Collections.Generic.List<DirectionButton>();

        switch (step.completionType)
        {
            case TutorialCompletionType.WashScreenEntered:
                // Determine which button leads to Wash screen based on current position
                // You'll need to configure this based on your actual screen layout
                required.Add(DirectionButton.Down); // Example: Wash is down
                break;

            case TutorialCompletionType.CookScreenEntered:
                required.Add(DirectionButton.Right); // Example: Cook is to the right
                break;

            case TutorialCompletionType.ServeScreenEntered:
                required.Add(DirectionButton.Up); // Example: Serve is up
                break;

            case TutorialCompletionType.BoilScreenEntered:
                required.Add(DirectionButton.Right); // Example: Boil is to the right
                break;

            case TutorialCompletionType.CutScreenEntered:
                required.Add(DirectionButton.Right); // Example: Cut is to the right
                break;

            default:
                // For non-navigation steps, no specific navigation is required
                // Returning empty list will enable all navigation buttons
                break;
        }

        return required;
    }

    #endregion

    #region Game Event Subscriptions

    private void SubscribeToGameEvents()
    {
        TutorialEvents.OnChickenPickedUp += HandleChickenPickedUp;
        TutorialEvents.OnChickenWashed += HandleChickenWashed;
        TutorialEvents.OnChickenEnteredBowl += HandleChickenEnteredBowl;
        TutorialEvents.OnBowlDroppedOnStation += HandleBowlDroppedOnStation;
        TutorialEvents.OnStirCompleted += HandleStirCompleted;
        TutorialEvents.OnAllStirsCompleted += HandleAllStirsCompleted;
        TutorialEvents.OnOilAdded += HandleOilAdded;
        TutorialEvents.OnSaltAdded += HandleSaltAdded;
        TutorialEvents.OnTossCompleted += HandleTossCompleted;
        TutorialEvents.OnAllTossesCompleted += HandleAllTossesCompleted;
        TutorialEvents.OnSinigangMixAdded += HandleSinigangMixAdded;
        TutorialEvents.OnSinigangCompleted += HandleSinigangCompleted;
        TutorialEvents.OnDishPlated += HandleDishPlated;
        TutorialEvents.OnDishServed += HandleDishServed;
        TutorialEvents.OnCustomerArrived += HandleCustomerArrived;
        TutorialEvents.OnCustomerWaiting += HandleCustomerWaiting;
        TutorialEvents.OnOrderSlotClicked += HandleOrderSlotClicked;
        TutorialEvents.OnWashScreenEntered += HandleWashScreenEntered;
        TutorialEvents.OnFaucetOpened += HandleFaucetOpened;
        TutorialEvents.OnFaucetClosed += HandleFaucetClosed;
        TutorialEvents.OnCookScreenEntered += HandleCookScreenEntered;
        TutorialEvents.OnServeScreenEntered += HandleServeScreenEntered;
        TutorialEvents.OnBoilScreenEntered += HandleBoilScreenEntered;
        TutorialEvents.OnCutScreenEntered += HandleCutScreenEntered;
        TutorialEvents.OnThreeChickensWashed += HandleThreeChickensWashed;
        TutorialEvents.OnChickenTransferred += HandleChickenTransferred;
        TutorialEvents.OnChickenAddedToPlate += HandleChickenAddedToPlate;
        TutorialEvents.OnFriedChickenCompleted += HandleFriedChickenCompleted;
    }

    private void UnsubscribeFromGameEvents()
    {
        TutorialEvents.OnChickenPickedUp -= HandleChickenPickedUp;
        TutorialEvents.OnChickenWashed -= HandleChickenWashed;
        TutorialEvents.OnChickenEnteredBowl -= HandleChickenEnteredBowl;
        TutorialEvents.OnBowlDroppedOnStation -= HandleBowlDroppedOnStation;
        TutorialEvents.OnStirCompleted -= HandleStirCompleted;
        TutorialEvents.OnAllStirsCompleted -= HandleAllStirsCompleted;
        TutorialEvents.OnOilAdded -= HandleOilAdded;
        TutorialEvents.OnSaltAdded -= HandleSaltAdded;
        TutorialEvents.OnTossCompleted -= HandleTossCompleted;
        TutorialEvents.OnAllTossesCompleted -= HandleAllTossesCompleted;
        TutorialEvents.OnSinigangMixAdded -= HandleSinigangMixAdded;
        TutorialEvents.OnSinigangCompleted -= HandleSinigangCompleted;
        TutorialEvents.OnDishPlated -= HandleDishPlated;
        TutorialEvents.OnDishServed -= HandleDishServed;
        TutorialEvents.OnCustomerArrived -= HandleCustomerArrived;
        TutorialEvents.OnCustomerWaiting -= HandleCustomerWaiting;
        TutorialEvents.OnOrderSlotClicked -= HandleOrderSlotClicked;
        TutorialEvents.OnWashScreenEntered -= HandleWashScreenEntered;
        TutorialEvents.OnFaucetOpened -= HandleFaucetOpened;
        TutorialEvents.OnFaucetClosed -= HandleFaucetClosed;
        TutorialEvents.OnCookScreenEntered -= HandleCookScreenEntered;
        TutorialEvents.OnServeScreenEntered -= HandleServeScreenEntered;
        TutorialEvents.OnBoilScreenEntered -= HandleBoilScreenEntered;
        TutorialEvents.OnCutScreenEntered -= HandleCutScreenEntered;
        TutorialEvents.OnThreeChickensWashed -= HandleThreeChickensWashed;
        TutorialEvents.OnChickenTransferred -= HandleChickenTransferred;
        TutorialEvents.OnChickenAddedToPlate -= HandleChickenAddedToPlate;
        TutorialEvents.OnFriedChickenCompleted -= HandleFriedChickenCompleted;
    }

    private void CheckAndCompleteStep(TutorialCompletionType completionType)
    {
        if (!isTutorialActive || currentStep == null) return;

        if (currentStep.completionType == completionType)
        {
            ResumeGame();
            CompleteCurrentStep();
        }
        else
        {
            // Mark the matching future step as done so it gets skipped when we reach it
            MarkFutureStepCompleted(completionType);
        }
    }

    private void MarkFutureStepCompleted(TutorialCompletionType completionType)
    {
        if (tutorialData == null) return;
        for (int i = currentStepIndex + 1; i < tutorialData.steps.Length; i++)
        {
            if (tutorialData.steps[i].completionType == completionType)
            {
                tutorialData.steps[i].isCompleted = true;
                return;
            }
        }
    }

    private void HandleChickenPickedUp() => CheckAndCompleteStep(TutorialCompletionType.ChickenPickedUp);

    private void HandleChickenWashed()
    {
        chickensWashedCount++;
        //Debug.Log($"TutorialManager: Chicken washed! Count: {chickensWashedCount}/3");

        CheckAndCompleteStep(TutorialCompletionType.ChickenWashed);

        // Check if 3 chickens have been washed
        if (chickensWashedCount >= 3)
        {
            TutorialEvents.ThreeChickensWashed();
        }
    }

    private void HandleChickenEnteredBowl(string bowlTag)
    {
        if (bowlTag == "BoilBowl")
            CheckAndCompleteStep(TutorialCompletionType.ChickenInBoilBowl);
        else if (bowlTag == "FryBowl")
            CheckAndCompleteStep(TutorialCompletionType.ChickenInFryBowl);
    }

    private void HandleBowlDroppedOnStation(string stationType)
    {
        if (stationType == "Pot")
            CheckAndCompleteStep(TutorialCompletionType.BowlOnPot);
        else if (stationType == "Pan")
            CheckAndCompleteStep(TutorialCompletionType.BowlOnPan);
    }

    private void HandleStirCompleted() => CheckAndCompleteStep(TutorialCompletionType.StirComplete);
    private void HandleAllStirsCompleted() => CheckAndCompleteStep(TutorialCompletionType.AllStirsComplete);
    private void HandleOilAdded() => CheckAndCompleteStep(TutorialCompletionType.OilAdded);
    private void HandleSaltAdded() => CheckAndCompleteStep(TutorialCompletionType.SaltAdded);
    private void HandleTossCompleted() => CheckAndCompleteStep(TutorialCompletionType.TossComplete);
    private void HandleAllTossesCompleted() => CheckAndCompleteStep(TutorialCompletionType.AllTossesComplete);
    private void HandleSinigangMixAdded() => CheckAndCompleteStep(TutorialCompletionType.SinigangMixAdded);
    private void HandleSinigangCompleted() => CheckAndCompleteStep(TutorialCompletionType.SinigangComplete);
    private void HandleDishPlated() => CheckAndCompleteStep(TutorialCompletionType.DishPlated);
    private void HandleDishServed() => CheckAndCompleteStep(TutorialCompletionType.DishServed);
    private void HandleCustomerArrived() => CheckAndCompleteStep(TutorialCompletionType.CustomerArrived);
    private void HandleCustomerWaiting() => CheckAndCompleteStep(TutorialCompletionType.CustomerWaiting);
    private void HandleOrderSlotClicked() => CheckAndCompleteStep(TutorialCompletionType.OrderSlotClicked);
    private void HandleWashScreenEntered() => CheckAndCompleteStep(TutorialCompletionType.WashScreenEntered);
    private void HandleFaucetOpened() => CheckAndCompleteStep(TutorialCompletionType.FaucetOpened);
    private void HandleFaucetClosed() => CheckAndCompleteStep(TutorialCompletionType.FaucetClosed);
    private void HandleCookScreenEntered() => CheckAndCompleteStep(TutorialCompletionType.CookScreenEntered);
    private void HandleServeScreenEntered() => CheckAndCompleteStep(TutorialCompletionType.ServeScreenEntered);
    private void HandleBoilScreenEntered() => CheckAndCompleteStep(TutorialCompletionType.BoilScreenEntered);
    private void HandleCutScreenEntered() => CheckAndCompleteStep(TutorialCompletionType.CutScreenEntered);
    private void HandleThreeChickensWashed() => CheckAndCompleteStep(TutorialCompletionType.ThreeChickensWashed);
    private void HandleChickenTransferred() => CheckAndCompleteStep(TutorialCompletionType.ChickenTransferred);
    private void HandleChickenAddedToPlate() => CheckAndCompleteStep(TutorialCompletionType.ChickenAddedToPlate);
    private void HandleFriedChickenCompleted() => CheckAndCompleteStep(TutorialCompletionType.FriedChickenCompleted);

    #endregion
}
