using UnityEngine;
using TMPro;

public class DayTimerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI dayNumberText;

    [Header("Warning Settings")]
    [SerializeField] private bool enableWarningColor = true;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.red;
    [SerializeField] private float warningFlashSpeed = 2f;

    private bool isWarningActive = false;
    private Color originalTimerColor;

    private void Start()
    {
        if (timerText != null)
        {
            originalTimerColor = timerText.color;
        }

        // Subscribe to DayManager events
        if (DayManager.Instance != null)
        {
            DayManager.Instance.OnTimerUpdated += UpdateTimerDisplay;
            DayManager.Instance.OnDayStarted += UpdateDayNumber;
            DayManager.Instance.OnOneMinuteWarning += ActivateWarning;
        }
        else
        {
            Debug.LogError("DayManager not found! DayTimerUI will not function.");
        }

        // Initial update
        if (DayManager.Instance != null)
        {
            UpdateDayNumber(DayManager.Instance.CurrentDay);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (DayManager.Instance != null)
        {
            DayManager.Instance.OnTimerUpdated -= UpdateTimerDisplay;
            DayManager.Instance.OnDayStarted -= UpdateDayNumber;
            DayManager.Instance.OnOneMinuteWarning -= ActivateWarning;
        }
    }

    private void Update()
    {
        // Flash warning color when < 1 minute
        if (isWarningActive && enableWarningColor && timerText != null)
        {
            float t = Mathf.PingPong(Time.time * warningFlashSpeed, 1f);
            timerText.color = Color.Lerp(normalColor, warningColor, t);
        }
    }

    private void UpdateTimerDisplay(float timeRemaining)
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        timerText.text = string.Format("{0:0}:{1:00}", minutes, seconds);
    }

    private void UpdateDayNumber(int dayNumber)
    {
        if (dayNumberText != null)
        {
            dayNumberText.text = $"Day {dayNumber}";
        }

        // Reset warning state
        isWarningActive = false;
        if (timerText != null)
        {
            timerText.color = originalTimerColor;
        }
    }

    private void ActivateWarning()
    {
        isWarningActive = true;
        Debug.Log("⏰ One minute warning activated!");
    }
}
