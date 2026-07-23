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

    [Header("Drag Settings (Manual Mode)")]
    [SerializeField] private float minDragDistanceToUse = 60f;

    private bool isDragging = false;
    private bool manualDragMode = false;
    private Vector3 originalScale;
    private Camera mainCamera;
    private Image imageComponent;
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private bool hasBeenUsed = false;
    private Vector2 spawnLocalPosition;

    private void Start()
    {
        mainCamera = Camera.main;
        imageComponent = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        originalScale = transform.localScale;

        if (animController == null && useFrameAnimation)
            animController = gameObject.AddComponent<IngredientAnimController>();
    }

    public void Initialize(Ingredient ingredient, int qty = 1)
    {
        ingredientData = ingredient;
        quantity = qty;

        if (imageComponent == null)
        {
            imageComponent = GetComponent<Image>();
            rectTransform = GetComponent<RectTransform>();
            parentCanvas = GetComponentInParent<Canvas>();
            mainCamera = Camera.main;
            originalScale = transform.localScale;
        }

        if (imageComponent != null && ingredient != null)
            imageComponent.sprite = ingredient.bowlVersionSprite != null
                ? ingredient.bowlVersionSprite
                : ingredient.animationFrames[0];
    }

    private void TryUseIngredient()
    {
        if (hasBeenUsed || ingredientData == null) return;

        bool wasUsed = false;
        Vector3 worldPos = GetWorldPosition();
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(worldPos, dropRadius);

        foreach (Collider2D collider in nearbyColliders)
        {
            Pan pan = collider.GetComponent<Pan>();
            if (pan != null && CanUseOnPan())
            {
                wasUsed = UseOnPan(pan);
                if (wasUsed) break;
            }

            Pot pot = collider.GetComponent<Pot>();
            if (pot != null && CanUseOnPot())
            {
                wasUsed = UseOnPot(pot);
                if (wasUsed) break;
            }
        }

        if (!wasUsed) DestroyIngredient();
    }

    private Vector3 GetWorldPosition()
    {
        Vector3 worldPos;
        if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ||
            parentCanvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            Camera canvasCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null : parentCanvas.worldCamera;
            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(canvasCamera, rectTransform.position);
            worldPos = mainCamera.ScreenToWorldPoint(screenPos);
            worldPos.z = 0;
        }
        else
        {
            worldPos = rectTransform.position;
        }
        return worldPos;
    }

    private bool CanUseOnPan()
    {
        if (ingredientData == null) return false;
        if (ingredientData.ingredientRole == IngredientRole.NoodleIngredient) return true;

        switch (ingredientData.usageType)
        {
            case IngredientUsageType.Oil:
            case IngredientUsageType.Seasoning:
            case IngredientUsageType.Liquid:
                return true;
            default:
                return false;
        }
    }

    private bool CanUseOnPot()
    {
        if (ingredientData == null) return false;
        if (ingredientData.ingredientRole == IngredientRole.SinigangMix) return false;

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
        IngredientRole role = ingredientData.ingredientRole;

        if (role == IngredientRole.NoodleIngredient)
        {
            if (pan.CanAcceptNoodles())
            {
                pan.AddNoodles(gameObject);
                hasBeenUsed = true;
                return true;
            }
            return false;
        }

        if (pan.CanAcceptIngredient(role))
        {
            pan.AddIngredient(role, ingredientData);
            hasBeenUsed = true;
            AnimateUse();
            return true;
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
                    AnimateUse();
                    return true;
                }
                break;

            case IngredientUsageType.Liquid:
                if (pot.CanAcceptLiquid())
                {
                    pot.AddLiquid(ingredientData);
                    hasBeenUsed = true;
                    AnimateUse();
                    return true;
                }
                break;
        }
        return false;
    }

    private void AnimateUse()
    {
        animController.PlayAnimation(ingredientData.animationFrames, false, FadeOutAndDestroy);
    }

    private void FadeOutAndDestroy()
    {
        if (imageComponent != null)
            imageComponent.DOFade(0f, 0.2f);
        transform.DOScale(Vector3.zero, 0.2f).OnComplete(() => Destroy(gameObject));
    }

    private void DestroyIngredient() => Destroy(gameObject);

    public void BeginDragImmediate()
    {
        if (hasBeenUsed) return;
        isDragging = true;
        manualDragMode = true;
        spawnLocalPosition = rectTransform.localPosition;
        transform.SetAsLastSibling();
        transform.localScale = originalScale * 1.1f;
    }

    private void Update()
    {
        if (!manualDragMode || !isDragging) return;

        if (Input.GetMouseButton(0))
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.GetComponent<RectTransform>(),
                Input.mousePosition,
                parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
                out localPoint);
            rectTransform.localPosition = localPoint;
        }
        else
        {
            isDragging = false;
            manualDragMode = false;
            transform.localScale = originalScale;

            float draggedDistance = Vector2.Distance(rectTransform.localPosition, spawnLocalPosition);
            if (draggedDistance >= minDragDistanceToUse)
                TryUseIngredient();
            else
                DestroyIngredient();
        }
    }

    #region Drag Interface

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (hasBeenUsed) return;
        isDragging = true;
        transform.SetAsLastSibling();
        transform.localScale = originalScale * 1.1f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (hasBeenUsed || !isDragging) return;
        float scaleFactor = parentCanvas != null ? parentCanvas.scaleFactor : 1f;
        rectTransform.anchoredPosition += eventData.delta / scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (hasBeenUsed) return;
        isDragging = false;
        transform.localScale = originalScale;
        TryUseIngredient();
    }

    #endregion
}
