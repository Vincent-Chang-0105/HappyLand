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
    public class DishConfig
    {
        public DishType dishType;
        public GameObject platedDishPrefab;
        public int ingredientsPerPlate = 3;
        [HideInInspector] public List<GameObject> waitingIngredients = new List<GameObject>();
    }

    [Header("Supported Dishes")]
    [SerializeField] private List<DishConfig> supportedDishes = new List<DishConfig>();

    [Header("Plate Spawning")]
    [SerializeField] private Transform servingArea;
    [SerializeField] private float plateSpawnDelay = 0.5f;
    [SerializeField] private Vector2 plateSpawnOffset = Vector2.zero;
    [SerializeField] private Vector2 plateSpacing = new Vector2(220f, 0f);
    [SerializeField] private int maxVisiblePlates = 3;
    [SerializeField] private ParticleSystem plateCompleteEffect;

    private int activePlateCount = 0;

    private class QueuedPlate
    {
        public DishConfig dish;
        public List<GameObject> ingredients;
    }
    private readonly Queue<QueuedPlate> plateQueue = new Queue<QueuedPlate>();

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
        RegisterIngredientForDish(DishType.FriedChicken, chicken,
            c => c.GetComponent<IFryable>()?.IsFried() ?? false);
    }

    /// <summary>
    /// Register a sinigang chicken as ready for plating
    /// </summary>
    public void RegisterSinigangChicken(GameObject chicken)
    {
        RegisterIngredientForDish(DishType.Sinigang, chicken,
            c => c.GetComponent<ISinigangable>()?.IsSiniganged() ?? false);
    }

    /// <summary>
    /// Register a noodled chicken as ready for plating
    /// </summary>
    public void RegisterNoodleChicken(GameObject chicken)
    {
        RegisterIngredientForDish(DishType.NoodleChicken, chicken,
            c => c.GetComponent<INoodleable>()?.IsNoodled() ?? false);
    }

    /// <summary>
    /// Register a mechado chicken as ready for plating
    /// </summary>
    public void RegisterMechadoChicken(GameObject chicken)
    {
        RegisterIngredientForDish(DishType.Mechado, chicken,
            c => c.GetComponent<IMechadoable>()?.IsMechado() ?? false);
    }

    /// <summary>
    /// Register an adobo chicken as ready for plating
    /// </summary>
    public void RegisterAdoboChicken(GameObject chicken)
    {
        RegisterIngredientForDish(DishType.Adobo, chicken,
            c => c.GetComponent<IAdoboable>()?.IsAdobo() ?? false);
    }

    /// <summary>
    /// Register the noodle object itself as ready for plating
    /// </summary>
    public void RegisterNoodleIngredient(GameObject ingredient)
    {
        RegisterIngredientForDish(DishType.NoodleChicken, ingredient, _ => true);
    }

    private void RegisterIngredientForDish(DishType dishType, GameObject ingredient, System.Func<GameObject, bool> validator)
    {
        if (ingredient == null) return;

        DishConfig dish = supportedDishes.Find(d => d.dishType == dishType);
        if (dish == null)
        {
            Debug.LogError($"Dish type '{dishType}' not configured in PlatingManager!");
            return;
        }

        if (!validator(ingredient))
        {
            Debug.LogWarning($"Attempted to register invalid ingredient for {dishType}: {ingredient.name}");
            return;
        }

        if (!dish.waitingIngredients.Contains(ingredient))
        {
            dish.waitingIngredients.Add(ingredient);
            Debug.Log($"{dishType} ingredient registered. Waiting: {dish.waitingIngredients.Count}/{dish.ingredientsPerPlate}");

            CheckAndCreatePlate(dish);
        }
    }

    private void CheckAndCreatePlate(DishConfig dish)
    {
        dish.waitingIngredients.RemoveAll(i => i == null);

        if (dish.waitingIngredients.Count >= dish.ingredientsPerPlate)
        {
            Debug.Log($"Creating {dish.dishType} plate with {dish.ingredientsPerPlate} ingredients!");
            CreatePlate(dish);
        }
    }

    private void CreatePlate(DishConfig dish)
    {
        if (dish.platedDishPrefab == null || servingArea == null)
        {
            Debug.LogError($"Cannot create {dish.dishType} plate: missing prefab or serving area");
            return;
        }

        List<GameObject> ingredientsForPlate = dish.waitingIngredients.Take(dish.ingredientsPerPlate).ToList();

        if (ingredientsForPlate.Count < dish.ingredientsPerPlate)
        {
            Debug.LogWarning($"Not enough ingredients for {dish.dishType}: {ingredientsForPlate.Count}/{dish.ingredientsPerPlate}");
            return;
        }

        // Remove from waiting list immediately so they aren't double-counted
        foreach (GameObject ingredient in ingredientsForPlate)
            dish.waitingIngredients.Remove(ingredient);

        if (activePlateCount >= maxVisiblePlates)
        {
            plateQueue.Enqueue(new QueuedPlate { dish = dish, ingredients = ingredientsForPlate });
            Debug.Log($"{dish.dishType} plate queued ({plateQueue.Count} in queue)");
            return;
        }

        SpawnPlate(dish, ingredientsForPlate);
    }

    private void SpawnPlate(DishConfig dish, List<GameObject> ingredients)
    {
        int plateIndex = activePlateCount;
        activePlateCount++;

        GameObject plateObject = Instantiate(dish.platedDishPrefab, servingArea);
        PlatedDish plate = plateObject.GetComponent<PlatedDish>();

        if (plate == null)
        {
            Debug.LogError($"{dish.dishType} prefab missing PlatedDish component!");
            Destroy(plateObject);
            activePlateCount--;
            return;
        }

        RectTransform plateRect = plateObject.GetComponent<RectTransform>();
        if (plateRect != null)
        {
            plateRect.anchoredPosition = plateSpawnOffset + plateSpacing * plateIndex;
            plateRect.localScale = Vector3.one;
        }

        foreach (GameObject ingredient in ingredients)
            plate.AddChicken(ingredient);

        Debug.Log($"✅ {dish.dishType} plate created! ({activePlateCount}/{maxVisiblePlates} active, {plateQueue.Count} queued)");
        PlayPlateCreationEffect(plateObject);
        TutorialEvents.DishPlated();
    }

    /// <summary>
    /// Called by PlatedDish when it is served to a customer. Frees a slot and spawns the next queued plate.
    /// </summary>
    public void NotifyPlateServed()
    {
        activePlateCount = Mathf.Max(0, activePlateCount - 1);

        if (plateQueue.Count > 0)
        {
            QueuedPlate next = plateQueue.Dequeue();
            SpawnPlate(next.dish, next.ingredients);
        }
    }

    /// <summary>
    /// Play visual/audio feedback when plate is created
    /// </summary>
    private void PlayPlateCreationEffect(GameObject plate)
    {
        // Add your effects here
        // Example: particle effects, sound, animation
        if (plateCompleteEffect != null)
        {
            ParticleSystem effect = Instantiate(plateCompleteEffect, plate.transform.position, Quaternion.identity, plate.transform);
            effect.Play();
        }

        // Note: Scale animation removed to prevent issues
        // If you want to add animation later, use DOTween or another animation system
    }

    /// <summary>
    /// Clear all waiting ingredients (for cleanup/reset)
    /// </summary>
    public void ClearWaitingIngredients()
    {
        foreach (DishConfig dish in supportedDishes)
        {
            dish.waitingIngredients.Clear();
        }
    }

    /// <summary>
    /// Destroys all active plates on screen, clears the queue, and resets state.
    /// Call this at the end of each day.
    /// </summary>
    public void ClearAllPlates()
    {
        // Destroy every PlatedDish currently on the serving area
        if (servingArea != null)
        {
            foreach (Transform child in servingArea)
            {
                Destroy(child.gameObject);
            }
        }

        plateQueue.Clear();
        activePlateCount = 0;
        ClearWaitingIngredients();
    }
}
