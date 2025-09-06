using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Level Hints", menuName = "Hint System/Level Hints")]
public class HintData : ScriptableObject
{
    public string levelName;
    public Hint[] hints;

    [Header("Settings")]
    public bool autoStartFirstHint = true;

    public Hint GetHint(int hintId)
    {
        foreach (var hint in hints)
        {
            if (hint.hintId == hintId)
                return hint;
        }
        return null;
    }

    public Hint GetNextIncompleteHint()
    {
        Hint lowestIdIncompleteHint = null;
        int lowestId = int.MaxValue;

        foreach (var hint in hints)
        {
            if (!hint.isCompleted && hint.hintId < lowestId)
            {
                lowestId = hint.hintId;
                lowestIdIncompleteHint = hint;
            }
        }

        return lowestIdIncompleteHint;
    }

    public void ResetAllHints()
    {
        foreach (var hint in hints)
        {
            hint.isCompleted = false;
        }
    }

    private void OnDisable()
    {
        // Reset hints when the scriptable object is disabled
        // This prevents the completed state from being saved to the asset
        ResetAllHints();
    }
    
    public void CompleteHintsUpTo(int hintId)
    {
        foreach (var hint in hints)
        {
            if (hint.hintId <= hintId)
            {
                hint.isCompleted = true;
            }
        }
    }

    public List<Hint> GetHintsUpTo(int hintId)
    {
        List<Hint> hintsToComplete = new List<Hint>();
        foreach (var hint in hints)
        {
            if (hint.hintId <= hintId)
            {
                hintsToComplete.Add(hint);
            }
        }
        return hintsToComplete;
    }

}
