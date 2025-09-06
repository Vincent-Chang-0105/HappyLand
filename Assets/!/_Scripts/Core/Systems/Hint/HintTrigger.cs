using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class HintTrigger : MonoBehaviour
{
    [Header("Hint Configuration")]
    [SerializeField] private List<int> hintIDs = new List<int>(); // Hint IDs that this trigger can complete (e.g., 4, 5, 6)
    
    [Header("Trigger Mode")]
    [SerializeField] private bool autoCompleteUpToHighest = false; // If true, completes up to highest ID regardless of current hint
    [SerializeField] private bool onlyTriggerMatching = true; // If true, only triggers if current hint matches one in the list

    [Header("Events")]
    [SerializeField] private UnityEvent onTriggerAction;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TriggerHints();
            onTriggerAction?.Invoke();
        }
    }

    public void OnInteract()
    {
        TriggerHints();
        onTriggerAction?.Invoke();
    }

    private void TriggerHints()
    {
        if (hintIDs == null || hintIDs.Count == 0)
        {
            Debug.LogWarning("No hint IDs assigned to HintTrigger!");
            return;
        }

        if (HintManager.Instance == null)
        {
            Debug.LogError("HintManager.Instance is null!");
            return;
        }

        // Mode 1: Auto-complete up to highest ID (regardless of current hint)
        if (autoCompleteUpToHighest)
        {
            int highestHintId = 0;
            foreach (int id in hintIDs)
            {
                if (id > highestHintId)
                {
                    highestHintId = id;
                }
            }

            HintEvents.CompleteHint(highestHintId);
            Debug.Log($"Auto-completed all hints up to {highestHintId} from trigger {gameObject.name}");
            return;
        }

        // Mode 2: Only trigger if current hint matches one in the list
        if (onlyTriggerMatching && HintManager.Instance.currentHint != null)
        {
            int currentHintId = HintManager.Instance.currentHint.hintId;
            
            if (hintIDs.Contains(currentHintId))
            {
                HintEvents.CompleteHint(currentHintId);
                Debug.Log($"Completed current hint {currentHintId} from trigger {gameObject.name}");
            }
            else
            {
                Debug.Log($"Current hint {currentHintId} not in trigger list {string.Join(", ", hintIDs)} - no action taken");
            }
        }
        else if (onlyTriggerMatching)
        {
            Debug.Log("No current hint displayed - no action taken");
        }
    }
}