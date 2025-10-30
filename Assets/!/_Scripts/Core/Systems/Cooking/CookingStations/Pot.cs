using UnityEngine;

/// <summary>
/// Pot cooking station - specialized for boiling ingredients
/// No special requirements before accepting ingredients
/// </summary>
public class Pot : CookingStation
{
    #region Abstract Method Implementations

    protected override bool CanCookIngredient(GameObject ingredient)
    {
        IBoilable boilable = ingredient.GetComponent<IBoilable>();
        return boilable != null && boilable.CanBeBoiled();
    }

    protected override void StartCookingIngredient(GameObject ingredient)
    {
        IBoilable boilable = ingredient.GetComponent<IBoilable>();
        if (boilable != null)
        {
            boilable.StartBoiling();
        }
    }

    protected override void StopCookingIngredient(GameObject ingredient)
    {
        IBoilable boilable = ingredient.GetComponent<IBoilable>();
        if (boilable != null)
        {
            boilable.StopBoiling();
        }
    }

    protected override void CompleteCookingIngredient(GameObject ingredient)
    {
        IBoilable boilable = ingredient.GetComponent<IBoilable>();
        if (boilable != null)
        {
            boilable.CompleteBoiling();
        }
    }

    protected override bool AdditionalBowlAcceptanceCheck()
    {
        // Pot has no additional requirements (unlike Pan which needs oil)
        return true;
    }

    protected override string GetCookingProcessName()
    {
        return "boiling";
    }

    #endregion

    #region Seasoning & Liquid Management (for backward compatibility with DraggableIngredient)

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
            Debug.Log($"🧂 Added seasoning to pot: {seasoning.ingredientName} (no SeasoningManager attached)");
        }
    }

    /// <summary>
    /// Check if liquid can be added
    /// </summary>
    public bool CanAcceptLiquid()
    {
        return !isCooking;
    }

    /// <summary>
    /// Add a liquid ingredient - delegates to SeasoningManager if available
    /// </summary>
    public void AddLiquid(Ingredient liquid)
    {
        if (!CanAcceptLiquid())
        {
            Debug.LogWarning($"Cannot add liquid while cooking!");
            return;
        }

        // Try to use SeasoningManager if available
        SeasoningManager seasoningMgr = GetComponent<SeasoningManager>();
        if (seasoningMgr != null)
        {
            seasoningMgr.AddLiquid(liquid);
        }
        else
        {
            // Fallback - just log if no manager is attached
            // Apply visual feedback
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(spriteRenderer.color, Color.cyan, 0.2f);
            }
            Debug.Log($"💧 Added liquid to pot: {liquid.ingredientName} (no SeasoningManager attached)");
        }
    }

    #endregion
}
