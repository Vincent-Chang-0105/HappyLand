using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PauseManager : StaticInstance<PauseManager>
{
    // Events that other scripts can subscribe to
    public event Action OnGamePaused;
    public event Action OnGameResumed;

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.3f;

    [Header("UI Components")]
    [SerializeField] private GameObject pauseMenuGO;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image PauseBackground;
    [SerializeField] private TextMeshProUGUI pauseTitle;
    [SerializeField] private Button[] menuButtons;

    [Header("Menu Screens")]
    [SerializeField] private GameObject mainPauseScreen;
    [SerializeField] private GameObject optionsScreen;

    [Header("Audio Settings")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;

    [Header("Visual Settings")]
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Volume postProcessVolume; // Assign your global post-process volume
    [SerializeField] private Light directionalLight; // Optional: for adjusting light intensity
    [SerializeField] private Color MaxBrightColor = new Color(1f, 1f, 1f, 1f); // Max brightness color
    [SerializeField] private Color MinBrightColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Min brightness color

    [Header("Slider Value Display")]
    [SerializeField] private TextMeshProUGUI masterVolumeText;
    [SerializeField] private TextMeshProUGUI sfxVolumeText;
    [SerializeField] private TextMeshProUGUI musicVolumeText;
    [SerializeField] private TextMeshProUGUI brightnessText;

    [Header("If MainMenu")]
    [SerializeField] private bool isMainMenu = false;

    // Audio mixer parameter names (these should match your mixer group names)
    private const string MASTER_VOLUME_PARAM = "MasterVolume";
    private const string SFX_VOLUME_PARAM = "SFXVolume";
    private const string MUSIC_VOLUME_PARAM = "MusicVolume";

    private enum PauseMenuState
    {
        Closed,
        MainMenu,
        Options
    }

    private PauseMenuState currentMenuState = PauseMenuState.Closed;

    public bool _isPaused { get; private set; } = false;

    // Start is called before the first frame update
    void Start()
    {
        if (pauseMenuGO != null)
        {
            pauseMenuGO.SetActive(false);

            // If components aren't assigned, try to find them automatically
            if (canvasGroup == null)
                canvasGroup = pauseMenuGO.GetComponent<CanvasGroup>();
            if (PauseBackground == null)
                PauseBackground = pauseMenuGO.GetComponentInChildren<Image>();
            if (menuButtons == null || menuButtons.Length == 0)
                menuButtons = pauseMenuGO.GetComponentsInChildren<Button>();
        }

        InputSystem.Instance.EscapeKeyEvent += TogglePause;

        InitializeAudioSettings();
        InitializeBrightnessSettings();
    }

    private void OnDestroy()
    {
        if (InputSystem.Instance != null)
            InputSystem.Instance.EscapeKeyEvent -= TogglePause;

        DOTween.Kill(this);

        // Unsubscribe from slider events
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveListener(SetMasterVolume);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveListener(SetSFXVolume);
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.RemoveListener(SetBrightness);

        // Save all settings
        PlayerPrefs.Save();
    }

    private void InitializeAudioSettings()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
            // Load saved value or set default
            float savedMaster = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
            masterVolumeSlider.value = savedMaster;
            SetMasterVolume(savedMaster);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
            float savedSFX = PlayerPrefs.GetFloat("SFXVolume", 0.75f);
            sfxVolumeSlider.value = savedSFX;
            SetSFXVolume(savedSFX);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
            float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
            musicVolumeSlider.value = savedMusic;
            SetMusicVolume(savedMusic);
        }
    }

    private void InitializeBrightnessSettings()
    {
        if (brightnessSlider != null)
        {
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
            float savedBrightness = PlayerPrefs.GetFloat("Brightness", 0.5f);
            brightnessSlider.value = savedBrightness;
            SetBrightness(savedBrightness);
        }
    }

    public void TogglePause()
    {
        // // Don't allow pausing during videos or cutscenes
        // if (VideoManager.Instance?.isPlayingVideo == true ||
        //     CutsceneManager.Instance?.IsPlayingCutscene == true)
        //     return;

        if (isMainMenu)
        {
            ToggleSettingsMenu();
        }
        else
        {
            // Normal in-game pause behavior
            switch (currentMenuState)
            {
                case PauseMenuState.Closed:
                    PauseGame();
                    break;
                case PauseMenuState.MainMenu:
                    ResumeGame();
                    break;
                case PauseMenuState.Options:
                    BackToMainPauseMenu();
                    break;
            }
        }
    }

    // Main menu specific toggle behavior
    private void ToggleSettingsMenu()
    {
        switch (currentMenuState)
        {
            case PauseMenuState.Closed:
                break;
            case PauseMenuState.MainMenu:
                CloseSettingsMenu();
                break;
            case PauseMenuState.Options:
                BackToMainPauseMenu();
                break;
        }
    }

    private void OpenSettingsMenu()
    {
        currentMenuState = PauseMenuState.MainMenu;
        
        // Don't pause time in main menu
        if (!isMainMenu)
        {
            _isPaused = true;
            Time.timeScale = 0f;
        }

        // Show settings menu
        if (pauseMenuGO != null)
            ShowMainPauseMenu();

        // Don't disable input in main menu
        if (!isMainMenu && InputSystem.Instance != null)
        {
            InputSystem.Instance.SetInputState(false);
        }

        // Only invoke pause event if not in main menu
        if (!isMainMenu)
            OnGamePaused?.Invoke();
    }

    private void CloseSettingsMenu()
    {
        currentMenuState = PauseMenuState.Closed;
        
        // Don't resume time in main menu
        if (!isMainMenu)
        {
            _isPaused = false;
            Time.timeScale = 1f;
        }

        // Hide settings menu
        if (pauseMenuGO != null)
            HidePauseMenu();

        // Don't re-enable input in main menu (it should stay enabled)
        if (!isMainMenu && InputSystem.Instance != null)
        {
            InputSystem.Instance.SetInputState(true);
        }

        // Only invoke resume event if not in main menu
        if (!isMainMenu)
            OnGameResumed?.Invoke();
    }

    public void PauseGame()
    {
        _isPaused = true;
        Time.timeScale = 0f;
        currentMenuState = PauseMenuState.MainMenu;

        // Show pause menu with simple fade
        if (pauseMenuGO != null)
            ShowMainPauseMenu();

        if (InputSystem.Instance != null)
        {
            InputSystem.Instance.SetInputState(false);
        }

        // Invoke pause event for subscribers
        OnGamePaused?.Invoke();
    }

    public void ResumeGame()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        currentMenuState = PauseMenuState.Closed;

        // Hide pause menu with simple fade
        if (pauseMenuGO != null)
            HidePauseMenu();

        if (InputSystem.Instance != null)
        {
            InputSystem.Instance.SetInputState(true);
        }

        // Invoke resume event for subscribers
        OnGameResumed?.Invoke();
    }

    public void LoadOptionsMenu()
    {
        currentMenuState = PauseMenuState.Options;
        ShowOptionsScreen();
    }

    public void BackToMainPauseMenu()
    {
        currentMenuState = PauseMenuState.MainMenu;
        ShowMainPauseMenu();
    }

    private void ShowMainPauseMenu()
    {
        pauseMenuGO.SetActive(true);

        if (mainPauseScreen != null)
            mainPauseScreen.SetActive(true);
        if (optionsScreen != null)
            optionsScreen.SetActive(false);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, fadeDuration).SetUpdate(true);
        }
    }

    private void ShowOptionsScreen()
    {
        if (mainPauseScreen != null)
            mainPauseScreen.SetActive(false);
        if (optionsScreen != null)
            optionsScreen.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, fadeDuration).SetUpdate(true);
        }
    }

    private void HidePauseMenu()
    {
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0f, fadeDuration)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    pauseMenuGO.SetActive(false);
                    if (mainPauseScreen != null)
                        mainPauseScreen.SetActive(false);
                    if (optionsScreen != null)
                        optionsScreen.SetActive(false);
                });
        }
        else
        {
            pauseMenuGO.SetActive(false);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void LoadMainMenu()
    {
        InputSystem.Instance.SetInputState(false);
        //GameManager.Instance.LoadScene("MainMenu");
        Time.timeScale = 1f;
    }

    public void OpenSettingsMenuFromButton()
    {
        if (isMainMenu)
        {
            OpenSettingsMenu();
        }
    }

    // Audio volume methods
    public void SetMasterVolume(float volume)
    {
        if (audioMixer != null)
        {
            float dbValue = VolumeToDecibel(volume);
            audioMixer.SetFloat(MASTER_VOLUME_PARAM, dbValue);
            PlayerPrefs.SetFloat("MasterVolume", volume);
        }

        // Update display text (0-10)
        if (masterVolumeText != null)
        {
            int displayValue = Mathf.RoundToInt(volume * 10);
            masterVolumeText.text = displayValue.ToString();
        }
    }

    public void SetSFXVolume(float volume)
    {
        if (audioMixer != null)
        {
            float dbValue = VolumeToDecibel(volume);
            audioMixer.SetFloat(SFX_VOLUME_PARAM, dbValue);
            PlayerPrefs.SetFloat("SFXVolume", volume);
        }

        // Update display text (0-10)
        if (sfxVolumeText != null)
        {
            int displayValue = Mathf.RoundToInt(volume * 10);
            sfxVolumeText.text = displayValue.ToString();
        }
    }

    public void SetMusicVolume(float volume)
    {
        if (audioMixer != null)
        {
            float dbValue = VolumeToDecibel(volume);
            audioMixer.SetFloat(MUSIC_VOLUME_PARAM, dbValue);
            PlayerPrefs.SetFloat("MusicVolume", volume);
        }

        // Update display text (0-10)
        if (musicVolumeText != null)
        {
            int displayValue = Mathf.RoundToInt(volume * 10);
            musicVolumeText.text = displayValue.ToString();
        }
    }

    // Brightness method
    public void SetBrightness(float brightness)
    {
        if(directionalLight != null)
        {
            // Adjust directional light intensity based on brightness
            //directionalLight.intensity = Mathf.Lerp(0.5f, 2f, brightness);
            
            // Change light color from black to mid-white based on brightness
            Color blackColor = Color.black;
            Color midWhiteColor = new Color(0.35f, 0.35f, 0.35f, 1f); // Mid-white color
            directionalLight.color = Color.Lerp(MinBrightColor, MaxBrightColor, brightness);
        }
        
        // Save brightness setting
        PlayerPrefs.SetFloat("Brightness", brightness);
        // if (postProcessVolume != null && postProcessVolume.profile != null)
        // {
        //     // Try to get ColorAdjustments effect
        //     if (postProcessVolume.profile.TryGet<ColorAdjustments>(out var colorAdjustments))
        //     {
        //         // Make sure the override is enabled
        //         colorAdjustments.postExposure.overrideState = true;

        //         // Map slider value (0-1) to a reasonable brightness range
        //         // Post exposure typically works well between -2 to 2
        //         float brightnessValue = Mathf.Lerp(-1f, 2f, brightness);
        //         colorAdjustments.postExposure.value = brightnessValue;

        //         Debug.Log($"Setting brightness to: {brightnessValue}");
        //     }
        //     else
        //     {
        //         Debug.LogError("ColorAdjustments not found in the post-process profile!");
        //     }

        //     PlayerPrefs.SetFloat("Brightness", brightness);
        // }
        // else
        // {
        //     Debug.LogError("Post Process Volume or Profile is null!");

        //     // Fallback: adjust screen brightness via RenderSettings
        //     RenderSettings.ambientIntensity = Mathf.Lerp(0.5f, 1.5f, brightness);
        //     PlayerPrefs.SetFloat("Brightness", brightness);
        // }

        // Update display text (0-10)
        if (brightnessText != null)
        {
            int displayValue = Mathf.RoundToInt(brightness * 10);
            brightnessText.text = displayValue.ToString();
        }
    }

    // Convert linear volume (0-1) to decibel (-80 to 0)
    private float VolumeToDecibel(float volume)
    {
        if (volume <= 0)
            return -80f;
        
        return Mathf.Log10(volume) * 20f;
    }
}