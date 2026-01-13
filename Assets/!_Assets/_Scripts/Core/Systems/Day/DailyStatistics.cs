using UnityEngine;

[System.Serializable]
public class DailyStatistics
{
    public int dayNumber;

    // Income
    public int totalSalesIncome;

    // Expenses
    public int totalIngredientCosts;
    public int electricityCost;
    public int waterCost;
    public int medicineCost;
    public int houseRepairsCost;
    public int rentCost;

    // Calculated fields
    public int TotalExpenses => totalIngredientCosts + electricityCost + waterCost + medicineCost + houseRepairsCost + rentCost;
    public int NetProfit => totalSalesIncome - TotalExpenses;

    public DailyStatistics(int day)
    {
        dayNumber = day;
        totalSalesIncome = 0;
        totalIngredientCosts = 0;
        electricityCost = 0;
        waterCost = 0;
        medicineCost = 0;
        houseRepairsCost = 0;
        rentCost = 0;
    }

    public void SetIncome(int sales)
    {
        totalSalesIncome = sales;
    }

    public void SetIngredientCosts(int costs)
    {
        totalIngredientCosts = costs;
    }

    public void SetExpenses(int electricity, int water, int medicine, int repairs, int rent)
    {
        electricityCost = electricity;
        waterCost = water;
        medicineCost = medicine;
        houseRepairsCost = repairs;
        rentCost = rent;
    }

    public bool MetMinimumSales(int minimum = 600)
    {
        return totalSalesIncome >= minimum;
    }

    public override string ToString()
    {
        return $"Day {dayNumber}\n" +
               $"Sales: {totalSalesIncome} PHP\n" +
               $"Ingredients: -{totalIngredientCosts} PHP\n" +
               $"Electricity: -{electricityCost} PHP\n" +
               $"Water: -{waterCost} PHP\n" +
               $"Medicine: -{medicineCost} PHP\n" +
               $"Repairs: -{houseRepairsCost} PHP\n" +
               $"Rent: -{rentCost} PHP\n" +
               $"Total Expenses: -{TotalExpenses} PHP\n" +
               $"Net Profit: {NetProfit} PHP";
    }
}
