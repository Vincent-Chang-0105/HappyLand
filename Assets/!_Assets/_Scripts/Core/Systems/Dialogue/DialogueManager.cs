using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class DialogueManager : PersistentSingleton<DialogueManager>
{

    public TextMeshProUGUI characterName;
    public TextMeshProUGUI dialogueArea;

    public GameObject dialogueBox;

    private Queue<DialogueLine> lines;

    public bool isDialogueActive = false;
    public bool isTyping = false;

    public event Action OnDialogueStarted;
    public event Action OnDialogueEnded;

    private string currentFullLine = "";

    public float typingSpeed = 0.2f;

    public Animator animator;

    private Dialogue currentDialogue;
    protected override void Awake()
    {
        base.Awake();
        
        lines = new Queue<DialogueLine>();
    }

    public void StartDialogue(Dialogue dialogue)
    {
        isDialogueActive = true;
        OnDialogueStarted?.Invoke();

        currentDialogue = dialogue;

        currentDialogue.OnDialogueStart?.Invoke();

        dialogueBox.SetActive(true);

        lines.Clear();

        foreach (DialogueLine dialogueLine in currentDialogue.dialogueLines)
        {
            lines.Enqueue(dialogueLine);
        }

        DisplayNextDialogueLine();
    }

    public void DisplayNextDialogueLine()
    {
        if (lines.Count == 0)
        {
            StartCoroutine(EndDialogueWithDelay());  // Added Coroutine for Delay before Ending Dialogue
            return;
        }

        DialogueLine currentLine = lines.Dequeue();

        Image dialogueCharacterImage = dialogueBox.GetComponent<Image>();
        if (dialogueCharacterImage != null && currentLine.character != null)
        {
            dialogueCharacterImage.sprite = currentLine.character.GetCharacterData().characterSprite;
        }
        //characterName.text = currentLine.character.GetName();

        StopAllCoroutines();

        StartCoroutine(TypeSentence(currentLine));
    }

    IEnumerator TypeSentence(DialogueLine dialogueLine)
    {
        isTyping = true;
        currentFullLine = dialogueLine.line;
        dialogueArea.text = "";

        foreach (char letter in dialogueLine.line.ToCharArray())
        {
            dialogueArea.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        // Wait for player to click — handled by HandleDialogueClick()
    }

    /// <summary>
    /// Call this from the dialogue box button (instead of DisplayNextDialogueLine directly).
    /// First click completes typing; second click advances to the next line.
    /// </summary>
    public void HandleDialogueClick()
    {
        if (!isDialogueActive) return;

        if (isTyping)
        {
            // Complete the current line instantly
            StopAllCoroutines();
            dialogueArea.text = currentFullLine;
            isTyping = false;
        }
        else
        {
            DisplayNextDialogueLine();
        }
    }

    public IEnumerator EndDialogueWithDelay()
    {
        yield return new WaitForSeconds(1f);  // Wait 1 second before ending the dialogue
        Debug.Log("Ending dialogue after delay");
        EndDialogue();
    }

    private void EndDialogue()
    {
        isDialogueActive = false;
        OnDialogueEnded?.Invoke();
        dialogueBox.SetActive(false);

        currentDialogue?.OnDialogueEnd?.Invoke();
        currentDialogue = null;
    }
}
