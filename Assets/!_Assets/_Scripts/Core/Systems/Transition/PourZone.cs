using UnityEngine;

/// <summary>
/// A zone where the Pot can be dragged to pour out its contents.
/// Place near FryBowl (fried chicken path) or serving area (sinigang path).
/// The Pot detects this zone on mouse release and transfers its ingredients to the destination tag.
/// </summary>
public class PourZone : MonoBehaviour
{
    [Header("Pour Settings")]
    [SerializeField] private string destinationTag = "FryStaging";
    [SerializeField] private float pourDuration = 0.5f;

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer highlightSprite;
    [SerializeField] private Color highlightColor = new Color(1f, 0.8f, 0.3f, 0.5f);
    [SerializeField] private Color defaultColor = new Color(1f, 1f, 1f, 0.2f);

    public string DestinationTag => destinationTag;
    public float PourDuration => pourDuration;

    private void Start()
    {
        if (highlightSprite != null)
            highlightSprite.color = defaultColor;
    }

    public void SetHighlight(bool highlight)
    {
        if (highlightSprite != null)
            highlightSprite.color = highlight ? highlightColor : defaultColor;
    }
}
