using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class DayMenuUnlock
{
    [Tooltip("Orders become available starting from this day")]
    public int unlockDay = 1;
    public List<Order> orders = new List<Order>();
}

public class CustomerGenerator : MonoBehaviour
{
    [Header("Customer Management")]
    [SerializeField] private GameObject customerPrefab; // Single customer prefab
    [SerializeField] private List<FacialExpressionSet> npcExpressionSets = new List<FacialExpressionSet>(); // List of NPC expression sets
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private List<Transform> orderingPoints = new List<Transform>(3);
    [SerializeField] private Transform exitPoint;
    private HashSet<FacialExpressionSet> usedExpressionSets = new HashSet<FacialExpressionSet>();

    //List of all active customers
    private Dictionary<Transform, Customer> occupiedOrderingPoints = new Dictionary<Transform, Customer>();
    private List<Customer> activeCustomers = new List<Customer>();
    
    [Header("Generation Settings")]
    [SerializeField] private float spawnInterval;
    [SerializeField] private int maxCustomers = 3;
    [SerializeField] private bool autoGenerate = true;
    
    [Header("Order Generation")]
    [SerializeField] private List<Order> availableOrders = new List<Order>();
    [SerializeField] private int minOrdersPerCustomer = 1;
    [SerializeField] private int maxOrdersPerCustomer = 3;

    [Header("Menu Unlocks (by Day)")]
    [Tooltip("Orders added to the menu on a specific day. All unlocks up to the current day are combined.")]
    [SerializeField] private List<DayMenuUnlock> menuUnlocks = new List<DayMenuUnlock>();
    
    // Runtime tracking
    private Queue<int> customerNameQueue = new Queue<int>();
    private int totalCustomersServed = 0;
    private bool isGenerating = false;

    [Header("Daily Sales Tracking")]
    [SerializeField] private int todaySalesIncome = 0;

    // Events
    public System.Action<Customer> OnCustomerSpawned;
    public System.Action<Customer> OnCustomerLeft;
    public System.Action<Customer, bool> OnOrderCompleted; // customer, wasCorrect
    
    [Header("Tutorial Settings")]
    [SerializeField] private bool disableAutoGenerateInTutorial = true;

    void Start()
    {
        InitializeOrderPoints();
        InitializeCustomerQueue();

        // Subscribe to day changes so the menu updates each day
        if (DayManager.Instance != null)
        {
            DayManager.Instance.OnDayStarted += ApplyMenuUnlocks;
            ApplyMenuUnlocks(DayManager.Instance.CurrentDay);
        }

        // Don't auto-generate if tutorial is active
        if (autoGenerate)
        {
            bool tutorialActive = TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive;

            if (tutorialActive && disableAutoGenerateInTutorial)
            {
                Debug.Log("CustomerGenerator: Auto-generation disabled during tutorial.");
            }
            else
            {
                StartCustomerGeneration();
            }
        }
    }

    void OnDestroy()
    {
        if (DayManager.Instance != null)
            DayManager.Instance.OnDayStarted -= ApplyMenuUnlocks;
    }

    void Update()
    {
        // Clean up customers who have left
        activeCustomers.RemoveAll(customer => customer == null);

        List<Transform> pointsToFree = new List<Transform>();
        foreach (var kvp in occupiedOrderingPoints)
        {
            if (kvp.Value != null && kvp.Value.gameObject == null)
            {
                pointsToFree.Add(kvp.Key);
            }
        }
        
        foreach (Transform point in pointsToFree)
        {
            occupiedOrderingPoints[point] = null;
        }
    }
    #region Spawn Point Setup
    private void InitializeOrderPoints()
    {
        occupiedOrderingPoints.Clear();
        foreach (Transform orderingPoint in orderingPoints)
        {
            if (orderingPoint != null)
            {
                occupiedOrderingPoints[orderingPoint] = null;
            }
        }

        //Debug.Log($"Initialized {occupiedOrderingPoints.Count} ordering points");
    }

    private bool HasAvailableOrderingPoint()
    {
        foreach (var kvp in occupiedOrderingPoints)
        {
            if (kvp.Value == null) // Point is available
            {
                return true;
            }
        }
        return false;
    }

    private Transform GetAvailableOrderingPoint()
    {
        foreach (var kvp in occupiedOrderingPoints)
        {
            if (kvp.Value == null) // Point is available
            {
                return kvp.Key;
            }
        }
        return null; // No available points
    }

    private Transform GetCustomerOrderingPoint(Customer customer)
    {
        foreach (var kvp in occupiedOrderingPoints)
        {
            if (kvp.Value == customer)
            {
                return kvp.Key;
            }
        }
        return null;
    }
            
    #endregion
    #region Customer Queue Management
    
    private void InitializeCustomerQueue()
    {
        // Initialize queue with numbers 1 to n (based on number of NPC expression sets)
        customerNameQueue.Clear();

        // Use number of expression sets as the customer count
        int customerCount = npcExpressionSets.Count > 0 ? npcExpressionSets.Count : 10; // fallback to 10

        for (int i = 1; i <= customerCount; i++)
        {
            customerNameQueue.Enqueue(i);
        }
    }
    
    private int GetNextCustomerNumber()
    {
        if (customerNameQueue.Count == 0)
        {
            InitializeCustomerQueue(); // Refill queue when empty
        }

        return customerNameQueue.Dequeue();
    }

    private FacialExpressionSet PickExpressionSet()
    {
        List<FacialExpressionSet> available = npcExpressionSets
            .Where(set => !usedExpressionSets.Contains(set))
            .ToList();

        // Fallback: all sets currently in use (more customers than sets) — pick randomly but warn
        if (available.Count == 0)
        {
            Debug.LogWarning("CustomerGenerator: All expression sets in use — duplicate sprite unavoidable. Add more FacialExpressionSets.");
            available = new List<FacialExpressionSet>(npcExpressionSets);
        }

        FacialExpressionSet chosen = available[Random.Range(0, available.Count)];
        usedExpressionSets.Add(chosen);
        return chosen;
    }

    #endregion
    #region Customer Generation
    
    public void StartCustomerGeneration()
    {
        if (!isGenerating)
        {
            isGenerating = true;
            StartCoroutine(GenerateCustomersRoutine());
        }
    }
    
    public void StopCustomerGeneration()
    {
        isGenerating = false;
        StopAllCoroutines();
    }
    
    private IEnumerator GenerateCustomersRoutine()
    {
        while (isGenerating)
        {
            yield return new WaitForSeconds(spawnInterval);
            
            if (HasAvailableOrderingPoint())
            {
                SpawnCustomer();
            }
        }
    }
    
    public void SpawnCustomer()
    {
        if (customerPrefab == null)
        {
            Debug.LogError("No customer prefab assigned!");
            return;
        }

        if (npcExpressionSets.Count == 0)
        {
            Debug.LogError("No NPC expression sets assigned!");
            return;
        }

        Transform availablePoint = GetAvailableOrderingPoint();
        if(availablePoint == null)
        {
            Debug.LogWarning("No available ordering points for new customer.");
            return;
        }

        FacialExpressionSet expressionSet = PickExpressionSet();

        // Instantiate single customer prefab
        GameObject customerObj = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);

        // Setup customer
        Customer customer = customerObj.GetComponent<Customer>();
        if (customer == null)
        {
            customer = customerObj.AddComponent<Customer>();
        }

        // Assign name and setup
        int customerNumber = GetNextCustomerNumber();
        customer.SetupCustomer($"NPC_{customerNumber}", GenerateRandomOrder(), this, expressionSet);

        // Set movement points
        customer.SetMovementPoints(spawnPoint, availablePoint, exitPoint);

        // Mark ordering point as occupied
        occupiedOrderingPoints[availablePoint] = customer;

        // Track customer
        activeCustomers.Add(customer);
        totalCustomersServed++;

        // Notify listeners
        OnCustomerSpawned?.Invoke(customer);

        // Tutorial event
        TutorialEvents.CustomerArrived();

        //Debug.Log($"Spawned {customerNumber} with expression set from NPC_{expressionSet.npcNumber} at ordering point: {availablePoint.name}");
    }
    
    #endregion
    
    #region Order Generation

    private void ApplyMenuUnlocks(int day)
    {
        if (menuUnlocks == null || menuUnlocks.Count == 0) return;

        availableOrders.Clear();
        foreach (DayMenuUnlock unlock in menuUnlocks)
        {
            if (unlock.unlockDay <= day)
            {
                foreach (Order order in unlock.orders)
                {
                    if (order != null && !availableOrders.Contains(order))
                        availableOrders.Add(order);
                }
            }
        }
    }

    private Order GenerateRandomOrder()
    {
        if (availableOrders.Count == 0)
        {
            Debug.LogError("No available orders configured!");
            return null;
        }
        
        Order selectedOrder = availableOrders[Random.Range(0, availableOrders.Count)];
        
        // Create a copy so each customer has their own instance
        Order customerOrder = Instantiate(selectedOrder);
        customerOrder.StartOrder();
        
        return customerOrder;
    }
    
    // Future expansion: Generate multiple orders per customer
    private List<Order> GenerateMultipleOrders()
    {
        List<Order> orders = new List<Order>();
        int orderCount = Random.Range(minOrdersPerCustomer, maxOrdersPerCustomer + 1);
        
        for (int i = 0; i < orderCount; i++)
        {
            orders.Add(GenerateRandomOrder());
        }
        
        return orders;
    }
    
    #endregion
    
    #region Customer Callbacks
    
    public void OnCustomerOrderReceived(Customer customer, bool wasCorrect)
    {
        customer.CurrentOrder.CompleteOrder(wasCorrect);

        // Handle rewards/penalties
        int reward = customer.CurrentOrder.GetReward();

        // Add money to player's balance
        if (wasCorrect)
        {
            if (MoneyManager.Instance != null)
            {
                MoneyManager.Instance.AddMoney(reward);
                todaySalesIncome += reward; // Track daily sales
                //Debug.Log($"MONEY EARNED! Customer {customer.CustomerName} paid {reward} PHP for correct order!");
            }
            else
            {
                Debug.LogError("MoneyManager not found! Cannot award money.");
            }
        }
        else
        {
            Debug.LogWarning($"❌ No money earned. Customer {customer.CustomerName} received wrong order.");
        }

        OnOrderCompleted?.Invoke(customer, wasCorrect);

        Debug.Log($"Customer {customer.CustomerName} order completed. Correct: {wasCorrect}, Reward: {reward}");
    }
    
    public void OnCustomerLeaving(Customer customer)
    {
        Transform customerPoint = GetCustomerOrderingPoint(customer);
        if (customerPoint != null)
        {
            occupiedOrderingPoints[customerPoint] = null; // Free up ordering point
            //Debug.Log($"Freed ordering point: {customerPoint.name}");
        }

        FacialExpressionSet customerSet = customer.GetExpressionSet();
        if (customerSet != null)
        {
            usedExpressionSets.Remove(customerSet);
        }
        
        activeCustomers.Remove(customer);
        OnCustomerLeft?.Invoke(customer);
        
        //Debug.Log($"Customer {customer.CustomerName} left the restaurant");
    }
    
    #endregion
    
    #region Public Interface
    
    public List<Customer> GetActiveCustomers()
    {
        return new List<Customer>(activeCustomers);
    }
    
    public int GetTotalCustomersServed()
    {
        return totalCustomersServed;
    }

    public void ForceSpawnCustomer()
    {
        if (activeCustomers.Count < maxCustomers)
        {
            SpawnCustomer();
        }
    }

    /// <summary>
    /// Spawns a customer with a specific order (used for tutorial)
    /// </summary>
    /// <param name="orderName">The name of the order to assign (e.g., "Fried Chicken")</param>
    public void SpawnCustomerWithSpecificOrder(string orderName)
    {
        if (customerPrefab == null)
        {
            Debug.LogError("No customer prefab assigned!");
            return;
        }

        if (npcExpressionSets.Count == 0)
        {
            Debug.LogError("No NPC expression sets assigned!");
            return;
        }

        Transform availablePoint = GetAvailableOrderingPoint();
        if (availablePoint == null)
        {
            Debug.LogWarning("No available ordering points for new customer.");
            return;
        }

        // Find the matching order
        Order targetOrder = null;
        foreach (Order order in availableOrders)
        {
            if (order.orderName.ToLower().Contains(orderName.ToLower()) ||
                orderName.ToLower().Contains(order.orderName.ToLower()))
            {
                targetOrder = order;
                break;
            }
        }

        if (targetOrder == null)
        {
            Debug.LogError($"Could not find order matching '{orderName}' in available orders!");
            return;
        }

        FacialExpressionSet expressionSet = PickExpressionSet();

        // Instantiate single customer prefab
        GameObject customerObj = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);

        // Setup customer
        Customer customer = customerObj.GetComponent<Customer>();
        if (customer == null)
        {
            customer = customerObj.AddComponent<Customer>();
        }

        // Create a copy of the specific order
        Order customerOrder = Instantiate(targetOrder);
        customerOrder.StartOrder();

        // Assign name and setup with specific order
        int customerNumber = GetNextCustomerNumber();
        customer.SetupCustomer($"NPC_{customerNumber}", customerOrder, this, expressionSet);

        // Set movement points
        customer.SetMovementPoints(spawnPoint, availablePoint, exitPoint);

        // Mark ordering point as occupied
        occupiedOrderingPoints[availablePoint] = customer;

        // Track customer
        activeCustomers.Add(customer);
        totalCustomersServed++;

        // Notify listeners
        OnCustomerSpawned?.Invoke(customer);

        // Tutorial event
        TutorialEvents.CustomerArrived();

        //Debug.Log($"[Tutorial] Spawned customer with specific order: {targetOrder.orderName} and expression set from NPC_{expressionSet.npcNumber}");
    }

    #endregion

    public int GetOrderingPointIndex(Transform orderingPoint)
    {
        for (int i = 0; i < orderingPoints.Count; i++)
        {
            if (orderingPoints[i] == orderingPoint)
            {
                return i;
            }
        }
        return -1; // Not found
    }

    /// <summary>
    /// Gets total sales income for today
    /// </summary>
    public int GetTodaySales()
    {
        return todaySalesIncome;
    }

    /// <summary>
    /// Resets daily sales tracking (called at start of new day)
    /// </summary>
    public void ResetDailySales()
    {
        todaySalesIncome = 0;
        usedExpressionSets.Clear();
    }
}