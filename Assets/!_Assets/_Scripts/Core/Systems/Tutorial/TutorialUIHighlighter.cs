using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Draws a pulsing outline/glow around a target RectTransform to draw the player's attention.
/// Attach to any persistent UI object (e.g. the TutorialManager's canvas).
/// Call Show/Hide from TutorialManager.
/// </summary>
public class TutorialUIHighlighter : MonoBehaviour
{
    [Header("Highlight Visual")]
    [SerializeField] private Image highlightImage;
    [SerializeField] private Color highlightColor = new Color(1f, 0.85f, 0f, 0.6f);
    [SerializeField] private float padding = 12f;

    [Header("Pulse Animation")]
    [SerializeField] private float pulseMinAlpha = 0.2f;
    [SerializeField] private float pulseMaxAlpha = 0.8f;
    [SerializeField] private float pulseDuration = 0.6f;

    private RectTransform highlightRect;
    private Tween pulseTween;

    private void Awake()
    {
        highlightRect = highlightImage != null ? highlightImage.GetComponent<RectTransform>() : null;
        if (highlightImage != null)
            highlightImage.gameObject.SetActive(false);
    }

    public void Show(RectTransform target)
    {
        if (highlightImage == null || highlightRect == null || target == null) return;

        // Become a child of the target, stretching to fill it with padding
        highlightRect.SetParent(target, false);
        highlightRect.anchorMin = Vector2.zero;
        highlightRect.anchorMax = Vector2.one;
        highlightRect.anchoredPosition = Vector2.zero;
        highlightRect.sizeDelta = Vector2.one * padding * 2f;
        highlightRect.pivot = new Vector2(0.5f, 0.5f);

        highlightImage.color = new Color(highlightColor.r, highlightColor.g, highlightColor.b, pulseMinAlpha);
        highlightImage.gameObject.SetActive(true);

        pulseTween?.Kill();
        pulseTween = highlightImage
            .DOFade(pulseMaxAlpha, pulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    public void Hide()
    {
        pulseTween?.Kill();
        pulseTween = null;

        if (highlightImage != null)
            highlightImage.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        pulseTween?.Kill();
    }
}
