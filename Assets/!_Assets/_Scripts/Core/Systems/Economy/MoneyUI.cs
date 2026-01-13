using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MoneyUI : MonoBehaviour
{
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI moneyText;

        [Header("Visual Feedback (Optional)")]
        [SerializeField] private bool enableColorFlash = true;
        [SerializeField] private Color earnColor = Color.green;
        [SerializeField] private Color spendColor = Color.red;
        [SerializeField] private Color insufficientFundsColor = new Color(1f, 0.5f, 0f); // Orange
        [SerializeField] private float flashDuration = 0.3f;

        private Color originalColor;

        private void Start()
        {
            // Get reference to MoneyManager
            if (MoneyManager.Instance == null)
            {
                Debug.LogError("MoneyManager not found in scene! Make sure MoneyManager exists.");
                return;
            }

            // Store original color
            if (moneyText != null)
            {
                originalColor = moneyText.color;
            }
            else
            {
                Debug.LogError("Money Text reference not assigned in MoneyUI!");
                return;
            }

            // Subscribe to events
            MoneyManager.Instance.OnMoneyChanged += UpdateMoneyDisplay;
            MoneyManager.Instance.OnMoneyEarned += OnMoneyEarned;
            MoneyManager.Instance.OnMoneySpent += OnMoneySpent;
            MoneyManager.Instance.OnInsufficientFunds += OnInsufficientFunds;

            // Initialize display
            UpdateMoneyDisplay(MoneyManager.Instance.CurrentMoney);
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (MoneyManager.Instance != null)
            {
                MoneyManager.Instance.OnMoneyChanged -= UpdateMoneyDisplay;
                MoneyManager.Instance.OnMoneyEarned -= OnMoneyEarned;
                MoneyManager.Instance.OnMoneySpent -= OnMoneySpent;
                MoneyManager.Instance.OnInsufficientFunds -= OnInsufficientFunds;
            }
        }

        private void UpdateMoneyDisplay(int newAmount)
        {
            if (moneyText != null)
            {
                moneyText.text = newAmount.ToString("N0");
            }
        }

        private void OnMoneyEarned(int _)
        {
            if (enableColorFlash)
            {
                FlashColor(earnColor);
            }
        }

        private void OnMoneySpent(int _)
        {
            if (enableColorFlash)
            {
                FlashColor(spendColor);
            }
        }

        private void OnInsufficientFunds()
        {
            if (enableColorFlash)
            {
                FlashColor(insufficientFundsColor);
            }
        }

        private void FlashColor(Color flashColor)
        {
            if (moneyText == null) return;

            // Cancel any existing flash
            StopAllCoroutines();

            // Start flash coroutine
            StartCoroutine(FlashCoroutine(flashColor));
        }

        private System.Collections.IEnumerator FlashCoroutine(Color flashColor)
        {
            moneyText.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            moneyText.color = originalColor;
        }
}
