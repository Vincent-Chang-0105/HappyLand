using UnityEngine;

public class FoodParticle : MonoBehaviour
{
    [Header("Physics Settings")]
    [SerializeField] private float gravity = 9.8f;
    [SerializeField] private float bounce = 0.3f;
    [SerializeField] private float friction = 0.95f;
    [SerializeField] private float minVelocity = 0.1f;
    
    [Header("Rotation Settings")]
    [SerializeField] private bool enableRotation = true;
    [SerializeField] private float rotationSpeed = 180f; // degrees per second
    [SerializeField] private float maxRotationSpeed = 720f; // max degrees per second
    [SerializeField] private float rotationFriction = 0.98f;
    [SerializeField] private bool rotateBasedOnVelocity = true;
    
    [Header("Bowl Detection")]
    [SerializeField] private LayerMask bowlLayer = -1;
    [SerializeField] private float bowlDetectionRadius = 1f;
    
    private Vector3 velocity;
    private Vector2 previousPosition;
    private float angularVelocity; // Rotation speed in degrees per second
    private bool isInBowl = false;
    private Bowl currentBowl;
    private SpriteRenderer spriteRenderer;
    
    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        previousPosition = transform.position;
        
        // Add some initial random velocity for realistic movement
        velocity = new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-1f, 1f));
        
        // Add initial random rotation
        if (enableRotation)
        {
            angularVelocity = Random.Range(-rotationSpeed, rotationSpeed);
        }
    }
    
    private void Update()
    {
        CheckBowlProximity();
        ApplyPhysics();
        UpdatePosition();
        UpdateRotation();
    }
    
    private void UpdateRotation()
    {
        if (!enableRotation) return;
        
        if (rotateBasedOnVelocity)
        {
            // Rotate based on movement velocity
            float velocityMagnitude = velocity.magnitude;
            if (velocityMagnitude > 0.1f)
            {
                // Calculate rotation based on velocity direction and magnitude
                float targetAngularVelocity = velocityMagnitude * rotationSpeed;
                
                // Add some randomness to rotation direction
                if (velocity.x < 0) targetAngularVelocity = -targetAngularVelocity;
                
                // Clamp to max rotation speed
                targetAngularVelocity = Mathf.Clamp(targetAngularVelocity, -maxRotationSpeed, maxRotationSpeed);
                
                // Smoothly transition to target angular velocity
                angularVelocity = Mathf.Lerp(angularVelocity, targetAngularVelocity, Time.deltaTime * 5f);
            }
            else
            {
                // Apply friction to rotation when not moving much
                angularVelocity *= rotationFriction;
                
                // Stop very slow rotation
                if (Mathf.Abs(angularVelocity) < 10f)
                {
                    angularVelocity = 0f;
                }
            }
        }
        else
        {
            // Simple constant rotation with friction
            angularVelocity *= rotationFriction;
        }
        
        // Apply rotation
        transform.Rotate(0, 0, angularVelocity * Time.deltaTime);
    }
    
    private void CheckBowlProximity()
    {
        // Find nearby bowls
        Collider2D[] bowlColliders = Physics2D.OverlapCircleAll(transform.position, bowlDetectionRadius, bowlLayer);
        
        Bowl nearestBowl = null;
        float nearestDistance = float.MaxValue;
        
        foreach (var collider in bowlColliders)
        {
            Bowl bowl = collider.GetComponent<Bowl>();
            if (bowl != null)
            {
                float distance = Vector2.Distance(transform.position, bowl.GetCenter());
                if (distance < nearestDistance && distance <= bowl.GetRadius())
                {
                    nearestDistance = distance;
                    nearestBowl = bowl;
                }
            }
        }
        
        // Update bowl state
        if (nearestBowl != currentBowl)
        {
            if (currentBowl != null)
            {
                currentBowl.RemoveFood(this);
            }
            
            currentBowl = nearestBowl;
            isInBowl = currentBowl != null;
            
            if (currentBowl != null)
            {
                currentBowl.AddFood(this);
            }
        }
    }
    
    private void ApplyPhysics()
    {
        if (isInBowl && currentBowl != null)
        {
            ApplyBowlPhysics();
        }
        else
        {
            ApplyNormalPhysics();
        }
    }
    
    private void ApplyBowlPhysics()
    {
        Vector3 bowlCenter = currentBowl.GetCenter();
        Vector3 currentPos = transform.position;
        float bowlRadius = currentBowl.GetRadius();
        
        // Calculate distance from bowl center
        Vector3 directionFromCenter = currentPos - bowlCenter;
        float distanceFromCenter = directionFromCenter.magnitude;
        
        // Apply gravity towards bowl bottom
        Vector3 gravityForce = Vector3.forward * gravity * Time.deltaTime;
        velocity += gravityForce;
        
        // Bowl collision detection and response
        if (distanceFromCenter >= bowlRadius - 0.1f) // Near bowl edge
        {
            // Calculate collision normal (pointing inward)
            Vector2 collisionNormal = -directionFromCenter.normalized;
            
            // Reflect velocity off bowl wall
            velocity = Vector3.Reflect(velocity, collisionNormal) * bounce;
            
            // Add rotation from collision
            if (enableRotation)
            {
                float collisionForce = velocity.magnitude;
                angularVelocity += Random.Range(-collisionForce * 100f, collisionForce * 100f);
                angularVelocity = Mathf.Clamp(angularVelocity, -maxRotationSpeed, maxRotationSpeed);
            }
            
            // Push particle back inside bowl
            Vector2 correctedPosition = bowlCenter + directionFromCenter.normalized * (bowlRadius - 0.1f);
            transform.position = correctedPosition;
        }
        
        // Apply friction when moving slowly
        if (velocity.magnitude < minVelocity * 2f)
        {
            velocity *= friction;
        }
        
        // Stop very slow particles
        if (velocity.magnitude < minVelocity)
        {
            velocity = Vector3.zero;
        }
    }
    
    private void ApplyNormalPhysics()
    {
        // Simple gravity when not in bowl
        velocity += Vector3.forward * gravity * Time.deltaTime;
        
        // Simple ground collision (you can adjust this based on your scene)
        if (transform.position.z < -5f)
        {
            velocity.z = Mathf.Abs(velocity.z) * bounce;
            transform.position = new Vector3(transform.position.x, transform.position.x, 5f);
            
            // Add rotation from ground bounce
            if (enableRotation)
            {
                angularVelocity += Random.Range(-200f, 200f);
            }
        }
    }
    
    private void UpdatePosition()
    {
        previousPosition = transform.position;
        transform.position += (Vector3)(velocity * Time.deltaTime);
    }
    
    public void AddForce(Vector3 force)
    {
        velocity += force;
        
        // Add rotational force based on linear force
        if (enableRotation)
        {
            float rotationalForce = force.magnitude * Random.Range(-50f, 50f);
            angularVelocity += rotationalForce;
            angularVelocity = Mathf.Clamp(angularVelocity, -maxRotationSpeed, maxRotationSpeed);
        }
    }
    
    public void AddTorque(float torque)
    {
        if (enableRotation)
        {
            angularVelocity += torque;
            angularVelocity = Mathf.Clamp(angularVelocity, -maxRotationSpeed, maxRotationSpeed);
        }
    }
    
    public void SetVelocity(Vector3 newVelocity)
    {
        velocity = newVelocity;
    }
    
    public void SetAngularVelocity(float newAngularVelocity)
    {
        angularVelocity = newAngularVelocity;
    }
    
    public Vector3 GetVelocity()
    {
        return velocity;
    }
    
    public float GetAngularVelocity()
    {
        return angularVelocity;
    }
    
    public bool IsInBowl()
    {
        return isInBowl;
    }
    
    public Bowl GetCurrentBowl()
    {
        return currentBowl;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, bowlDetectionRadius);
        
        if (velocity != Vector3.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, velocity);
        }
        
        // Show rotation direction
        if (enableRotation && angularVelocity != 0)
        {
            Gizmos.color = Color.blue;
            Vector3 rotationIndicator = transform.right * 0.5f;
            if (angularVelocity < 0) rotationIndicator = -rotationIndicator;
            Gizmos.DrawRay(transform.position, rotationIndicator);
        }
    }
}