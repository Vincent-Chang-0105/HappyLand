using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class CookingManager : MonoBehaviour
{
    [Header("Cooking Steps")]
    [SerializeField] private CookingAction[] cookingSteps;
    
    [Header("UI")]
    [SerializeField] private GameObject instructionPanel;
    [SerializeField] private TMPro.TextMeshProUGUI instructionText;
    
    private int currentStepIndex = 0;
    private CookingAction currentStep;
    
    private void Start()
    {
        StartCookingProcess();
    }
    
    public void StartCookingProcess()
    {
        if (cookingSteps.Length > 0)
        {
            StartCurrentStep();
        }
    }
    
    private void StartCurrentStep()
    {
        if (currentStepIndex >= cookingSteps.Length)
        {
            CompleteCooking();
            return;
        }
        
        currentStep = cookingSteps[currentStepIndex];
        currentStep.OnActionComplete.AddListener(OnStepComplete);
        
        ShowInstruction(currentStep.actionName);
        currentStep.StartAction();
    }
    
    private void Update()
    {
        if (currentStep != null && !currentStep.isCompleted)
        {
            currentStep.UpdateAction();
        }
    }
    
    private void OnStepComplete()
    {
        currentStepIndex++;
        StartCoroutine(TransitionToNextStep());
    }
    
    private IEnumerator TransitionToNextStep()
    {
        yield return new WaitForSeconds(1f);
        StartCurrentStep();
    }
    
    private void ShowInstruction(string instruction)
    {
        instructionText.text = instruction;
        instructionPanel.SetActive(true);
    }
    
    private void CompleteCooking()
    {
        instructionText.text = "Adobo Complete! Well done!";
        // Show completion celebration
    }
}