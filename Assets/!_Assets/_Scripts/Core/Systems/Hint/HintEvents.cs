using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HintEvents
{
    // Event for triggering hints
    public static event Action<int> OnTriggerHint;
    
    // Event for completing hints
    public static event Action<int> OnCompleteHint;
    
    // Event for progressing to next hint
    public static event Action OnShowNextHint;

    public static event Action<HintData> OnLoadlevels;
    
    // Generic game events that can trigger hints
    public static event Action<string> OnDoorInteracted;
    public static event Action<string> OnItemPickedUp;
    public static event Action<string> OnAreaEntered;
    public static event Action<string> OnPuzzleSolved;
    public static event Action<string> OnObjectInteracted;
    
    // Methods to trigger events
    public static void TriggerHint(int hintId)
    {
        OnTriggerHint?.Invoke(hintId);
    }
    
    public static void CompleteHint(int hintId)
    {
        OnCompleteHint?.Invoke(hintId);
    }
    
    public static void ShowNextHint()
    {
        OnShowNextHint?.Invoke();
    }

    public static void LoadLevels(HintData hintdata)
    {
        OnLoadlevels?.Invoke(hintdata);
    }
    
    // Game-specific event triggers
    public static void DoorInteracted(string doorId)
    {
        OnDoorInteracted?.Invoke(doorId);
    }
    
    public static void ItemPickedUp(string itemId)
    {
        OnItemPickedUp?.Invoke(itemId);
    }
    
    public static void AreaEntered(string areaId)
    {
        OnAreaEntered?.Invoke(areaId);
    }
    
    public static void PuzzleSolved(string puzzleId)
    {
        OnPuzzleSolved?.Invoke(puzzleId);
    }
    
    public static void ObjectInteracted(string objectId)
    {
        OnObjectInteracted?.Invoke(objectId);
    }
}
