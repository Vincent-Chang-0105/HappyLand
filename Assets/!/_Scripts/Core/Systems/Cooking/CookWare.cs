using UnityEngine;
using DG.Tweening;

public class CookWare : MonoBehaviour
{
    [Header("Drag Settings")]
    [SerializeField] private bool isDraggable = true;
    [SerializeField] private float dragSmoothness = 0.1f;

    [Header("Visual Feedback")]
    [SerializeField] private float animationDuration = 0.2f;
    [SerializeField] private Color dragTint = new Color(1f, 1f, 1f, 0.8f);

    protected bool isDragging = false; // Changed to protected
    protected Vector3 dragOffset; // Changed to protected
    protected Vector3 originalPosition; // Changed to protected
    protected Camera mainCamera; // Changed to protected
    protected SpriteRenderer spriteRenderer; // Changed to protected
    protected Collider2D col2D; // Changed to protected
    protected Color originalColor; // Changed to protected

    protected virtual void Start() // Made virtual so Bowl can override
    {
        mainCamera = Camera.main;
        if(mainCamera == null)
        {
            Debug.LogError("Main Camera not found. Please ensure there is a camera tagged as 'MainCamera' in the scene.");
        }
        spriteRenderer = GetComponent<SpriteRenderer>();
        col2D = GetComponent<Collider2D>();

        originalPosition = transform.position;
        originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        if (col2D == null)
        {
            col2D = gameObject.AddComponent<BoxCollider2D>();
        }
    }

    protected virtual void Update() // Made virtual so Bowl can override
    {
        HandleInput();

        if (isDragging)
        {
            DragObject();
        }
    }

    protected virtual void HandleInput() // Made virtual
    {
        if (!isDraggable || mainCamera == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = (Vector2)mainCamera.ScreenToWorldPoint(Input.mousePosition);

            if (col2D.OverlapPoint(mousePos))
            {
                StartDragging(mousePos);
            }
        }
        else if (Input.GetMouseButtonUp(0) && isDragging)
        {
            StopDragging();
        }
    }

    protected virtual void StartDragging(Vector2 mousePosition) // Made virtual
    {
        isDragging = true;
        dragOffset = transform.position - (Vector3)mousePosition;

        if (spriteRenderer != null)
        {
            spriteRenderer.DOColor(dragTint, animationDuration);
        }

        Debug.Log($"Started dragging {gameObject.name}");
        OnStartDrag(); // Add event for subclasses
    }

    protected virtual void DragObject() // Made virtual
    {
        if (mainCamera == null) return;
        
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = transform.position.z;

        Vector3 targetPosition = mouseWorldPos + dragOffset;
        Vector3 previousPosition = transform.position;
        
        transform.position = Vector3.Lerp(transform.position, targetPosition, 1f - Mathf.Pow(dragSmoothness, Time.deltaTime));
        
        OnDrag(transform.position - previousPosition); // Add event for subclasses
    }

    protected virtual void StopDragging() // Made virtual
    {
        isDragging = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.DOColor(originalColor, animationDuration);
        }

        Debug.Log($"Stopped dragging {gameObject.name}");
        OnStopDrag(); // Add event for subclasses
    }

    // Virtual methods that subclasses can override for custom behavior
    protected virtual void OnStartDrag() { }
    protected virtual void OnDrag(Vector3 deltaPosition) { }
    protected virtual void OnStopDrag() { }

    public virtual void ResetToOriginalPosition()
    {
        if (isDragging)
        {
            StopDragging();
        }

        transform.DOMove(originalPosition, 0.5f).SetEase(Ease.OutQuart);
    }

    // Public getters
    public bool IsDragging() { return isDragging; }
    public bool IsDraggable() { return isDraggable; }
    public void SetDraggable(bool draggable) { isDraggable = draggable; }
}