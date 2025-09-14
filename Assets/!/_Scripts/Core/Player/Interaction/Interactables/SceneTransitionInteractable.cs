using System;
using System.Collections;
using UnityEngine;

public class SceneTransitionInteractable : Interactable
{
    [Header("Position Reference")]
    [SerializeField] private GameObject doorPos;

    [Header("Player Direction")]
    [SerializeField] private bool faceRight = true; // True = face right, False = face left
    private bool isTransitioning = false;

    public override void Interact()
    {
        if (isTransitioning) return;
        
        StartCoroutine(GoToTargetScene());
        TriggerHints();
    }

    private IEnumerator GoToTargetScene()
    {
        isTransitioning = true;

        // Find the player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        if (player != null && doorPos != null)
        {
            // Use seamless transition if available
            if (TransitionManager.Instance != null)
            {
                TransitionManager.Instance.TransitionToPosition(doorPos.transform.position, player.transform, faceRight);
            }
        }
        else
        {
            Debug.LogWarning("Player or Door Position not found!");
        }
        
        isTransitioning = false;
        yield break;
    }

    protected new void Awake()
    {
        base.Awake();
        
        // Set default values if not set in inspector
        if (string.IsNullOrEmpty(objectName))
            objectName = "Door";
        if (string.IsNullOrEmpty(objectDescription))
            objectDescription = "Enter";
    }
}