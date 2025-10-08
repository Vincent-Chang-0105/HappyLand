using UnityEngine;
using System.Collections.Generic;

public class Bowl : CookWare
{
    [Header("Bowl Properties")]
    [SerializeField] private float radius = 1.5f;
    [SerializeField] private Vector2 centerOffset = Vector2.zero;
    [SerializeField] private float depth = 0.5f;
    
    [Header("Food Management")]
    [SerializeField] private List<FoodParticle> containedFood = new List<FoodParticle>();
    [SerializeField] private int maxFoodCapacity = 50;
    
    [Header("Shake Settings")]
    [SerializeField] private float shakeThreshold = 0.05f;
    [SerializeField] private float shakeMultiplier = 5f;
    
    [Header("Visual")]
    [SerializeField] private bool showBowlBounds = true;
    [SerializeField] private Color bowlBoundsColor = Color.cyan;
    
    private CircleCollider2D bowlCollider;
    private Vector3 lastPosition;
    
    protected override void Start() // Override Start from CookWare
    {
        base.Start(); // Call parent Start first
        SetupBowlCollider();
        lastPosition = transform.position;
    }
    
    private void SetupBowlCollider()
    {
        bowlCollider = GetComponent<CircleCollider2D>();
        if (bowlCollider == null)
        {
            bowlCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        
        bowlCollider.radius = radius;
        bowlCollider.offset = centerOffset;
        bowlCollider.isTrigger = true;
        
        // Set layer for bowl detection
        if (gameObject.layer == 0) // If no layer set
        {
            gameObject.layer = LayerMask.NameToLayer("Bowl");
        }
    }
    
    protected virtual void Update() // Make it virtual in case we want to override later
    {
        base.Update(); // Call CookWare update for dragging
        
        // Check for movement and shake bowl contents
        CheckForMovement();
        
        // Clean up null references
        containedFood.RemoveAll(food => food == null);
    }
    
    private void CheckForMovement()
    {
        Vector3 deltaPosition = transform.position - lastPosition;
        
        if (deltaPosition.magnitude > shakeThreshold)
        {
            float shakeIntensity = deltaPosition.magnitude * shakeMultiplier;
            ShakeBowl(shakeIntensity);
        }
        
        lastPosition = transform.position;
    }
    
    public Vector2 GetCenter()
    {
        return (Vector2)transform.position + centerOffset;
    }
    
    public float GetRadius()
    {
        return radius * transform.localScale.x;
    }
    
    public void AddFood(FoodParticle food)
    {
        if (!containedFood.Contains(food) && containedFood.Count < maxFoodCapacity)
        {
            containedFood.Add(food);
            OnFoodAdded(food);
        }
    }
    
    public void RemoveFood(FoodParticle food)
    {
        if (containedFood.Contains(food))
        {
            containedFood.Remove(food);
            OnFoodRemoved(food);
        }
    }
    
    public List<FoodParticle> GetContainedFood()
    {
        return new List<FoodParticle>(containedFood);
    }
    
    public int GetFoodCount()
    {
        return containedFood.Count;
    }
    
    public bool IsFull()
    {
        return containedFood.Count >= maxFoodCapacity;
    }
    
    public void ShakeBowl(float intensity)
    {
        foreach (FoodParticle food in containedFood)
        {
            if (food != null)
            {
                Vector2 shakeForce = new Vector2(
                    Random.Range(-intensity, intensity),
                    Random.Range(0, intensity * 0.5f)
                );
                food.AddForce(shakeForce);
                
                // Add rotational shake
                float torque = Random.Range(-intensity * 100f, intensity * 100f);
                food.AddTorque(torque);
            }
        }
    }
    
    public void PourContents(Vector2 direction, float force)
    {
        foreach (FoodParticle food in containedFood)
        {
            if (food != null)
            {
                food.AddForce(direction.normalized * force);
            }
        }
    }
    
    private void OnFoodAdded(FoodParticle food)
    {
        Debug.Log($"Food {food.name} added to bowl {gameObject.name}. Total: {containedFood.Count}");
    }
    
    private void OnFoodRemoved(FoodParticle food)
    {
        Debug.Log($"Food {food.name} removed from bowl {gameObject.name}. Total: {containedFood.Count}");
    }
    
    private void OnDrawGizmos()
    {
        if (showBowlBounds)
        {
            Gizmos.color = bowlBoundsColor;
            Vector2 center = GetCenter();
            DrawWireCircle(center, GetRadius(), 64);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(center, 0.1f);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Vector2 center = GetCenter();
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(center + Vector2.up * (GetRadius() + 0.5f), 
            $"Food: {containedFood.Count}/{maxFoodCapacity}");
        #endif
    }

    // Helper method to draw a wire circle using Gizmos
    private void DrawWireCircle(Vector2 center, float radius, int segments = 32)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector2(Mathf.Cos(0), Mathf.Sin(0)) * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 nextPoint = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }
}