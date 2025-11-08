using System;
using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private LayerMask interactionLayerMask = -1;
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI interactionText;
    [SerializeField] private TextMeshProUGUI interactionName;
    [SerializeField] private GameObject interactionIcon;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    //private Outline lastOutlinedObject;
    private Interactable currentInteractable;
    private List<Interactable> nearbyInteractables = new List<Interactable>();

    private void Start()
    {
        // Subscribe to input events
        if (InputSystem.Instance != null)
        {
            InputSystem.Instance.EKeyEvent += OnInteractionInput;
        }
        
        // Hide UI initially
        if (interactionIcon != null)
            interactionIcon.SetActive(false);
    }

    private void OnDestroy()
    {
        // Unsubscribe from input events
        if (InputSystem.Instance != null)
        {
            InputSystem.Instance.EKeyEvent -= OnInteractionInput;
        }
    }

    private void Update()
    {
        DetectNearbyInteractables();
        UpdateInteractionUI();
    }

    private void DetectNearbyInteractables()
    {
        nearbyInteractables.Clear();
        
        // Get all colliders within interaction range
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, interactionDistance, interactionLayerMask);
        
        // Find all interactables
        foreach (Collider col in nearbyColliders)
        {
            Interactable interactable = GetInteractableFromCollider(col);
            
            if (interactable != null && !nearbyInteractables.Contains(interactable))
            {
                nearbyInteractables.Add(interactable);
            }
        }
        
        // Get the closest interactable
        currentInteractable = GetClosestInteractable();
    }

    private Interactable GetInteractableFromCollider(Collider col)
    {
        // Try to get Interactable from the collider itself
        Interactable interactable = col.GetComponent<Interactable>();
        
        // If not found, try parent
        if (interactable == null)
        {
            interactable = col.GetComponentInParent<Interactable>();
        }
        
        // If still not found, try children
        if (interactable == null)
        {
            interactable = col.GetComponentInChildren<Interactable>();
        }
        
        return interactable;
    }

    private Interactable GetClosestInteractable()
    {
        if (nearbyInteractables.Count == 0)
            return null;
            
        Interactable closest = null;
        float closestDistance = float.MaxValue;
        
        foreach (Interactable interactable in nearbyInteractables)
        {
            float distance = Vector3.Distance(transform.position, interactable.transform.position);
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = interactable;
            }
        }
        
        return closest;
    }

    private void UpdateInteractionUI()
    {
        if (currentInteractable != null)
        {
            ShowInteractionPrompt();
            //UpdateOutline(GetOutlineFromInteractable(currentInteractable));
        }
        else
        {
            HideInteractionPrompt();
            //UpdateOutline(null);
        }
    }

    private void ShowInteractionPrompt()
    {
        if (currentInteractable != null)
        {
            // Update UI text
            if (interactionText != null)
                interactionText.text = "[Space] " + currentInteractable.GetDescription();
                
            if (interactionName != null)
                interactionName.text = currentInteractable.GetName();
            
            // Show interaction icon
            if (interactionIcon != null)
                interactionIcon.SetActive(true);
        }
    }

    private void HideInteractionPrompt()
    {
        if (interactionText != null)
            interactionText.text = "";
            
        if (interactionName != null)
            interactionName.text = "";
            
        if (interactionIcon != null)
            interactionIcon.SetActive(false);
    }

    // private Outline GetOutlineFromInteractable(Interactable interactable)
    // {
    //     if (interactable == null) return null;
        
    //     // Try to get Outline from the interactable's gameObject
    //     Outline outline = interactable.GetComponent<Outline>();
        
    //     // If not found, try parent
    //     if (outline == null)
    //     {
    //         outline = interactable.GetComponentInParent<Outline>();
    //     }
        
    //     // If still not found, try children
    //     if (outline == null)
    //     {
    //         outline = interactable.GetComponentInChildren<Outline>();
    //     }
        
    //     return outline;
    // }

    private void OnInteractionInput()
    {
        if (currentInteractable != null)
        {
            HandleInteraction(currentInteractable);
            Debug.Log("Interacted with: " + currentInteractable.GetName());
        }
    }

    private void HandleInteraction(Interactable interactable)
    {
        switch (interactable.interactionType)
        {
            case Interactable.InteractionType.Click:
                interactable.Interact();
                break;
            case Interactable.InteractionType.Hold:
                interactable.Interact();
                break;
            default:
                throw new System.Exception("Unsupported type of interactable");
        }
    }

    // private void UpdateOutline(Outline newOutline)
    // {
    //     if (lastOutlinedObject != null && lastOutlinedObject != newOutline)
    //     {
    //         lastOutlinedObject.enabled = false;
    //     }

    //     if (newOutline != null)
    //     {
    //         newOutline.enabled = true;
    //     }

    //     lastOutlinedObject = newOutline;
    // }
    
    private void OnDisable()
    {
        HideInteractionPrompt();
        
        // if (lastOutlinedObject != null)
        // {
        //     lastOutlinedObject.enabled = false;
        //     lastOutlinedObject = null;
        // }
    }

    // Debug visualization
    private void OnDrawGizmos()
    {
        if (showDebugGizmos)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, interactionDistance);
            
            // Draw line to current interactable
            if (currentInteractable != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, currentInteractable.transform.position);
            }
        }
    }
}