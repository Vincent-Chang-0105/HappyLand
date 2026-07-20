using UnityEngine;
using DG.Tweening;

/// <summary>
/// A trigger zone that transfers items to another screen when dropped.
/// Place at screen edges to allow manual item transfer between screens.
/// </summary>
public class TransferZone : MonoBehaviour
{
    [Header("Transfer Settings")]
    [SerializeField] private string destinationTag = "CookStaging";
    [SerializeField] private float transferDuration = 0.5f;
    [SerializeField] private Ease transferEase = Ease.OutQuad;

    [Header("Requirements")]
    [SerializeField] private bool requireWashed = true;

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer highlightSprite;
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.5f, 0.5f);

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if item meets transfer requirements
        if (!CanTransfer(other.gameObject))
            return;

        TransferItem(other.gameObject);

        // Fire tutorial event if this is a chicken
        if (other.GetComponent<Chicken>() != null)
        {
            TutorialEvents.ChickenTransferred();
        }
    }

    private bool CanTransfer(GameObject item)
    {
        if (requireWashed)
        {
            IWashable washable = item.GetComponent<IWashable>();
            if (washable == null || !washable.IsWashed())
                return false;
        }

        return true;
    }

    private void TransferItem(GameObject item)
    {
        // Find destination staging area
        GameObject destination = GameObject.FindWithTag(destinationTag);
        if (destination == null)
        {
            Debug.LogWarning($"TransferZone: No destination found with tag '{destinationTag}'");
            return;
        }

        // If chicken was in a bowl, properly exit it so the next bowl can accept it
        ChickenBowlInteraction bowlInteraction = item.GetComponent<ChickenBowlInteraction>();
        if (bowlInteraction != null && bowlInteraction.IsInBowl)
            bowlInteraction.ExitBowl();

        // Disable dragging and all colliders during transfer to prevent re-triggering
        var dragBehavior = item.GetComponent<ChickenDragBehavior>();
        if (dragBehavior != null) dragBehavior.enabled = false;

        foreach (Collider2D col in item.GetComponents<Collider2D>())
            col.enabled = false;

        // Animate to destination
        item.transform.DOMove(destination.transform.position, transferDuration)
            .SetEase(transferEase)
            .OnComplete(() => {
                // Re-enable dragging and ALL colliders so the destination bowl can detect the chicken
                if (dragBehavior != null) dragBehavior.enabled = true;

                foreach (Collider2D col in item.GetComponents<Collider2D>())
                    col.enabled = true;
            });
    }

    // Visual feedback when item is near (optional)
    private void OnTriggerStay2D(Collider2D other)
    {
        if (highlightSprite != null && CanTransfer(other.gameObject))
        {
            highlightSprite.color = highlightColor;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (highlightSprite != null)
        {
            highlightSprite.color = Color.clear;
        }
    }
}
