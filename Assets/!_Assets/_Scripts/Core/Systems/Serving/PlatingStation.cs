using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// A physical plate in the game world that accepts ingredients poured from a Pan.
/// When enough ingredients are poured (default 3), creates a PlatedDish on the serving screen
/// via PlatingManager and resets for the next batch.
/// </summary>
public class PlatingStation : MonoBehaviour
{
    [Header("Plating Settings")]
    [SerializeField] private int requiredChickens = 3;

    [Header("Slot Positions")]
    [SerializeField] private List<Transform> ingredientSlots = new List<Transform>();

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer plateSprite;
    [SerializeField] private Color emptyColor = Color.white;
    [SerializeField] private Color highlightColor = new Color(0.6f, 1f, 0.6f);
    [SerializeField] private ParticleSystem placeEffect;
    [SerializeField] private ParticleSystem plateCompleteEffect;

    // State
    private List<GameObject> placedChickens = new List<GameObject>();
    private string currentDishType = null; // "FriedChicken", "Sinigang", or "NoodleChicken"
    private bool isProcessing = false;

    private void CompletePlating()
    {
        isProcessing = true;

        if (PlatingManager.Instance != null)
        {
            foreach (GameObject chicken in placedChickens)
            {
                if (chicken == null) continue;

                if (currentDishType == "FriedChicken")
                {
                    PlatingManager.Instance.RegisterFriedChicken(chicken);
                }
                else if (currentDishType == "Sinigang")
                {
                    PlatingManager.Instance.RegisterSinigangChicken(chicken);
                }
                else if (currentDishType == "NoodleChicken")
                {
                    // Chickens have INoodleable; the noodle object does not
                    if (chicken.GetComponent<INoodleable>() != null)
                        PlatingManager.Instance.RegisterNoodleChicken(chicken);
                    else
                        PlatingManager.Instance.RegisterNoodleIngredient(chicken);
                }
                else if (currentDishType == "Mechado")
                {
                    PlatingManager.Instance.RegisterMechadoChicken(chicken);
                }
                else if (currentDishType == "Adobo")
                {
                    PlatingManager.Instance.RegisterAdoboChicken(chicken);
                }
            }
            // Fire dish-specific tutorial event once after all registrations
            if (currentDishType == "FriedChicken")
                TutorialEvents.FriedChickenCompleted();
        }
        else
        {
            Debug.LogError("PlatingStation: PlatingManager not found!");
        }

        DOVirtual.DelayedCall(0.5f, ResetPlate);
    }

    private void ResetPlate()
    {
        if (plateCompleteEffect != null)
        {
            ParticleSystem effect = Instantiate(plateCompleteEffect, gameObject.transform.position, Quaternion.identity, gameObject.transform);
            effect.Play();
            Destroy(effect.gameObject, effect.main.duration + effect.main.startLifetime.constantMax);
        }

        foreach (GameObject chicken in placedChickens)
        {
            if (chicken != null)
                Destroy(chicken);
        }

        placedChickens.Clear();
        currentDishType = null;
        isProcessing = false;

        if (plateSprite != null)
            plateSprite.color = emptyColor;
    }

    #region Pour Integration

    /// <summary>
    /// Toggle highlight when a Pan is dragged nearby
    /// </summary>
    public void SetHighlight(bool highlighted)
    {
        if (plateSprite != null)
            plateSprite.color = highlighted ? highlightColor : emptyColor;
    }

    /// <summary>
    /// Accept a pour from a Pan. Receives all ingredients at once,
    /// animates them to plate slots, and triggers CompletePlating when full.
    /// </summary>
    public void AcceptPour(List<GameObject> ingredients, string dishType)
    {
        if (isProcessing || ingredients == null || ingredients.Count == 0) return;

        if (currentDishType == null)
        {
            currentDishType = dishType;
        }
        else if (currentDishType != dishType)
        {
            Debug.LogWarning($"PlatingStation: Rejected pour - plate is for {currentDishType}, got {dishType}");
            return;
        }

        for (int i = 0; i < ingredients.Count; i++)
        {
            GameObject chickenObj = ingredients[i];
            if (chickenObj == null) continue;
            if (placedChickens.Count >= requiredChickens) break;

            // Parent to plate
            chickenObj.transform.SetParent(transform);

            // Disable dragging
            ChickenDragBehavior drag = chickenObj.GetComponent<ChickenDragBehavior>();
            if (drag != null) drag.enabled = false;

            // Disable collider
            Collider2D col = chickenObj.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            placedChickens.Add(chickenObj);
            TutorialEvents.ChickenAddedToPlate();

            // Animate to slot position with stagger
            int slotIndex = placedChickens.Count - 1;
            Vector3 targetPos = slotIndex < ingredientSlots.Count && ingredientSlots[slotIndex] != null
                ? ingredientSlots[slotIndex].position
                : transform.position + new Vector3(slotIndex * 0.3f - 0.3f, 0.2f, 0);

            float delay = i * 0.1f;
            chickenObj.transform.DOMove(targetPos, 0.3f).SetDelay(delay).SetEase(Ease.OutBack);
            chickenObj.transform.DORotate(Vector3.zero, 0.3f).SetDelay(delay);

            if (placeEffect != null)
            {
                DOVirtual.DelayedCall(delay, () =>
                {
                    placeEffect.transform.position = targetPos;
                    placeEffect.Stop();
                    placeEffect.Play();
                });
            }
        }

        if (placedChickens.Count >= requiredChickens)
        {
            float totalDelay = ingredients.Count * 0.1f + 0.3f;
            DOVirtual.DelayedCall(totalDelay, CompletePlating);
        }
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        // Draw slot positions
        Gizmos.color = Color.yellow;
        foreach (Transform slot in ingredientSlots)
        {
            if (slot != null)
                Gizmos.DrawWireCube(slot.position, Vector3.one * 0.3f);
        }
    }
}
