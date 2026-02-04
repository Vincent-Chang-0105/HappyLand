using UnityEngine;
using System.Collections;

public class TutorialManager : PersistentSingleton<TutorialManager>
{
    [Header("Tutorial Data")]
    [SerializeField] private TutorialData tutorialData;

    [Header("UI References")]
    [SerializeField] private TutorialUIPanel uiPanel;
    [SerializeField] private TutorialArrowIndicator arrowIndicator;

    [Header("VFX")]
    [SerializeField] private ParticleSystem taskCompletionVFX;

    [Header("Settings")]
    [SerializeField] private bool pauseGameDuringSteps = true;
    [SerializeField] private float pauseDelay = 0.1f;

    // State
    private TutorialStep currentStep;
    private TutorialBranch activeBranch = TutorialBranch.Common;
    private bool isTutorialActive = false;
    private bool isWaitingForAcknowledge = false;
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
    public TutorialBranch ActiveBranch => activeBranch;

    protected override void Awake()
    {
        base.Awake();
        SubscribeToGameEvents();
    }

    private void OnDestroy()
    {
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

        // Validate step IDs
        Debug.Log($"TutorialManager: Found {tutorialData.steps.Length} steps with IDs: {string.Join(", ", System.Array.ConvertAll(tutorialData.steps, s => s.stepId.ToString()))}");

        tutorialData.ResetAllSteps();
        isTutorialActive = true;
        activeBranch = TutorialBranch.Common;

        Debug.Log("Tutorial started!");

        if (tutorialData.autoStartFirstStep && tutorialData.steps.Length > 0)
        {
            Debug.Log($"Auto-starting first step: ID={tutorialData.steps[0].stepId}, Name={tutorialData.steps[0].stepName}, CompletionType={tutorialData.steps[0].completionType}");
            ShowStep(tutorialData.steps[0]);
        }
    }

    public void StopTutorial()
    {
        isTutorialActive = false;
        HideCurrentStep();
        ResumeGame();
        OnTutorialCompleted?.Invoke();
        Debug.Log("Tutorial completed!");
    }

    public void SetBranch(TutorialBranch branch)
    {
        activeBranch = branch;
        Debug.Log($"Tutorial branch set to: {branch}");
    }

    private void ShowStep(TutorialStep step)
    {
        if (step == null)
        {
            StopTutorial();
            return;
        }

        currentStep = step;
        OnStepStarted?.Invoke(step);

        Debug.Log($"Tutorial Step {step.stepId}: {step.stepName} (showUI={step.showUI})");

        // Silent step - just wait for event, no UI/pause
        if (!step.showUI)
        {
            isWaitingForAcknowledge = false;
            // Game keeps running, waiting for event to trigger completion
            return;
        }

        // Determine if this is the branch selection step
        bool isBranchSelection = step.stepName == "Choose Path" ||
                                  step.instructionText.Contains("Choose:");

        // Show UI
        if (uiPanel != null)
        {
            uiPanel.ShowInstruction(step.instructionText, isBranchSelection);
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

        // Handle completion type
        if (step.completionType == TutorialCompletionType.ButtonPress)
        {
            isWaitingForAcknowledge = true;
        }
        else if (step.autoCompleteDelay > 0)
        {
            StartCoroutine(AutoCompleteAfterDelay(step.autoCompleteDelay));
        }
        else
        {
            // Waiting for game event to complete this step
            isWaitingForAcknowledge = false;
        }
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

        Debug.Log($"OnContinueButtonPressed called. currentStep={currentStep?.stepName}, completionType={currentStep?.completionType}");

        // For ButtonPress completion, complete the step immediately
        if (currentStep.completionType == TutorialCompletionType.ButtonPress)
        {
            isWaitingForAcknowledge = false;
            ResumeGame();
            CompleteCurrentStep();
        }
        else
        {
            // For event-based completion, just resume the game and hide UI
            // The step will complete when the actual event fires
            Debug.Log("OnContinueButtonPressed: Resuming game, waiting for event to complete step");
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

        Debug.Log($"Completed step {currentStep.stepId}: {currentStep.stepName}");

        // Play completion VFX
        if (taskCompletionVFX != null)
        {
            // Ensure particle system uses unscaled time so it plays even when paused
            var main = taskCompletionVFX.main;
            main.useUnscaledTime = true;

            taskCompletionVFX.Stop();
            taskCompletionVFX.Play();
            Debug.Log("TutorialManager: Playing completion VFX");
        }
        else
        {
            Debug.LogWarning("TutorialManager: taskCompletionVFX is not assigned!");
        }

        // Execute action on complete
        ExecuteStepAction(currentStep);

        HideCurrentStep();

        // Get next step
        TutorialStep nextStep = tutorialData.GetNextStep(currentStep.stepId, activeBranch);

        if (nextStep != null)
        {
            Debug.Log($"Moving to next step {nextStep.stepId}: {nextStep.stepName}");

            // Check if next step has a show delay
            if (nextStep.showDelay > 0)
            {
                StartCoroutine(ShowStepAfterDelay(nextStep, nextStep.showDelay));
            }
            else
            {
                ShowStep(nextStep);
            }
        }
        else
        {
            Debug.Log($"No next step found after step {currentStep.stepId}. Tutorial ending.");
            StopTutorial();
        }
    }

    private IEnumerator ShowStepAfterDelay(TutorialStep step, float delay)
    {
        Debug.Log($"Waiting {delay}s before showing step {step.stepId}: {step.stepName}");
        yield return new WaitForSecondsRealtime(delay);
        ShowStep(step);
    }

    private void ExecuteStepAction(TutorialStep step)
    {
        if (step.actionOnComplete == TutorialActionType.None)
            return;

        Debug.Log($"Executing action: {step.actionOnComplete} with parameter: {step.actionParameter}");

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
        {
            uiPanel.HideInstruction();
        }

        if (arrowIndicator != null)
        {
            arrowIndicator.HideArrow();
        }
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
                Debug.Log("TutorialManager: Unknown target type or not implemented: " + type);
                return null;
        }
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
        TutorialEvents.OnCookScreenEntered += HandleCookScreenEntered;
        TutorialEvents.OnServeScreenEntered += HandleServeScreenEntered;
        TutorialEvents.OnBoilScreenEntered += HandleBoilScreenEntered;
        TutorialEvents.OnCutScreenEntered += HandleCutScreenEntered;
        TutorialEvents.OnThreeChickensWashed += HandleThreeChickensWashed;
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
        TutorialEvents.OnCookScreenEntered -= HandleCookScreenEntered;
        TutorialEvents.OnServeScreenEntered -= HandleServeScreenEntered;
        TutorialEvents.OnBoilScreenEntered -= HandleBoilScreenEntered;
        TutorialEvents.OnCutScreenEntered -= HandleCutScreenEntered;
        TutorialEvents.OnThreeChickensWashed -= HandleThreeChickensWashed;
    }

    private void CheckAndCompleteStep(TutorialCompletionType completionType)
    {
        if (!isTutorialActive || currentStep == null) return;
        if (currentStep.completionType == completionType)
        {
            ResumeGame(); // Resume before completing so next step can pause again
            CompleteCurrentStep();
        }
    }

    private void HandleChickenPickedUp() => CheckAndCompleteStep(TutorialCompletionType.ChickenPickedUp);

    private void HandleChickenWashed()
    {
        chickensWashedCount++;
        Debug.Log($"TutorialManager: Chicken washed! Count: {chickensWashedCount}/3");

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
    private void HandleCookScreenEntered() => CheckAndCompleteStep(TutorialCompletionType.CookScreenEntered);
    private void HandleServeScreenEntered() => CheckAndCompleteStep(TutorialCompletionType.ServeScreenEntered);
    private void HandleBoilScreenEntered() => CheckAndCompleteStep(TutorialCompletionType.BoilScreenEntered);
    private void HandleCutScreenEntered() => CheckAndCompleteStep(TutorialCompletionType.CutScreenEntered);
    private void HandleThreeChickensWashed() => CheckAndCompleteStep(TutorialCompletionType.ThreeChickensWashed);

    #endregion
}
