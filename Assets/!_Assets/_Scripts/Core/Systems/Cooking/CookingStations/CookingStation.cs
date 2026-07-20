using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Abstract base class for all cooking stations (Pan, Pot, Wok, etc.)
/// Handles common functionality: bowl acceptance, ingredient transfer, cooking state, UI, and effects
/// </summary>
public abstract class CookingStation : MonoBehaviour
{
    [Header("Cooking Station Settings")]
    [SerializeField] protected float cookDuration = 5f;
    [SerializeField] protected float acceptRadius = 1.5f;
    [SerializeField] protected LayerMask bowlLayer = -1;
    [SerializeField] protected Transform ingredientContainer;

    [Header("Animation Settings")]
    [SerializeField] protected float ingredientMoveDuration = 0.6f;
    [SerializeField] protected Ease ingredientMoveEase = Ease.OutBounce;

    [Header("Visual Feedback")]
    [SerializeField] protected Color normalColor = Color.white;
    [SerializeField] protected Color canAcceptColor = Color.red;
    [SerializeField] protected ParticleSystem cookingEffect;
    [SerializeField] protected ParticleSystem steamEffect;
    [SerializeField] protected ParticleSystem ingredientDropEffect;

    [Header("UI")]
    [SerializeField] protected GameObject cookingMeterUI;
    [SerializeField] protected Image cookingProgressBar;
    [SerializeField] protected Canvas worldCanvas;

    [Header("Sprite")]
    [SerializeField] protected SpriteRenderer spriteRenderer; // Made protected for derived class access

    // State - accessible to derived classes
    protected bool isCooking = false;
    protected bool isHighlighted = false;
    protected float cookTimer = 0f;
    protected List<GameObject> ingredientsInStation = new List<GameObject>();
    private Vector3 originalScale;

    #region Unity Lifecycle

    protected virtual void Start()
    {
        // Create ingredient container if not assigned
        if (ingredientContainer == null)
        {
            GameObject container = new GameObject("IngredientContainer");
            container.transform.parent = transform;
            container.transform.localPosition = Vector3.zero;
            ingredientContainer = container.transform;
        }

        // Store original scale for highlight animation
        originalScale = transform.localScale;

        // Setup UI
        SetupCookingMeterUI();

        // Hide UI initially
        if (cookingMeterUI != null)
        {
            cookingMeterUI.SetActive(false);
        }
    }

    protected virtual void Update()
    {
        CheckForNearbyBowls();

        if (isCooking)
        {
            UpdateCooking();
        }
    }

    protected virtual void OnDestroy()
    {
        // Clean up tweens
        foreach (GameObject ingredient in ingredientsInStation)
        {
            if (ingredient != null)
            {
                ingredient.transform.DOKill();
            }
        }
    }

    #endregion

    #region Abstract Methods - Must be implemented by derived classes

    /// <summary>
    /// Check if the ingredient can be cooked in this station
    /// </summary>
    protected abstract bool CanCookIngredient(GameObject ingredient);

    /// <summary>
    /// Start cooking the ingredient using the appropriate interface (IFryable, IBoilable, etc.)
    /// </summary>
    protected abstract void StartCookingIngredient(GameObject ingredient);

    /// <summary>
    /// Stop cooking the ingredient
    /// </summary>
    protected abstract void StopCookingIngredient(GameObject ingredient);

    /// <summary>
    /// Complete cooking the ingredient
    /// </summary>
    protected abstract void CompleteCookingIngredient(GameObject ingredient);

    /// <summary>
    /// Additional validation before accepting bowl (e.g., oil check for Pan)
    /// </summary>
    protected abstract bool AdditionalBowlAcceptanceCheck();

    /// <summary>
    /// Get the name of the cooking process for debugging (e.g., "frying", "boiling")
    /// </summary>
    protected abstract string GetCookingProcessName();

    #endregion

    #region Bowl Acceptance & Ingredient Transfer

    public bool CanAcceptBowl(Bowl bowl)
    {
        if (bowl == null || isCooking) return false;

        // Check additional requirements (like oil for Pan)
        if (!AdditionalBowlAcceptanceCheck())
        {
            return false;
        }

        // Check if bowl has ingredients that can be cooked
        List<GameObject> ingredients = bowl.GetContainedIngredients();
        foreach (GameObject ingredient in ingredients)
        {
            if (CanCookIngredient(ingredient))
            {
                return true;
            }
        }

        return false;
    }

    public void AcceptBowl(Bowl bowl)
    {
        if (!CanAcceptBowl(bowl)) return;

        // Get all cookable ingredients from the bowl
        List<GameObject> ingredients = bowl.GetContainedIngredients();
        List<GameObject> cookableIngredients = new List<GameObject>();

        foreach (GameObject ingredient in ingredients)
        {
            if (CanCookIngredient(ingredient))
            {
                cookableIngredients.Add(ingredient);
            }
        }

        if (cookableIngredients.Count > 0)
        {
            // Move ingredients from bowl to cooking station
            TransferIngredientsToStation(bowl, cookableIngredients);

            // Tutorial event - determine station type
            string stationType = GetCookingProcessName() == "boiling" ? "Pot" : "Pan";
            TutorialEvents.BowlDroppedOnStation(stationType);
        }
    }

    protected virtual void TransferIngredientsToStation(Bowl bowl, List<GameObject> ingredients)
    {
        int ingredientCount = ingredients.Count;
        int transferredCount = 0;

        foreach (GameObject ingredient in ingredients)
        {
            // Remove ingredient from bowl
            bowl.RemoveIngredient(ingredient);

            // Reset the ingredient's bowl interaction state (RemoveIngredient doesn't do this)
            ChickenBowlInteraction bowlInteraction = ingredient.GetComponent<ChickenBowlInteraction>();
            if (bowlInteraction != null)
            {
                bowlInteraction.ForceExitBowl();
            }

            // Add to station's ingredient list
            ingredientsInStation.Add(ingredient);

            // Calculate target position (spread them out in a circle)
            Vector3 targetPosition = ingredientContainer.position;

            // Add some offset for multiple ingredients
            if (ingredientCount > 1)
            {
                float angle = (transferredCount / (float)(ingredientCount - 1)) * 360f * Mathf.Deg2Rad;
                float radius = 0.3f;
                targetPosition.x += Mathf.Cos(angle) * radius;
                targetPosition.z += Mathf.Sin(angle) * radius;
            }

            // Animate ingredient moving to station
            MoveIngredientToStation(ingredient, targetPosition, transferredCount * 0.1f);
            transferredCount++;
        }
    }

    protected virtual void MoveIngredientToStation(GameObject ingredient, Vector3 targetPosition, float delay)
    {
        // Kill any existing tweens
        ingredient.transform.DOKill();

        // Animate movement with staggered delay
        ingredient.transform.DOMove(targetPosition, ingredientMoveDuration)
            .SetDelay(delay)
            .SetEase(ingredientMoveEase)
            .OnComplete(() => {
                OnIngredientArrivedInStation(ingredient);
            });
    }

    protected virtual void OnIngredientArrivedInStation(GameObject ingredient)
    {
        // Parent ingredient to station container
        ingredient.transform.SetParent(ingredientContainer);

        // Lock the ingredient so it can't be dragged out or picked up by a bowl
        ChickenDragBehavior drag = ingredient.GetComponent<ChickenDragBehavior>();
        if (drag != null) drag.enabled = false;

        foreach (Collider2D col in ingredient.GetComponents<Collider2D>())
            col.enabled = false;

        // Play drop effect
        if (ingredientDropEffect != null)
        {
            ingredientDropEffect.Play();
        }

        SpriteRenderer ingredientSprite = ingredient.GetComponent<SpriteRenderer>();
        if (ingredientSprite != null)
        {
            ingredientSprite.sortingOrder = spriteRenderer.sortingOrder + 2;
        }

        // Check if all ingredients have arrived, then start cooking
        CheckIfReadyToStartCooking();
    }

    protected virtual void CheckIfReadyToStartCooking()
    {
        // Count how many ingredients are still moving
        int movingIngredients = 0;
        foreach (GameObject ingredient in ingredientsInStation)
        {
            if (ingredient != null && DOTween.IsTweening(ingredient.transform))
            {
                movingIngredients++;
            }
        }

        // If no ingredients are moving, start cooking
        if (movingIngredients == 0 && ingredientsInStation.Count > 0 && !isCooking)
        {
            StartCooking();
        }
    }

    #endregion

    #region Cooking State Management

    protected virtual void StartCooking()
    {
        if (isCooking || ingredientsInStation.Count == 0) return;

        isCooking = true;
        cookTimer = 0f;

        // Start cooking each ingredient
        foreach (GameObject ingredient in ingredientsInStation)
        {
            if (ingredient != null)
            {
                StartCookingIngredient(ingredient);
            }
        }

        // Start effects
        if (cookingEffect != null)
        {
            cookingEffect.Play();
        }

        if (steamEffect != null)
        {
            steamEffect.Play();
        }

        // Show UI
        if (cookingMeterUI != null)
        {
            cookingMeterUI.SetActive(true);
            // Position UI above station
            cookingMeterUI.transform.position = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        }
    }

    protected virtual void StopCooking()
    {
        if (!isCooking) return;

        isCooking = false;
        cookTimer = 0f;

        // Stop cooking each ingredient
        foreach (GameObject ingredient in ingredientsInStation)
        {
            if (ingredient != null)
            {
                StopCookingIngredient(ingredient);
            }
        }

        // Stop effects
        if (cookingEffect != null)
        {
            cookingEffect.Stop();
        }

        if (steamEffect != null)
        {
            steamEffect.Stop();
        }

        // Hide UI
        if (cookingMeterUI != null)
        {
            cookingMeterUI.SetActive(false);
        }
    }

    protected virtual void CompleteCooking()
    {
        if (!isCooking) return;

        // Complete cooking for each ingredient
        foreach (GameObject ingredient in ingredientsInStation)
        {
            if (ingredient != null)
            {
                CompleteCookingIngredient(ingredient);
            }
        }

        StopCooking();
    }

    /// <summary>
    /// Update cooking progress. Can be overridden by derived classes for custom cooking behavior (e.g., gesture-based)
    /// </summary>
    protected virtual void UpdateCooking()
    {
        cookTimer += Time.deltaTime;

        // Update progress bar
        if (cookingProgressBar != null)
        {
            float progress = cookTimer / cookDuration;
            cookingProgressBar.fillAmount = progress;

            // Color transition from red to green
            cookingProgressBar.color = Color.Lerp(Color.red, Color.green, progress);
        }

        // Update UI position (follow station)
        if (cookingMeterUI != null && cookingMeterUI.activeInHierarchy)
        {
            cookingMeterUI.transform.position = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        }

        // Complete when timer reaches duration
        if (cookTimer >= cookDuration)
        {
            CompleteCooking();
        }
    }

    #endregion

    #region UI Setup

    protected virtual void SetupCookingMeterUI()
    {
        if (cookingMeterUI == null && worldCanvas != null)
        {
            // Create cooking meter UI
            cookingMeterUI = new GameObject("CookingMeter");
            cookingMeterUI.transform.SetParent(worldCanvas.transform);

            // Add background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(cookingMeterUI.transform);
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.7f);

            // Add progress bar
            GameObject progressBarObj = new GameObject("ProgressBar");
            progressBarObj.transform.SetParent(cookingMeterUI.transform);
            cookingProgressBar = progressBarObj.AddComponent<Image>();
            cookingProgressBar.color = Color.red;
            cookingProgressBar.type = Image.Type.Filled;
            cookingProgressBar.fillMethod = Image.FillMethod.Horizontal;

            cookingMeterUI.SetActive(false);
        }
    }

    #endregion

    #region Bowl Detection & Highlighting

    protected virtual void CheckForNearbyBowls()
    {
        if (isCooking) return; // Don't accept new bowls while cooking

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

    protected virtual void SetHighlight(bool highlight)
    {
        if (isHighlighted == highlight) return;

        isHighlighted = highlight;

        transform.DOKill(true);
        transform.DOScale(highlight ? originalScale * 1.1f : originalScale, 0.2f)
            .SetEase(highlight ? Ease.OutBack : Ease.OutQuad);
    }

    #endregion

    #region Public API

    public List<GameObject> GetIngredientsInStation()
    {
        return new List<GameObject>(ingredientsInStation);
    }

    public bool IsCooking()
    {
        return isCooking;
    }

    #endregion

    #region Gizmos for Debugging

    protected virtual void OnDrawGizmosSelected()
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

        // Draw lines to ingredients in station
        Gizmos.color = Color.green;
        foreach (GameObject ingredient in ingredientsInStation)
        {
            if (ingredient != null)
            {
                Gizmos.DrawLine(transform.position, ingredient.transform.position);
            }
        }
    }

    #endregion
}
