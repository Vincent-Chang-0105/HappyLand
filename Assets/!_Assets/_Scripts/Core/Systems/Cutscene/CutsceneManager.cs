using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System;
using System.Collections.Generic;
using AudioSystem;

/// <summary>
/// Which timing slot a cutscene occupies.
/// </summary>
public enum CutsceneTiming
{
    BeforeGameplay, // Plays at the start of a day, before the player can act
    BeforeReport,   // Plays before the end-of-day report panel appears
    AfterReport     // Plays after the player clicks Continue on the report
}

/// <summary>
/// Associates a VideoClip with a specific day number and timing slot.
/// Configure the list in the Inspector — no code changes needed to add new cutscenes.
/// </summary>
[Serializable]
public class CutsceneEntry
{
    public int dayNumber;
    public VideoClip clip;
    public CutsceneTiming timing;
}

/// <summary>
/// Manages full-screen cutscene playback between gameplay days.
///
/// Usage from other scripts:
///   CutsceneManager.Instance.TryPlay(dayNumber, CutsceneTiming.AfterReport, OnDone);
///
/// If no cutscene is configured for that day+timing, OnDone is called immediately
/// so callers never need to check for null — they just provide a callback.
/// </summary>
public class CutsceneManager : PersistentSingleton<CutsceneManager>
{

    [Header("Cutscene Schedule")]
    [Tooltip("Add one entry per cutscene. Assign the VideoClip, day number, and timing.")]
    [SerializeField] private List<CutsceneEntry> cutscenes = new List<CutsceneEntry>();

    [Header("UI References")]
    [Tooltip("Root CanvasGroup for the full-screen cutscene overlay. Starts inactive.")]
    [SerializeField] private CanvasGroup cutsceneCanvasGroup;
    [Tooltip("Full-screen RawImage that displays the video render texture.")]
    [SerializeField] private RawImage videoDisplay;
    [Tooltip("Unity VideoPlayer component used for playback.")]
    [SerializeField] private VideoPlayer videoPlayer;
    [Tooltip("Skip button shown in the corner during playback.")]
    [SerializeField] private Button skipButton;

    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    private Action onCutsceneComplete;
    private RenderTexture renderTexture;
    private float savedTimeScale;

    // -------------------------------------------------------------------------
    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake(); // handles Instance assignment via Singleton<T>

        // Hide the overlay — it becomes visible only when a cutscene plays
        if (cutsceneCanvasGroup != null)
            cutsceneCanvasGroup.gameObject.SetActive(false);

        // Wire up buttons and video events
        if (skipButton != null)
            skipButton.onClick.AddListener(SkipCutscene);

        if (videoPlayer != null)
            videoPlayer.loopPointReached += OnVideoFinished;

        // Re-subscribe to DayManager whenever a new scene loads, because
        // DayManager only exists in gameplay scenes (Level1), not in Tutorial/Menu.
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        UnsubscribeFromDayManager();

        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;

        if (skipButton != null)
            skipButton.onClick.RemoveListener(SkipCutscene);

        ReleaseRenderTexture();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Unsubscribe first to avoid duplicate subscriptions if Level1 reloads
        UnsubscribeFromDayManager();

        if (DayManager.Instance != null)
            DayManager.Instance.OnDayStarted += HandleDayStarted;
    }

    private void UnsubscribeFromDayManager()
    {
        if (DayManager.Instance != null)
            DayManager.Instance.OnDayStarted -= HandleDayStarted;
    }

    #endregion

    // -------------------------------------------------------------------------
    #region Public API

    /// <summary>
    /// Plays the cutscene configured for the given day and timing, then calls onComplete.
    /// If no cutscene is configured, onComplete is called immediately without any delay.
    /// </summary>
    public void TryPlay(int dayNumber, CutsceneTiming timing, Action onComplete)
    {
        CutsceneEntry entry = cutscenes.Find(c => c.dayNumber == dayNumber && c.timing == timing);

        if (entry == null || entry.clip == null)
        {
            // Nothing to play — call the callback right away
            onComplete?.Invoke();
            return;
        }

        PlayCutscene(entry.clip, onComplete);
    }

    #endregion

    // -------------------------------------------------------------------------
    #region BeforeGameplay Handler

    // Called automatically when DayManager fires OnDayStarted.
    // Pauses gameplay, plays the cutscene (if configured), then resumes.
    private void HandleDayStarted(int dayNumber)
    {
        CutsceneEntry entry = cutscenes.Find(c => c.dayNumber == dayNumber && c.timing == CutsceneTiming.BeforeGameplay);

        if (entry == null || entry.clip == null)
            return; // No cutscene for this day — gameplay continues normally

        // PlayCutscene saves/restores timeScale automatically.
        // Gameplay is running at timeScale=1 here, so it will resume at 1 after.
        PlayCutscene(entry.clip, onComplete: null);
    }

    #endregion

    // -------------------------------------------------------------------------
    #region Playback

    private void PlayCutscene(VideoClip clip, Action onComplete)
    {
        onCutsceneComplete = onComplete;

        // Save the caller's timeScale so we can restore it after the cutscene,
        // then pause — VideoPlayer uses unscaled time so it plays regardless.
        savedTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // Mute music during cutscene (fades out smoothly)
        if (MusicManager.Instance != null)
            MusicManager.Instance.Pause();

        // Create a render texture sized to the current screen
        ReleaseRenderTexture();
        renderTexture = new RenderTexture(Screen.width, Screen.height, 0);
        videoPlayer.targetTexture = renderTexture;
        videoDisplay.texture = renderTexture;

        // Assign and prepare the clip
        videoPlayer.clip = clip;

        // Show overlay with fade-in, then start playback
        // SetUpdate(true) ensures the tween works regardless of Time.timeScale
        cutsceneCanvasGroup.gameObject.SetActive(true);
        cutsceneCanvasGroup.alpha = 0f;
        cutsceneCanvasGroup.DOFade(1f, fadeInDuration)
            .SetUpdate(true)
            .OnComplete(() => videoPlayer.Play());
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        FinishCutscene();
    }

    private void SkipCutscene()
    {
        videoPlayer.Stop();
        FinishCutscene();
    }

    private void FinishCutscene()
    {
        // Fade out, then clean up and fire the callback
        cutsceneCanvasGroup.DOFade(0f, fadeOutDuration)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                cutsceneCanvasGroup.gameObject.SetActive(false);

                ReleaseRenderTexture();
                videoDisplay.texture = null;
                videoPlayer.clip = null;

                // Restore the timeScale that was active before the cutscene started
                Time.timeScale = savedTimeScale;

                // Resume music after cutscene
                if (MusicManager.Instance != null)
                    MusicManager.Instance.Resume();

                // Fire the callback (e.g. show report panel or proceed to next day)
                Action callback = onCutsceneComplete;
                onCutsceneComplete = null;
                callback?.Invoke();
            });
    }

    private void ReleaseRenderTexture()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            renderTexture = null;
        }

        if (videoPlayer != null)
            videoPlayer.targetTexture = null;
    }

    #endregion
}
