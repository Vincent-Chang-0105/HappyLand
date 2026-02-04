using UnityEngine;

public class ChickenDragBehavior : MonoBehaviour
{
    private bool isDragging = false;
    private Vector3 dragOffset;
    private Camera mainCamera;

    public bool IsDragging => isDragging;

    // Events
    public System.Action OnDragStarted;
    public System.Action OnDragEnded;

    // Function to check if dragging is allowed
    public System.Func<bool> CanDrag;

    // Called before drag starts - return false to cancel the drag (e.g., payment failed)
    public System.Func<bool> OnBeforeDragStart;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    public void StartDrag()
    {
        if (CanDrag != null && !CanDrag()) return;

        isDragging = true;
        OnDragStarted?.Invoke();

        // Tutorial event
        TutorialEvents.ChickenPickedUp();
    }

    public void DragToPosition(Vector3 worldPosition)
    {
        if (!isDragging) return;
        transform.position = worldPosition + dragOffset;
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

        // Call before drag callback (e.g., for payment processing)
        if (OnBeforeDragStart != null && !OnBeforeDragStart())
        {
            return; // Callback returned false, cancel drag
        }

        StartDrag();
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
        transform.position = mouseWorldPos + dragOffset;
    }
}
