using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IPointerDownHandler
{
    [Header("UI References")]
    [SerializeField] private Image ingredientIcon;
    [SerializeField] private RawImage backgroundImage;
    [SerializeField] private TextMeshProUGUI quantityText;

    [Header("Drag Settings")]
    [SerializeField] private GameObject draggableIngredientPrefab;
    
    private Ingredient currentIngredient;
    private int currentQuantity = 0;
    private bool isEmpty = true;
    private IngredientsDrawer ingredientsDrawer; // Reference to the IngredientsDrawer
    private Canvas parentCanvas;
    
    private void Start()
    {
        // Get components if not assigned
        if (ingredientIcon == null) ingredientIcon = GetComponentInChildren<Image>();
        if (backgroundImage == null) backgroundImage = GetComponent<RawImage>();
        if (quantityText == null) quantityText = GetComponentInChildren<TextMeshProUGUI>();

        //Get parentCanvas for spawning draggable ingredients
        parentCanvas = GetComponentInParent<Canvas>();

        // Find the IngredientsDrawer in the scene
        ingredientsDrawer = GetComponentInParent<IngredientsDrawer>();

        UpdateSlotVisual();
    }
    
    public void SetIngredient(Ingredient ingredient, int quantity = 1)
    {
        if (ingredient == null)
        {
            ClearSlot();
            return;
        }
        
        currentIngredient = ingredient;
        currentQuantity = Mathf.Clamp(quantity, 1, ingredient.maxStackSize);
        isEmpty = false;
        UpdateSlotVisual();
    }
    
    public bool TryAddIngredient(Ingredient ingredient, int quantityToAdd = 1)
    {
        // If slot is empty, add the ingredient
        if (isEmpty)
        {
            SetIngredient(ingredient, quantityToAdd);
            return true;
        }
        
        // If same ingredient and stackable, try to add to stack
        if (currentIngredient == ingredient && ingredient.isStackable)
        {
            int newQuantity = currentQuantity + quantityToAdd;
            
            if (newQuantity <= ingredient.maxStackSize)
            {
                currentQuantity = newQuantity;
                UpdateSlotVisual();
                return true;
            }
            else
            {
                // Can only add partial amount
                int canAdd = ingredient.maxStackSize - currentQuantity;
                if (canAdd > 0)
                {
                    currentQuantity = ingredient.maxStackSize;
                    UpdateSlotVisual();
                }
                return false; // Couldn't add all requested quantity
            }
        }
        
        return false; // Can't add different ingredient or non-stackable
    }
    
    public bool TryRemoveIngredient(int quantityToRemove = 1)
    {
        if (isEmpty || quantityToRemove <= 0)
            return false;
            
        if (quantityToRemove >= currentQuantity)
        {
            // Remove all
            ClearSlot();
            return true;
        }
        else
        {
            // Remove partial
            currentQuantity -= quantityToRemove;
            UpdateSlotVisual();
            return true;
        }
    }
    
    public Ingredient GetIngredient()
    {
        return currentIngredient;
    }
    
    public int GetQuantity()
    {
        return currentQuantity;
    }
    
    public bool IsEmpty()
    {
        return isEmpty || currentQuantity <= 0;
    }
    
    public bool CanAcceptIngredient(Ingredient ingredient, int quantity = 1)
    {
        if (isEmpty)
            return true;
            
        if (currentIngredient == ingredient && ingredient.isStackable)
        {
            return (currentQuantity + quantity) <= ingredient.maxStackSize;
        }
        
        return false;
    }

    private void OnSlotClick()
    {
        if (!isEmpty && currentIngredient != null)
        {
            // Check if player can afford this ingredient
            int ingredientCost = currentIngredient.cost;

            if (ingredientCost > 0)
            {
                if (MoneyManager.Instance == null)
                {
                    Debug.LogError("MoneyManager not found! Cannot process ingredient purchase.");
                    return;
                }

                if (!MoneyManager.Instance.CanAfford(ingredientCost))
                {
                    Debug.Log($"Cannot afford {currentIngredient.ingredientName}. Cost: {ingredientCost} PHP");
                    return;
                }

                // Deduct the cost
                if (!MoneyManager.Instance.TrySpendMoney(ingredientCost))
                {
                    Debug.LogWarning("Failed to spend money for ingredient.");
                    return;
                }
            }

            // Instantiate as UI element in the canvas
            GameObject dragObject = Instantiate(draggableIngredientPrefab, parentCanvas.transform);

            // Position at mouse
            RectTransform dragRect = dragObject.GetComponent<RectTransform>();
            if (dragRect != null)
            {
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas.GetComponent<RectTransform>(),
                    Input.mousePosition,
                    parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
                    out localPoint
                );
                dragRect.localPosition = localPoint;
            }

            // Initialize the draggable ingredient
            DraggableIngredient dragComponent = dragObject.GetComponent<DraggableIngredient>();
            if (dragComponent != null)
            {
                dragComponent.Initialize(currentIngredient, 1);
                // Create a fake pointer event and trigger drag immediately
                PointerEventData eventData = new PointerEventData(EventSystem.current)
                {
                    position = Input.mousePosition,
                    button = PointerEventData.InputButton.Left
                };
                
                ExecuteEvents.Execute(dragObject, eventData, ExecuteEvents.pointerDownHandler);
            }

            // Remove one from inventory
            TryRemoveIngredient(1);

            // Close drawer
            ingredientsDrawer.CloseDrawer();
        }
    }
    
    private void UpdateSlotVisual()
    {
        if (isEmpty || currentIngredient == null || currentQuantity <= 0)
        {
            // Empty slot
            if (ingredientIcon != null)
            {
                ingredientIcon.sprite = null;
                ingredientIcon.color = Color.clear;
            }
            
            if (quantityText != null)
            {
                quantityText.text = "";
                quantityText.gameObject.SetActive(false);
            }
        }
        else
        {
            // Filled slot
            if (ingredientIcon != null)
            {
                ingredientIcon.sprite = currentIngredient.ingredientIcon;
                ingredientIcon.color = Color.white;
            }
            
            // Update quantity text
            if (quantityText != null)
            {
                if (currentQuantity > 1)
                {
                    quantityText.text = currentQuantity.ToString();
                    quantityText.gameObject.SetActive(true);
                }
                else
                {
                    quantityText.gameObject.SetActive(false); // Hide "1" for single items
                }
            }
        }
    }

    public void ClearSlot()
    {
        currentIngredient = null;
        currentQuantity = 0;
        isEmpty = true;
        UpdateSlotVisual();
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (isEmpty || currentIngredient == null) return;

        OnSlotClick();
    }
}