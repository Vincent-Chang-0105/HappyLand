using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simplified End of Day UI for quick testing - doesn't require all fields
/// Shows basic report in console and displays a simple panel
/// </summary>
public class SimpleEndOfDayUI : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private GameObject endOfDayPanel;
    [SerializeField] private Button continueButton;

    [Header("Optional Text Display")]
    [SerializeField] private TextMeshProUGUI reportText;
    [SerializeField] private TextMeshProUGUI totalExpensesText;
    [SerializeField] private TextMeshProUGUI remainingCashText;

    [Header("Optional Expense Checkboxes")]
    [SerializeField] private Toggle electricityCheckbox;
    [SerializeField] private Toggle waterCheckbox;
    [SerializeField] private Toggle medicineCheckbox;
    [SerializeField] private Toggle repairsCheckbox;
    [SerializeField] private Toggle rentCheckbox;

    private int currentElectricity, currentWater, currentMedicine, currentRepairs, currentRent;

    private void Start()
    {
        // Subscribe to DayManager
        if (DayManager.Instance != null)
        {
            DayManager.Instance.OnDayEnded += OnDayEnded;
            Debug.Log("SimpleEndOfDayUI: Subscribed to DayManager");
        }
        else
        {
            Debug.LogError("SimpleEndOfDayUI: DayManager not found!");
        }

        // Setup button
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        // Hide panel
        if (endOfDayPanel != null)
        {
            endOfDayPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (DayManager.Instance != null)
        {
            DayManager.Instance.OnDayEnded -= OnDayEnded;
        }
    }

    private void OnDayEnded(int dayNumber)
    {
        Debug.Log($"=== DAY {dayNumber} ENDED ===");

        // Get data
        int sales = 0;
        int ingredientCost = 0;

        CustomerGenerator customerGen = FindObjectOfType<CustomerGenerator>();
        if (customerGen != null)
        {
            sales = customerGen.GetTodaySales();
        }

        if (MoneyManager.Instance != null)
        {
            ingredientCost = MoneyManager.Instance.GetTodayExpenses();
        }

        if (ExpenseManager.Instance != null)
        {
            currentElectricity = ExpenseManager.Instance.ElectricityCost;
            currentWater = ExpenseManager.Instance.WaterCost;
            currentMedicine = ExpenseManager.Instance.MedicineCost;
            currentRepairs = ExpenseManager.Instance.HouseRepairsCost;
            currentRent = ExpenseManager.Instance.RentCost;
        }

        // Setup checkboxes with listeners
        SetupCheckbox(electricityCheckbox, true);
        SetupCheckbox(waterCheckbox, true);
        SetupCheckbox(medicineCheckbox, currentMedicine > 0);
        SetupCheckbox(repairsCheckbox, currentRepairs > 0);
        SetupCheckbox(rentCheckbox, true);

        int totalExpenses = ingredientCost + currentElectricity + currentWater + currentMedicine + currentRepairs + currentRent;
        int currentMoney = MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : 0;

        // Build report
        string report = $"END OF DAY {dayNumber}\n\n" +
                       $"INCOME:\n" +
                       $"  Pagpag Sales: ₱{sales} {(sales >= 600 ? "✓" : "✗ (Min: ₱600)")}\n\n" +
                       $"EXPENSES:\n" +
                       $"  Ingredients: ₱{ingredientCost}\n" +
                       $"  Electricity: ₱{currentElectricity}\n" +
                       $"  Water: ₱{currentWater}\n" +
                       $"  Medicine: ₱{currentMedicine}\n" +
                       $"  Repairs: ₱{currentRepairs}\n" +
                       $"  Rent: ₱{currentRent}\n" +
                       $"  Total Expenses: ₱{totalExpenses}";

        Debug.Log(report);

        // Display on UI
        if (reportText != null)
        {
            reportText.text = report;
        }

        // Update totals display
        UpdateTotalsDisplay();

        // Show panel
        if (endOfDayPanel != null)
        {
            endOfDayPanel.SetActive(true);
            Debug.Log("SimpleEndOfDayUI: Panel shown");
        }
        else
        {
            Debug.LogError("SimpleEndOfDayUI: endOfDayPanel is NULL!");
        }

        // Pause game
        Time.timeScale = 0f;
    }

    private void SetupCheckbox(Toggle checkbox, bool defaultChecked)
    {
        if (checkbox != null)
        {
            checkbox.onValueChanged.RemoveAllListeners();
            checkbox.isOn = defaultChecked;
            checkbox.interactable = true;
            checkbox.onValueChanged.AddListener((_) => UpdateTotalsDisplay());
        }
    }

    private void UpdateTotalsDisplay()
    {
        if (MoneyManager.Instance == null) return;

        // Calculate selected expenses
        int selectedExpenses = 0;

        if (electricityCheckbox != null && electricityCheckbox.isOn)
            selectedExpenses += currentElectricity;
        if (waterCheckbox != null && waterCheckbox.isOn)
            selectedExpenses += currentWater;
        if (medicineCheckbox != null && medicineCheckbox.isOn)
            selectedExpenses += currentMedicine;
        if (repairsCheckbox != null && repairsCheckbox.isOn)
            selectedExpenses += currentRepairs;
        if (rentCheckbox != null && rentCheckbox.isOn)
            selectedExpenses += currentRent;

        // Update total expenses text
        if (totalExpensesText != null)
        {
            totalExpensesText.text = $"Total to Pay: ₱{selectedExpenses}";
        }

        // Update remaining cash
        if (remainingCashText != null)
        {
            int futureBalance = MoneyManager.Instance.CurrentMoney - selectedExpenses;
            remainingCashText.text = $"Remaining: ₱{futureBalance}";
        }
    }

    private void OnContinueClicked()
    {
        Debug.Log("Continue button clicked");

        // Get checkbox states
        bool payElectricity = electricityCheckbox != null && electricityCheckbox.isOn;
        bool payWater = waterCheckbox != null && waterCheckbox.isOn;
        bool payMedicine = medicineCheckbox != null && medicineCheckbox.isOn;
        bool payRepairs = repairsCheckbox != null && repairsCheckbox.isOn;
        bool payRent = rentCheckbox != null && rentCheckbox.isOn;

        // Apply selected expenses
        if (ExpenseManager.Instance != null)
        {
            ExpenseManager.Instance.ApplySelectedExpenses(payElectricity, payWater, payMedicine, payRepairs, payRent);
        }

        // Hide panel
        if (endOfDayPanel != null)
        {
            endOfDayPanel.SetActive(false);
        }

        // Resume game
        Time.timeScale = 1f;

        // Continue to next day
        if (DayManager.Instance != null)
        {
            DayManager.Instance.ContinueToNextDay();
        }
    }
}
