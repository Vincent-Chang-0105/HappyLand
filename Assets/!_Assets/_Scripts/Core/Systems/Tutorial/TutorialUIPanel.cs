using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using System.Collections;

public class TutorialUIPanel : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI tapHintText; // Shows "Tap anywhere to continue..."
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    [SerializeField] private float typingSpeed = 0.03f;

    private Coroutine typingCoroutine;
    private bool isTypingComplete = false;
    private string currentFullText = "";

    private void Start()
    {
        // Validate required references
        if (instructionText == null)
        {
            Debug.LogError("TutorialUIPanel: instructionText (TextMeshProUGUI) is not assigned!");
        }

        if (panel == null)
        {
            Debug.LogError("TutorialUIPanel: panel GameObject is not assigned!");
        }

        if (canvasGroup == null)
        {
            Debug.LogWarning("TutorialUIPanel: canvasGroup is not assigned. Fade animations won't work.");
        }

        if (tapHintText == null)
        {
            Debug.LogWarning("TutorialUIPanel: tapHintText is not assigned. Hint won't show.");
        }

        panel?.SetActive(false);
        tapHintText?.gameObject.SetActive(false);
    }

    public void ShowInstruction(string text)
    {
        // Kill any existing tweens to prevent race conditions
        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
        }

        panel?.SetActive(true);

        // Hide hint by default
        tapHintText?.gameObject.SetActive(false);

        // Fade in
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, fadeInDuration).SetUpdate(true);
        }

        // Type text
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        isTypingComplete = false;
        typingCoroutine = StartCoroutine(TypeText(text));
    }

    public void HideInstruction()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0f, fadeOutDuration)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    panel?.SetActive(false);
                });
        }
        else
        {
            panel?.SetActive(false);
        }
    }

    private IEnumerator TypeText(string fullText)
    {
        currentFullText = fullText;

        if (instructionText != null)
        {
            instructionText.text = "";

            foreach (char c in fullText)
            {
                instructionText.text += c;
                yield return new WaitForSecondsRealtime(typingSpeed);
            }
        }

        isTypingComplete = true;
        tapHintText?.gameObject.SetActive(true);
    }

    /// <summary>
    /// Called when the player clicks anywhere on the panel
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // Only respond if panel is visible
        if (panel == null || !panel.activeSelf)
            return;

        OnContinueClicked();
    }

    private void OnContinueClicked()
    {
        if (!isTypingComplete)
        {
            // First click: complete typing instantly, wait for second click
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            if (instructionText != null)
                instructionText.text = currentFullText;

            isTypingComplete = true;
            tapHintText?.gameObject.SetActive(true);
            return;
        }

        if (TutorialManager.Instance == null)
        {
            Debug.LogError("TutorialUIPanel: TutorialManager.Instance is null!");
            return;
        }

        TutorialManager.Instance.OnContinueButtonPressed();
    }
}
