using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;

public class DraggableIngredient : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Ingredient Data")]
    [SerializeField] private Ingredient ingredientData;
    [SerializeField] private int quantity = 1;

    [Header("Drag Settings")]
    [SerializeField] private float dragSmoothness = 0.001f;
    [SerializeField] private float dropRadius = 1f;

    [Header("Animation")]
    [SerializeField] private IngredientAnimController animController;
    [SerializeField] private bool useFrameAnimation = true;

    private bool isDragging = false;
    private Vector3 originalScale;
    private Camera mainCamera;
    private Image imageComponent;
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private bool hasBeenUsed = false;
    
    private void Start()
    {
        mainCamera = Camera.main;
        imageComponent = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

        // Store original values
        originalScale = transform.localScale;

        if (animController == null && useFrameAnimation)
        {
            animController = gameObject.AddComponent<IngredientAnimController>();
        }
    }

    
    public void Initialize(Ingredient ingredient, int qty = 1)
    {
        ingredientData = ingredient;
        quantity = qty;

        // Initialize components if not already done (in case Initialize is called before Start)
        if (imageComponent == null)
        {
            imageComponent = GetComponent<Image>();
            rectTransform = GetComponent<RectTransform>();
            parentCanvas = GetComponentInParent<Canvas>();
            mainCamera = Camera.main;
            originalScale = transform.localScale;
        }

        if (imageComponent != null && ingredient != null)
        {
            imageComponent.sprite = ingredient.draggableIcon;
        }

    }
    
    
    private void TryUseIngredient()
    {
        if (hasBeenUsed || ingredientData == null) return;

        bool wasUsed = false;

        // Convert UI position to world position for detecting Pan/Pot
        Vector3 worldPos = GetWorldPosition();

        // Check for pans and pots in range
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(worldPos, dropRadius);

        foreach (Collider2D collider in nearbyColliders)
        {
            // Try to use on Pan
            Pan pan = collider.GetComponent<Pan>();
            if (pan != null && CanUseOnPan())
            {
                wasUsed = UseOnPan(pan);
                if (wasUsed) break;
            }

            // Try to use on Pot
            Pot pot = collider.GetComponent<Pot>();
            if (pot != null && CanUseOnPot())
            {
                wasUsed = UseOnPot(pot);
                if (wasUsed) break;
            }
        }

        if (!wasUsed)
        {
            // Ingredient wasn't used, destroy it
            Debug.Log($"❌ {ingredientData.ingredientName} not used, destroying");
            DestroyIngredient();
        }
    }

    private Vector3 GetWorldPosition()
    {
        // Convert UI element position to world space
        Vector3 worldPos;

        if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ||
            parentCanvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            // Get camera for canvas
            Camera canvasCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;

            // Convert screen position to world position
            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(canvasCamera, rectTransform.position);
            worldPos = mainCamera.ScreenToWorldPoint(screenPos);
            worldPos.z = 0;
        }
        else
        {
            // World space canvas - use position directly
            worldPos = rectTransform.position;
        }

        return worldPos;
    }
    
    private bool CanUseOnPan()
    {
        switch (ingredientData.usageType)
        {
            case IngredientUsageType.Oil:
            case IngredientUsageType.Seasoning:
                return true;
            default:
                return false;
        }
    }
    
    private bool CanUseOnPot()
    {
        switch (ingredientData.usageType)
        {
            case IngredientUsageType.Seasoning:
            case IngredientUsageType.Liquid:
                return true;
            default:
                return false;
        }
    }
    
    private bool UseOnPan(Pan pan)
    {
        switch (ingredientData.usageType)
        {
            case IngredientUsageType.Oil:
                if (pan.CanAcceptOil())
                {
                    pan.AddOil();
                    hasBeenUsed = true;
                    Debug.Log($"🛢️ Added {ingredientData.ingredientName} to pan");
                    AnimateUse();
                    return true;
                }
                break;
                
            case IngredientUsageType.Seasoning:
                if (pan.CanAcceptSeasoning())
                {
                    pan.AddSeasoning(ingredientData);
                    hasBeenUsed = true;
                    Debug.Log($"🧂 Added {ingredientData.ingredientName} to pan");
                    AnimateUse();
                    return true;
                }
                break;
        }
        
        return false;
    }
    
    private bool UseOnPot(Pot pot)
    {
        switch (ingredientData.usageType)
        {
            case IngredientUsageType.Seasoning:
                if (pot.CanAcceptSeasoning())
                {
                    pot.AddSeasoning(ingredientData);
                    hasBeenUsed = true;
                    Debug.Log($"🧂 Added {ingredientData.ingredientName} to pot");
                    AnimateUse();
                    return true;
                }
                break;

            case IngredientUsageType.Liquid:
                if (pot.CanAcceptLiquid())
                {
                    pot.AddLiquid(ingredientData);
                    hasBeenUsed = true;
                    Debug.Log($"💧 Added {ingredientData.ingredientName} to pot");
                    AnimateUse();
                    return true;
                }
                break;
        }

        return false;
    }

    private void AnimateUse()
    {
        // Play the sprite animation
        animController.PlayAnimation(ingredientData.animationFrames, false, () =>
        {
            // After animation completes, fade out and destroy
            FadeOutAndDestroy();
        });
    }

    private void FadeOutAndDestroy()
    {
        // Fade out after sprite animation
        if (imageComponent != null)
        {
            imageComponent.DOFade(0f, 0.2f);
        }

        transform.DOScale(Vector3.zero, 0.2f).OnComplete(() =>
        {
            Destroy(gameObject);
        });
    }
    
    private void DestroyIngredient()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// Manually initiates the drag state without requiring pointer events.
    /// Call this immediately after instantiation to allow instant dragging.
    /// </summary>
    public void BeginDragImmediate()
    {
        if (hasBeenUsed) return;

        isDragging = true;

        // Bring to front in UI hierarchy
        transform.SetAsLastSibling();

        // Scale up slightly for visual feedback
        transform.localScale = originalScale * 1.1f;
    }

#region Drag Interface 
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (hasBeenUsed) return;

        Debug.Log($"🖱️ Began dragging {ingredientData.ingredientName}");
        
        isDragging = true;

        // Bring to front in UI hierarchy
        transform.SetAsLastSibling();

        // Scale up slightly
        transform.localScale = originalScale * 1.1f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (hasBeenUsed || !isDragging) return;

        Debug.Log($"Dragging {ingredientData.ingredientName}");
        // Move by the delta (how much the pointer moved since last frame)
        // Scale the delta by canvas scale factor for proper movement
        Canvas canvas = parentCanvas;
        float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;

        rectTransform.anchoredPosition += eventData.delta / scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (hasBeenUsed) return;

        isDragging = false;

        // Reset visual state
        transform.localScale = originalScale;

        // Try to use the ingredient on nearby cooking equipment
        TryUseIngredient();
    }
#endregion
}
