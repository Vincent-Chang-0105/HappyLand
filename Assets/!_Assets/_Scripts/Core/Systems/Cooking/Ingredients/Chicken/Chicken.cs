using UnityEngine;

public class Chicken : MonoBehaviour, IWashable, IBoilable, IFryable, ISinigangable
{
    [Header("Economy")]
    [SerializeField] private int chickenCost = 5;
    private bool hasPaidForChicken = false;

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
        dragBehavior.CanDrag = CanBeDragged;
        dragBehavior.OnBeforeDragStart = TryPayForChicken;
        dragBehavior.OnDragEnded = () => bowlInteraction.CheckForBowlDrop(cookingState.IsWashed);

        // Configure cooking state events
        cookingState.OnWashComplete = HandleWashComplete;
        cookingState.OnBoilComplete = HandleBoilComplete;
        cookingState.OnFryComplete = HandleFryComplete;
        cookingState.OnSinigangComplete += HandleSinigangComplete;
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
            // Check if we're in a pot with sinigang mix (don't teleport if so)
            Pot pot = FindNearbyPot();
            if (pot != null && pot.IsSinigangMode())
            {
                Debug.Log("Chicken boiled in sinigang mode - staying in pot");
                return; // Stay in pot for sinigang cooking
            }

            // Normal flow: teleport to fry bowl
            bowlInteraction.TeleportToBowlWithTag(cookingState.GetFryBowlTag());
        }
    }

    private Pot FindNearbyPot()
    {
        // Check if there's a pot nearby (within reasonable range)
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 5f);
        foreach (Collider2D col in colliders)
        {
            Pot pot = col.GetComponent<Pot>();
            if (pot != null)
            {
                return pot;
            }
        }
        return null;
    }

    private void HandleFryComplete()
    {
        // Future: Handle serving or plating logic
    }

    private void HandleSinigangComplete()
    {
        Debug.Log("Chicken sinigang cooking complete!");
        // NOTE: Do NOT teleport - chicken stays in pot for plating
    }
    #endregion

    #region Drag Logic
    private bool TryPayForChicken()
    {
        // If already paid or free, allow drag
        if (hasPaidForChicken || chickenCost <= 0)
            return true;

        if (MoneyManager.Instance == null)
        {
            Debug.LogError("MoneyManager not found! Cannot process chicken purchase.");
            return false;
        }

        if (!MoneyManager.Instance.TrySpendMoney(chickenCost))
        {
            Debug.Log($"Cannot afford chicken. Cost: {chickenCost} PHP");
            return false;
        }

        hasPaidForChicken = true;
        return true;
    }

    private bool CanBeDragged()
    {
        // Can't drag while being washed or in a bowl
        if (cookingState.IsBeingWashed || bowlInteraction.IsInBowl)
            return false;

        // If already paid, allow dragging
        if (hasPaidForChicken || chickenCost <= 0)
            return true;

        // Check if player can afford it
        if (MoneyManager.Instance == null)
            return false;

        return MoneyManager.Instance.CanAfford(chickenCost);
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

    #region ISinigangable Implementation
    public void StartSiniganging()
    {
        cookingState.StartSiniganging();
    }

    public void StopSiniganging()
    {
        cookingState.StopSiniganging();
    }

    public void CompleteSiniganging()
    {
        cookingState.CompleteSiniganging();
    }

    public bool CanBeSiniganged()
    {
        return cookingState.CanBeSiniganged() && bowlInteraction.IsInBowl;
    }

    public bool IsSiniganged()
    {
        return cookingState.IsSiniganged;
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