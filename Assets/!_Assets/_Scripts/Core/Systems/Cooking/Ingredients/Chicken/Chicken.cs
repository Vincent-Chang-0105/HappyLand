using UnityEngine;
using DG.Tweening;

public class Chicken : MonoBehaviour, IWashable, IBoilable, IFryable, ISinigangable, INoodleable, IMechadoable, IAdoboable
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
        // Chicken stays where it was washed - player must manually drag to transfer zone
        // Just ensure it's draggable
        if (dragBehavior != null)
        {
            dragBehavior.enabled = true;
        }
        if (col2D != null)
        {
            col2D.enabled = true;
        }

        // No auto-teleport - player drags to transfer zone to send to Cook screen
    }

    private void HandleBoilComplete()
    {
        // Chicken stays in pot - player pours the pot to transfer boiled chicken out
        Debug.Log("Chicken boil complete - staying in pot until poured");
    }

    private void HandleFryComplete()
    {
        // Future: Handle serving or plating logic
    }

    private void HandleSinigangComplete()
    {
        Debug.Log("Chicken sinigang cooking complete!");
        // Chicken stays in pan - player drags to PlatingStation
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
        // Can't drag while being washed, in a bowl, or in a cooking station
        if (cookingState.IsBeingWashed || bowlInteraction.IsInBowl)
            return false;

        // Can't drag while being cooked in a pot or pan
        if (cookingState.IsBeingBoiled || cookingState.IsBeingFried || cookingState.IsBeingSiniganged || cookingState.IsBeingNoodled || cookingState.IsBeingMechado || cookingState.IsBeingAdobo)
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
        return cookingState.CanBeBoiled();
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
        return cookingState.CanBeFried();
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
        return cookingState.CanBeSiniganged();
    }

    public bool IsSiniganged()
    {
        return cookingState.IsSiniganged;
    }
    #endregion

    #region INoodleable Implementation
    public void StartNoodling()
    {
        cookingState.StartNoodling();
    }

    public void StopNoodling()
    {
        cookingState.StopNoodling();
    }

    public void CompleteNoodling()
    {
        cookingState.CompleteNoodling();
    }

    public bool CanBeNoodled()
    {
        return cookingState.CanBeNoodled();
    }

    public bool IsNoodled()
    {
        return cookingState.IsNoodled;
    }
    #endregion

    #region IMechadoable Implementation
    public void StartMechado()
    {
        cookingState.StartMechado();
    }

    public void StopMechado()
    {
        cookingState.StopMechado();
    }

    public void CompleteMechado()
    {
        cookingState.CompleteMechado();
    }

    public bool CanBeMechado()
    {
        return cookingState.CanBeMechado();
    }

    public bool IsMechado()
    {
        return cookingState.IsMechado;
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

    #region IAdoboable Implementation
    public void StartAdobo()
    {
        cookingState.StartAdobo();
    }

    public void StopAdobo()
    {
        cookingState.StopAdobo();
    }

    public void CompleteAdobo()
    {
        cookingState.CompleteAdobo();
    }

    public bool CanBeAdobo()
    {
        return cookingState.CanBeAdobo();
    }

    public bool IsAdobo()
    {
        return cookingState.IsAdobo;
    }
    #endregion

    #region DevMode Methods

    /// <summary>
    /// [DevMode] Instantly sets chicken to fully cooked fried state
    /// </summary>
    public void SetAsFried()
    {
        if (cookingState != null)
        {
            cookingState.DevSetAsFried();
        }
    }

    /// <summary>
    /// [DevMode] Instantly sets chicken to fully cooked sinigang state
    /// </summary>
    public void SetAsSinigang()
    {
        if (cookingState != null)
        {
            cookingState.DevSetAsSinigang();
        }
    }

    #endregion
}