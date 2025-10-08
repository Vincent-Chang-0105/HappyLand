using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class InventorySlot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image ingredientIcon;
    [SerializeField] private RawImage backgroundImage;
    [SerializeField] private Button slotButton;
    [SerializeField] private TextMeshProUGUI quantityText; // Add this for quantity display
    
    private Ingredient currentIngredient;
    private int currentQuantity = 0;
    private bool isEmpty = true;
    private IngredientsDrawer ingredientsDrawer; // Reference to the IngredientsDrawer
    
    private void Start()
    {
        // Get components if not assigned
        if (slotButton == null) slotButton = GetComponent<Button>();
        if (ingredientIcon == null) ingredientIcon = GetComponentInChildren<Image>();
        if (backgroundImage == null) backgroundImage = GetComponent<RawImage>();
        if (quantityText == null) quantityText = GetComponentInChildren<TextMeshProUGUI>();

        // Find the IngredientsDrawer in the scene
        ingredientsDrawer = GetComponentInParent<IngredientsDrawer>();

        slotButton.onClick.AddListener(OnSlotClick);
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
            // Animate click feedback
            transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);

            // Debug log the ingredient description with quantity
            Debug.Log($"{currentIngredient.ingredientName} x{currentQuantity}: {currentIngredient.description}");

            // Optional: Use one ingredient when clicked
            TryRemoveIngredient(1);
        }
        
        // Close the drawer after clicking
            if (ingredientsDrawer != null)
            {
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
}