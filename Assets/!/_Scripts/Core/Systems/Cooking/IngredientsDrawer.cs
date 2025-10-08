using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.UI;

public class IngredientsDrawer : MonoBehaviour
{
    [Header("Drawer Settings")]
    [SerializeField] private Button HandleButton;
    [SerializeField] private RectTransform DrawerPanel;
    [SerializeField] private float AnimationDuration = 0.5f;
    [SerializeField] private Vector2 OpenPosition;
    [SerializeField] private Vector2 ClosedPosition;

    [Header("Inventory System")]
    [SerializeField] private InventorySlot[] inventorySlots;
    [SerializeField] private List<IngredientStack> startingIngredients = new List<IngredientStack>();

    private bool isOpen = false;

    private void Start()
    {
        HandleButton.onClick.AddListener(ToggleDrawer);
        DrawerPanel.anchoredPosition = ClosedPosition;
        InitializeInventory();
    }

    private void InitializeInventory()
    {
        // Clear all slots first
        foreach (InventorySlot slot in inventorySlots)
        {
            slot.ClearSlot();
        }

        // Add starting ingredients with quantities
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
    }

    private void ToggleDrawer()
    {
        isOpen = !isOpen;
        Vector2 targetPosition = isOpen ? OpenPosition : ClosedPosition;
        DrawerPanel.DOAnchorPos(targetPosition, AnimationDuration).SetEase(Ease.OutQuart);
    }

    #region Public Methods
    public bool AddIngredient(Ingredient ingredient, int quantity = 1)
    {
        // Try to add to existing stacks first
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

        // If couldn't stack, try to find empty slot
        InventorySlot emptySlot = System.Array.Find(inventorySlots, slot => slot.IsEmpty());
        if (emptySlot != null)
        {
            emptySlot.SetIngredient(ingredient, quantity);
            return true;
        }

        Debug.LogWarning($"No space available for {quantity}x {ingredient.ingredientName}");
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

    public void OpenDrawer()
    {
        if (!isOpen)
        {
            isOpen = true;
            DrawerPanel.DOAnchorPos(OpenPosition, AnimationDuration).SetEase(Ease.OutQuart);
        }
    }
    
    public void CloseDrawer()
    {
        if (isOpen)
        {
            isOpen = false;
            DrawerPanel.DOAnchorPos(ClosedPosition, AnimationDuration).SetEase(Ease.OutQuart);
        }
    }
    #endregion
}



[System.Serializable]
public class IngredientStack
{
    public Ingredient ingredient;
    public int quantity = 1;
}