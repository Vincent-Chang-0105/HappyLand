using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AudioSystem;

public class DialogueInteractable : Interactable
{
    [Header("Dialogue")]
    [SerializeField] private Dialogue dialogue;

    [Header("Sounds")]
    [SerializeField] SoundData interactButtonClick;
    private SoundBuilder soundBuilder;

    public override void Interact()
    {
        // Play interaction sound
        soundBuilder.WithPosition(gameObject.transform.position).Play(interactButtonClick);

        // Start the dialogue
        if (dialogue != null && dialogue.dialogueLines.Count > 0)
        {
            DialogueManager.Instance.StartDialogue(dialogue);
        }
        else
        {
            Debug.LogWarning("No dialogue assigned or dialogue is empty in " + gameObject.name);
        }

        TriggerHints();
    }

    void Start()
    {
        hintTrigger = GetComponent<HintTrigger>();
        soundBuilder = SoundManager.Instance.CreateSoundBuilder();
    }
    
}