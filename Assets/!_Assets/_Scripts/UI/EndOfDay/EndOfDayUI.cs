using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class EndOfDayUI : MonoBehaviour
{
    [Header("UI Panel")]
    [SerializeField] private GameObject endOfDayPanel;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI dayNumberText;
    [SerializeField] private TextMeshProUGUI salesIncomeText;
    [SerializeField] private TextMeshProUGUI ingredientCostText;
    [SerializeField] private TextMeshProUGUI electricityText;
    [SerializeField] private TextMeshProUGUI waterText;
    [SerializeField] private TextMeshProUGUI medicineText;
    [SerializeField] private TextMeshProUGUI repairsText;
    [SerializeField] private TextMeshProUGUI rentText;
    [SerializeField] private TextMeshProUGUI totalExpensesText;
    [SerializeField] private TextMeshProUGUI remainingCashText;

    [Header("Checkboxes")]
    [SerializeField] private Toggle electricityCheckbox;
    [SerializeField] private Toggle waterCheckbox;
    [SerializeField] private Toggle medicineCheckbox;
    [SerializeField] private Toggle repairsCheckbox;
    [SerializeField] private Toggle rentCheckbox;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.red;
    [SerializeField] private Color positiveColor = Color.green;
    [SerializeField] private Color negativeColor = Color.red;

    [Header("Continue Button")]
    [SerializeField] private Button continueButton;

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    private DailyStatistics currentReport;
    private const int MINIMUM_SALES = 600;

    private void Start()
    {
        // Subscribe to DayManager events
        if (DayManager.Instance != null)
        {
            DayManager.Instance.OnDayEnded += ShowEndOfDayReport;
            Debug.Log("EndOfDayUI: Successfully subscribed to DayManager.OnDayEnded event");
        }
        else
        {
            Debug.LogError("EndOfDayUI: DayManager.Instance is NULL! Cannot subscribe to events.");
        }

        // Setup continue button
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
        else
        {
            Debug.LogWarning("EndOfDayUI: Continue button not assigned!");
        }

        // Hide panel initially
        if (endOfDayPanel != null)
        {
            endOfDayPanel.SetActive(false);
            Debug.Log("EndOfDayUI: Panel hidden initially");
        }
        else
        {
            Debug.LogError("EndOfDayUI: endOfDayPanel reference is NULL!");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (DayManager.Instance != null)
        {
            DayManager.Instance.OnDayEnded -= ShowEndOfDayReport;
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinueClicked);
        }
    }

    private void ShowEndOfDayReport(int dayNumber)
    {
        Debug.Log($"EndOfDayUI: ShowEndOfDayReport called for Day {dayNumber}");

        // Generate report
        currentReport = GenerateDailyReport(dayNumber);

        // Display report
        DisplayReport(currentReport);

        // Show panel with animation
        if (endOfDayPanel != null)
        {
            endOfDayPanel.SetActive(true);
            Debug.Log("EndOfDayUI: Panel activated and showing");

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, fadeInDuration).SetUpdate(true); // SetUpdate(true) to work with Time.timeScale = 0
            }
        }
        else
        {
            Debug.LogError("EndOfDayUI: Cannot show panel - endOfDayPanel is NULL!");
        }

        // Pause game or stop time (optional)
        Time.timeScale = 0f; // Pause game during report
        Debug.Log("EndOfDayUI: Game paused (Time.timeScale = 0)");
    }

    private DailyStatistics GenerateDailyReport(int dayNumber)
    {
        DailyStatistics report = new DailyStatistics(dayNumber);

        // Get sales income from CustomerGenerator
        CustomerGenerator customerGen = FindObjectOfType<CustomerGenerator>();
        if (customerGen != null)
        {
            report.SetIncome(customerGen.GetTodaySales());
        }

        // Get ingredient costs from MoneyManager
        if (MoneyManager.Instance != null)
        {
            report.SetIngredientCosts(MoneyManager.Instance.GetTodayExpenses());
        }

        // Get expenses from ExpenseManager
        if (ExpenseManager.Instance != null)
        {
            report.SetExpenses(
                ExpenseManager.Instance.ElectricityCost,
                ExpenseManager.Instance.WaterCost,
                ExpenseManager.Instance.MedicineCost,
                ExpenseManager.Instance.HouseRepairsCost,
                ExpenseManager.Instance.RentCost
            );
        }

        return report;
    }

    private void DisplayReport(DailyStatistics report)
    {
        // Day number
        if (dayNumberText != null)
        {
            dayNumberText.text = $"End of Day {report.dayNumber}";
        }

        // Income
        if (salesIncomeText != null)
        {
            salesIncomeText.text = $"₱{report.totalSalesIncome}";

            // Color based on minimum sales
            if (report.MetMinimumSales(MINIMUM_SALES))
            {
                salesIncomeText.color = positiveColor;
            }
            else
            {
                salesIncomeText.color = warningColor;
            }
        }

        // Ingredient costs
        if (ingredientCostText != null)
        {
            ingredientCostText.text = $"₱{report.totalIngredientCosts}";
        }

        // Fixed expenses - interactive checkboxes
        UpdateExpenseDisplay(electricityText, electricityCheckbox, report.electricityCost, true, true);
        UpdateExpenseDisplay(waterText, waterCheckbox, report.waterCost, true, true);
        UpdateExpenseDisplay(rentText, rentCheckbox, report.rentCost, true, true);

        // Variable expenses - interactive checkboxes (checked by default if cost > 0)
        UpdateExpenseDisplay(medicineText, medicineCheckbox, report.medicineCost, report.medicineCost > 0, true);
        UpdateExpenseDisplay(repairsText, repairsCheckbox, report.houseRepairsCost, report.houseRepairsCost > 0, true);

        // Subscribe to checkbox changes to update totals
        SetupCheckboxListeners();

        // Calculate initial totals
        UpdateTotals();
    }

    private void UpdateExpenseDisplay(TextMeshProUGUI text, Toggle checkbox, int cost, bool isChecked, bool isInteractable)
    {
        if (text != null)
        {
            text.text = $"+ ₱{cost}";
        }

        if (checkbox != null)
        {
            // Remove old listeners to avoid duplicates
            checkbox.onValueChanged.RemoveAllListeners();

            checkbox.isOn = isChecked;
            checkbox.interactable = isInteractable;

            // Add listener to update totals when toggled
            checkbox.onValueChanged.AddListener((_) => UpdateTotals());
        }
    }

    private void SetupCheckboxListeners()
    {
        // Already set up in UpdateExpenseDisplay
    }

    private void UpdateTotals()
    {
        if (currentReport == null || MoneyManager.Instance == null) return;

        // Calculate selected expenses
        int selectedExpenses = 0;

        if (electricityCheckbox != null && electricityCheckbox.isOn)
            selectedExpenses += currentReport.electricityCost;

        if (waterCheckbox != null && waterCheckbox.isOn)
            selectedExpenses += currentReport.waterCost;

        if (medicineCheckbox != null && medicineCheckbox.isOn)
            selectedExpenses += currentReport.medicineCost;

        if (repairsCheckbox != null && repairsCheckbox.isOn)
            selectedExpenses += currentReport.houseRepairsCost;

        if (rentCheckbox != null && rentCheckbox.isOn)
            selectedExpenses += currentReport.rentCost;

        // Update total expenses display
        if (totalExpensesText != null)
        {
            totalExpensesText.text = $"₱{selectedExpenses}";
        }

        // Update remaining cash (current money - selected expenses)
        if (remainingCashText != null)
        {
            int futureBalance = MoneyManager.Instance.CurrentMoney - selectedExpenses;
            remainingCashText.text = $"₱{futureBalance}";
            remainingCashText.color = futureBalance >= 0 ? positiveColor : negativeColor;
        }
    }

    private void OnContinueClicked()
    {
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

        // Hide panel with animation
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0f, fadeOutDuration).SetUpdate(true).OnComplete(() =>
            {
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
            });
        }
        else
        {
            if (endOfDayPanel != null)
            {
                endOfDayPanel.SetActive(false);
            }

            Time.timeScale = 1f;

            if (DayManager.Instance != null)
            {
                DayManager.Instance.ContinueToNextDay();
            }
        }
    }
}
