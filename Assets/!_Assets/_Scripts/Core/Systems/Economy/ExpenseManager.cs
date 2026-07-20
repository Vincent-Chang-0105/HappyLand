using UnityEngine;
using System;

public class ExpenseManager : MonoBehaviour
{
    public static ExpenseManager Instance { get; private set; }

    [Header("Fixed Daily Expenses")]
    [SerializeField] private int electricityCost = 100;
    [SerializeField] private int waterCost = 70;
    [SerializeField] private int rentCost = 350;

    [Header("Random Expense Ranges")]
    [SerializeField] private int medicineCostMin = 0;
    [SerializeField] private int medicineCostMax = 50;
    [SerializeField] private int houseRepairsCostMin = 0;
    [SerializeField] private int houseRepairsCostMax = 100;

    [Header("Current Day Expenses")]
    [SerializeField] private int currentElectricityCost;
    [SerializeField] private int currentWaterCost;
    [SerializeField] private int currentMedicineCost;
    [SerializeField] private int currentHouseRepairsCost;
    [SerializeField] private int currentRentCost;

    [Header("Accumulated Unpaid Expenses")]
    [SerializeField] private int unpaidElectricity = 0;
    [SerializeField] private int unpaidWater = 0;
    [SerializeField] private int unpaidMedicine = 0;
    [SerializeField] private int unpaidRepairs = 0;
    [SerializeField] private int unpaidRent = 0;

    // Properties - includes accumulated unpaid amounts
    public int ElectricityCost => currentElectricityCost + unpaidElectricity;
    public int WaterCost => currentWaterCost + unpaidWater;
    public int MedicineCost => currentMedicineCost + unpaidMedicine;
    public int HouseRepairsCost => currentHouseRepairsCost + unpaidRepairs;
    public int RentCost => currentRentCost + unpaidRent;
    public int TotalFixedExpenses => ElectricityCost + WaterCost + RentCost;
    public int TotalVariableExpenses => MedicineCost + HouseRepairsCost;
    public int TotalDailyExpenses => TotalFixedExpenses + TotalVariableExpenses;
    public int TotalUnpaidBills => unpaidElectricity + unpaidWater + unpaidMedicine + unpaidRepairs + unpaidRent;

    // Events
    public event Action<int> OnExpensesGenerated;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void GenerateRandomExpenses()
    {
        // Fixed expenses stay the same
        currentElectricityCost = electricityCost;
        currentWaterCost = 0; // Water cost is now usage-based, set by WaterCostTracker
        currentRentCost = rentCost;

        // Generate random expenses
        currentMedicineCost = UnityEngine.Random.Range(medicineCostMin, medicineCostMax + 1);
        currentHouseRepairsCost = UnityEngine.Random.Range(houseRepairsCostMin, houseRepairsCostMax + 1);

        OnExpensesGenerated?.Invoke(TotalDailyExpenses);

        // Debug.Log($"Daily Expenses Generated: " +
        //           $"Electricity: {currentElectricityCost}, " +
        //           $"Water: {currentWaterCost}, " +
        //           $"Medicine: {currentMedicineCost}, " +
        //           $"Repairs: {currentHouseRepairsCost}, " +
        //           $"Rent: {currentRentCost} " +
        //           $"(Total: {TotalDailyExpenses} PHP)");
    }

    /// <summary>
    /// Apply selected expenses to money and carry over unpaid amounts
    /// </summary>
    /// <param name="payElectricity">Pay electricity bill</param>
    /// <param name="payWater">Pay water bill</param>
    /// <param name="payMedicine">Pay medicine cost</param>
    /// <param name="payRepairs">Pay house repairs</param>
    /// <param name="payRent">Pay rent</param>
    public void ApplySelectedExpenses(bool payElectricity, bool payWater, bool payMedicine, bool payRepairs, bool payRent)
    {
        if (MoneyManager.Instance == null)
        {
            Debug.LogError("MoneyManager not found! Cannot apply expenses.");
            return;
        }

        int totalPaid = 0;

        // Pay electricity or add to unpaid
        if (payElectricity)
        {
            int amount = ElectricityCost;
            if (MoneyManager.Instance.TrySpendMoney(amount))
            {
                totalPaid += amount;
                unpaidElectricity = 0; // Clear accumulated debt
            }
            else
            {
                Debug.LogWarning($"Could not afford Electricity ({amount} PHP)");
                unpaidElectricity += currentElectricityCost; // Add current to unpaid
            }
        }
        else
        {
            unpaidElectricity += currentElectricityCost;
        }

        // Pay water or add to unpaid
        if (payWater)
        {
            int amount = WaterCost;
            if (MoneyManager.Instance.TrySpendMoney(amount))
            {
                totalPaid += amount;
                unpaidWater = 0;
            }
            else
            {
                Debug.LogWarning($"Could not afford Water ({amount} PHP)");
                unpaidWater += currentWaterCost;
            }
        }
        else
        {
            unpaidWater += currentWaterCost;
        }

        // Pay medicine or add to unpaid
        if (payMedicine && MedicineCost > 0)
        {
            int amount = MedicineCost;
            if (MoneyManager.Instance.TrySpendMoney(amount))
            {
                totalPaid += amount;
                unpaidMedicine = 0;
            }
            else
            {
                Debug.LogWarning($"Could not afford Medicine ({amount} PHP)");
                unpaidMedicine += currentMedicineCost;
            }
        }
        else if (currentMedicineCost > 0)
        {
            unpaidMedicine += currentMedicineCost;
        }

        // Pay repairs or add to unpaid
        if (payRepairs && HouseRepairsCost > 0)
        {
            int amount = HouseRepairsCost;
            if (MoneyManager.Instance.TrySpendMoney(amount))
            {
                totalPaid += amount;
                unpaidRepairs = 0;
            }
            else
            {
                Debug.LogWarning($"Could not afford Repairs ({amount} PHP)");
                unpaidRepairs += currentHouseRepairsCost;
            }
        }
        else if (currentHouseRepairsCost > 0)
        {
            unpaidRepairs += currentHouseRepairsCost;
        }

        // Pay rent or add to unpaid
        if (payRent)
        {
            int amount = RentCost;
            if (MoneyManager.Instance.TrySpendMoney(amount))
            {
                totalPaid += amount;
                unpaidRent = 0;
            }
            else
            {
                Debug.LogWarning($"Could not afford Rent ({amount} PHP)");
                unpaidRent += currentRentCost;
            }
        }
        else
        {
            unpaidRent += currentRentCost;
        }

    }

    /// <summary>
    /// Set the water cost to the actual usage-based amount from WaterCostTracker.
    /// Called before end-of-day calculations.
    /// </summary>
    public void SetWaterUsageCost(int usageCost)
    {
        currentWaterCost = usageCost;
    }

    public bool HasMedicine()
    {
        return currentMedicineCost > 0;
    }

    public bool HasHouseRepairs()
    {
        return currentHouseRepairsCost > 0;
    }

    // Debug methods
    #if UNITY_EDITOR
    [ContextMenu("Generate Random Expenses (Debug)")]
    private void DebugGenerateExpenses()
    {
        GenerateRandomExpenses();
    }

    [ContextMenu("Pay All Expenses (Debug)")]
    private void DebugPayAllExpenses()
    {
        ApplySelectedExpenses(true, true, true, true, true);
    }

    [ContextMenu("Skip All Expenses (Debug)")]
    private void DebugSkipAllExpenses()
    {
        ApplySelectedExpenses(false, false, false, false, false);
    }
    #endif
}
