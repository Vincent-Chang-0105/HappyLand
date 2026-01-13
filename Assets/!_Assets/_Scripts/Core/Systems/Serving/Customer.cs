using UnityEngine;
using DG.Tweening;
using System.Collections;

public enum CustomerState
{
    Moving,
    Ordering,
    Waiting,
    Receiving,
    Leaving,
    Angry // when order takes too long
}

public class Customer : MonoBehaviour
{
    [Header("Customer Info")]
    [SerializeField] private string customerName;
    [SerializeField] private Order currentOrder;
    [SerializeField] private CustomerState currentState = CustomerState.Moving;
    
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float rotationSpeed = 5f;
    
    [Header("Patience")]
    [SerializeField] private float maxPatienceTime = 120f; // 2 minutes
    [SerializeField] private float currentPatience;
    
    // Movement points
    private Transform spawnPoint;
    private Transform orderingPoint;
    private Transform exitPoint;
    
    // References
    private CustomerGenerator generator;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    
    // Properties
    public string CustomerName => customerName;
    public Order CurrentOrder => currentOrder;
    public CustomerState CurrentState => currentState;
    public float PatiencePercentage => currentPatience / maxPatienceTime;
    private Transform assignedOrderingPoint;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }
    
    void Update()
    {
        if (currentState == CustomerState.Waiting || currentState == CustomerState.Ordering)
        {
            UpdatePatience();
        }
    }
    
    #region Setup
    
    public void SetupCustomer(string name, Order order, CustomerGenerator gen)
    {
        customerName = name;
        currentOrder = order;
        generator = gen;
        currentPatience = maxPatienceTime;
        
        // Set name on GameObject for easy identification
        gameObject.name = customerName;
    }

    public void SetMovementPoints(Transform spawn, Transform ordering, Transform exit)
    {
        spawnPoint = spawn;
        orderingPoint = ordering;
        exitPoint = exit;
        assignedOrderingPoint = ordering;

        // Start moving to ordering point
        StartCoroutine(MoveToOrderingPoint());
    }
    
    public Transform GetAssignedOrderingPoint()
    {
        return assignedOrderingPoint;
    }
    
    #endregion
    
    #region Movement
    
    private IEnumerator MoveToOrderingPoint()
    {
        currentState = CustomerState.Moving;
        yield return StartCoroutine(MoveToPoint(orderingPoint));
        
        currentState = CustomerState.Ordering;
        StartOrdering();
    }
    
    private IEnumerator MoveToPoint(Transform target)
    {
        while (Vector3.Distance(transform.position, target.position) > 0.1f)
        {
            // Move towards target
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
            
            // Rotate towards movement direction
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            yield return null;
        }
        
        transform.position = target.position;
    }
    
    #endregion
    
    #region Ordering Behavior
    
    private void StartOrdering()
    {
        currentState = CustomerState.Ordering;
        
        // Show order UI or speech bubble here
        DisplayOrder();
        
        // Wait a moment then start waiting
        StartCoroutine(WaitForOrder());
    }
    
    private void DisplayOrder()
    {
        // This would show the order UI/speech bubble
        Debug.Log($"{customerName} wants: {currentOrder.orderName}");
    }
    
    private IEnumerator WaitForOrder()
    {
        yield return new WaitForSeconds(2f); // Time to "place order"
        currentState = CustomerState.Waiting;
    }
    
    #endregion
    
    #region Order Handling
    
    public bool ReceiveOrder(GameObject deliveredFood)
    {
        if (currentState != CustomerState.Waiting)
        {
            Debug.LogWarning($"Customer {name} is not waiting for order (current state: {currentState})");
            return false;
        }

        currentState = CustomerState.Receiving;

        // Check if order is correct
        bool isCorrect = ValidateOrder(deliveredFood);

        // Notify generator
        generator.OnCustomerOrderReceived(this, isCorrect);

        // Show satisfaction/dissatisfaction
        StartCoroutine(ReactToOrder(isCorrect));

        return isCorrect;
    }

    private bool ValidateOrder(GameObject deliveredFood)
    {
        if (deliveredFood == null)
        {
            Debug.LogWarning("Customer received null food!");
            return false;
        }

        // Check if it's a plated dish
        PlatedDish plate = deliveredFood.GetComponent<PlatedDish>();
        if (plate != null)
        {
            // Validate the plated dish
            Debug.Log($"Customer received plated dish with {plate.ChickenCount} ingredients");

            // Check if plate is complete and valid
            if (!plate.ValidateDish())
            {
                Debug.LogWarning("Customer rejected incomplete or invalid plate!");
                return false;
            }

            // Flexible order matching
            string orderLower = currentOrder.orderName.ToLower();
            string dishLower = plate.DishName.ToLower();

            // Direct match or substring match
            if (dishLower.Contains(orderLower) || orderLower.Contains(dishLower))
            {
                Debug.Log($"✅ Customer accepted plated dish: {plate.DishName}");
                return true;
            }

            // Specific dish matching
            if (orderLower.Contains("sinigang") && dishLower.Contains("sinigang"))
            {
                Debug.Log($"✅ Customer accepted sinigang dish");
                return true;
            }

            if (orderLower.Contains("fried") && dishLower.Contains("fried"))
            {
                Debug.Log($"✅ Customer accepted fried chicken");
                return true;
            }

            if (orderLower.Contains("chicken") && dishLower.Contains("chicken"))
            {
                Debug.Log($"✅ Customer accepted chicken dish");
                return true;
            }

            Debug.LogWarning($"❌ Dish name '{plate.DishName}' doesn't match order '{currentOrder.orderName}'");
            return false;
        }

        // Fallback: Original name-based check for other food items
        bool matches = deliveredFood.name.Contains(currentOrder.orderName);
        Debug.Log($"Customer validation (name check): {deliveredFood.name} vs {currentOrder.orderName} = {matches}");
        return matches;
    }
    
    private IEnumerator ReactToOrder(bool wasCorrect)
    {
        // Play animation/show reaction
        if (wasCorrect)
        {
            Debug.Log($"{customerName} is happy with their order!");
            // Play happy animation
        }
        else
        {
            Debug.Log($"{customerName} is disappointed with their order!");
            // Play disappointed animation
        }
        
        yield return new WaitForSeconds(1f);
        
        // Leave restaurant
        StartLeaving();
    }
    
    /// <summary>
    /// Check if customer is currently waiting for their order
    /// </summary>
    public bool IsWaitingForOrder()
    {
        return currentState == CustomerState.Waiting;
    }

    #endregion

    #region Patience System
    
    private void UpdatePatience()
    {
        currentPatience -= Time.deltaTime;
        
        if (currentPatience <= 0)
        {
            BecomeAngry();
        }
    }
    
    private void BecomeAngry()
    {
        currentState = CustomerState.Angry;
        Debug.Log($"{customerName} got angry and left!");
        
        // Notify generator of failed order
        generator.OnCustomerOrderReceived(this, false);
        
        StartLeaving();
    }
    
    #endregion
    
    #region Leaving
    
    private void StartLeaving()
    {
        currentState = CustomerState.Leaving;
        generator.OnCustomerLeaving(this);
        StartCoroutine(LeaveRestaurant());
    }
    
    private IEnumerator LeaveRestaurant()
    {
        yield return StartCoroutine(MoveToPoint(exitPoint));
        
        // Notify generator
        //generator.OnCustomerLeaving(this);
        
        // Destroy customer
        Destroy(gameObject);
    }
    
    #endregion
}