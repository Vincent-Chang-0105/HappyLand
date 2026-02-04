using UnityEngine;

[CreateAssetMenu(fileName = "TutorialData", menuName = "Tutorial/Tutorial Data")]
public class TutorialData : ScriptableObject
{
    public string tutorialName;
    public TutorialStep[] steps;
    public bool autoStartFirstStep = true;

    public TutorialStep GetStep(int stepId)
    {
        foreach (var step in steps)
        {
            if (step.stepId == stepId)
                return step;
        }
        return null;
    }

    public TutorialStep GetNextStep(int currentStepId, TutorialBranch activeBranch)
    {
        TutorialStep currentStep = GetStep(currentStepId);
        if (currentStep == null)
        {
            UnityEngine.Debug.Log($"GetNextStep: Current step {currentStepId} not found!");
            return null;
        }

        // If explicit next step specified
        if (currentStep.nextStepId >= 0)
        {
            UnityEngine.Debug.Log($"GetNextStep: Using explicit nextStepId {currentStep.nextStepId}");
            return GetStep(currentStep.nextStepId);
        }

        // Find next sequential step that matches branch
        int nextId = currentStepId + 1;
        UnityEngine.Debug.Log($"GetNextStep: Looking for step {nextId} (auto-increment from {currentStepId})");

        // Search through all steps to find a matching one
        for (int i = 0; i < steps.Length; i++)
        {
            TutorialStep step = GetStep(nextId);
            if (step != null)
            {
                UnityEngine.Debug.Log($"GetNextStep: Found step {nextId} with branch {step.branch}, active branch is {activeBranch}");
                // Accept if it's common OR matches our active branch
                if (step.branch == TutorialBranch.Common || step.branch == activeBranch)
                {
                    return step;
                }
                // Skip this step, try next ID
                UnityEngine.Debug.Log($"GetNextStep: Skipping step {nextId} due to branch mismatch");
                nextId++;
            }
            else
            {
                // No step found at this ID, we're done
                UnityEngine.Debug.Log($"GetNextStep: No step found with ID {nextId}. Available step IDs: {string.Join(", ", System.Array.ConvertAll(steps, s => s.stepId.ToString()))}");
                break;
            }
        }

        return null;
    }

    public void ResetAllSteps()
    {
        foreach (var step in steps)
        {
            step.isCompleted = false;
        }
    }
}
