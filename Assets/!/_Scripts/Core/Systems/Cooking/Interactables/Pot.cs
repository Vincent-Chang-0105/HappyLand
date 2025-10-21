using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

public class Pot : MonoBehaviour
{
    [Header("Pot Settings")]
    [SerializeField] private float boilDuration = 5f;
    [SerializeField] private float acceptRadius = 1.5f;
    [SerializeField] private LayerMask bowlLayer = -1;
    [SerializeField] private Transform ingredientContainer; // Where ingredients go inside the pot
    
    [Header("Animation Settings")]
    [SerializeField] private float ingredientMoveDuration = 0.6f;
    [SerializeField] private Ease ingredientMoveEase = Ease.OutBounce;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color canAcceptColor = Color.orange;
    [SerializeField] private ParticleSystem boilingEffect;
    [SerializeField] private ParticleSystem steamEffect;
    [SerializeField] private ParticleSystem ingredientDropEffect;
    
    [Header("UI")]
    [SerializeField] private GameObject boilingMeterUI;
    [SerializeField] private Image boilingProgressBar;
    [SerializeField] private Canvas worldCanvas; // For world space UI
    
    private bool isBoiling = false;
    private bool isHighlighted = false;
    private float boilTimer = 0f;
    private SpriteRenderer spriteRenderer;
    
    // Boiling state
    private List<GameObject> ingredientsInPot = new List<GameObject>();
    
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
        SetupBoilingMeterUI();
        
        // Hide UI initially
        if (boilingMeterUI != null)
        {
            boilingMeterUI.SetActive(false);
        }
    }
    
    private void Update()
    {
        CheckForNearbyBowls();
        
        if (isBoiling)
        {
            UpdateBoiling();
        }
    }
    
    private void SetupBoilingMeterUI()
    {
        if (boilingMeterUI == null && worldCanvas != null)
        {
            // Create boiling meter UI
            boilingMeterUI = new GameObject("BoilingMeter");
            boilingMeterUI.transform.SetParent(worldCanvas.transform);
            
            // Add background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(boilingMeterUI.transform);
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.7f);
            
            // Add progress bar
            GameObject progressBarObj = new GameObject("ProgressBar");
            progressBarObj.transform.SetParent(boilingMeterUI.transform);
            boilingProgressBar = progressBarObj.AddComponent<Image>();
            boilingProgressBar.color = Color.red;
            boilingProgressBar.type = Image.Type.Filled;
            boilingProgressBar.fillMethod = Image.FillMethod.Horizontal;
            
            boilingMeterUI.SetActive(false);
        }
    }
    
    public bool CanAcceptBowl(Bowl bowl)
    {
        if (bowl == null || isBoiling) return false;
        
        // Check if bowl has ingredients that can be boiled
        List<GameObject> ingredients = bowl.GetContainedIngredients();
        foreach (GameObject ingredient in ingredients)
        {
            IBoilable boilable = ingredient.GetComponent<IBoilable>();
            if (boilable != null && boilable.CanBeBoiled())
            {
                return true;
            }
        }
        
        return false;
    }
    
    public void AcceptBowl(Bowl bowl)
    {
        if (!CanAcceptBowl(bowl)) return;
        
        // Get all boilable ingredients from the bowl
        List<GameObject> ingredients = bowl.GetContainedIngredients();
        List<GameObject> boilableIngredients = new List<GameObject>();
        
        foreach (GameObject ingredient in ingredients)
        {
            IBoilable boilable = ingredient.GetComponent<IBoilable>();
            if (boilable != null && boilable.CanBeBoiled())
            {
                boilableIngredients.Add(ingredient);
            }
        }
        
        if (boilableIngredients.Count > 0)
        {
            // Move ingredients from bowl to pot
            TransferIngredientsToPot(bowl, boilableIngredients);
        }
        
        Debug.Log($"🍲 Bowl brought to pot - transferring {boilableIngredients.Count} ingredients!");
    }
    
    private void TransferIngredientsToPot(Bowl bowl, List<GameObject> ingredients)
    {
        int ingredientCount = ingredients.Count;
        int transferredCount = 0;
        
        foreach (GameObject ingredient in ingredients)
        {
            // Remove ingredient from bowl
            bowl.RemoveIngredient(ingredient);
            
            // Add to pot's ingredient list
            ingredientsInPot.Add(ingredient);
            
            // Calculate target position in pot (spread them out a bit)
            Vector3 targetPosition = ingredientContainer.position;
            
            // Add some random offset for multiple ingredients
            if (ingredientCount > 1)
            {
                float angle = (transferredCount / (float)(ingredientCount - 1)) * 360f * Mathf.Deg2Rad;
                float radius = 0.3f;
                targetPosition.x += Mathf.Cos(angle) * radius;
                targetPosition.z += Mathf.Sin(angle) * radius;
            }
            
            // Animate ingredient moving to pot
            MoveIngredientToPot(ingredient, targetPosition, transferredCount * 0.1f); // Stagger animations
            transferredCount++;
        }
    }
    
    private void MoveIngredientToPot(GameObject ingredient, Vector3 targetPosition, float delay)
    {
        // Kill any existing tweens
        ingredient.transform.DOKill();
        
        // Animate movement to pot with a slight delay for staggering
        ingredient.transform.DOMove(targetPosition, ingredientMoveDuration)
            .SetDelay(delay)
            .SetEase(ingredientMoveEase)
            .OnComplete(() => {
                OnIngredientArrivedInPot(ingredient);
            });
    }
    
    private void OnIngredientArrivedInPot(GameObject ingredient)
    {
        // Parent ingredient to pot container
        ingredient.transform.SetParent(ingredientContainer);
        
        // Play drop effect
        if (ingredientDropEffect != null)
        {
            ingredientDropEffect.Play();
        }
        
        Debug.Log($"🍲 Ingredient {ingredient.name} arrived in pot!");
        
        // Check if all ingredients have arrived, then start boiling
        CheckIfReadyToStartBoiling();
    }
    
    private void CheckIfReadyToStartBoiling()
    {
        // Count how many ingredients are still moving
        int movingIngredients = 0;
        foreach (GameObject ingredient in ingredientsInPot)
        {
            if (ingredient != null && DOTween.IsTweening(ingredient.transform))
            {
                movingIngredients++;
            }
        }
        
        // If no ingredients are moving, start boiling
        if (movingIngredients == 0 && ingredientsInPot.Count > 0 && !isBoiling)
        {
            StartBoiling();
        }
    }
    
    private void StartBoiling()
    {
        if (isBoiling || ingredientsInPot.Count == 0) return;
        
        isBoiling = true;
        boilTimer = 0f;
        
        // Start boiling each ingredient
        foreach (GameObject ingredient in ingredientsInPot)
        {
            if (ingredient != null)
            {
                IBoilable boilable = ingredient.GetComponent<IBoilable>();
                if (boilable != null)
                {
                    boilable.StartBoiling();
                }
            }
        }
        
        // Start effects
        if (boilingEffect != null)
        {
            boilingEffect.Play();
        }
        
        if (steamEffect != null)
        {
            steamEffect.Play();
        }
        
        // Show UI
        if (boilingMeterUI != null)
        {
            boilingMeterUI.SetActive(true);
            // Position UI above pot
            boilingMeterUI.transform.position = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        }
        
        Debug.Log("🔥 Started boiling ingredients in pot!");
    }
    
    private void StopBoiling()
    {
        if (!isBoiling) return;
        
        isBoiling = false;
        boilTimer = 0f;
        
        // Stop boiling each ingredient
        foreach (GameObject ingredient in ingredientsInPot)
        {
            if (ingredient != null)
            {
                IBoilable boilable = ingredient.GetComponent<IBoilable>();
                if (boilable != null)
                {
                    boilable.StopBoiling();
                }
            }
        }
        
        // Stop effects
        if (boilingEffect != null)
        {
            boilingEffect.Stop();
        }
        
        if (steamEffect != null)
        {
            steamEffect.Stop();
        }
        
        // Hide UI
        if (boilingMeterUI != null)
        {
            boilingMeterUI.SetActive(false);
        }
        
        Debug.Log("⏹️ Stopped boiling");
    }
    
    private void CompleteBoiling()
    {
        if (!isBoiling) return;
        
        // Complete boiling for each ingredient
        foreach (GameObject ingredient in ingredientsInPot)
        {
            if (ingredient != null)
            {
                IBoilable boilable = ingredient.GetComponent<IBoilable>();
                if (boilable != null)
                {
                    boilable.CompleteBoiling();
                }
            }
        }
        
        StopBoiling();
        
        Debug.Log("✅ Boiling completed! Ingredients are now cooked!");
    }
    
    private void UpdateBoiling()
    {
        boilTimer += Time.deltaTime;
        
        // Update progress bar
        if (boilingProgressBar != null)
        {
            float progress = boilTimer / boilDuration;
            boilingProgressBar.fillAmount = progress;
            
            // Color transition from red to green
            boilingProgressBar.color = Color.Lerp(Color.red, Color.green, progress);
        }
        
        // Update UI position (follow pot)
        if (boilingMeterUI != null && boilingMeterUI.activeInHierarchy)
        {
            boilingMeterUI.transform.position = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        }
        
        // Complete when timer reaches duration
        if (boilTimer >= boilDuration)
        {
            CompleteBoiling();
        }
    }
    
    private void CheckForNearbyBowls()
    {
        if (isBoiling) return; // Don't accept new bowls while boiling
        
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
    
    public List<GameObject> GetIngredientsInPot()
    {
        return new List<GameObject>(ingredientsInPot);
    }
    
    public bool IsBoiling()
    {
        return isBoiling;
    }
    
    private void OnDestroy()
    {
        // Clean up tweens
        foreach (GameObject ingredient in ingredientsInPot)
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
        
        // Draw lines to ingredients in pot
        Gizmos.color = Color.green;
        foreach (GameObject ingredient in ingredientsInPot)
        {
            if (ingredient != null)
            {
                Gizmos.DrawLine(transform.position, ingredient.transform.position);
            }
        }
    }
    #endregion
}