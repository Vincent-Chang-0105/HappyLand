using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// UI component that prompts the player to shake the pan side-to-side and shows visual feedback.
/// Used for sinigang cooking gesture.
/// </summary>
public class ShakePrompt : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private TextMeshProUGUI counterText;
    [SerializeField] private Image progressBar;
    [SerializeField] private Image leftArrow;
    [SerializeField] private Image rightArrow;

    [Header("VFX")]
    [SerializeField] private ParticleSystem shakeSuccessVFX;

    [Header("Animation Settings")]
    [SerializeField] private float arrowBounceDistance = 15f;
    [SerializeField] private float arrowBounceDuration = 0.4f;

    private Tween leftArrowTween;
    private Tween rightArrowTween;
    private int currentShakeCount = 0;
    private int totalShakeCount = 3;

    private void Awake()
    {
        if (promptPanel != null)
            promptPanel.SetActive(false);
    }

    public void ShowPrompt(int currentShakes, int totalShakes)
    {
        currentShakeCount = currentShakes;
        totalShakeCount = totalShakes;

        if (promptPanel != null)
            promptPanel.SetActive(true);

        if (promptText != null)
            promptText.text = "Shake Left & Right!";

        UpdateCounter();
        ResetProgress();

        // Animate prompt appearance
        if (promptPanel != null)
        {
            promptPanel.transform.localScale = Vector3.zero;
            promptPanel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        }

        StartArrowAnimation();
    }

    public void HidePrompt()
    {
        StopArrowAnimation();

        if (promptPanel != null)
        {
            promptPanel.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
            {
                promptPanel.SetActive(false);
            });
        }
    }

    public void UpdateProgress(float progress)
    {
        if (progressBar != null)
        {
            progressBar.fillAmount = progress;
            progressBar.color = Color.Lerp(Color.red, Color.green, progress);
        }
    }

    public void ResetProgress()
    {
        if (progressBar != null)
        {
            progressBar.fillAmount = 0f;
            progressBar.color = Color.red;
        }
    }

    private void UpdateCounter()
    {
        if (counterText != null)
        {
            counterText.text = $"Shake {currentShakeCount + 1}/{totalShakeCount}";
        }
    }

    public void ShowShakeSuccess()
    {
        StopArrowAnimation();

        if (shakeSuccessVFX != null)
        {
            shakeSuccessVFX.Stop();
            shakeSuccessVFX.Play();
        }

        if (promptPanel != null)
        {
            promptPanel.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
        }

        if (promptText != null)
        {
            promptText.text = "Good Shake!";
        }
    }

    private void StartArrowAnimation()
    {
        StopArrowAnimation();

        if (leftArrow != null)
        {
            leftArrowTween = leftArrow.transform
                .DOLocalMoveX(leftArrow.transform.localPosition.x - arrowBounceDistance, arrowBounceDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        if (rightArrow != null)
        {
            rightArrowTween = rightArrow.transform
                .DOLocalMoveX(rightArrow.transform.localPosition.x + arrowBounceDistance, arrowBounceDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
    }

    private void StopArrowAnimation()
    {
        if (leftArrowTween != null && leftArrowTween.IsActive())
        {
            leftArrowTween.Kill();
            leftArrowTween = null;
        }
        if (rightArrowTween != null && rightArrowTween.IsActive())
        {
            rightArrowTween.Kill();
            rightArrowTween = null;
        }
    }

    private void OnDestroy()
    {
        StopArrowAnimation();
    }
}
