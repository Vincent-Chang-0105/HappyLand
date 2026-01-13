using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// Manages automatic plating of cooked dishes.
/// Monitors cooking completion and creates plated dishes when enough ingredients are ready.
/// Supports multiple dish types (fried chicken, sinigang, etc.)
/// </summary>
public class PlatingManager : Singleton<PlatingManager>
{
    [System.Serializable]
    public class DishType
    {
        public string dishName;
        public GameObject platedDishPrefab;
        public int ingredientsPerPlate = 3;
        [HideInInspector] public List<GameObject> waitingIngredients = new List<GameObject>();
    }

    [Header("Supported Dishes")]
    [SerializeField] private List<DishType> supportedDishes = new List<DishType>();

    [Header("Plate Spawning")]
    [SerializeField] private Transform servingArea;
    [SerializeField] private float plateSpawnDelay = 0.5f;
    [SerializeField] private Vector2 plateSpawnOffset = Vector2.zero;

    private void Start()
    {
        if (supportedDishes == null || supportedDishes.Count == 0)
        {
            Debug.LogError("PlatingManager: No supported dishes configured!", this);
        }

        if (servingArea == null)
        {
            Debug.LogError("PlatingManager: No serving area assigned!", this);
        }
    }

    /// <summary>
    /// Register a fried chicken as ready for plating
    /// </summary>
    public void RegisterFriedChicken(GameObject chicken)
    {
        RegisterIngredientForDish("FriedChicken", chicken,
            c => c.GetComponent<IFryable>()?.IsFried() ?? false);
    }

    /// <summary>
    /// Register a sinigang chicken as ready for plating
    /// </summary>
    public void RegisterSinigangChicken(GameObject chicken)
    {
        RegisterIngredientForDish("Sinigang", chicken,
            c => c.GetComponent<ISinigangable>()?.IsSiniganged() ?? false);
    }

    private void RegisterIngredientForDish(string dishName, GameObject ingredient, System.Func<GameObject, bool> validator)
    {
        if (ingredient == null) return;

        // Find dish type
        DishType dish = supportedDishes.Find(d => d.dishName == dishName);
        if (dish == null)
        {
            Debug.LogError($"Dish type '{dishName}' not configured in PlatingManager!");
            return;
        }

        // Validate ingredient state
        if (!validator(ingredient))
        {
            Debug.LogWarning($"Attempted to register invalid ingredient for {dishName}: {ingredient.name}");
            return;
        }

        // Add to waiting list
        if (!dish.waitingIngredients.Contains(ingredient))
        {
            dish.waitingIngredients.Add(ingredient);
            Debug.Log($"{dishName} ingredient registered. Waiting: {dish.waitingIngredients.Count}/{dish.ingredientsPerPlate}");

            CheckAndCreatePlate(dish);
        }
    }

    private void CheckAndCreatePlate(DishType dish)
    {
        dish.waitingIngredients.RemoveAll(i => i == null);

        if (dish.waitingIngredients.Count >= dish.ingredientsPerPlate)
        {
            Debug.Log($"Creating {dish.dishName} plate with {dish.ingredientsPerPlate} ingredients!");
            CreatePlate(dish);
        }
    }

    private void CreatePlate(DishType dish)
    {
        if (dish.platedDishPrefab == null || servingArea == null)
        {
            Debug.LogError($"Cannot create {dish.dishName} plate: missing prefab or serving area");
            return;
        }

        // Take ingredients for this plate
        List<GameObject> ingredientsForPlate = dish.waitingIngredients
            .Take(dish.ingredientsPerPlate)
            .ToList();

        if (ingredientsForPlate.Count < dish.ingredientsPerPlate)
        {
            Debug.LogWarning($"Not enough ingredients for {dish.dishName}: {ingredientsForPlate.Count}/{dish.ingredientsPerPlate}");
            return;
        }

        // Spawn plated dish
        GameObject plateObject = Instantiate(dish.platedDishPrefab, servingArea);
        PlatedDish plate = plateObject.GetComponent<PlatedDish>();

        if (plate == null)
        {
            Debug.LogError($"{dish.dishName} prefab missing PlatedDish component!");
            Destroy(plateObject);
            return;
        }

        // Apply spawn settings
        RectTransform plateRect = plateObject.GetComponent<RectTransform>();
        if (plateRect != null)
        {
            plateRect.anchoredPosition = plateSpawnOffset;
            plateRect.localScale = Vector3.one;
        }

        // Add ingredients to plate
        foreach (GameObject ingredient in ingredientsForPlate)
        {
            plate.AddChicken(ingredient); // Method name is generic despite name
            Destroy(ingredient);
        }

        // Remove from waiting list
        foreach (GameObject ingredient in ingredientsForPlate)
        {
            dish.waitingIngredients.Remove(ingredient);
        }

        Debug.Log($"✅ {dish.dishName} plate created! Remaining: {dish.waitingIngredients.Count}");
        PlayPlateCreationEffect(plateObject);
    }

    /// <summary>
    /// Play visual/audio feedback when plate is created
    /// </summary>
    private void PlayPlateCreationEffect(GameObject plate)
    {
        // Add your effects here
        // Example: particle effects, sound, animation

        // Note: Scale animation removed to prevent issues
        // If you want to add animation later, use DOTween or another animation system
    }

    /// <summary>
    /// Clear all waiting ingredients (for cleanup/reset)
    /// </summary>
    public void ClearWaitingIngredients()
    {
        foreach (DishType dish in supportedDishes)
        {
            dish.waitingIngredients.Clear();
        }
        Debug.Log("Cleared all waiting ingredients");
    }
}
