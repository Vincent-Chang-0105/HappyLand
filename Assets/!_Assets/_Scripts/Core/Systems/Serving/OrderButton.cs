using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OrderButton : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Button button;
    [SerializeField] private Image orderImage;           // Shows order sprite
    [SerializeField] private TextMeshProUGUI customerNameText;
    [SerializeField] private TextMeshProUGUI orderNameText;
    [SerializeField] private Image patienceBar;          // Visual patience indicator
    
    // References
    private Customer assignedCustomer;
    private OrderUIManager uiManager;
    
    void Awake()
    {
        // Get components if not assigned
        if (button == null) button = GetComponent<Button>();
        if (orderImage == null) orderImage = GetComponentInChildren<Image>();
        
        // Setup button click listener
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
    }
    
    void Update()
    {
        // Update patience bar in real-time
        if (assignedCustomer != null && patienceBar != null)
        {
            UpdatePatienceBar();
        }
    }
    
    public void SetupOrderButton(Customer customer, OrderUIManager manager)
    {
        assignedCustomer = customer;
        uiManager = manager;
        
        // Setup visual elements
        UpdateButtonVisuals();
    }
    
    void UpdateButtonVisuals()
    {
        if (assignedCustomer == null) return;
        
        Order customerOrder = assignedCustomer.CurrentOrder;
        
        // Set order sprite
        if (orderImage != null && customerOrder.orderSprite != null)
        {
            orderImage.sprite = customerOrder.orderSprite;
        }
        
        // Set customer name
        if (customerNameText != null)
        {
            customerNameText.text = assignedCustomer.CustomerName;
        }
        
        // Set order name
        if (orderNameText != null)
        {
            orderNameText.text = customerOrder.orderName;
        }
        
        // Set order color theme
        if (orderImage != null)
        {
            orderImage.color = customerOrder.orderColor;
        }
    }
    
    void UpdatePatienceBar()
    {
        if (patienceBar != null && assignedCustomer != null)
        {
            float patiencePercent = assignedCustomer.PatiencePercentage;
            patienceBar.fillAmount = patiencePercent;
            
            // Change color based on patience level
            if (patiencePercent > 0.6f)
                patienceBar.color = Color.green;
            else if (patiencePercent > 0.3f)
                patienceBar.color = Color.yellow;
            else
                patienceBar.color = Color.red;
        }
    }
    
    void OnButtonClicked()
    {
        if (uiManager != null && assignedCustomer != null)
        {
            uiManager.OnOrderButtonClicked(assignedCustomer);
            //button.enabled = false;

            // Tutorial event
            TutorialEvents.OrderSlotClicked();
        }
    }
    
    public Customer GetAssignedCustomer()
    {
        return assignedCustomer;
    }
}