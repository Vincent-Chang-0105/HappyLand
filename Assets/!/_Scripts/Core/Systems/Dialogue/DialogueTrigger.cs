using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class DialogueCharacter
{
    [SerializeField] private CharacterID characterID;
    
    private static CharacterDatabase database;
    
    private CharacterDatabase GetDatabase()
    {
        if (database == null)
            database = Resources.Load<CharacterDatabase>("CharacterDatabase");
        return database;
    }
    
    public CharacterData GetCharacterData()
    {
        return GetDatabase()?.GetCharacterData(characterID);
    }
}

[System.Serializable]
public class DialogueLine
{
    public DialogueCharacter character;
    [TextArea(3, 10)]
    public string line;

}

[System.Serializable]
public class Dialogue
{
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();
    public UnityEvent OnDialogueStart;
    public UnityEvent OnDialogueEnd;
}

public class DialogueTrigger : MonoBehaviour
{
    public Dialogue dialogue;

    [Header("Trigger Settings")]
    public bool canTriggerMultipleTimes = false;
    public bool disableColliderAfterUse = true;

    private bool hasTriggered = false;
    private Collider triggerCollider;

    private void Start()
    {
        triggerCollider = GetComponent<Collider>();
    }

    public void TriggerDialogue()
    {
        if (hasTriggered && !canTriggerMultipleTimes)
        {
            return;
        }

        DialogueManager.Instance.StartDialogue(dialogue);
        hasTriggered = true;

        if (disableColliderAfterUse && triggerCollider != null)
        {
            triggerCollider.enabled = false;
            Debug.Log("DialogueTrigger has been disabled after first use.");
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.tag == "Player")
        {
            TriggerDialogue();
            Debug.Log("Player has collided with the DialogueTrigger.");
        }
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
        if (triggerCollider != null)
        {
            triggerCollider.enabled = true;
        }
        Debug.Log("DialogueTrigger has been reset.");
    }
}

public enum CharacterType
{
    Carol,
    PJ,
    Primo,
    // Add more characters as needed
}
