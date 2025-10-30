using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Reusable component for managing seasonings and liquids on cooking stations
/// Can be attached to any cookware (Pan, Pot, etc.)
/// </summary>
public class SeasoningManager : MonoBehaviour
{
    [Header("Seasoning Settings")]
    [SerializeField] private List<Ingredient> addedSeasonings = new List<Ingredient>();
    [SerializeField] private ParticleSystem seasoningEffect;

    [Header("Liquid Settings")]
    [SerializeField] private List<Ingredient> addedLiquids = new List<Ingredient>();
    [SerializeField] private ParticleSystem liquidEffect;

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer targetSpriteRenderer;
    [SerializeField] private Color liquidTintColor = Color.cyan;
    [SerializeField] private float liquidTintStrength = 0.2f;

    private CookingStation cookingStation;

    private void Start()
    {
        cookingStation = GetComponent<CookingStation>();

        // Auto-find sprite renderer if not assigned
        if (targetSpriteRenderer == null)
        {
            targetSpriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    #region Seasoning Management

    /// <summary>
    /// Check if seasoning can be added (usually not while cooking)
    /// </summary>
    public bool CanAcceptSeasoning()
    {
        if (cookingStation != null)
        {
            return !cookingStation.IsCooking();
        }
        return true; // Default to true if no cooking station
    }

    /// <summary>
    /// Add a seasoning ingredient
    /// </summary>
    public void AddSeasoning(Ingredient seasoning)
    {
        if (!CanAcceptSeasoning())
        {
            Debug.LogWarning($"Cannot add seasoning while cooking!");
            return;
        }

        addedSeasonings.Add(seasoning);

        // Play particle effect
        if (seasoningEffect != null)
        {
            seasoningEffect.Play();
        }

        Debug.Log($"🧂 Added seasoning: {seasoning.ingredientName}");
    }

    /// <summary>
    /// Get all added seasonings
    /// </summary>
    public List<Ingredient> GetSeasonings()
    {
        return new List<Ingredient>(addedSeasonings);
    }

    /// <summary>
    /// Clear all seasonings
    /// </summary>
    public void ClearSeasonings()
    {
        addedSeasonings.Clear();
    }

    #endregion

    #region Liquid Management

    /// <summary>
    /// Check if liquid can be added (usually not while cooking)
    /// </summary>
    public bool CanAcceptLiquid()
    {
        if (cookingStation != null)
        {
            return !cookingStation.IsCooking();
        }
        return true; // Default to true if no cooking station
    }

    /// <summary>
    /// Add a liquid ingredient
    /// </summary>
    public void AddLiquid(Ingredient liquid)
    {
        if (!CanAcceptLiquid())
        {
            Debug.LogWarning($"Cannot add liquid while cooking!");
            return;
        }

        addedLiquids.Add(liquid);

        // Play particle effect
        if (liquidEffect != null)
        {
            liquidEffect.Play();
        }

        // Visual feedback - tint the sprite
        if (targetSpriteRenderer != null)
        {
            targetSpriteRenderer.color = Color.Lerp(
                targetSpriteRenderer.color,
                liquidTintColor,
                liquidTintStrength
            );
        }

        Debug.Log($"💧 Added liquid: {liquid.ingredientName}");
    }

    /// <summary>
    /// Get all added liquids
    /// </summary>
    public List<Ingredient> GetLiquids()
    {
        return new List<Ingredient>(addedLiquids);
    }

    /// <summary>
    /// Clear all liquids
    /// </summary>
    public void ClearLiquids()
    {
        addedLiquids.Clear();
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Check if a specific seasoning has been added
    /// </summary>
    public bool HasSeasoning(Ingredient seasoning)
    {
        return addedSeasonings.Contains(seasoning);
    }

    /// <summary>
    /// Check if a specific liquid has been added
    /// </summary>
    public bool HasLiquid(Ingredient liquid)
    {
        return addedLiquids.Contains(liquid);
    }

    /// <summary>
    /// Clear all seasonings and liquids
    /// </summary>
    public void ClearAll()
    {
        ClearSeasonings();
        ClearLiquids();
    }

    /// <summary>
    /// Get total number of added ingredients (seasonings + liquids)
    /// </summary>
    public int GetTotalIngredientCount()
    {
        return addedSeasonings.Count + addedLiquids.Count;
    }

    #endregion
}
