using UnityEngine;

[CreateAssetMenu(fileName = "TutorialData", menuName = "Tutorial/Tutorial Data")]
public class TutorialData : ScriptableObject
{
    public string tutorialName;
    public TutorialStep[] steps;
    public bool autoStartFirstStep = true;

    public TutorialStep GetStep(int index)
    {
        if (index >= 0 && index < steps.Length)
            return steps[index];
        return null;
    }

    public TutorialStep GetNextStep(int currentIndex)
    {
        int nextIndex = currentIndex + 1;
        if (nextIndex < steps.Length)
            return steps[nextIndex];
        return null;
    }

    public void ResetAllSteps()
    {
        foreach (var step in steps)
        {
            step.isCompleted = false;
        }
    }

    private void OnValidate()
    {
        if (steps == null) return;
        for (int i = 0; i < steps.Length; i++)
        {
            steps[i].stepId = i;
        }
    }
}
