using System.Collections;
using System.Collections.Generic;
using AudioSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class MainMenuEventHandler : ColorChangeMenuHandler
{
    [Header("Sounds")]
    //[SerializeField] SoundData soundDataButtonHover;
    //[SerializeField] SoundData soundDataButtonClick;    
    //private SoundBuilder soundBuilder;
    
    private Dictionary<Selectable, string> presetMappings = new Dictionary<Selectable, string>();

    public override void Awake()
    {
        base.Awake();

        //soundBuilder = SoundManager.Instance.CreateSoundBuilder();
    }
    
    private void UpdateContinueButton()
    {

    }
    
    protected override void HandleSelect(Selectable selectable)
    {
        // Handle text color change from base class
        base.HandleSelect(selectable);

        // Make Sound
        soundBuilder.WithRandomPitch().Play(soundDataButtonHover);
    }

    protected override void HandleDeselect(Selectable selectable)
    {
        // Handle text color reset from base class
        base.HandleDeselect(selectable);
    }

    protected override void HandleClick(Selectable selectable)
    {
        base.HandleClick(selectable);

        // Make Sound
        soundBuilder.WithRandomPitch().Play(soundDataButtonClick);
    }
    // Add game functionality methods
    public void StartGame()
    {
        Debug.Log("Starting game...");
    }
    
    public void LevelSelect()
    {
        
    }
    
    public void OpenSettings()
    {
        Debug.Log("Opening settings...");
    }
    
    public void ExitGame()
    {
        Debug.Log("Exiting game...");
    }
}
