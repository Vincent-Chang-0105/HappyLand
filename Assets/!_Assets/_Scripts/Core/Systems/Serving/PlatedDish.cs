using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Represents a plated dish containing multiple fried chickens.
/// Can be dragged to customers for serving using Unity's event system.
/// </summary>
public class PlatedDish : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Dish Contents")]
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
    public string DishName => gameObject.name;
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
            Debug.Log($"Added chicken to plate. Count: {containedChickens.Count}/{requiredChickenCount}");
        }
    }

    /// <summary>
    /// Check if this dish contains all required chickens and they're properly fried
    /// </summary>
    public bool ValidateDish()
    {
        if (containedChickens.Count < requiredChickenCount)
        {
            Debug.LogWarning($"Plate incomplete: {containedChickens.Count}/{requiredChickenCount} chickens");
            return false;
        }

        // Verify all chickens are fried
        foreach (GameObject chicken in containedChickens)
        {
            if (chicken == null) continue;

            IFryable fryable = chicken.GetComponent<IFryable>();
            if (fryable == null || !fryable.IsFried())
            {
                Debug.LogWarning("Plate contains unfried chicken!");
                return false;
            }
        }

        return true;
    }

    #region Drag Handlers

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("Started dragging plated dish");
        isDragging = true;
        originalPosition = rectTransform.anchoredPosition;

        // Make dish semi-transparent while dragging
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false; // Allow clicking through
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        // Convert screen point to canvas position
        // Vector2 localPoint;
        // if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
        //     canvas.transform as RectTransform,
        //     eventData.position,
        //     canvas.worldCamera,
        //     out localPoint))
        // {
        //     // Smooth drag movement
        //     Vector2 targetPos = localPoint;
        //     rectTransform.anchoredPosition = Vector2.Lerp(
        //         rectTransform.anchoredPosition,
        //         targetPos,
        //         dragSmoothness
        //     );
        // }
        float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;

        rectTransform.anchoredPosition += eventData.delta / scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("Stopped dragging plated dish");
        isDragging = false;

        // Restore opacity
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        // Check if dropped near a customer
        Customer nearestCustomer = FindNearestCustomer();
        if (nearestCustomer != null)
        {
            Debug.Log($"Dish dropped near customer: {nearestCustomer.name}");
            ServeToCustomer(nearestCustomer);
        }
        else
        {
            // No customer nearby, return to original position
            Debug.Log("No customer nearby, returning to original position");
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

        // Find all customers in range
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, customerDetectionRadius, customerLayer);

        Customer nearestCustomer = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            Customer customer = hit.GetComponent<Customer>();
            if (customer != null)
            {
                Debug.Log($"Found customer: {customer.name}, IsWaiting: {customer.IsWaitingForOrder()}");
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
            Debug.Log($"Nearest customer: {nearestCustomer.name} at distance {nearestDistance}");
        }
        else
        {
            Debug.Log("No customer found nearby");
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
            Debug.Log("Customer accepted the dish!");
            // Destroy the plate after successful serving
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("Customer rejected the dish!");
            ReturnToOriginalPosition();
        }
    }

    private void ReturnToOriginalPosition()
    {
        rectTransform.anchoredPosition = originalPosition;
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
