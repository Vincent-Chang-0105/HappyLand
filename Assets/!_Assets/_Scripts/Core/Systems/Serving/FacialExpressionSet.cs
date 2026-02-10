using UnityEngine;

/// <summary>
/// Stores the 3 facial expression sprites for a single NPC character.
/// Can be configured directly in the Inspector as a list.
/// </summary>
[System.Serializable]
public class FacialExpressionSet
{
    [Header("NPC Identity")]
    [Tooltip("The NPC number this expression set belongs to (e.g., 1 for NPC_1)")]
    public int npcNumber = 1;

    [Header("Facial Expressions")]
    [Tooltip("The default/neutral expression")]
    public Sprite defaultExpression;

    [Tooltip("The happy/satisfied expression")]
    public Sprite happyExpression;

    [Tooltip("The angry/disappointed expression")]
    public Sprite angryExpression;

    /// <summary>
    /// Gets the sprite for the specified emotion
    /// </summary>
    public Sprite GetExpression(CustomerEmotion emotion)
    {
        switch (emotion)
        {
            case CustomerEmotion.Default:
                return defaultExpression;
            case CustomerEmotion.Happy:
                return happyExpression;
            case CustomerEmotion.Angry:
                return angryExpression;
            default:
                return defaultExpression;
        }
    }

    /// <summary>
    /// Validates that all expression sprites are assigned
    /// </summary>
    public bool IsValid()
    {
        return defaultExpression != null && happyExpression != null && angryExpression != null;
    }
}

public enum CustomerEmotion
{
    Default,
    Happy,
    Angry
}
