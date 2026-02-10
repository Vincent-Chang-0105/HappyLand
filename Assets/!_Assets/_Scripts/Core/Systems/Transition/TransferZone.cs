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

        Debug.Log($"TransferZone: Transferring {item.name} to {destinationTag}");

        // Disable dragging during transfer
        var dragBehavior = item.GetComponent<ChickenDragBehavior>();
        if (dragBehavior != null)
        {
            dragBehavior.enabled = false;
        }

        // Disable collider during transfer to prevent re-triggering
        var collider = item.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        // Animate to destination
        item.transform.DOMove(destination.transform.position, transferDuration)
            .SetEase(transferEase)
            .OnComplete(() => {
                // Re-enable dragging after transfer
                if (dragBehavior != null)
                {
                    dragBehavior.enabled = true;
                }
                // Re-enable collider
                if (collider != null)
                {
                    collider.enabled = true;
                }

                Debug.Log($"TransferZone: {item.name} arrived at staging area");
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
