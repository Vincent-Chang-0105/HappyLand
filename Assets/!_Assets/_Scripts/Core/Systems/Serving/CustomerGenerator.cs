using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CustomerGenerator : MonoBehaviour
{
    [Header("Customer Management")]
    [SerializeField] private List<GameObject> customerPrefabs = new List<GameObject>();
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private List<Transform> orderingPoints = new List<Transform>(3);
    [SerializeField] private Transform exitPoint;

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

        Debug.Log($"Initialized {occupiedOrderingPoints.Count} ordering points");
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
        // Initialize queue with numbers 1 to n (assuming you have NPC_1 to NPC_n)
        customerNameQueue.Clear();
        
        // If you have specific number of customer prefabs, use that count
        int customerCount = customerPrefabs.Count > 0 ? customerPrefabs.Count : 10; // fallback to 10
        
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
        if (customerPrefabs.Count == 0)
        {
            Debug.LogError("No customer prefabs assigned!");
            return;
        }

        Transform availablePoint = GetAvailableOrderingPoint();
        if(availablePoint == null)
        {
            Debug.LogWarning("No available ordering points for new customer.");
            return;
        }
        
        // Get random customer prefab
        GameObject customerPrefab = customerPrefabs[Random.Range(0, customerPrefabs.Count)];
        GameObject customerObj = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);
        
        // Setup customer
        Customer customer = customerObj.GetComponent<Customer>();
        if (customer == null)
        {
            customer = customerObj.AddComponent<Customer>();
        }
        
        // Assign name and setup
        int customerNumber = GetNextCustomerNumber();
        customer.SetupCustomer($"NPC_{customerNumber}", GenerateRandomOrder(), this);

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

        Debug.Log($"Spawned {customerNumber} at ordering point: {availablePoint.name}");
    }
    
    #endregion
    
    #region Order Generation
    
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
                Debug.Log($"MONEY EARNED! Customer {customer.CustomerName} paid {reward} PHP for correct order!");
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
            Debug.Log($"Freed ordering point: {customerPoint.name}");
        }
        
        activeCustomers.Remove(customer);
        OnCustomerLeft?.Invoke(customer);
        
        Debug.Log($"Customer {customer.CustomerName} left the restaurant");
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
        if (customerPrefabs.Count == 0)
        {
            Debug.LogError("No customer prefabs assigned!");
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

        // Get random customer prefab
        GameObject customerPrefab = customerPrefabs[Random.Range(0, customerPrefabs.Count)];
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
        customer.SetupCustomer($"NPC_{customerNumber}", customerOrder, this);

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

        Debug.Log($"[Tutorial] Spawned customer with specific order: {targetOrder.orderName}");
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
        Debug.Log("Daily sales tracking reset.");
    }
}