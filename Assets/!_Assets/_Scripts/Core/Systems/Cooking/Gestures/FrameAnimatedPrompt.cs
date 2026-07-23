using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class FrameAnimatedPrompt : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private Image animatedImage;
    [SerializeField] private TextMeshProUGUI counterText;

    [Header("Frames")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float fps = 12f;

    [Header("VFX")]
    [SerializeField] private ParticleSystem successVFX;

    private Coroutine animationCoroutine;
    private int currentCount;
    private int totalCount;

    private void Awake()
    {
        if (promptPanel != null)
            promptPanel.SetActive(false);
    }

    public void ShowPrompt(int current, int total)
    {
        currentCount = current;
        totalCount = total;

        promptPanel?.SetActive(true);
        UpdateCounter();

        promptPanel.transform.localScale = Vector3.zero;
        promptPanel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

        StartAnimation();
    }

    public void HidePrompt()
    {
        StopAnimation();

        promptPanel.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack)
            .OnComplete(() => promptPanel?.SetActive(false));
    }

    public void ShowTossSuccess() => ShowSuccess();
    public void ShowStirSuccess() => ShowSuccess();
    public void ShowScrubSuccess() => ShowSuccess();

    private void ShowSuccess()
    {
        StopAnimation();
        if (successVFX != null)
            successVFX.Play();
    }
    public void UpdateProgress(float progress) { }
    public void UpdateVelocity(float velocity) { }
    public void ResetProgress() { }

    private void UpdateCounter()
    {
        if (counterText != null)
            counterText.text = $"{currentCount + 1}/{totalCount}";
    }

    private void StartAnimation()
    {
        StopAnimation();
        if (frames != null && frames.Length > 0)
            animationCoroutine = StartCoroutine(PlayFrames());
    }

    private void StopAnimation()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
    }

    private IEnumerator PlayFrames()
    {
        float delay = 1f / fps;
        int index = 0;
        while (true)
        {
            if (animatedImage != null)
                animatedImage.sprite = frames[index];
            index = (index + 1) % frames.Length;
            yield return new WaitForSeconds(delay);
        }
    }

    private void OnDestroy()
    {
        StopAnimation();
    }
}
