using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.EventSystems;

public class Bowl : MonoBehaviour
{
    [Header("Bowl Settings")]
    [SerializeField] private float dropZoneRadius = 1f;
    [SerializeField] private Transform ingredientContainer; // Where ingredients go inside the bowl
    [SerializeField] private LayerMask ingredientLayer = -1;
    
    [Header("Animation Settings")]
    [SerializeField] private float moveToBowlDuration = 0.8f;
    [SerializeField] private Ease moveToBowlEase = Ease.OutQuart;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color canDropColor = Color.green;
    [SerializeField] private ParticleSystem dropEffect;
    
    private bool isDragging = false;
    private Vector3 dragOffset;
    private Camera mainCamera;
    private Collider2D col2D;
    private SpriteRenderer spriteRenderer;
    private Vector3 originalPosition;
    
    // Container management
    private List<GameObject> containedIngredients = new List<GameObject>();
    private bool isHighlighted = false;
    
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
        
        // Create ingredient container if not assigned
        if (ingredientContainer == null)
        {
            GameObject container = new GameObject("IngredientContainer");
            container.transform.parent = transform;
            container.transform.localPosition = Vector3.zero;
            ingredientContainer = container.transform;
        }
    }
    
    private void Update()
    {
        CheckForNearbyIngredients();
    }
    
    #region Drag Handling
    private void OnMouseDown()
    {
        // Don't pick up bowl if pointer is over a UI element (e.g. ingredient drawer)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = transform.position.z;
        dragOffset = transform.position - mouseWorldPos;

        isDragging = true;
    }

    private void OnMouseUp()
    {
        if (isDragging)
        {
            isDragging = false;
            SetHighlight(false);
            Invoke(nameof(ReturnToOriginalPosition), 0.1f);
        }
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = transform.position.z;
        transform.position = mouseWorldPos + dragOffset;
    }

    private void ReturnToOriginalPosition()
    {
        transform.DOMove(originalPosition, 0.2f).SetEase(Ease.OutQuad);
    }
    #endregion
    
    #region Ingredient Management
    public bool CanAcceptIngredient(GameObject ingredient)
    {
        if (ingredient.GetComponent<Chicken>() != null)
        {
            Chicken chicken = ingredient.GetComponent<Chicken>();
            
            // For fry bowls, accept boiled chicken
            if (gameObject.CompareTag("FryBowl"))
            {
                return chicken.IsWashed() && chicken.IsBoiled() && !chicken.IsFried();
            }
            
            // For boil bowls, accept washed chicken
            if (gameObject.CompareTag("BoilBowl"))
            {
                return chicken.IsWashed() && !chicken.IsBoiled();
            }
            
            // Default: accept washed chicken
            return chicken.IsWashed();
        }
        
        return false;
    }
    
    public void AddIngredient(GameObject ingredient)
    {
        if (!CanAcceptIngredient(ingredient)) return;
        
        if (!containedIngredients.Contains(ingredient))
        {
            containedIngredients.Add(ingredient);
            
            // Stop and lock dragging while inside the bowl
            ChickenDragBehavior dragBehavior = ingredient.GetComponent<ChickenDragBehavior>();
            if (dragBehavior != null)
            {
                dragBehavior.EndDrag();
                dragBehavior.enabled = false;
            }

            // Disable all colliders so the chicken can't be grabbed or detected by other bowls
            foreach (Collider2D col in ingredient.GetComponents<Collider2D>())
                col.enabled = false;
            
            // Start DOTween animation to move ingredient to bowl
            MoveIngredientToBowlWithDOTween(ingredient);

            // Tutorial event
            TutorialEvents.ChickenEnteredBowl(gameObject.tag);

            //Debug.Log($"🥣 Added {ingredient.name} to bowl!");
        }
    }
    
    private void MoveIngredientToBowlWithDOTween(GameObject ingredient)
    {
        // Calculate target position (world space)
        Vector3 targetWorldPosition = ingredientContainer.position;
        targetWorldPosition.y -= 0.2f; // Slightly lower than bowl center
        
        // Kill any existing tweens on this ingredient
        ingredient.transform.DOKill();
        
        // Animate the movement using DOTween
        ingredient.transform.DOMove(targetWorldPosition, moveToBowlDuration)
            .SetEase(moveToBowlEase)
            .OnComplete(() => {
                // Called when animation completes
                OnIngredientMoveComplete(ingredient);
            });
    }
    
    private void OnIngredientMoveComplete(GameObject ingredient)
    {
        // Parent it to the container after animation completes
        ingredient.transform.SetParent(ingredientContainer);
        
        // Convert to local position for proper parenting
        Vector3 localPos = Vector3.zero;
        localPos.y = -0.2f;
        ingredient.transform.localPosition = localPos;
        
        // Play drop effect when animation completes
        if (dropEffect != null)
        {
            dropEffect.Play();
        }
        
        //Debug.Log($"✅ Ingredient {ingredient.name} animation completed!");
    }
    
    public void RemoveIngredient(GameObject ingredient)
    {
        if (containedIngredients.Contains(ingredient))
        {
            containedIngredients.Remove(ingredient);

            // Kill any ongoing tweens
            ingredient.transform.DOKill();

            // Unparent the ingredient
            ingredient.transform.SetParent(null);

            // Re-enable dragging — ChickenBowlInteraction.ExitBowl/ForceExitBowl handles colliders
            ChickenDragBehavior dragBehavior = ingredient.GetComponent<ChickenDragBehavior>();
            if (dragBehavior != null) dragBehavior.enabled = true;
            
            //Debug.Log($"🥣 Removed {ingredient.name} from bowl!");
        }
    }
    
    public List<GameObject> GetContainedIngredients()
    {
        return new List<GameObject>(containedIngredients);
    }
    
    public bool IsEmpty()
    {
        return containedIngredients.Count == 0;
    }

    public void Reset()
    {
        DOTween.Kill(transform);
        foreach (GameObject ingredient in containedIngredients)
        {
            if (ingredient != null)
                Destroy(ingredient);
        }
        containedIngredients.Clear();
        transform.position = originalPosition;
        transform.rotation = Quaternion.identity;
        if (spriteRenderer != null)
            spriteRenderer.color = normalColor;
    }
    #endregion
    
    private void CheckForNearbyIngredients()
    {
        // Check for nearby ingredients that could be dropped
        Collider2D[] nearbyObjects = Physics2D.OverlapCircleAll(transform.position, dropZoneRadius, ingredientLayer);
        
        bool shouldHighlight = false;
        
        foreach (Collider2D obj in nearbyObjects)
        {
            if (obj.gameObject == gameObject) continue; // Skip self
            
            // Skip ingredients that are already in this bowl
            if (containedIngredients.Contains(obj.gameObject)) continue;
            
            // Check if it's a draggable ingredient (chicken)
            ChickenDragBehavior dragBehavior = obj.GetComponent<ChickenDragBehavior>();
            if (dragBehavior != null && dragBehavior.IsDragging && CanAcceptIngredient(obj.gameObject))
            {
                shouldHighlight = true;
                break;
            }
        }
        
        SetHighlight(shouldHighlight);
    }
    
    private void SetHighlight(bool highlight)
    {
        if (isHighlighted == highlight) return;
        
        isHighlighted = highlight;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = highlight ? canDropColor : normalColor;
        }
    }
    
    // Called when an ingredient is dropped nearby
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only add if not already contained
        if (!containedIngredients.Contains(other.gameObject) && CanAcceptIngredient(other.gameObject))
        {
            AddIngredient(other.gameObject);
        }
    }
    
    private void OnDestroy()
    {
        // Clean up any ongoing tweens when the bowl is destroyed
        foreach (GameObject ingredient in containedIngredients)
        {
            if (ingredient != null)
            {
                ingredient.transform.DOKill();
            }
        }
    }
    
    #region Gizmos for Debugging
    private void OnDrawGizmosSelected()
    {
        // Draw drop zone radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, dropZoneRadius);
        
        // Draw lines to contained ingredients
        Gizmos.color = Color.green;
        foreach (GameObject ingredient in containedIngredients)
        {
            if (ingredient != null)
            {
                Gizmos.DrawLine(transform.position, ingredient.transform.position);
            }
        }
    }
    #endregion

    public bool IsDragging()
    {
        return isDragging;
    }
}