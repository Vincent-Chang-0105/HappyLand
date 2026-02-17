using UnityEngine;
using System;

public class MoneyManager : MonoBehaviour
{
        public static MoneyManager Instance { get; private set; }

        [Header("Money Settings")]
        [SerializeField] private int startingMoney = 200;

        [Header("Current Money")]
        [SerializeField] private int currentMoney;

        [Header("Daily Tracking")]
        [SerializeField] private int todayIncome = 0;
        [SerializeField] private int todayExpenses = 0;

        // Events
        public event Action<int> OnMoneyChanged;
        public event Action<int> OnMoneyEarned;
        public event Action<int> OnMoneySpent;
        public event Action OnInsufficientFunds;

        // Properties
        public int CurrentMoney => currentMoney;
        public int TodayIncome => todayIncome;
        public int TodayExpenses => todayExpenses;

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

        private void Start()
        {
            currentMoney = startingMoney;
            OnMoneyChanged?.Invoke(currentMoney);
        }

        /// <summary>
        /// Adds money to the player's balance
        /// </summary>
        /// <param name="amount">Amount to add</param>
        public void AddMoney(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"Attempted to add invalid amount: {amount}");
                return;
            }

            currentMoney += amount;
            todayIncome += amount; // Track daily income
            OnMoneyChanged?.Invoke(currentMoney);
            OnMoneyEarned?.Invoke(amount);
        }

        /// <summary>
        /// Attempts to spend money. Returns true if successful, false if insufficient funds
        /// </summary>
        /// <param name="amount">Amount to spend</param>
        /// <returns>True if transaction successful</returns>
        public bool TrySpendMoney(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"Attempted to spend invalid amount: {amount}");
                return false;
            }

            if (!CanAfford(amount))
            {
                OnInsufficientFunds?.Invoke();
                return false;
            }

            currentMoney -= amount;
            todayExpenses += amount; // Track daily expenses
            OnMoneyChanged?.Invoke(currentMoney);
            OnMoneySpent?.Invoke(amount);

            return true;
        }

        /// <summary>
        /// Checks if the player can afford a specific amount
        /// </summary>
        /// <param name="amount">Amount to check</param>
        /// <returns>True if player has enough money</returns>
        public bool CanAfford(int amount)
        {
            return currentMoney >= amount;
        }

        /// <summary>
        /// Resets money to starting amount
        /// </summary>
        public void ResetMoney()
        {
            currentMoney = startingMoney;
            OnMoneyChanged?.Invoke(currentMoney);
        }

        /// <summary>
        /// Gets today's income (from sales)
        /// </summary>
        public int GetTodayIncome()
        {
            return todayIncome;
        }

        /// <summary>
        /// Gets today's expenses (ingredients + chickens purchased)
        /// </summary>
        public int GetTodayExpenses()
        {
            return todayExpenses;
        }

        /// <summary>
        /// Resets daily tracking for new day
        /// </summary>
        public void ResetDailyTracking()
        {
            todayIncome = 0;
            todayExpenses = 0;
        }

        // Debug methods for testing
        #if UNITY_EDITOR
        [ContextMenu("Add 100 PHP (Debug)")]
        private void DebugAddMoney()
        {
            AddMoney(100);
        }

        [ContextMenu("Remove 50 PHP (Debug)")]
        private void DebugRemoveMoney()
        {
            TrySpendMoney(50);
        }
        #endif
}
