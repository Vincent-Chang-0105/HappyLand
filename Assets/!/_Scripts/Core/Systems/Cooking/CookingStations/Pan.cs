using UnityEngine;

/// <summary>
/// Pan cooking station - specialized for frying ingredients
/// Requires oil before accepting ingredients
/// </summary>
public class Pan : CookingStation
{
    [Header("Pan-Specific Settings")]
    [SerializeField] private bool hasOil = false;
    [SerializeField] private GameObject oilVisual;
    [SerializeField] private ParticleSystem oilEffect;

    #region Abstract Method Implementations

    protected override bool CanCookIngredient(GameObject ingredient)
    {
        IFryable fryable = ingredient.GetComponent<IFryable>();
        return fryable != null && fryable.CanBeFried();
    }

    protected override void StartCookingIngredient(GameObject ingredient)
    {
        IFryable fryable = ingredient.GetComponent<IFryable>();
        if (fryable != null)
        {
            fryable.StartFrying();
        }
    }

    protected override void StopCookingIngredient(GameObject ingredient)
    {
        IFryable fryable = ingredient.GetComponent<IFryable>();
        if (fryable != null)
        {
            fryable.StopFrying();
        }
    }

    protected override void CompleteCookingIngredient(GameObject ingredient)
    {
        IFryable fryable = ingredient.GetComponent<IFryable>();
        if (fryable != null)
        {
            fryable.CompleteFrying();
        }
    }

    protected override bool AdditionalBowlAcceptanceCheck()
    {
        // Pan requires oil before accepting ingredients
        if (!hasOil)
        {
            Debug.LogWarning("⚠️ Add oil to the pan first before frying!");
            return false;
        }
        return true;
    }

    protected override string GetCookingProcessName()
    {
        return "frying";
    }

    #endregion

    #region Oil Management

    /// <summary>
    /// Check if oil can be added to the pan
    /// </summary>
    public bool CanAcceptOil()
    {
        return !hasOil && !isCooking;
    }

    /// <summary>
    /// Add oil to the pan
    /// </summary>
    public void AddOil()
    {
        if (hasOil) return;

        hasOil = true;

        // Play oil effect
        if (oilEffect != null)
        {
            oilEffect.Play();
        }

        // Show oil visual
        if (oilVisual != null)
        {
            oilVisual.SetActive(true);
        }

        // Visual feedback - tint sprite yellow
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, Color.yellow, 0.3f);
        }

        Debug.Log("🛢️ Oil added to pan!");
    }

    /// <summary>
    /// Check if pan has oil
    /// </summary>
    public bool HasOil()
    {
        return hasOil;
    }

    #endregion

    #region Seasoning Management (for backward compatibility with DraggableIngredient)

    /// <summary>
    /// Check if seasoning can be added
    /// </summary>
    public bool CanAcceptSeasoning()
    {
        return !isCooking;
    }

    /// <summary>
    /// Add a seasoning ingredient - delegates to SeasoningManager if available
    /// </summary>
    public void AddSeasoning(Ingredient seasoning)
    {
        if (!CanAcceptSeasoning())
        {
            Debug.LogWarning($"Cannot add seasoning while cooking!");
            return;
        }

        // Try to use SeasoningManager if available
        SeasoningManager seasoningMgr = GetComponent<SeasoningManager>();
        if (seasoningMgr != null)
        {
            seasoningMgr.AddSeasoning(seasoning);
        }
        else
        {
            // Fallback - just log if no manager is attached
            Debug.Log($"🧂 Added seasoning: {seasoning.ingredientName} (no SeasoningManager attached)");
        }
    }

    #endregion
}
