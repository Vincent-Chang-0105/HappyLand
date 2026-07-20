using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "New Ingredient", menuName = "Cooking/Ingredient")]
public class Ingredient : ScriptableObject
{
    [Header("Basic Info")]
    public string ingredientName;
    public Sprite ingredientIcon;
    public string description;
    
    [Header("Stacking Properties")]
    public bool isStackable = true;
    public int maxStackSize = 99;
    
    [Header("Cooking Properties")]
    public IngredientType ingredientType;
    public float cookingTime = 1f;
    public bool requiresCutting = false;
    public bool requiresWashing = false;

    [Header("Animation")]
    public Sprite[] animationFrames;
    
    [Header("Drag Properties")]
    public Sprite bowlVersionSprite;
    public IngredientUsageType usageType = IngredientUsageType.Seasoning;
    
    [Header("Visual")]
    public Color backgroundColor = Color.white;

    [Header("Economy")]
    [Tooltip("Cost in PHP to use this ingredient")]
    public int cost = 0;

    [Header("Cooking Role")]
    [Tooltip("Determines which pan method this ingredient triggers. Set this in the Inspector for every ingredient asset.")]
    public IngredientRole ingredientRole = IngredientRole.None;
}

public enum IngredientType
{
    Vegetable,
    Seasoning,
    Liquid,
    Spice,
    Oil,
}

public enum IngredientUsageType
{
    Seasoning,  // For salt, spices - can be added to pan/pot
    Oil,        // For cooking oil - only for pan
    Liquid,      // For water, broth - only for pot
}

public enum IngredientRole
{
    None = 0,
    FryingOil = 1,
    FryingSalt = 2,
    AdoboOnionGarlic = 10,
    AdoboSoySauce    = 11,
    AdoboVinegar     = 12,
    AdoboSugar       = 13,
    AdoboLaurel      = 14,
    MechadoCatsup       = 20,
    MechadoPowderedMilk = 21,
    SinigangMix      = 30,
    NoodleIngredient = 40,
    CookingWater     = 50,
}