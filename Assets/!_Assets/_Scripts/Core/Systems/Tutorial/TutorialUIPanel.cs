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

    [Header("Branch Selection")]
    [SerializeField] private GameObject branchSelectionPanel;
    [SerializeField] private Button friedChickenButton;
    [SerializeField] private Button sinigangButton;

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    [SerializeField] private float typingSpeed = 0.03f;

    private Coroutine typingCoroutine;
    private bool isTypingComplete = false;

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

        if (friedChickenButton != null)
        {
            friedChickenButton.onClick.AddListener(() => OnBranchSelected(TutorialBranch.FriedChicken));
        }

        if (sinigangButton != null)
        {
            sinigangButton.onClick.AddListener(() => OnBranchSelected(TutorialBranch.Sinigang));
        }

        panel?.SetActive(false);
        branchSelectionPanel?.SetActive(false);
        tapHintText?.gameObject.SetActive(false);
    }

    public void ShowInstruction(string text, bool showBranchSelection = false)
    {
        // Kill any existing tweens to prevent race conditions
        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
        }

        panel?.SetActive(true);

        // Hide branch selection and hint by default
        branchSelectionPanel?.SetActive(false);
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
        typingCoroutine = StartCoroutine(TypeText(text, showBranchSelection));
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
                    branchSelectionPanel?.SetActive(false);
                });
        }
        else
        {
            panel?.SetActive(false);
            branchSelectionPanel?.SetActive(false);
        }
    }

    private IEnumerator TypeText(string fullText, bool showBranchSelection)
    {
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

        // Show appropriate UI after typing
        if (showBranchSelection)
        {
            branchSelectionPanel?.SetActive(true);
        }
        else
        {
            tapHintText?.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Called when the player clicks anywhere on the panel
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // Only respond if panel is visible
        if (panel == null || !panel.activeSelf)
            return;

        // Don't trigger if branch selection is showing (user must choose a branch)
        if (branchSelectionPanel != null && branchSelectionPanel.activeSelf)
            return;

        OnContinueClicked();
    }

    private void OnContinueClicked()
    {
        Debug.Log("TutorialUIPanel: Panel clicked - continuing tutorial");

        if (!isTypingComplete)
        {
            // Skip typing animation
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }
            // Show full text immediately
            // The TutorialManager handles the actual progression
        }

        if (TutorialManager.Instance == null)
        {
            Debug.LogError("TutorialUIPanel: TutorialManager.Instance is null!");
            return;
        }

        TutorialManager.Instance.OnContinueButtonPressed();
    }

    private void OnBranchSelected(TutorialBranch branch)
    {
        TutorialManager.Instance?.SetBranch(branch);
        TutorialManager.Instance?.OnContinueButtonPressed();
    }

    public void ShowBranchSelection()
    {
        branchSelectionPanel?.SetActive(true);
        tapHintText?.gameObject.SetActive(false);
    }
}
