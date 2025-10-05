using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using System.Collections;

public abstract class CookingAction : MonoBehaviour
{
    [Header("Action Settings")]
    public string actionName;
    public float completionThreshold = 1f;
    public bool isCompleted = false;
    
    [Header("Feedback")]
    public UnityEvent OnActionStart;
    public UnityEvent OnActionProgress;
    public UnityEvent OnActionComplete;
    public UnityEvent OnActionFail;
    
    protected float currentProgress = 0f;
    
    public abstract void StartAction();
    public abstract void UpdateAction();
    public abstract void CompleteAction();
    
    protected void UpdateProgress(float progress)
    {
        currentProgress = Mathf.Clamp01(progress);
        OnActionProgress?.Invoke();
        
        if (currentProgress >= completionThreshold && !isCompleted)
        {
            CompleteAction();
        }
    }
}