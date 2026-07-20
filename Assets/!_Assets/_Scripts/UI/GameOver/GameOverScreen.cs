using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class GameOverScreen : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private CanvasGroup overlay; // Full-screen dark background, set color to black in Inspector

    [Header("Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button quitButton;

    [Header("Animation")]
    [SerializeField] private float overlayFadeDuration = 0.35f;
    [SerializeField] private float stampDelay = 0.25f;      // How long after overlay fades before paper slams in
    [SerializeField] private float stampDuration = 0.45f;
    [SerializeField] private float stampOvershoot = 1f;     // How much it overshoots before settling (OutBack, 1 = none)
    [SerializeField] private float overlayTargetAlpha = 0.75f;

    private void Start()
    {
        if (panel != null)
            panel.SetActive(false);

        if (overlay != null)
        {
            overlay.gameObject.SetActive(false);
            overlay.alpha = 0f;
        }

        if (DayManager.Instance != null)
            DayManager.Instance.OnGameOver += ShowGameOver;

        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetry);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuit);
    }

    private void OnDestroy()
    {
        if (DayManager.Instance != null)
            DayManager.Instance.OnGameOver -= ShowGameOver;
    }

    private void ShowGameOver()
    {
        // 1. Fade in dark overlay
        if (overlay != null)
        {
            overlay.gameObject.SetActive(true);
            overlay.alpha = 0f;
            overlay.DOFade(overlayTargetAlpha, overlayFadeDuration).SetUpdate(true);
        }

        // 2. Prepare panel off-screen (scaled to 0, slightly rotated like a paper being tossed)
        panel.SetActive(true);
        panel.transform.localScale = Vector3.zero;
        panel.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(5f, 10f));

        // 3. After overlay fades, slam the paper down
        DOVirtual.DelayedCall(stampDelay, () =>
        {
            // Scale in with overshoot
            panel.transform.DOScale(1f, stampDuration)
                .SetEase(Ease.OutBack, stampOvershoot)
                .SetUpdate(true);

            // Rotate to a slight tilt (like it was thrown and landed)
            panel.transform.DORotate(new Vector3(0f, 0f, Random.Range(-2f, 2f)), stampDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    // 4. Small thud punch on landing, then pause the game
                    panel.transform.DOPunchScale(Vector3.one * 0.05f, 0.25f, 5, 0.3f)
                        .SetUpdate(true)
                        .OnComplete(() => Time.timeScale = 0f);
                });

        }, true); // true = use unscaled time so the delay works even after timeScale = 0
    }

    private void OnRetry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnQuit()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0); // Assuming main menu is at index 0
    }
}
