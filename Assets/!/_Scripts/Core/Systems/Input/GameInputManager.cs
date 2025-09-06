using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameInputManager : PersistentSingleton<GameInputManager>
{
    [Header("Input Priority System")]
    [SerializeField] private bool debugMode = true;

    // Define input contexts with priorities (higher number = higher priority)
    public enum InputContext
    {
        Gameplay = 0,
        UI = 1,
        Pause = 2,
        Video = 3,
        Dialog = 4
    }

    private InputContext currentContext = InputContext.Gameplay;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        if (InputSystem.Instance != null)
        {
            InputSystem.Instance.EscapeKeyEvent += HandleEscapeKey;
        }
    }

    private void HandleEscapeKey()
    {
        UpdateCurrentContext();
        
        if (debugMode) Debug.Log($"Handling escape key in context: {currentContext}");

        switch (currentContext)
        {
            case InputContext.Video:
                //VideoManager.Instance?.SkipVideo();
                break;
                
            case InputContext.Dialog:
                // Handle dialog closing
                break;
                
            case InputContext.Pause:
                PauseManager.Instance?.ResumeGame();
                break;
                
            case InputContext.UI:
                // Handle UI back navigation
                break;
                
            case InputContext.Gameplay:
                PauseManager.Instance?.PauseGame();
                break;
        }
    }

    private void UpdateCurrentContext()
    {
        // Determine current context based on game state
        // if (VideoManager.Instance != null && VideoManager.Instance.IsPlayingVideo)
        // {
        //     currentContext = InputContext.Video;
        // }
        // else if (PauseManager.Instance != null && PauseManager.Instance._isPaused)
        // {
        //     currentContext = InputContext.Pause;
        // }
        // else
        // {
        //     currentContext = InputContext.Gameplay;
        // }
    }

    private void OnDestroy()
    {
        if (InputSystem.Instance != null)
        {
            InputSystem.Instance.EscapeKeyEvent -= HandleEscapeKey;
        }
    }
}
