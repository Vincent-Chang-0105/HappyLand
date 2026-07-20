using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Represents a plated dish containing multiple fried chickens.
/// Can be dragged to customers for serving using Unity's event system.
/// </summary>
public class PlatedDish : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Dish Contents")]
    [SerializeField] private DishType dishType = DishType.Unknown;
    [SerializeField] private List<GameObject> containedChickens = new List<GameObject>();
    [SerializeField] private int requiredChickenCount = 3;

    [Header("Drag Settings")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private float dragSmoothness = 0.3f;

    [Header("Customer Detection")]
    [SerializeField] private float customerDetectionRadius = 2f;
    [SerializeField] private LayerMask customerLayer;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 originalPosition;
    private Camera mainCamera;
    private bool isDragging = false;

    // Dish information
    public DishType Dish => dishType;
    public string DishName => dishType.ToString();
    public int ChickenCount => containedChickens.Count;
    public bool IsComplete => containedChickens.Count >= requiredChickenCount;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        mainCamera = Camera.main;

        // Get or add canvas group for drag transparency
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Find canvas if not assigned
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        originalPosition = rectTransform.anchoredPosition;
    }

    /// <summary>
    /// Add a chicken to this plated dish
    /// </summary>
    public void AddChicken(GameObject chicken)
    {
        if (!containedChickens.Contains(chicken))
        {
            containedChickens.Add(chicken);
        }
    }

    /// <summary>
    /// Check if this dish contains all required ingredients and they're properly cooked
    /// </summary>
    public bool ValidateDish()
    {
        if (containedChickens.Count < requiredChickenCount)
        {
            Debug.LogWarning($"Plate incomplete: {containedChickens.Count}/{requiredChickenCount} ingredients");
            return false;
        }

        // Verify all ingredients based on dish type
        foreach (GameObject chicken in containedChickens)
        {
            if (chicken == null) continue;

            // Check what type of cooked ingredient this is
            IFryable fryable = chicken.GetComponent<IFryable>();
            ISinigangable sinigang = chicken.GetComponent<ISinigangable>();

            switch (dishType)
            {
                case DishType.Sinigang:
                    if (sinigang == null || !sinigang.IsSiniganged())
                    {
                        Debug.LogWarning($"Invalid sinigang chicken in plate: {chicken.name}");
                        return false;
                    }
                    break;
                case DishType.FriedChicken:
                    if (fryable == null || !fryable.IsFried())
                    {
                        Debug.LogWarning($"Invalid fried chicken in plate: {chicken.name}");
                        return false;
                    }
                    break;
                case DishType.Mechado:
                    IMechadoable mechadoable = chicken.GetComponent<IMechadoable>();
                    if (mechadoable == null || !mechadoable.IsMechado())
                    {
                        Debug.LogWarning($"Chicken in Mechado plate is not mechado-cooked: {chicken.name}");
                        return false;
                    }
                    break;
                case DishType.Adobo:
                    IAdoboable adoboable = chicken.GetComponent<IAdoboable>();
                    if (adoboable == null || !adoboable.IsAdobo())
                    {
                        Debug.LogWarning($"Chicken in Adobo plate is not adobo-cooked: {chicken.name}");
                        return false;
                    }
                    break;
                case DishType.NoodleChicken:
                    INoodleable noodleable = chicken.GetComponent<INoodleable>();
                    if (noodleable == null) continue;
                    if (!noodleable.IsNoodled())
                    {
                        Debug.LogWarning($"Chicken in NoodleChicken plate is not noodled: {chicken.name}");
                        return false;
                    }
                    break;
                default:
                    INoodleable nl = chicken.GetComponent<INoodleable>();
                    bool isCooked = (fryable != null && fryable.IsFried()) ||
                                    (sinigang != null && sinigang.IsSiniganged()) ||
                                    (nl != null && nl.IsNoodled());
                    if (!isCooked)
                    {
                        Debug.LogWarning($"Plate contains uncooked chicken: {chicken.name}");
                        return false;
                    }
                    break;
            }
        }

        return true;
    }

    #region Drag Handlers

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        originalPosition = rectTransform.anchoredPosition;

        // Make dish semi-transparent while dragging
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false; // Allow clicking through
        }

        // Pop up slightly when picked up
        transform.DOKill();
        transform.DOScale(1.12f, 0.1f).SetEase(Ease.OutBack);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;

        rectTransform.anchoredPosition += eventData.delta / scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;

        // Restore opacity and scale
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
        transform.DOKill();
        transform.DOScale(1f, 0.15f).SetEase(Ease.OutBack);

        // Check if dropped near a customer
        Customer nearestCustomer = FindNearestCustomer();
        if (nearestCustomer != null)
        {
            ServeToCustomer(nearestCustomer);
        }
        else
        {
            // No customer nearby, return to original position
            ReturnToOriginalPosition();
        }
    }

    #endregion

    #region Customer Interaction

    private Customer FindNearestCustomer()
    {
        // Convert UI position to world position
        Vector3 screenPos = rectTransform.position;
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        worldPos.z = 0;


        // Find all customers in range (ignore layer mask if it's set to Nothing)
        Collider2D[] hits;
        if (customerLayer.value == 0)
        {
            // Layer mask not set, search all layers
            hits = Physics2D.OverlapCircleAll(worldPos, customerDetectionRadius);
        }
        else
        {
            hits = Physics2D.OverlapCircleAll(worldPos, customerDetectionRadius, customerLayer);
        }

        Customer nearestCustomer = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            Customer customer = hit.GetComponent<Customer>();
            if (customer != null)
            {
                if (customer.IsWaitingForOrder())
                {
                    float distance = Vector3.Distance(worldPos, hit.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestCustomer = customer;
                    }
                }
            }
        }

        if (nearestCustomer != null)
        {
            //Debug.Log($"Nearest customer: {nearestCustomer.name} at distance {nearestDistance}");
        }
        else
        {
            //Debug.Log("No customer found nearby");
        }

        return nearestCustomer;
    }

    private void ServeToCustomer(Customer customer)
    {
        // Validate the dish before serving
        if (!ValidateDish())
        {
            Debug.LogWarning("Cannot serve incomplete or invalid dish!");
            ReturnToOriginalPosition();
            return;
        }

        // Serve the dish to the customer
        bool orderAccepted = customer.ReceiveOrder(gameObject);

        if (orderAccepted)
        {
            // Pop and shrink away before destroying
            transform.DOKill();
            transform.DOPunchScale(Vector3.one * 0.25f, 0.2f, 4, 0.5f)
                .OnComplete(() => transform.DOScale(0f, 0.15f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => Destroy(gameObject)));
        }
        else
        {
            ReturnToOriginalPosition();
        }
    }

    private void ReturnToOriginalPosition()
    {
        transform.DOKill();
        transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
        rectTransform.DOAnchorPos(originalPosition, 0.35f).SetEase(Ease.OutBack);
    }

    #endregion

    #region Debug Visualization

    private void OnDrawGizmosSelected()
    {
        if (mainCamera == null) return;

        // Draw customer detection radius
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(transform.position);
        worldPos.z = 0;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(worldPos, customerDetectionRadius);
    }

    #endregion
}
