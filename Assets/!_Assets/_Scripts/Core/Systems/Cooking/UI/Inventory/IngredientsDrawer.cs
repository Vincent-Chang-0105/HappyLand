using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class IngredientsDrawer : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Drawer Settings")]
    [SerializeField] private Image HandleImage; // Changed from Button to Image
    [SerializeField] private RectTransform DrawerPanel;
    [SerializeField] private float AnimationDuration = 0.5f;
    [SerializeField] private Vector2 OpenPosition;
    [SerializeField] private Vector2 ClosedPosition;

    [Header("Drag Settings")]
    [SerializeField] private float dragThreshold = 50f;
    [SerializeField] private bool allowDragAnywhere = false;
    [SerializeField] private float snapThreshold = 0.5f;

    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color dragColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    [Header("Scroll")]
    [SerializeField] private ScrollRect scrollRect;

    [Header("Inventory System")]
    [SerializeField] private InventorySlot[] inventorySlots;
    [SerializeField] private List<IngredientStack> startingIngredients = new List<IngredientStack>();

    [Header("Ingredient Unlocks (by Day)")]
    [Tooltip("Ingredients added to the drawer on a specific day. All unlocks up to the current day are combined.")]
    [SerializeField] private List<DayIngredientUnlock> ingredientUnlocks = new List<DayIngredientUnlock>();

    private bool isOpen = false;
    private bool isDragging = false;
    private bool wasOpenWhenDragStarted = false;
    private Vector2 dragStartPosition;
    private Vector2 drawerStartPosition;
    private RectTransform handleRectTransform;
    private Camera uiCamera;

    private void Start()
    {
        DrawerPanel.anchoredPosition = ClosedPosition;
        
        // Get handle RectTransform from Image
        if (HandleImage != null)
        {
            handleRectTransform = HandleImage.GetComponent<RectTransform>();
            
            // Ensure the Image can receive raycast events
            HandleImage.raycastTarget = true;
        }
        
        // Get UI camera
        Canvas canvas = GetComponentInParent<Canvas>();
        uiCamera = canvas.worldCamera ?? Camera.main;
        
        // Make sure this GameObject can receive events too (if allowDragAnywhere is true)
        Image thisImage = GetComponent<Image>();
        if (thisImage == null && allowDragAnywhere)
        {
            // Add transparent image for event detection
            thisImage = gameObject.AddComponent<Image>();
            thisImage.color = new Color(0, 0, 0, 0); // Fully transparent
            thisImage.raycastTarget = true;
        }
        
        if (scrollRect != null) scrollRect.enabled = false;

        if (DayManager.Instance != null)
        {
            DayManager.Instance.OnDayStarted += ApplyIngredientUnlocks;
            ApplyIngredientUnlocks(DayManager.Instance.CurrentDay);
        }
        else
        {
            InitializeInventory();
        }
    }

    private void OnDestroy()
    {
        if (DayManager.Instance != null)
            DayManager.Instance.OnDayStarted -= ApplyIngredientUnlocks;
    }

    private void ApplyIngredientUnlocks(int day)
    {
        // Clear all slots
        foreach (InventorySlot slot in inventorySlots)
            slot.ClearSlot();

        // Build the combined ingredient list for this day
        List<IngredientStack> toAdd = new List<IngredientStack>(startingIngredients);
        foreach (DayIngredientUnlock unlock in ingredientUnlocks)
        {
            if (unlock.unlockDay <= day)
            {
                foreach (IngredientStack stack in unlock.ingredients)
                {
                    if (stack.ingredient != null)
                        toAdd.Add(stack);
                }
            }
        }

        // Populate slots
        int slotIndex = 0;
        foreach (IngredientStack stack in toAdd)
        {
            if (slotIndex >= inventorySlots.Length) break;
            inventorySlots[slotIndex].SetIngredient(stack.ingredient, stack.quantity);
            slotIndex++;
        }

        // Force scroll to top after layout is calculated
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void InitializeInventory()
    {
        foreach (InventorySlot slot in inventorySlots)
        {
            slot.ClearSlot();
        }

        int slotIndex = 0;
        foreach (IngredientStack ingredientStack in startingIngredients)
        {
            if (slotIndex >= inventorySlots.Length) break;

            if (ingredientStack.ingredient != null)
            {
                inventorySlots[slotIndex].SetIngredient(ingredientStack.ingredient, ingredientStack.quantity);
                slotIndex++;
            }
        }

        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        bool canDrag = false;
        
        if (allowDragAnywhere)
        {
            // Check if we're clicking on an inventory slot - if so, don't start drawer drag
            if (IsClickingOnInventorySlot(eventData))
            {
                return;
            }
            canDrag = true;
        }
        else if (handleRectTransform != null)
        {
            // Check if we clicked on the handle image
            canDrag = RectTransformUtility.RectangleContainsScreenPoint(
                handleRectTransform, 
                eventData.position, 
                uiCamera
            );
        }

        if (canDrag)
        {
            isDragging = true;
            dragStartPosition = eventData.position;
            drawerStartPosition = DrawerPanel.anchoredPosition;
            wasOpenWhenDragStarted = isOpen;

            // Stop any ongoing tween
            DrawerPanel.DOKill();
            
            // Visual feedback
            if (HandleImage != null)
            {
                HandleImage.color = dragColor;
            }
        }
    }

    private bool IsClickingOnInventorySlot(PointerEventData eventData)
    {
        foreach (InventorySlot slot in inventorySlots)
        {
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            if (slotRect != null && RectTransformUtility.RectangleContainsScreenPoint(slotRect, eventData.position, uiCamera))
            {
                return true;
            }
        }
        return false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) 
        {
            return;
        }

        // Calculate drag delta in screen space
        Vector2 dragDelta = eventData.position - dragStartPosition;
        
        // Convert to local space based on canvas scale
        Canvas canvas = GetComponentInParent<Canvas>();
        float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
        
        // Apply drag delta to drawer position
        Vector2 newPosition = drawerStartPosition;
        newPosition.y += dragDelta.y / scaleFactor;
        
        // Clamp between open and closed positions
        newPosition = ClampDrawerPosition(newPosition);
        
        DrawerPanel.anchoredPosition = newPosition;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        isDragging = false;
        
        // Reset visual feedback
        if (HandleImage != null)
        {
            HandleImage.color = normalColor;
        }
        
        // If barely moved (tap), toggle the drawer
        float dragDistance = Vector2.Distance(eventData.position, dragStartPosition);
        bool shouldOpen;
        if (dragDistance < dragThreshold)
        {
            shouldOpen = !wasOpenWhenDragStarted;
        }
        else
        {
            shouldOpen = ShouldDrawerBeOpen(DrawerPanel.anchoredPosition);
        }

        // Animate to final position
        if (shouldOpen)
        {
            OpenDrawer();
        }
        else
        {
            CloseDrawer();
        }
    }

    private Vector2 ClampDrawerPosition(Vector2 position)
    {
        if (OpenPosition.y > ClosedPosition.y)
        {
            // Opening upward
            position.y = Mathf.Clamp(position.y, ClosedPosition.y, OpenPosition.y);
        }
        else
        {
            // Opening downward
            position.y = Mathf.Clamp(position.y, OpenPosition.y, ClosedPosition.y);
        }
        
        // Keep X position fixed
        position.x = ClosedPosition.x;
        
        return position;
    }

    private bool ShouldDrawerBeOpen(Vector2 currentPosition)
    {
        float totalDistance = Vector2.Distance(ClosedPosition, OpenPosition);
        float currentDistance = Vector2.Distance(ClosedPosition, currentPosition);
        float openProgress = currentDistance / totalDistance;
        
        return openProgress > snapThreshold;
    }

    public void OpenDrawer()
    {
        if (!isOpen)
        {
            isOpen = true;
            DrawerPanel.DOAnchorPos(OpenPosition, AnimationDuration).SetEase(Ease.OutQuart);
            if (scrollRect != null) scrollRect.enabled = true;
        }
    }

    public void CloseDrawer()
    {
        if (isOpen)
        {
            isOpen = false;
            DrawerPanel.DOAnchorPos(ClosedPosition, AnimationDuration).SetEase(Ease.OutQuart);
            if (scrollRect != null)
            {
                scrollRect.enabled = false;
                scrollRect.verticalNormalizedPosition = 1f; // scroll back to top
            }
        }
    }

    #region Public Methods
    public bool AddIngredient(Ingredient ingredient, int quantity = 1)
    {
        foreach (InventorySlot slot in inventorySlots)
        {
            if (slot.CanAcceptIngredient(ingredient, quantity))
            {
                if (slot.TryAddIngredient(ingredient, quantity))
                {
                    return true;
                }
            }
        }

        InventorySlot emptySlot = System.Array.Find(inventorySlots, slot => slot.IsEmpty());
        if (emptySlot != null)
        {
            emptySlot.SetIngredient(ingredient, quantity);
            return true;
        }

        Debug.LogWarning($"No space available for {quantity}x {ingredient.ingredientName}", this);
        return false;
    }

    public bool RemoveIngredient(Ingredient ingredient, int quantity = 1)
    {
        InventorySlot slotWithIngredient = System.Array.Find(inventorySlots,
            slot => !slot.IsEmpty() && slot.GetIngredient() == ingredient);

        if (slotWithIngredient != null)
        {
            return slotWithIngredient.TryRemoveIngredient(quantity);
        }

        return false;
    }

    public int GetIngredientCount(Ingredient ingredient)
    {
        int totalCount = 0;
        foreach (InventorySlot slot in inventorySlots)
        {
            if (!slot.IsEmpty() && slot.GetIngredient() == ingredient)
            {
                totalCount += slot.GetQuantity();
            }
        }
        return totalCount;
    }

    public bool IsOpen() { return isOpen; }
    public bool IsDragging() { return isDragging; }
    #endregion
}

[System.Serializable]
public class IngredientStack
{
    public Ingredient ingredient;
    public int quantity = 1;
}

[System.Serializable]
public class DayIngredientUnlock
{
    [Tooltip("Ingredients become available in the drawer starting from this day")]
    public int unlockDay = 1;
    public List<IngredientStack> ingredients = new List<IngredientStack>();
}