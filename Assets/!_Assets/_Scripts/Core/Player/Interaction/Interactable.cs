using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public enum InteractionType{
        Click,
        Hold
    }

    [Header("Interaction Settings")]
    public InteractionType interactionType;
    
    [Header("Base Settings")]
    [SerializeField] protected string objectName;
    [SerializeField] protected string objectDescription;
    
    protected HintTrigger hintTrigger;

    protected void  Awake()
    {
        hintTrigger = GetComponent<HintTrigger>();
    }
    protected void TriggerHints()
    {
        Debug.Log("Triggering Hint");
        hintTrigger?.OnInteract();
    }

    public virtual string GetDescription() => objectDescription;
    public virtual string GetName() => objectName;
    public abstract void Interact();
}
