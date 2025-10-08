using UnityEngine;

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
    
    [Header("Visual")]
    public Color backgroundColor = Color.white;
}

public enum IngredientType
{
    Vegetable,
    Seasoning,
    Liquid,
    Spice
}