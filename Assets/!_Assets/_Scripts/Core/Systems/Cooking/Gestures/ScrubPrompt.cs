using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// UI component that prompts the player to scrub and shows visual feedback.
/// Shows a horizontal progress bar and left-right arrows guide.
/// </summary>
public class ScrubPrompt : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private TextMeshProUGUI counterText;
    [SerializeField] private Image progressBar; // Horizontal progress bar
    [SerializeField] private Image scrubGuideArrows; // Left-right arrows visual

    [Header("VFX")]
    [SerializeField] private ParticleSystem scrubSuccessVFX;

    [Header("Animation Settings")]
    [SerializeField] private float bounceDistance = 20f;
    [SerializeField] private float bounceDuration = 0.4f;

    private Tween bounceTween;
    private int currentScrubCount = 0;
    private int totalScrubCount = 4;
    private Vector2 originalArrowPosition;

    private void Awake()
    {
        if (promptPanel != null)
            promptPanel.SetActive(false);

        if (scrubGuideArrows != null)
            originalArrowPosition = scrubGuideArrows.rectTransform.anchoredPosition;
    }

    /// <summary>
    /// Show the scrub prompt with current scrub count
    /// </summary>
    public void ShowPrompt(int currentScrub, int totalScrubs)
    {
        currentScrubCount = currentScrub;
        totalScrubCount = totalScrubs;

        if (promptPanel != null)
            promptPanel.SetActive(true);

        if (promptText != null)
            promptText.text = "Scrub back and forth!";

        UpdateCounter();
        ResetProgress();

        // Animate prompt appearance
        if (promptPanel != null)
        {
            promptPanel.transform.localScale = Vector3.zero;
            promptPanel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        }

        // Start bouncing animation on arrows
        StartBounceAnimation();
    }

    /// <summary>
    /// Hide the scrub prompt
    /// </summary>
    public void HidePrompt()
    {
        StopBounceAnimation();

        if (promptPanel != null)
        {
            promptPanel.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
            {
                promptPanel.SetActive(false);
            });
        }
    }

    /// <summary>
    /// Update the progress bar based on scrub movement
    /// </summary>
    public void UpdateProgress(float progress)
    {
        if (progressBar != null)
        {
            progressBar.fillAmount = progress;

            // Color feedback: red -> yellow -> green
            Color progressColor = Color.Lerp(Color.red, Color.green, progress);
            progressBar.color = progressColor;
        }
    }

    /// <summary>
    /// Reset progress bar to 0
    /// </summary>
    public void ResetProgress()
    {
        if (progressBar != null)
        {
            progressBar.fillAmount = 0f;
            progressBar.color = Color.red;
        }
    }

    /// <summary>
    /// Update the counter text showing scrubs completed
    /// </summary>
    private void UpdateCounter()
    {
        if (counterText != null)
        {
            counterText.text = $"Scrub {currentScrubCount}/{totalScrubCount}";
        }
    }

    /// <summary>
    /// Show success feedback when a scrub cycle is completed
    /// </summary>
    public void ShowScrubSuccess()
    {
        currentScrubCount++;
        UpdateCounter();

        // Play success VFX
        if (scrubSuccessVFX != null)
        {
            scrubSuccessVFX.Stop();
            scrubSuccessVFX.Clear();
            scrubSuccessVFX.Play();
        }

        // Scale animation
        if (promptPanel != null)
        {
            promptPanel.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
        }

        // Update text briefly
        if (promptText != null)
        {
            promptText.text = "Good scrub!";
            DOVirtual.DelayedCall(0.5f, () =>
            {
                if (promptText != null)
                    promptText.text = "Scrub back and forth!";
            });
        }

        // Reset progress bar for next scrub
        ResetProgress();
    }

    /// <summary>
    /// Start horizontal bouncing animation for the arrow guide
    /// </summary>
    private void StartBounceAnimation()
    {
        StopBounceAnimation();

        if (scrubGuideArrows != null)
        {
            // Bounce horizontally to indicate scrubbing motion
            bounceTween = scrubGuideArrows.rectTransform
                .DOAnchorPosX(originalArrowPosition.x + bounceDistance, bounceDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
    }

    /// <summary>
    /// Stop bouncing animation
    /// </summary>
    private void StopBounceAnimation()
    {
        if (bounceTween != null && bounceTween.IsActive())
        {
            bounceTween.Kill();
            bounceTween = null;
        }

        // Reset position
        if (scrubGuideArrows != null)
        {
            scrubGuideArrows.rectTransform.anchoredPosition = originalArrowPosition;
        }
    }

    private void OnDestroy()
    {
        StopBounceAnimation();
    }

    /// <summary>
    /// Set whether the arrow guide is visible
    /// </summary>
    public void SetGuideVisible(bool visible)
    {
        if (scrubGuideArrows != null)
            scrubGuideArrows.enabled = visible;
    }
}
