using UnityEngine;
using UnityEngine.EventSystems;

public class Chicken : MonoBehaviour, IDraggable, IWashable, IDragHandler
{
    [Header("Chicken Settings")]
    [SerializeField] private Sprite washedVersionSprite;
    [SerializeField] private float washDuration = 2f;
    [SerializeField] private ParticleSystem washEffect;
    
    [Header("Drag Settings")]
    [SerializeField] private float dragSmoothness = 0.01f;
    [SerializeField] private LayerMask faucetLayer = -1;
    
    private bool isDragging = false;
    private bool isWashed = false;
    private bool isBeingWashed = false;
    private Vector3 dragOffset;
    private Camera mainCamera;
    private Collider2D col2D;
    private SpriteRenderer spriteRenderer;
    private Vector3 originalPosition;
    
    // Washing state
    private float washTimer = 0f;
    private Faucet currentFaucet = null;
    
    private void Start()
    {
        mainCamera = Camera.main;
        col2D = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalPosition = transform.position;
        
        if (col2D == null)
        {
            col2D = gameObject.AddComponent<CircleCollider2D>();
        }
    }
    
    private void Update()
    {
        if (isBeingWashed)
        {
            UpdateWashing();
        }
    }
    
    #region IDraggable Implementation
    public void OnDragStart()
    {
        isDragging = true;
        Debug.Log($"Started dragging chicken: {gameObject.name}");
    }
    
    public void OnDrag(Vector3 worldPosition)
    {
        if (!isDragging) return;
        
        Vector3 targetPosition = worldPosition + dragOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, 1f - Mathf.Pow(dragSmoothness, Time.deltaTime));
        
        // Check if over faucet
        CheckFaucetProximity();
    }
    
    public void OnDragEnd()
    {
        isDragging = false;
        
        // Check if dropped on faucet for washing
        if (currentFaucet != null && currentFaucet.IsOn() && !isWashed)
        {
            StartWashing();
        }
        
        Debug.Log($"Stopped dragging chicken: {gameObject.name}");
    }
    
    public bool IsDraggable()
    {
        return !isBeingWashed;
    }
    #endregion
    
    #region IWashable Implementation
    public void StartWashing()
    {
        if (isWashed || isBeingWashed) return;
        
        isBeingWashed = true;
        washTimer = 0f;
        
        // Start wash effect
        if (washEffect != null)
        {
            washEffect.Play();
        }
        
        Debug.Log($"Started washing chicken: {gameObject.name}");
    }
    
    public void CompleteWashing()
    {
        if (!isBeingWashed) return;
        
        isBeingWashed = false;
        isWashed = true;
        
        // Stop wash effect
        if (washEffect != null)
        {
            washEffect.Stop();
        }
        
        // Spawn washed version
        if (washedVersionSprite != null)
        {
            spriteRenderer.sprite = washedVersionSprite;    
        }
        
        Debug.Log($"Completed washing chicken: {gameObject.name}");
    }
    
    public bool IsWashed()
    {
        return isWashed;
    }
    
    public bool CanBeWashed()
    {
        return !isWashed && !isBeingWashed;
    }
    #endregion
    
    #region Event System Handlers
    private void OnMouseDown()
    {
        if (!IsDraggable()) return;
        
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = transform.position.z;
        dragOffset = transform.position - mouseWorldPos;
        
        OnDragStart();
    }

    private void OnMouseUp()
    {
        if (isDragging)
        {
            OnDragEnd();
        }
    }
    
    private void OnMouseDrag()
    {
        if (!isDragging) return;
        
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = transform.position.z;
        
        Vector3 targetPosition = mouseWorldPos + dragOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, 1f - Mathf.Pow(dragSmoothness, Time.deltaTime));
        
        CheckFaucetProximity();
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(eventData.position);
        mouseWorldPos.z = transform.position.z;
        
        OnDrag(mouseWorldPos);
    }
    #endregion
    
    private void CheckFaucetProximity()
    {
        // Check for nearby faucets
        Collider2D[] faucets = Physics2D.OverlapCircleAll(transform.position, 1f, faucetLayer);
        
        if (faucets.Length > 0)
        {
            // Get the Faucet component instead of just the Transform
            Faucet faucetComponent = faucets[0].GetComponent<Faucet>();
            if (faucetComponent != null)
            {
                currentFaucet = faucetComponent;
                
                // Visual feedback based on faucet state
                if (faucetComponent.IsOn())
                {
                    Debug.Log("Near faucet - ready to wash!");
                }
                else
                {
                    Debug.Log("Near faucet - but it's turned off");
                }
            }
            else
            {
                currentFaucet = null;
            }
        }
        else
        {
            currentFaucet = null;
        }
    }
    
    private void UpdateWashing()
    {
        washTimer += Time.deltaTime;
        
        if (washTimer >= washDuration)
        {
            CompleteWashing();
        }
        
        // Visual feedback - could add washing animation here
        if (spriteRenderer != null)
        {
            float alpha = Mathf.Lerp(0.7f, 1f, Mathf.PingPong(washTimer * 3f, 1f));
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
    }
}