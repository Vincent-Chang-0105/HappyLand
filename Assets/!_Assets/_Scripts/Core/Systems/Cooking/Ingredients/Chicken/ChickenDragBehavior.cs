using UnityEngine;
using UnityEngine.EventSystems;

public class ChickenDragBehavior : MonoBehaviour, IDragHandler
{
    [Header("Drag Settings")]
    [SerializeField] private float dragSmoothness = 0.01f;

    private bool isDragging = false;
    private Vector3 dragOffset;
    private Camera mainCamera;

    public bool IsDragging => isDragging;

    // Events
    public System.Action OnDragStarted;
    public System.Action OnDragEnded;

    // Function to check if dragging is allowed
    public System.Func<bool> CanDrag;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    public void StartDrag()
    {
        if (CanDrag != null && !CanDrag()) return;

        isDragging = true;
        OnDragStarted?.Invoke();
    }

    public void DragToPosition(Vector3 worldPosition)
    {
        if (!isDragging) return;

        Vector3 targetPosition = worldPosition + dragOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, 1f - Mathf.Pow(dragSmoothness, Time.deltaTime));
    }

    public void EndDrag()
    {
        if (!isDragging) return;

        isDragging = false;
        OnDragEnded?.Invoke();
    }

    private void OnMouseDown()
    {
        if (CanDrag != null && !CanDrag()) return;

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = transform.position.z;
        dragOffset = transform.position - mouseWorldPos;

        // Call IDraggable interface if available (for payment check)
        IDraggable draggable = GetComponent<IDraggable>();
        if (draggable != null)
        {
            draggable.OnDragStart();
        }
        else
        {
            StartDrag();
        }
    }

    private void OnMouseUp()
    {
        if (isDragging)
        {
            EndDrag();
        }
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = transform.position.z;

        Vector3 targetPosition = mouseWorldPos + dragOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, 1f - Mathf.Pow(dragSmoothness, Time.deltaTime));
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(eventData.position);
        mouseWorldPos.z = transform.position.z;

        DragToPosition(mouseWorldPos);
    }
}
