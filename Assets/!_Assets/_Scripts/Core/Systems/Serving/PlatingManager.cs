using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// Manages automatic plating of fried chickens.
/// Monitors frying completion and creates plated dishes when enough chickens are ready.
/// </summary>
public class PlatingManager : Singleton<PlatingManager>
{
    [Header("Plate Settings")]
    [SerializeField] private GameObject platedDishPrefab;
    [SerializeField] private Transform servingArea; // Where plates spawn in UI
    [SerializeField] private int chickensPerPlate = 3;

    [Header("Chicken Tracking")]
    [SerializeField] private List<GameObject> friedChickensWaitingToPlate = new List<GameObject>();

    [Header("Plate Spawning")]
    [SerializeField] private float plateSpawnDelay = 0.5f;
    [SerializeField] private Vector2 plateSpawnOffset = Vector2.zero;

    private void Start()
    {
        if (platedDishPrefab == null)
        {
            Debug.LogError("PlatingManager: No plated dish prefab assigned!", this);
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
        if (chicken == null) return;

        // Verify it's actually fried
        IFryable fryable = chicken.GetComponent<IFryable>();
        if (fryable == null || !fryable.IsFried())
        {
            Debug.LogWarning($"Attempted to register unfried chicken: {chicken.name}");
            return;
        }

        // Add to waiting list if not already there
        if (!friedChickensWaitingToPlate.Contains(chicken))
        {
            friedChickensWaitingToPlate.Add(chicken);
            Debug.Log($"Chicken registered for plating. Waiting chickens: {friedChickensWaitingToPlate.Count}/{chickensPerPlate}");

            // Check if we have enough for a plate
            CheckAndCreatePlate();
        }
    }

    /// <summary>
    /// Check if we have enough fried chickens to create a plate
    /// </summary>
    private void CheckAndCreatePlate()
    {
        // Remove any null references first
        friedChickensWaitingToPlate.RemoveAll(c => c == null);

        // Check if we have enough chickens
        if (friedChickensWaitingToPlate.Count >= chickensPerPlate)
        {
            Debug.Log($"Creating plate with {chickensPerPlate} fried chickens!");
            CreatePlate();
        }
    }

    /// <summary>
    /// Create a plated dish with the available fried chickens
    /// </summary>
    private void CreatePlate()
    {
        if (platedDishPrefab == null || servingArea == null)
        {
            Debug.LogError("Cannot create plate: missing prefab or serving area");
            return;
        }

        // Take the first N chickens from the waiting list
        List<GameObject> chickensForPlate = friedChickensWaitingToPlate
            .Take(chickensPerPlate)
            .ToList();

        if (chickensForPlate.Count < chickensPerPlate)
        {
            Debug.LogWarning($"Not enough chickens for plate: {chickensForPlate.Count}/{chickensPerPlate}");
            return;
        }

        // Spawn the plated dish
        GameObject plateObject = Instantiate(platedDishPrefab, servingArea);
        PlatedDish plate = plateObject.GetComponent<PlatedDish>();

        if (plate == null)
        {
            Debug.LogError("Plated dish prefab doesn't have PlatedDish component!");
            Destroy(plateObject);
            return;
        }

        // Apply spawn offset and ensure proper scale
        RectTransform plateRect = plateObject.GetComponent<RectTransform>();
        if (plateRect != null)
        {
            plateRect.anchoredPosition = plateSpawnOffset;
            plateRect.localScale = Vector3.one; // Ensure plate has proper scale
        }

        // Add chickens to the plate
        foreach (GameObject chicken in chickensForPlate)
        {
            plate.AddChicken(chicken);

            // Hide or destroy the individual chicken objects
            // Option 1: Destroy them
            Destroy(chicken);

            // Option 2: Hide them (uncomment if you prefer)
            // chicken.SetActive(false);
        }

        // Remove these chickens from the waiting list
        foreach (GameObject chicken in chickensForPlate)
        {
            friedChickensWaitingToPlate.Remove(chicken);
        }

        Debug.Log($"✅ Plate created! Remaining chickens waiting: {friedChickensWaitingToPlate.Count}");

        // Play plate creation effect (optional)
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
    /// Manually trigger plate creation (for testing/debugging)
    /// </summary>
    [ContextMenu("Force Create Plate")]
    public void ForceCreatePlate()
    {
        if (friedChickensWaitingToPlate.Count >= chickensPerPlate)
        {
            CreatePlate();
        }
        else
        {
            Debug.LogWarning($"Not enough chickens to force create plate: {friedChickensWaitingToPlate.Count}/{chickensPerPlate}");
        }
    }

    /// <summary>
    /// Clear all waiting chickens (for cleanup/reset)
    /// </summary>
    public void ClearWaitingChickens()
    {
        friedChickensWaitingToPlate.Clear();
        Debug.Log("Cleared all waiting chickens");
    }
}
