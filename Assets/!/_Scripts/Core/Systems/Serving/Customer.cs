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
        
        // Start moving to ordering point
        StartCoroutine(MoveToOrderingPoint());
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
    
    public void ReceiveOrder(GameObject deliveredFood)
    {
        if (currentState != CustomerState.Waiting) return;
        
        currentState = CustomerState.Receiving;
        
        // Check if order is correct
        bool isCorrect = ValidateOrder(deliveredFood);
        
        // Notify generator
        generator.OnCustomerOrderReceived(this, isCorrect);
        
        // Show satisfaction/dissatisfaction
        StartCoroutine(ReactToOrder(isCorrect));
    }
    
    private bool ValidateOrder(GameObject deliveredFood)
    {
        // Implement your order validation logic here
        // This is a simple name-based check, you can make it more sophisticated
        return deliveredFood.name.Contains(currentOrder.orderName);
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
        StartCoroutine(LeaveRestaurant());
    }
    
    private IEnumerator LeaveRestaurant()
    {
        yield return StartCoroutine(MoveToPoint(exitPoint));
        
        // Notify generator
        generator.OnCustomerLeaving(this);
        
        // Destroy customer
        Destroy(gameObject);
    }
    
    #endregion
}