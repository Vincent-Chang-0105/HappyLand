using UnityEngine;

public class Chicken : MonoBehaviour, IDraggable, IWashable, IBoilable, IFryable
{
    private ChickenCookingState cookingState;
    private ChickenBowlInteraction bowlInteraction;
    private ChickenDragBehavior dragBehavior;
    private Collider2D col2D;

    private void Awake()
    {
        // Get required components (should be pre-attached to prefab)
        cookingState = GetComponent<ChickenCookingState>();
        bowlInteraction = GetComponent<ChickenBowlInteraction>();
        dragBehavior = GetComponent<ChickenDragBehavior>();
        col2D = GetComponent<Collider2D>();

        // Validate components
        if (cookingState == null)
            Debug.LogError($"ChickenCookingState component missing on {gameObject.name}! Please add it to the prefab.");
        if (bowlInteraction == null)
            Debug.LogError($"ChickenBowlInteraction component missing on {gameObject.name}! Please add it to the prefab.");
        if (dragBehavior == null)
            Debug.LogError($"ChickenDragBehavior component missing on {gameObject.name}! Please add it to the prefab.");

        // Setup colliders
        SetupColliders();

        // Wire up events
        SetupEvents();
    }

    private void SetupColliders()
    {
        if (col2D == null)
        {
            col2D = gameObject.AddComponent<CircleCollider2D>();
        }

        // Make sure it has a trigger collider for bowl detection
        if (col2D.isTrigger == false)
        {
            CircleCollider2D triggerCollider = gameObject.AddComponent<CircleCollider2D>();
            triggerCollider.isTrigger = true;
            triggerCollider.radius = 0.6f;
        }
    }

    private void SetupEvents()
    {
        // Configure drag behavior
        dragBehavior.CanDrag = () => !cookingState.IsBeingWashed && !bowlInteraction.IsInBowl;
        dragBehavior.OnDragEnded = () => bowlInteraction.CheckForBowlDrop(cookingState.IsWashed);

        // Configure cooking state events
        cookingState.OnWashComplete = HandleWashComplete;
        cookingState.OnBoilComplete = HandleBoilComplete;
        cookingState.OnFryComplete = HandleFryComplete;
    }
    
    #region Event Handlers
    private void HandleWashComplete()
    {
        if (cookingState.ShouldAutoTeleport())
        {
            Bowl boilBowl = bowlInteraction.FindBowlByTag(cookingState.GetBoilBowlTag());
            if (boilBowl != null && boilBowl.CanAcceptIngredient(gameObject))
            {
                bowlInteraction.EnterBowl(boilBowl);
            }
        }
    }

    private void HandleBoilComplete()
    {
        if (cookingState.ShouldAutoTeleport())
        {
            bowlInteraction.TeleportToBowlWithTag(cookingState.GetFryBowlTag());
        }
    }

    private void HandleFryComplete()
    {
        // Future: Handle serving or plating logic
    }
    #endregion

    #region IDraggable Implementation
    public void OnDragStart()
    {
        dragBehavior.StartDrag();
    }

    public void OnDrag(Vector3 worldPosition)
    {
        dragBehavior.DragToPosition(worldPosition);
    }

    public void OnDragEnd()
    {
        dragBehavior.EndDrag();
    }

    public bool IsDraggable()
    {
        return !cookingState.IsBeingWashed && !bowlInteraction.IsInBowl;
    }
    #endregion
    
    #region IWashable Implementation
    public void StartWashing()
    {
        if (bowlInteraction.IsInBowl) return;
        cookingState.StartWashing();
    }

    public void StopWashing()
    {
        cookingState.StopWashing();
    }

    public void CompleteWashing()
    {
        cookingState.CompleteWashing();
    }

    public bool IsWashed()
    {
        return cookingState.IsWashed;
    }

    public bool CanBeWashed()
    {
        return cookingState.CanBeWashed() && !bowlInteraction.IsInBowl;
    }
    #endregion
    
    #region IBoilable Implementation
    public void StartBoiling()
    {
        cookingState.StartBoiling();
    }

    public void StopBoiling()
    {
        cookingState.StopBoiling();
    }

    public void CompleteBoiling()
    {
        cookingState.CompleteBoiling();
    }

    public bool CanBeBoiled()
    {
        return cookingState.CanBeBoiled() && bowlInteraction.IsInBowl;
    }

    public bool IsBoiled()
    {
        return cookingState.IsBoiled;
    }
    #endregion
    
    #region IFryable Implementation
    public void StartFrying()
    {
        cookingState.StartFrying();
    }

    public void StopFrying()
    {
        cookingState.StopFrying();
    }

    public void CompleteFrying()
    {
        cookingState.CompleteFrying();
    }

    public bool CanBeFried()
    {
        return cookingState.CanBeFried() && bowlInteraction.IsInBowl;
    }

    public bool IsFried()
    {
        return cookingState.IsFried;
    }
    #endregion

    #region Public Bowl Interaction Methods
    public bool IsInBowl()
    {
        return bowlInteraction.IsInBowl;
    }

    public void EnterBowl(Bowl bowl)
    {
        if (!cookingState.IsWashed) return;
        bowlInteraction.EnterBowl(bowl);
    }

    public void ExitBowl()
    {
        bowlInteraction.ExitBowl();
    }
    #endregion
}