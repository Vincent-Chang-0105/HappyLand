using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;

public class OrderUIManager : MonoBehaviour
{

    [Header("UI References")]
    [SerializeField] private Transform orderButtonContainer; // Where order buttons appear
    [SerializeField] private GameObject orderButtonPrefab;   // Button prefab template
    [SerializeField] private List<RectTransform> orderButtonSlots = new List<RectTransform>(3); // Fixed slots for order buttons

    [Header("Ticket System")]
    [SerializeField] private TicketManager ticketManager;

    private Dictionary<Customer, OrderButton> activeOrderButtons = new Dictionary<Customer, OrderButton>();
    private Dictionary<Customer, GameObject> customerTicket = new Dictionary<Customer, GameObject>();
    private HashSet<Customer> customersWithOrders = new HashSet<Customer>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CustomerGenerator generator = FindFirstObjectByType<CustomerGenerator>();
            if (generator != null)
            {
                generator.OnCustomerSpawned += OnCustomerSpawned;
                generator.OnCustomerLeft += OnCustomerLeft;
            }
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnCustomerSpawned(Customer customer)
    {
        StartCoroutine(WaitForCustomerToReachOrderingPoint(customer));
        //CreateOrderButton(customer);
    }

    #region Courtines 
    IEnumerator WaitForCustomerToReachOrderingPoint(Customer customer)
    {
        while (customer != null && customer.CurrentState != CustomerState.Ordering)
        {
            yield return new WaitForSeconds(0.1f); // Check every 0.1 seconds
        }

        // Customer reached ordering point - now create the button
        if (customer != null)
        {
            CreateOrderButton(customer);
        }
    }
    #endregion

    void OnCustomerLeft(Customer customer)
    {
        RemoveOrderButton(customer);
        RemoveCustomerTicket(customer);
        customersWithOrders.Remove(customer);
    }
    
    int GetOrderingPointIndex(Customer customer)
    {
        // Get the customer's ordering point from CustomerGenerator
        CustomerGenerator generator = FindFirstObjectByType<CustomerGenerator>();
        return generator.GetOrderingPointIndex(customer.GetAssignedOrderingPoint());
    }
    
    void CreateOrderButton(Customer customer)
    {
        // Find which ordering point the customer is using
        int orderingPointIndex = GetOrderingPointIndex(customer);
        
        if (orderingPointIndex >= 0 && orderingPointIndex < orderButtonSlots.Count)
        {
            // Create button in the corresponding UI slot
            Transform targetSlot = orderButtonSlots[orderingPointIndex];
            GameObject buttonObj = Instantiate(orderButtonPrefab, targetSlot);
            
            // Center the button in its slot
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchoredPosition = Vector2.zero;
            
            // Setup the button
            OrderButton orderButton = buttonObj.GetComponent<OrderButton>();
            if (orderButton == null)
            {
                orderButton = buttonObj.AddComponent<OrderButton>();
            }
            
            orderButton.SetupOrderButton(customer, this);
            activeOrderButtons[customer] = orderButton;
        
        }
        else
        {
            Debug.LogError($"Invalid ordering point index: {orderingPointIndex}");
        }
    }

    void RemoveOrderButton(Customer customer)
    {
        if (activeOrderButtons.ContainsKey(customer))
        {
            OrderButton button = activeOrderButtons[customer];
            if (button != null)
            {
                Destroy(button.gameObject);
            }
            activeOrderButtons.Remove(customer);
        }
    }
    
    void RemoveCustomerTicket(Customer customer)
    {
        if (customerTicket.ContainsKey(customer))
        {
            GameObject ticket = customerTicket[customer];
            if (ticket != null && ticketManager != null)
            {
                ticketManager.RemoveTicket(ticket);
            }
            customerTicket.Remove(customer);
        }
    }
    
    // Called by OrderButton when clicked
    public void OnOrderButtonClicked(Customer customer)
    {
        if (customersWithOrders.Contains(customer))
        {
            return;
        }
        
        customersWithOrders.Add(customer);

        if (ticketManager != null)
        {
            GameObject spawnedTicket = ticketManager.SpawnTicket(customer.CurrentOrder);

            if (spawnedTicket != null)
            {
                customerTicket[customer] = spawnedTicket;
            }
        }

        if (activeOrderButtons.ContainsKey(customer))
        {
            OrderButton button = activeOrderButtons[customer];
            if (button != null)
            {
                button.GetComponent<Button>().interactable = false;
            }
        }
    }
    
    // Public methods for external access
    public int GetActiveOrderCount()
    {
        return activeOrderButtons.Count;
    }

    public List<Customer> GetActiveCustomers()
    {
        return activeOrderButtons.Keys.ToList();
    }

    /// <summary>
    /// Clears all active order buttons - called when day ends
    /// </summary>
    public void ClearAllOrderButtons()
    {
        // Destroy all order button GameObjects
        foreach (var kvp in activeOrderButtons)
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
            {
                Destroy(kvp.Value.gameObject);
            }
        }

        // Clear the dictionary
        activeOrderButtons.Clear();

        // Also clear customer tickets
        foreach (var kvp in customerTicket)
        {
            if (kvp.Value != null && ticketManager != null)
            {
                ticketManager.RemoveTicket(kvp.Value);
            }
        }
        customerTicket.Clear();

        // Clear customers with orders set
        customersWithOrders.Clear();

    }
}