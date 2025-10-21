using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

public class Pan : MonoBehaviour
{
    [Header("Pan Settings")]
    [SerializeField] private float fryDuration = 5f;
    [SerializeField] private float acceptRadius = 1.5f;
    [SerializeField] private LayerMask bowlLayer = -1;
    [SerializeField] private Transform ingredientContainer; // Where ingredients go inside the pot
    
    [Header("Animation Settings")]
    [SerializeField] private float ingredientMoveDuration = 0.6f;
    [SerializeField] private Ease ingredientMoveEase = Ease.OutBounce;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color canAcceptColor = Color.orange;
    [SerializeField] private ParticleSystem fryingEffect;
    [SerializeField] private ParticleSystem steamEffect;
    [SerializeField] private ParticleSystem ingredientDropEffect;
    
    [Header("UI")]
    [SerializeField] private GameObject fryingMeterUI;
    [SerializeField] private Image fryingProgressBar;
    [SerializeField] private Canvas worldCanvas; // For world space UI

    private bool isFrying = false;
    private bool isHighlighted = false;
    private float fryTimer = 0f;
    private SpriteRenderer spriteRenderer;
    
    // Frying state
    private List<GameObject> ingredientsInPan = new List<GameObject>();
    
    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Create ingredient container if not assigned
        if (ingredientContainer == null)
        {
            GameObject container = new GameObject("IngredientContainer");
            container.transform.parent = transform;
            container.transform.localPosition = Vector3.zero;
            ingredientContainer = container.transform;
        }
        
        // Setup UI
        SetupFryingMeterUI();
        
        // Hide UI initially
        if (fryingMeterUI != null)
        {
            fryingMeterUI.SetActive(false);
        }
    }
    
    private void Update()
    {
        CheckForNearbyBowls();

        if (isFrying)
        {
            UpdateFrying();
        }
    }
    
    private void SetupFryingMeterUI()
    {
        if (fryingMeterUI == null && worldCanvas != null)
        {
            // Create frying meter UI
            fryingMeterUI = new GameObject("FryingMeter");
            fryingMeterUI.transform.SetParent(worldCanvas.transform);
            
            // Add background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(fryingMeterUI.transform);
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.7f);
            
            // Add progress bar
            GameObject progressBarObj = new GameObject("ProgressBar");
            progressBarObj.transform.SetParent(fryingMeterUI.transform);
            fryingProgressBar = progressBarObj.AddComponent<Image>();
            fryingProgressBar.color = Color.red;
            fryingProgressBar.type = Image.Type.Filled;
            fryingProgressBar.fillMethod = Image.FillMethod.Horizontal;

            fryingMeterUI.SetActive(false);
        }
    }
    
    public bool CanAcceptBowl(Bowl bowl)
    {
        if (bowl == null || isFrying) return false;

        // Check if bowl has ingredients that can be fried
        List<GameObject> ingredients = bowl.GetContainedIngredients();
        foreach (GameObject ingredient in ingredients)
        {
            IFryable fryable = ingredient.GetComponent<IFryable>();
            if (fryable != null && fryable.CanBeFried())
            {
                return true;
            }
        }
        
        return false;
    }
    
    public void AcceptBowl(Bowl bowl)
    {
        if (!CanAcceptBowl(bowl)) return;
        
        // Get all fryable ingredients from the bowl
        List<GameObject> ingredients = bowl.GetContainedIngredients();
        List<GameObject> fryableIngredients = new List<GameObject>();
        
        foreach (GameObject ingredient in ingredients)
        {
            IFryable fryable = ingredient.GetComponent<IFryable>();
            if (fryable != null && fryable.CanBeFried())
            {
                fryableIngredients.Add(ingredient);
            }
        }
        
        if (fryableIngredients.Count > 0)
        {
            // Move ingredients from bowl to pan
            TransferIngredientsToPan(bowl, fryableIngredients);
        }

        Debug.Log($"🍲 Bowl brought to pan - transferring {fryableIngredients.Count} ingredients!");
    }

    private void TransferIngredientsToPan(Bowl bowl, List<GameObject> ingredients)
    {
        int ingredientCount = ingredients.Count;
        int transferredCount = 0;
        
        foreach (GameObject ingredient in ingredients)
        {
            // Remove ingredient from bowl
            bowl.RemoveIngredient(ingredient);
            // Add to pan's ingredient list
            ingredientsInPan.Add(ingredient);

            // Calculate target position in pan (spread them out a bit)
            Vector3 targetPosition = ingredientContainer.position;
            
            // Add some random offset for multiple ingredients
            if (ingredientCount > 1)
            {
                float angle = (transferredCount / (float)(ingredientCount - 1)) * 360f * Mathf.Deg2Rad;
                float radius = 0.3f;
                targetPosition.x += Mathf.Cos(angle) * radius;
                targetPosition.z += Mathf.Sin(angle) * radius;
            }

            // Animate ingredient moving to pan
            MoveIngredientToPan(ingredient, targetPosition, transferredCount * 0.1f); // Stagger animations
            transferredCount++;
        }
    }

    private void MoveIngredientToPan(GameObject ingredient, Vector3 targetPosition, float delay)
    {
        // Kill any existing tweens
        ingredient.transform.DOKill();

        // Animate movement to pan with a slight delay for staggering
        ingredient.transform.DOMove(targetPosition, ingredientMoveDuration)
            .SetDelay(delay)
            .SetEase(ingredientMoveEase)
            .OnComplete(() => {
                OnIngredientArrivedInPan(ingredient);
            });
    }

    private void OnIngredientArrivedInPan(GameObject ingredient)
    {
        // Parent ingredient to pan container
        ingredient.transform.SetParent(ingredientContainer);
        
        // Play drop effect
        if (ingredientDropEffect != null)
        {
            ingredientDropEffect.Play();
        }
        
        Debug.Log($"🍲 Ingredient {ingredient.name} arrived in pot!");
        
        // Check if all ingredients have arrived, then start boiling
        CheckIfReadyToStartFrying();
    }

    private void CheckIfReadyToStartFrying()
    {
        // Count how many ingredients are still moving
        int movingIngredients = 0;
        foreach (GameObject ingredient in ingredientsInPan)
        {
            if (ingredient != null && DOTween.IsTweening(ingredient.transform))
            {
                movingIngredients++;
            }
        }
        
        // If no ingredients are moving, start frying
        if (movingIngredients == 0 && ingredientsInPan.Count > 0 && !isFrying)
        {
            StartFrying();
        }
    }

    private void StartFrying()
    {
        if (isFrying || ingredientsInPan.Count == 0) return;

        isFrying = true;
        fryTimer = 0f;

        // Start frying each ingredient
        foreach (GameObject ingredient in ingredientsInPan)
        {
            if (ingredient != null)
            {
                IFryable fryable = ingredient.GetComponent<IFryable>();
                if (fryable != null)
                {
                    fryable.StartFrying();
                }
            }
        }
        
        // Start effects
        if (fryingEffect != null)
        {
            fryingEffect.Play();
        }
        
        if (steamEffect != null)
        {
            steamEffect.Play();
        }
        
        // Show UI
        if (fryingMeterUI != null)
        {
            fryingMeterUI.SetActive(true);
            // Position UI above pot
            fryingMeterUI.transform.position = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        }
        
        Debug.Log("🔥 Started frying ingredients in pan!");
    }
    
    private void StopFrying()
    {
        if (!isFrying) return;

        isFrying = false;
        fryTimer = 0f;

        // Stop frying each ingredient
        foreach (GameObject ingredient in ingredientsInPan)
        {
            if (ingredient != null)
            {
                IFryable fryable = ingredient.GetComponent<IFryable>();
                if (fryable != null)
                {
                    fryable.StopFrying();
                }
            }
        }
        
        // Stop effects
        if (fryingEffect != null)
        {
            fryingEffect.Stop();
        }
        
        if (steamEffect != null)
        {
            steamEffect.Stop();
        }
        
        // Hide UI
        if (fryingMeterUI != null)
        {
            fryingMeterUI.SetActive(false);
        }

        Debug.Log("⏹️ Stopped frying");
    }

    private void CompleteFrying()
    {
        if (!isFrying) return;

        // Complete frying for each ingredient
        foreach (GameObject ingredient in ingredientsInPan)
        {
            if (ingredient != null)
            {
                IFryable fryable = ingredient.GetComponent<IFryable>();
                if (fryable != null)
                {
                    fryable.CompleteFrying();
                }
            }
        }

        StopFrying();

        Debug.Log("✅ Frying completed! Ingredients are now cooked!");
    }

    private void UpdateFrying()
    {
        fryTimer += Time.deltaTime;

        // Update progress bar
        if (fryingProgressBar != null)
        {
            float progress = fryTimer / fryDuration;
            fryingProgressBar.fillAmount = progress;
            
            // Color transition from red to green
            fryingProgressBar.color = Color.Lerp(Color.red, Color.green, progress);
        }
        
        // Update UI position (follow pot)
        if (fryingMeterUI != null && fryingMeterUI.activeInHierarchy)
        {
            fryingMeterUI.transform.position = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        }
        
        // Complete when timer reaches duration
        if (fryTimer >= fryDuration)
        {
            CompleteFrying();
        }
    }
    
    private void CheckForNearbyBowls()
    {
        if (isFrying) return; // Don't accept new bowls while frying
        
        // Check for nearby bowls
        Collider2D[] bowls = Physics2D.OverlapCircleAll(transform.position, acceptRadius, bowlLayer);
        
        bool shouldHighlight = false;
        
        foreach (Collider2D bowlCollider in bowls)
        {
            Bowl bowlComponent = bowlCollider.GetComponent<Bowl>();
            if (bowlComponent != null && CanAcceptBowl(bowlComponent))
            {
                shouldHighlight = true;
                
                // Auto-accept if bowl is dropped nearby and not being dragged
                if (!bowlComponent.IsDragging())
                {
                    float distance = Vector3.Distance(transform.position, bowlCollider.transform.position);
                    if (distance <= acceptRadius)
                    {
                        AcceptBowl(bowlComponent);
                        break;
                    }
                }
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
            spriteRenderer.color = highlight ? canAcceptColor : normalColor;
        }
    }
    
    public List<GameObject> GetIngredientsInPan()
    {
        return new List<GameObject>(ingredientsInPan);
    }
    
    public bool IsFrying()
    {
        return isFrying;
    }
    
    private void OnDestroy()
    {
        // Clean up tweens
        foreach (GameObject ingredient in ingredientsInPan)
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
        // Draw accept radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, acceptRadius);
        
        // Draw ingredient container position
        if (ingredientContainer != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(ingredientContainer.position, Vector3.one * 0.5f);
        }
        
        // Draw lines to ingredients in pan
        Gizmos.color = Color.green;
        foreach (GameObject ingredient in ingredientsInPan)
        {
            if (ingredient != null)
            {
                Gizmos.DrawLine(transform.position, ingredient.transform.position);
            }
        }
    }
    #endregion
}