using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// UI component that prompts the player to stir and shows visual feedback.
/// </summary>
public class StirPrompt : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private TextMeshProUGUI counterText;
    [SerializeField] private Image progressCircle; // Circular progress indicator
    [SerializeField] private Image circularGuide; // Visual guide showing circular motion

    [Header("VFX")]
    [SerializeField] private ParticleSystem stirSuccessVFX;

    [Header("Animation Settings")]
    [SerializeField] private float pulseScale = 1.1f;
    [SerializeField] private float pulseDuration = 0.5f;

    private Tween pulseTween;
    private int currentStirCount = 0;
    private int totalStirCount = 3;

    private void Awake()
    {
        if (promptPanel != null)
            promptPanel.SetActive(false);
    }

    /// <summary>
    /// Show the stir prompt with current stir count
    /// </summary>
    public void ShowPrompt(int currentStir, int totalStirs)
    {
        currentStirCount = currentStir;
        totalStirCount = totalStirs;

        if (promptPanel != null)
            promptPanel.SetActive(true);

        if (promptText != null)
            promptText.text = "Hold and Stir!";

        UpdateCounter();
        ResetProgress();

        // Animate prompt appearance
        if (promptPanel != null)
        {
            promptPanel.transform.localScale = Vector3.zero;
            promptPanel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        }

        // Start pulsing animation
        StartPulseAnimation();
    }

    /// <summary>
    /// Hide the stir prompt
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
    /// Update the progress circle based on stir completion
    /// </summary>
    public void UpdateProgress(float progress)
    {
        if (progressCircle != null)
        {
            progressCircle.fillAmount = progress;

            // Color feedback: red -> yellow -> green
            Color progressColor = Color.Lerp(Color.red, Color.green, progress);
            progressCircle.color = progressColor;
        }
    }

    /// <summary>
    /// Reset progress to 0
    /// </summary>
    public void ResetProgress()
    {
        if (progressCircle != null)
        {
            progressCircle.fillAmount = 0f;
            progressCircle.color = Color.red;
        }
    }

    /// <summary>
    /// Update the counter text showing stirs completed
    /// </summary>
    private void UpdateCounter()
    {
        if (counterText != null)
        {
            counterText.text = $"Stir {currentStirCount + 1}/{totalStirCount}";
        }
    }

    /// <summary>
    /// Show success feedback when a stir is completed
    /// </summary>
    public void ShowStirSuccess()
    {
        StopPulseAnimation();

        // Play success VFX
        Debug.Log($"StirPrompt.ShowStirSuccess called. VFX assigned: {stirSuccessVFX != null}");
        if (stirSuccessVFX != null)
        {
            Debug.Log($"StirPrompt: Playing VFX. IsPlaying before: {stirSuccessVFX.isPlaying}");
            stirSuccessVFX.Stop();
            stirSuccessVFX.Clear();
            stirSuccessVFX.Play();
            Debug.Log($"StirPrompt: VFX Play called. IsPlaying after: {stirSuccessVFX.isPlaying}");
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
            promptText.text = "Good Stir!";
        }
    }

    /// <summary>
    /// Start pulsing animation for attention
    /// </summary>
    private void StartPulseAnimation()
    {
        StopPulseAnimation();

        if (circularGuide != null)
        {
            pulseTween = circularGuide.transform
                .DOScale(pulseScale, pulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
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

        if (circularGuide != null)
        {
            circularGuide.transform.localScale = Vector3.one;
        }
    }

    private void OnDestroy()
    {
        StopPulseAnimation();
    }

    /// <summary>
    /// Set whether the circular guide is visible
    /// </summary>
    public void SetGuideVisible(bool visible)
    {
        if (circularGuide != null)
            circularGuide.enabled = visible;
    }
}
