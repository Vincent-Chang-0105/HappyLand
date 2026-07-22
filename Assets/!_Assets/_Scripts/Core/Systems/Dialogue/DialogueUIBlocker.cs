using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Disables GraphicRaycasters on specified canvases while a dialogue is active,
/// preventing the player from clicking game UI during tutorial dialogue.
/// Attach to any persistent GameObject in the scene and assign the canvases to block.
/// </summary>
public class DialogueUIBlocker : MonoBehaviour
{
    [Tooltip("Canvases whose raycasters will be disabled during dialogue (e.g. MainUI, HUD)")]
    [SerializeField] private List<Canvas> canvasesToBlock = new List<Canvas>();

    private void OnEnable()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueStarted += Block;
            DialogueManager.Instance.OnDialogueEnded += Unblock;
        }
    }

    private void OnDisable()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueStarted -= Block;
            DialogueManager.Instance.OnDialogueEnded -= Unblock;
        }
    }

    private void Block()
    {
        SetRaycasters(false);
    }

    private void Unblock()
    {
        SetRaycasters(true);
    }

    private void SetRaycasters(bool enabled)
    {
        foreach (Canvas canvas in canvasesToBlock)
        {
            if (canvas == null) continue;
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                raycaster.enabled = enabled;
        }
    }
}
