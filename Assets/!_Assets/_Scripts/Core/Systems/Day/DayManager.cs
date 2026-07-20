using UnityEngine;
using System;

public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }

    [Header("Day Settings")]
    [SerializeField] private float dayDurationInSeconds = 300f; // 5 minutes
    [SerializeField] private int startingDay = 1;
    [Tooltip("Game over is checked at the end of this day. Set to 5 for prototype, 20 for final.")]
    [SerializeField] private int gameOverCheckDay = 5;

    [Header("Current Day Info")]
    [SerializeField] private int currentDay = 1;
    [SerializeField] private float currentTimeRemaining;
    [SerializeField] private bool isDayActive = false;

    // Events
    public event Action<int> OnDayStarted;
    public event Action<int> OnDayEnded;
    public event Action<float> OnTimerUpdated; // Passes time remaining in seconds
    public event Action OnOneMinuteWarning;
    public event Action OnGameOver; // Fired when game over condition is met at the check day

    // Properties
    public int CurrentDay => currentDay;
    public float TimeRemaining => currentTimeRemaining;
    public bool IsDayActive => isDayActive;
    public float DayProgress => 1f - (currentTimeRemaining / dayDurationInSeconds); // 0 to 1

    private bool oneMinuteWarningTriggered = false;
    private CustomerGenerator customerGenerator;

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
        currentDay = startingDay;
        customerGenerator = FindAnyObjectByType<CustomerGenerator>();

        if (customerGenerator == null)
        {
            Debug.LogWarning("CustomerGenerator not found! DayManager may not function correctly.");
        }

        // Auto-start first day
        StartDay();
    }

    private void Update()
    {
        if (!isDayActive) return;

        // Update timer
        currentTimeRemaining -= Time.deltaTime;

        // Trigger one minute warning
        if (!oneMinuteWarningTriggered && currentTimeRemaining <= 60f)
        {
            oneMinuteWarningTriggered = true;
            OnOneMinuteWarning?.Invoke();
        }

        // Update UI
        OnTimerUpdated?.Invoke(currentTimeRemaining);

        // Check if day ended
        if (currentTimeRemaining <= 0f)
        {
            EndDay();
        }
    }

    public void StartDay()
    {
        isDayActive = true;
        currentTimeRemaining = dayDurationInSeconds;
        oneMinuteWarningTriggered = false;

        // Reset daily tracking
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.ResetDailyTracking();
        }

        if (ExpenseManager.Instance != null)
        {
            ExpenseManager.Instance.GenerateRandomExpenses();
        }

        // Reset and start customer spawning
        if (customerGenerator != null)
        {
            customerGenerator.ResetDailySales();
            customerGenerator.StartCustomerGeneration();
        }

        // Transition to cooking screen to start the day
        ScreenTransitionManager screenManager = FindAnyObjectByType<ScreenTransitionManager>();
        if (screenManager != null)
        {
            // Try to move to the cooking/prep screen (grid position 0,0 is typically the starting screen)
            screenManager.MoveToScreenDirect(new Vector2(0, 1));
        }

        OnDayStarted?.Invoke(currentDay);
        Debug.Log($"Day {currentDay} started! Time: {dayDurationInSeconds} seconds");
    }

    public void EndDay()
    {
        if (!isDayActive) return;

        isDayActive = false;
        currentTimeRemaining = 0f;

        // Clean up order buttons BEFORE destroying customers
        OrderUIManager orderUIManager = FindAnyObjectByType<OrderUIManager>();
        if (orderUIManager != null)
        {
            orderUIManager.ClearAllOrderButtons();
        }

        // Stop customer spawning immediately
        if (customerGenerator != null)
        {
            customerGenerator.StopCustomerGeneration();

            // Clear all active customers
            Customer[] activeCustomers = FindObjectsOfType<Customer>();
            foreach (Customer customer in activeCustomers)
            {
                Destroy(customer.gameObject);
            }
        }

        OnDayEnded?.Invoke(currentDay);
        Debug.Log($"Day {currentDay} ended!");

        // End of day report will be shown by EndOfDayUI listening to OnDayEnded event
    }

    public void ContinueToNextDay()
    {
        // Check game over condition at the designated day
        if (currentDay >= gameOverCheckDay)
        {
            bool hasUnpaidBills = ExpenseManager.Instance != null && ExpenseManager.Instance.TotalUnpaidBills > 0;
            bool hasNoMoney = MoneyManager.Instance != null && MoneyManager.Instance.CurrentMoney <= 0;

            if (hasUnpaidBills || hasNoMoney)
            {
                Debug.Log($"[DayManager] Game Over triggered on Day {currentDay}. Unpaid bills: {ExpenseManager.Instance?.TotalUnpaidBills}, Money: {MoneyManager.Instance?.CurrentMoney}");
                OnGameOver?.Invoke();
                return;
            }
        }

        currentDay++;
        ResetCookingStations();
        StartDay();
    }

    private void ResetCookingStations()
    {
        foreach (Pan pan in FindObjectsOfType<Pan>())
            pan.Reset();

        foreach (Pot pot in FindObjectsOfType<Pot>())
            pot.Reset();

        foreach (Bowl bowl in FindObjectsOfType<Bowl>())
            bowl.Reset();
    }

    public string GetFormattedTimeRemaining()
    {
        int minutes = Mathf.FloorToInt(currentTimeRemaining / 60f);
        int seconds = Mathf.FloorToInt(currentTimeRemaining % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    /// <summary>
    /// Adds time to the current day timer. Useful for dev mode.
    /// </summary>
    /// <param name="seconds">Number of seconds to add</param>
    public void AddTime(int seconds)
    {
        currentTimeRemaining += seconds;
        Debug.Log($"[DayManager] Added {seconds} seconds. New time: {GetFormattedTimeRemaining()}");
    }

    // Debug methods
    #if UNITY_EDITOR
    [ContextMenu("End Day Now (Debug)")]
    private void DebugEndDay()
    {
        EndDay();
    }

    [ContextMenu("Add 1 Minute (Debug)")]
    private void DebugAddTime()
    {
        currentTimeRemaining += 60f;
    }

    [ContextMenu("Skip to 10 Seconds (Debug)")]
    private void DebugSkipTo10Seconds()
    {
        currentTimeRemaining = 10f;
    }
    #endif
}
