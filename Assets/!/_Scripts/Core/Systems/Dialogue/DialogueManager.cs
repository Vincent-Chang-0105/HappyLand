using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class DialogueManager : StaticInstance<DialogueManager>
{

    public TextMeshProUGUI characterName;
    public TextMeshProUGUI dialogueArea;

    public GameObject dialogueBox;

    private Queue<DialogueLine> lines;

    public bool isDialogueActive = false;

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

        characterName.text = currentLine.character.name;

        StopAllCoroutines();

        StartCoroutine(TypeSentence(currentLine));
    }

    IEnumerator TypeSentence(DialogueLine dialogueLine)
    {
        dialogueArea.text = "";
        foreach (char letter in dialogueLine.line.ToCharArray())
        {
            dialogueArea.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        // Wait for 1 second after the line finishes typing
        yield return new WaitForSeconds(2f);

        // Call DisplayNextDialogueLine to continue or end dialogue
        DisplayNextDialogueLine();
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
        dialogueBox.SetActive(false);

        currentDialogue?.OnDialogueEnd?.Invoke();
        currentDialogue = null;
    }
}
