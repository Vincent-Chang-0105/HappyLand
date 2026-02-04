using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// UI component that prompts the player to toss the pan and shows visual feedback.
/// </summary>
public class TossPrompt : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private TextMeshProUGUI counterText;
    [SerializeField] private Image velocityBar; // Vertical bar showing drag velocity
    [SerializeField] private Image upwardArrow; // Arrow pointing up to show toss direction

    [Header("VFX")]
    [SerializeField] private ParticleSystem tossSuccessVFX;

    [Header("Animation Settings")]
    [SerializeField] private float pulseScale = 1.1f;
    [SerializeField] private float pulseDuration = 0.5f;

    private Tween pulseTween;
    private int currentTossCount = 0;
    private int totalTossCount = 3;

    private void Awake()
    {
        if (promptPanel != null)
            promptPanel.SetActive(false);
    }

    /// <summary>
    /// Show the toss prompt with current toss count
    /// </summary>
    public void ShowPrompt(int currentToss, int totalTosses)
    {
        currentTossCount = currentToss;
        totalTossCount = totalTosses;

        if (promptPanel != null)
            promptPanel.SetActive(true);

        if (promptText != null)
            promptText.text = "Hold and Toss Up!";

        UpdateCounter();
        ResetVelocity();

        // Animate prompt appearance
        if (promptPanel != null)
        {
            promptPanel.transform.localScale = Vector3.zero;
            promptPanel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        }

        // Start pulsing animation on arrow
        StartPulseAnimation();
    }

    /// <summary>
    /// Hide the toss prompt
    /// </summary>
    public void HidePrompt()
    {
        StopPulseAnimation();

        if (promptPanel != null)
        {
            promptPanel.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
            {
                promptPanel.SetActive(false);
            });
        }
    }

    /// <summary>
    /// Update the velocity bar based on toss speed
    /// </summary>
    public void UpdateVelocity(float velocityProgress)
    {
        if (velocityBar != null)
        {
            velocityBar.fillAmount = velocityProgress;

            // Color feedback: red -> yellow -> green as velocity increases
            Color velocityColor = Color.Lerp(Color.red, Color.green, velocityProgress);
            velocityBar.color = velocityColor;
        }
    }

    /// <summary>
    /// Reset velocity bar to 0
    /// </summary>
    public void ResetVelocity()
    {
        if (velocityBar != null)
        {
            velocityBar.fillAmount = 0f;
            velocityBar.color = Color.red;
        }
    }

    /// <summary>
    /// Update the counter text showing tosses completed
    /// </summary>
    private void UpdateCounter()
    {
        if (counterText != null)
        {
            counterText.text = $"Toss {currentTossCount + 1}/{totalTossCount}";
        }
    }

    /// <summary>
    /// Show success feedback when a toss is completed
    /// </summary>
    public void ShowTossSuccess()
    {
        StopPulseAnimation();

        // Play success VFX
        if (tossSuccessVFX != null)
        {
            tossSuccessVFX.Stop();
            tossSuccessVFX.Play();
        }

        // Flash green
        // if (promptPanel != null)
        // {
        //     Image panelImage = promptPanel.GetComponent<Image>();
        //     if (panelImage != null)
        //     {
        //         Color originalColor = panelImage.color;
        //         panelImage.DOColor(Color.green, 0.2f).OnComplete(() =>
        //         {
        //             panelImage.DOColor(originalColor, 0.2f);
        //         });
        //     }
        // }

        // Scale animation
        if (promptPanel != null)
        {
            promptPanel.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
        }

        // Update text
        if (promptText != null)
        {
            promptText.text = "Good Toss!";
        }
    }

    /// <summary>
    /// Start pulsing animation for attention
    /// </summary>
    private void StartPulseAnimation()
    {
        StopPulseAnimation();

        if (upwardArrow != null)
        {
            // Pulse the arrow up and down
            pulseTween = upwardArrow.transform
                .DOLocalMoveY(upwardArrow.transform.localPosition.y + 20f, pulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetRelative(false);
        }
    }

    /// <summary>
    /// Stop pulsing animation
    /// </summary>
    private void StopPulseAnimation()
    {
        if (pulseTween != null && pulseTween.IsActive())
        {
            pulseTween.Kill();
            pulseTween = null;
        }
    }

    private void OnDestroy()
    {
        StopPulseAnimation();
    }

    /// <summary>
    /// Set whether the upward arrow is visible
    /// </summary>
    public void SetArrowVisible(bool visible)
    {
        if (upwardArrow != null)
            upwardArrow.enabled = visible;
    }
}
