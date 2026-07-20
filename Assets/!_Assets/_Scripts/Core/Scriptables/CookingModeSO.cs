using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using AudioSystem;

/// <summary>
/// Abstract base for all cooking modes (Frying, Adobo, Mechado, Sinigang, Noodle).
/// Subclasses live in Scriptables/Modes/ and are created as .asset files in the Inspector.
/// Pan holds a priority-ordered list of these and delegates all mode-specific logic to
/// whichever one is currently active.
/// </summary>
public abstract class CookingModeSO : ScriptableObject
{
    [Header("Gesture")]
    public int requiredGestures = 3;
    public float gestureCookInterval = 2.5f;

    [Header("Output")]
    public DishType outputDish;

    [Header("Audio")]
    public SoundData gestureCompleteSound;
    public SoundData allGesturesCompleteSound;

    [Header("Visual")]
    [Tooltip("Color applied to the liquid/broth visual in the pan when this mode is active.")]
    public Color brothColor = Color.white;

    // --- Subclass contract ---

    /// <summary>Ingredient roles that must be added before the bowl is accepted.</summary>
    public abstract IngredientRole[] UnlockRoles { get; }

    /// <summary>
    /// Additional roles added AFTER the bowl is dropped (e.g. Adobo's soy/vinegar/sugar/laurel).
    /// All must be present before tossing can start. Override to return non-empty array.
    /// </summary>
    public virtual IngredientRole[] PostBowlRoles => System.Array.Empty<IngredientRole>();

    /// <summary>Notification shown while waiting for PostBowlRoles.</summary>
    public virtual string SeasoningNotification => "Add seasoning to start cooking!";

    /// <summary>
    /// Roles that make this mode impossible (mutual exclusion).
    /// E.g. Oil-based modes exclude CookingWater and vice versa.
    /// </summary>
    public virtual IngredientRole[] ConflictRoles => System.Array.Empty<IngredientRole>();

    /// <summary>Cooking interface dispatch — returns true if the ingredient can enter this mode.</summary>
    public abstract bool CanCook(GameObject ingredient);
    public abstract void StartCooking(GameObject ingredient);
    public abstract void StopCooking(GameObject ingredient);
    public abstract void CompleteCooking(GameObject ingredient);

    // --- Helpers used by Pan ---

    public bool IsUnlockedBy(IReadOnlyCollection<IngredientRole> roles)
        => UnlockRoles.All(r => roles.Contains(r));

    public bool IsFullySeasoned(IReadOnlyCollection<IngredientRole> roles)
        => PostBowlRoles.All(r => roles.Contains(r));

    public bool IsBlockedBy(IReadOnlyCollection<IngredientRole> roles)
        => ConflictRoles.Any(r => roles.Contains(r));

    /// <summary>
    /// Returns true if adding this role would make progress toward this mode
    /// and the mode isn't already blocked by existing roles.
    /// </summary>
    public bool AcceptsRole(IngredientRole role, IReadOnlyCollection<IngredientRole> currentRoles)
    {
        if (IsBlockedBy(currentRoles)) return false;
        return UnlockRoles.Contains(role) || PostBowlRoles.Contains(role);
    }
}
