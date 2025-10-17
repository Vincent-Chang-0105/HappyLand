using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CustomerGenerator : MonoBehaviour
{
    [Header("Customer Management")]
    [SerializeField] private List<GameObject> customerPrefabs = new List<GameObject>();
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform orderingPoint;
    [SerializeField] private Transform exitPoint;
    
    [Header("Generation Settings")]
    [SerializeField] private float spawnInterval = 10f;
    [SerializeField] private int maxCustomers = 3;
    [SerializeField] private bool autoGenerate = true;
    
    [Header("Order Generation")]
    [SerializeField] private List<Order> availableOrders = new List<Order>();
    [SerializeField] private int minOrdersPerCustomer = 1;
    [SerializeField] private int maxOrdersPerCustomer = 3;
    
    // Runtime tracking
    private List<Customer> activeCustomers = new List<Customer>();
    private Queue<int> customerNameQueue = new Queue<int>();
    private int totalCustomersServed = 0;
    private bool isGenerating = false;
    
    // Events
    public System.Action<Customer> OnCustomerSpawned;
    public System.Action<Customer> OnCustomerLeft;
    public System.Action<Customer, bool> OnOrderCompleted; // customer, wasCorrect
    
    void Start()
    {
        InitializeCustomerQueue();
        
        if (autoGenerate)
        {
            StartCustomerGeneration();
        }
    }
    
    void Update()
    {
        // Clean up customers who have left
        activeCustomers.RemoveAll(customer => customer == null);
    }
    
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
        StopCoroutine(GenerateCustomersRoutine());
    }
    
    private IEnumerator GenerateCustomersRoutine()
    {
        while (isGenerating)
        {
            yield return new WaitForSeconds(spawnInterval);
            
            if (activeCustomers.Count < maxCustomers)
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
        customer.SetMovementPoints(spawnPoint, orderingPoint, exitPoint);
        
        // Track customer
        activeCustomers.Add(customer);
        totalCustomersServed++;
        
        // Notify listeners
        OnCustomerSpawned?.Invoke(customer);
        
        Debug.Log($"Spawned customer: NPC_{customerNumber} with order: {customer.CurrentOrder.orderName}");
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
        
        // For now, generate single random order
        // You can extend this to generate multiple orders per customer
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
        // Add to game score/money system here
        
        OnOrderCompleted?.Invoke(customer, wasCorrect);
        
        Debug.Log($"Customer {customer.CustomerName} order completed. Correct: {wasCorrect}, Reward: {reward}");
    }
    
    public void OnCustomerLeaving(Customer customer)
    {
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
    
    #endregion
}