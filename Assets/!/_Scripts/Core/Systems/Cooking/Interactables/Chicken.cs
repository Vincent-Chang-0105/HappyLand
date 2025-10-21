using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class Chicken : MonoBehaviour, IDraggable, IWashable, IDragHandler, IBoilable, IFryable
{
    [Header("Chicken Settings")]
    [SerializeField] private Sprite washedVersionSprite;
    [SerializeField] private Sprite boiledVersionSprite;
    [SerializeField] private Sprite friedVersionSprite;
    [SerializeField] private float washDuration = 2f;
    [SerializeField] private ParticleSystem washEffect;
    
    [Header("Drag Settings")]
    [SerializeField] private float dragSmoothness = 0.01f;
    [SerializeField] private LayerMask bowlLayer = -1;
    [SerializeField] private float bowlDropRadius = 1f;
    
    [Header("Auto Bowl Settings")]
    [SerializeField] private bool autoTeleportToBowl = true;
    [SerializeField] private float teleportDelay = 0.5f; // Delay before teleporting to bowl
    [SerializeField] private string boilBowlTag = "BoilBowl";
    [SerializeField] private string fryBowlTag = "FryBowl";
    
    private bool isDragging = false;
    private bool isWashed = false;
    private bool isBeingWashed = false;
    private bool isInBowl = false;
    private bool isBoiled = false;
    private bool isBeingBoiled = false;
    private bool isFried = false;
    private bool isBeingFried = false;
    private Vector3 dragOffset;
    private Camera mainCamera;
    private Collider2D col2D;
    private SpriteRenderer spriteRenderer;
    
    // Washing state
    private float washTimer = 0f;
    
    // Bowl state
    private Bowl currentBowl = null;
    
    private void Start()
    {
        mainCamera = Camera.main;
        col2D = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (col2D == null)
        {
            col2D = gameObject.AddComponent<CircleCollider2D>();
        }
        
        // Make sure it has a trigger collider for bowl detection
        if (col2D.isTrigger == false)
        {
            CircleCollider2D triggerCollider = gameObject.AddComponent<CircleCollider2D>();
            triggerCollider.isTrigger = true;
            triggerCollider.radius = 0.6f;
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
        if (isInBowl) return;
        
        isDragging = true;
    }
    
    public void OnDrag(Vector3 worldPosition)
    {
        if (!isDragging || isInBowl) return;
        
        Vector3 targetPosition = worldPosition + dragOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, 1f - Mathf.Pow(dragSmoothness, Time.deltaTime));
    }
    
    public void OnDragEnd()
    {
        if (!isDragging) return;
        
        isDragging = false;
        
        // Only check for bowl drop if not already in bowl
        if (!isInBowl)
        {
            CheckForBowlDrop();
        }
    }
    
    public bool IsDraggable()
    {
        return !isBeingWashed && !isInBowl;
    }
    #endregion
    
    #region IWashable Implementation
    public void StartWashing()
    {
        if (isWashed || isBeingWashed || isInBowl) return;
        
        isBeingWashed = true;
        washTimer = 0f;
        
        if (washEffect != null)
        {
            washEffect.Play();
        }
    }
    
    public void StopWashing()
    {
        if (!isBeingWashed) return;
        
        isBeingWashed = false;
        washTimer = 0f;
        
        if (washEffect != null)
        {
            washEffect.Stop();
        }
        
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;
        }
    }
    
    public void CompleteWashing()
    {
        if (!isBeingWashed) return;
        
        isBeingWashed = false;
        isWashed = true;
        
        if (washEffect != null)
        {
            washEffect.Stop();
        }
        
        if (washedVersionSprite != null)
        {
            spriteRenderer.sprite = washedVersionSprite;    
        }
        
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;
        }

        if (autoTeleportToBowl)
        {
            Bowl boilBowl = FindBowl(boilBowlTag);
            if (boilBowl != null && boilBowl.CanAcceptIngredient(gameObject))
            {
                EnterBowl(boilBowl);
            }
        }
    }
    
    public bool IsWashed()
    {
        return isWashed;
    }
    
    public bool CanBeWashed()
    {
        return !isWashed && !isBeingWashed && !isInBowl;
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
        if (!isDragging || isInBowl) return;
        
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = transform.position.z;
        
        Vector3 targetPosition = mouseWorldPos + dragOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, 1f - Mathf.Pow(dragSmoothness, Time.deltaTime));
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || isInBowl) return;
        
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(eventData.position);
        mouseWorldPos.z = transform.position.z;
        
        OnDrag(mouseWorldPos);
    }
    #endregion
    
    #region Bowl Interaction
    private void TeleportToNearestBowl()
    {
        // Don't teleport if already in bowl or being dragged
        if (isInBowl || isDragging) return;
        
        // Find all bowls in the scene
        Bowl[] allBowls = FindObjectsOfType<Bowl>();
        
        Bowl nearestBowl = null;
        float nearestDistance = float.MaxValue;
        
        foreach (Bowl bowl in allBowls)
        {
            if (bowl.CanAcceptIngredient(gameObject))
            {
                float distance = Vector3.Distance(transform.position, bowl.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestBowl = bowl;
                }
            }
        }
        
        if (nearestBowl != null)
        {
            Debug.Log($"🏃‍♂️ Auto-teleporting washed chicken to bowl: {nearestBowl.name}");
            EnterBowl(nearestBowl);
        }
        else
        {
            Debug.Log("⚠️ No available bowl found for auto-teleport");
        }
    }
    
    private void CheckForBowlDrop()
    {
        if (isInBowl || !isWashed) return;
        
        Collider2D[] bowls = Physics2D.OverlapCircleAll(transform.position, bowlDropRadius, bowlLayer);
        
        Bowl closestBowl = null;
        float closestDistance = float.MaxValue;
        
        foreach (Collider2D bowlCollider in bowls)
        {
            Bowl bowlComponent = bowlCollider.GetComponent<Bowl>();
            if (bowlComponent != null && bowlComponent.CanAcceptIngredient(gameObject))
            {
                float distance = Vector3.Distance(transform.position, bowlCollider.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestBowl = bowlComponent;
                }
            }
        }
        
        if (closestBowl != null)
        {
            EnterBowl(closestBowl);
        }
    }
    
    public void EnterBowl(Bowl bowl)
    {
        if (isInBowl || !isWashed) return;
        
        // Cancel any pending teleport
        CancelInvoke(nameof(TeleportToNearestBowl));
        
        // Stop dragging immediately when entering bowl
        isDragging = false;
        
        currentBowl = bowl;
        isInBowl = true;
        
        bowl.AddIngredient(gameObject);
        
        Debug.Log($"🥣 Chicken entered bowl: {bowl.name}");
    }
    
    public void ExitBowl()
    {
        if (!isInBowl || currentBowl == null) return;
        
        currentBowl.RemoveIngredient(gameObject);
        currentBowl = null;
        isInBowl = false;
        
        Debug.Log($"🥣 Chicken exited bowl");
    }
    
    public bool IsInBowl()
    {
        return isInBowl;
    }
    #endregion
    
    private void UpdateWashing()
    {    
        washTimer += Time.deltaTime;
        
        if (washTimer >= washDuration)
        {
            CompleteWashing();
            return;
        }
        
        // Visual feedback - washing animation
        if (spriteRenderer != null)
        {
            float alpha = Mathf.Lerp(0.7f, 1f, Mathf.PingPong(washTimer * 3f, 1f));
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
    }
    
    #region IBoilable Implementation
    public void StartBoiling()
    {
        if (isBoiled || isBeingBoiled || !isWashed) return;
        
        isBeingBoiled = true;
    }
    
    public void StopBoiling()
    {
        if (!isBeingBoiled) return;
        
        isBeingBoiled = false;
    }
    
    public void CompleteBoiling()
    {
        if (!isBeingBoiled) return;
        
        isBeingBoiled = false;
        isBoiled = true;

        if (boiledVersionSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = boiledVersionSprite;
        }
        
        if (autoTeleportToBowl)
        {
            Debug.Log("🍲 Auto-teleporting boiled chicken to fry bowl");
            
            // First, exit the current bowl state (from pot cooking)
            if (isInBowl && currentBowl != null)
            {
                ExitBowl();
            }
            
            // Unparent from pot's ingredient container
            if (transform.parent != null)
            {
                transform.SetParent(null);
                Debug.Log("🔓 Unparented chicken from pot container");
            }
            
            // Find and enter fry bowl
            Bowl fryBowl = FindBowl(fryBowlTag);
            if (fryBowl != null && fryBowl.CanAcceptIngredient(gameObject))
            {
                Debug.Log($"🍳 Found fry bowl: {fryBowl.name}, teleporting...");
                EnterBowl(fryBowl);
            }
            else
            {
                Debug.Log("⚠️ No fry bowl found or can't accept ingredient");
                // Try nearest bowl as fallback
                TeleportToNearestBowl();
            }
        }
    }
    
    public bool CanBeBoiled()
    {
        return isWashed && !isBoiled && !isBeingBoiled && isInBowl;
    }

    public bool IsBoiled()
    {
        return isBoiled;
    }
    #endregion
    
    #region IFryable Implementation
    public void StartFrying()
    {
        if (isFried || isBeingFried || !isWashed) return;
        
        isBeingFried = true;
        Debug.Log($"🍳 Started frying chicken: {gameObject.name}");
    }

    public void StopFrying()
    {
        if (!isBeingFried) return;
        
        isBeingFried = false;
        Debug.Log($"⏹️ Stopped frying chicken: {gameObject.name}");
    }

    public void CompleteFrying()
    {
        if (!isBeingFried) return;
        
        isBeingFried = false;
        isFried = true;
        
        // Change to fried version
        if (friedVersionSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = friedVersionSprite;
        }
        
        Debug.Log($"✅ Completed frying chicken: {gameObject.name}");
    }

    public bool CanBeFried()
    {
        bool canFry = isWashed && isBoiled && !isFried && !isBeingFried && isInBowl;
        Debug.Log($"🍳 CanBeFried check for {gameObject.name}: washed={isWashed}, boiled={isBoiled}, fried={isFried}, beingFried={isBeingFried}, inBowl={isInBowl}, result={canFry}");
        return canFry;
    }

    public bool IsFried()
    {
        return isFried;
    }
    #endregion
    
    #region Cleanup
    private void OnDestroy()
    {
        // Cancel any pending teleport when object is destroyed
        CancelInvoke(nameof(TeleportToNearestBowl));
    }
    #endregion

    #region Gizmos for Debugging
    private void OnDrawGizmosSelected()
    {
        // Draw bowl drop radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, bowlDropRadius);

        // Draw line to current bowl
        if (currentBowl != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentBowl.transform.position);
        }
    }
    #endregion
    
    private Bowl FindBowl(string bowlTag)
    {
        GameObject bowlObj = GameObject.FindGameObjectWithTag(bowlTag);
        Debug.Log(bowlObj != null ? $"🔍 Found bowl with tag '{bowlTag}': {bowlObj.name}" : $"⚠️ No bowl found with tag '{bowlTag}'");
        return bowlObj != null ? bowlObj.GetComponent<Bowl>() : null;
    }
}