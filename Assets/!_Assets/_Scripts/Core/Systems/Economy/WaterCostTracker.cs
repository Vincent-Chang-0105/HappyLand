using UnityEngine;
using TMPro;
using DG.Tweening;
using System;

/// <summary>
/// Tracks and displays the running water cost while the faucet is on.
/// Deducts money from MoneyManager in real-time and auto-shuts the faucet
/// when the player runs out of money.
/// </summary>
public class WaterCostTracker : MonoBehaviour
{
    [Header("Cost Settings")]
    [SerializeField] private float costPerSecond = 2f;

    [Header("References")]
    [SerializeField] private Faucet faucet;

    [Header("UI References")]
    [SerializeField] private GameObject costPanel;
    [SerializeField] private TextMeshProUGUI costText;

    [Header("Animation")]
    [SerializeField] private float showDuration = 0.3f;
    [SerializeField] private float hideDuration = 0.2f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.red;
    [SerializeField] private int warningThreshold = 10;

    // Internal tracking
    private float accumulatedFractional = 0f;
    private int totalWaterCostToday = 0;

    // Events
    public event Action<int> OnWaterCostChanged;

    // Properties
    public int TotalWaterCostToday => totalWaterCostToday;

    private void Start()
    {
        if (costPanel != null)
            costPanel.SetActive(false);

        if (DayManager.Instance != null)
            DayManager.Instance.OnDayStarted += ResetDailyWaterCost;
    }

    private void OnDestroy()
    {
        if (DayManager.Instance != null)
            DayManager.Instance.OnDayStarted -= ResetDailyWaterCost;
    }

    private void Update()
    {
        if (faucet == null || !faucet.IsOn()) return;

        // Accumulate fractional cost
        accumulatedFractional += costPerSecond * Time.deltaTime;

        // Deduct whole pesos when threshold reached
        if (accumulatedFractional >= 1f)
        {
            int toDeduct = Mathf.FloorToInt(accumulatedFractional);
            accumulatedFractional -= toDeduct;

            if (MoneyManager.Instance != null)
            {
                if (!MoneyManager.Instance.TrySpendMoney(toDeduct))
                {
                    ForceShutFaucet();
                    return;
                }

                totalWaterCostToday += toDeduct;
                OnWaterCostChanged?.Invoke(totalWaterCostToday);
            }
        }

        UpdateCostDisplay();
        UpdateWarningState();
    }

    private void UpdateCostDisplay()
    {
        if (costText == null) return;

        float displayValue = totalWaterCostToday + accumulatedFractional;
        costText.text = $"₱{displayValue:F1}";
    }

    private void UpdateWarningState()
    {
        if (costText == null || MoneyManager.Instance == null) return;

        if (MoneyManager.Instance.CurrentMoney <= warningThreshold)
        {
            float t = Mathf.PingPong(Time.time * 3f, 1f);
            costText.color = Color.Lerp(normalColor, warningColor, t);
        }
        else
        {
            costText.color = normalColor;
        }
    }

    private void ForceShutFaucet()
    {
        if (faucet != null && faucet.IsOn())
        {
            faucet.ToggleFaucet();
        }

        accumulatedFractional = 0f;
        HideCostPanel();
    }

    public void OnFaucetTurnedOn()
    {
        // Check if player can afford at least 1 PHP
        if (MoneyManager.Instance != null && !MoneyManager.Instance.CanAfford(1))
        {
            ForceShutFaucet();
            return;
        }

        ShowCostPanel();
    }

    public void OnFaucetTurnedOff()
    {
        accumulatedFractional = 0f;
        HideCostPanel();
    }

    private void ShowCostPanel()
    {
        if (costPanel == null) return;

        costPanel.SetActive(true);
        costPanel.transform.localScale = Vector3.zero;
        costPanel.transform.DOScale(1f, showDuration).SetEase(Ease.OutBack);
        UpdateCostDisplay();
    }

    private void HideCostPanel()
    {
        if (costPanel == null) return;

        costPanel.transform.DOScale(0f, hideDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() => costPanel.SetActive(false));
    }

    private void ResetDailyWaterCost(int dayNumber)
    {
        totalWaterCostToday = 0;
        accumulatedFractional = 0f;
        OnWaterCostChanged?.Invoke(0);

        if (costText != null)
            costText.text = "₱0.0";
    }
}
