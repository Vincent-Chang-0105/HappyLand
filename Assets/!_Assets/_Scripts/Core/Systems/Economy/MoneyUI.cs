using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class MoneyUI : MonoBehaviour
{
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI moneyText;

        [Header("Visual Feedback")]
        [SerializeField] private bool enableColorFlash = true;
        [SerializeField] private Color earnColor = Color.green;
        [SerializeField] private Color spendColor = Color.red;
        [SerializeField] private Color insufficientFundsColor = new Color(1f, 0.5f, 0f);
        [SerializeField] private float flashDuration = 0.3f;

        [Header("Floating Popup")]
        [SerializeField] private bool enablePopup = true;
        [SerializeField] private float popupFloatDistance = 50f;
        [SerializeField] private float popupDuration = 0.8f;
        [SerializeField] private int popupFontSize = 24;
        [SerializeField] private int minPopupAmount = 2;
        [SerializeField] private float popupOffsetX = -80f;

        [Header("Count Animation")]
        [SerializeField] private bool enableCountAnimation = true;
        [SerializeField] private float countDuration = 0.4f;

        private Color originalColor;
        private float displayedAmount;
        private Tween countTween;

        private void Start()
        {
            if (MoneyManager.Instance == null)
            {
                Debug.LogError("MoneyManager not found in scene! Make sure MoneyManager exists.");
                return;
            }

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
            displayedAmount = MoneyManager.Instance.CurrentMoney;
            UpdateMoneyDisplay(MoneyManager.Instance.CurrentMoney);
        }

        private void OnDestroy()
        {
            countTween?.Kill();

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
            if (moneyText == null) return;

            if (enableCountAnimation && gameObject.activeInHierarchy)
            {
                AnimateCountTo(newAmount);
            }
            else
            {
                displayedAmount = newAmount;
                moneyText.text = newAmount.ToString("N0");
            }
        }

        private void AnimateCountTo(int target)
        {
            countTween?.Kill();

            countTween = DOTween.To(
                () => displayedAmount,
                x =>
                {
                    displayedAmount = x;
                    if (moneyText != null)
                        moneyText.text = Mathf.RoundToInt(displayedAmount).ToString("N0");
                },
                target,
                countDuration
            ).SetEase(Ease.OutQuad);
        }

        private void OnMoneyEarned(int amount)
        {
            if (enableColorFlash)
            {
                FlashColor(earnColor);
            }

            if (enablePopup && amount >= minPopupAmount)
            {
                SpawnPopup($"+₱{amount}", earnColor);
            }
        }

        private void OnMoneySpent(int amount)
        {
            if (enableColorFlash)
            {
                FlashColor(spendColor);
            }

            if (enablePopup && amount >= minPopupAmount)
            {
                SpawnPopup($"-₱{amount}", spendColor);
            }
        }

        private void OnInsufficientFunds()
        {
            if (enableColorFlash)
            {
                FlashColor(insufficientFundsColor);
            }
        }

        private void SpawnPopup(string text, Color color)
        {
            if (moneyText == null) return;

            // Create popup as sibling of money text
            GameObject popupObj = new GameObject("MoneyPopup");
            RectTransform popupRect = popupObj.AddComponent<RectTransform>();
            popupObj.transform.SetParent(moneyText.transform.parent, false);

            // Copy position from money text
            RectTransform moneyRect = moneyText.GetComponent<RectTransform>();
            popupRect.anchoredPosition = moneyRect.anchoredPosition + new Vector2(popupOffsetX, 0f);
            popupRect.anchorMin = moneyRect.anchorMin;
            popupRect.anchorMax = moneyRect.anchorMax;
            popupRect.pivot = moneyRect.pivot;

            // Setup text
            TextMeshProUGUI popupText = popupObj.AddComponent<TextMeshProUGUI>();
            popupText.text = text;
            popupText.font = moneyText.font;
            popupText.fontSize = popupFontSize;
            popupText.color = color;
            popupText.alignment = moneyText.alignment;
            popupText.raycastTarget = false;

            // Punch the popup in for emphasis
            popupRect.localScale = Vector3.one;
            popupRect.DOPunchScale(Vector3.one * 0.3f, 0.3f, 5, 0.5f);

            // Animate: float up + fade out
            Sequence seq = DOTween.Sequence();
            seq.Join(popupRect.DOAnchorPosY(popupRect.anchoredPosition.y + popupFloatDistance, popupDuration)
                .SetEase(Ease.OutQuad));
            seq.Join(popupText.DOFade(0f, popupDuration).SetEase(Ease.InQuad));
            seq.OnComplete(() => Destroy(popupObj));
        }

        private void FlashColor(Color flashColor)
        {
            if (moneyText == null) return;

            StopAllCoroutines();
            StartCoroutine(FlashCoroutine(flashColor));
        }

        private System.Collections.IEnumerator FlashCoroutine(Color flashColor)
        {
            moneyText.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            moneyText.color = originalColor;
        }
}
