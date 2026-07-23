using UnityEngine;
using System;
using AudioSystem;

/// <summary>
/// Detects back-and-forth horizontal scrubbing gestures from mouse input.
/// Only registers scrubbing when the mouse is within a certain distance of the target.
/// Used for washing mechanics where the player scrubs items clean.
/// </summary>
public class ScrubGestureDetector : MonoBehaviour
{
    [Header("Gesture Settings")]
    [SerializeField] private float minimumScrubDistance = 0.5f;
    [SerializeField] private int scrubCyclesRequired = 4;
    [SerializeField] private float maxScrubDistance = 2f; // Max distance from target to register scrubbing

    // Events
    public event Action<float> OnScrubProgress; // 0-1 progress of current scrub
    public event Action OnScrubCycleComplete;   // Single scrub cycle done
    public event Action OnAllScrubsComplete;    // All scrubs done

    [Header("Sounds")]
    [SerializeField] private SoundData scrubbingLoopSound;

    // State
    private bool isTracking = false;
    private SoundEmitter scrubbingLoopEmitter;
    private Transform targetTransform;
    private Vector2 lastPosition;
    private Vector2 scrubStartPosition;
    private int currentDirection = 0; // -1 = left, 1 = right, 0 = none
    private int completedCycles = 0;
    private float currentScrubDistance = 0f;
    private Camera mainCamera;

    // Public properties
    public int CompletedCycles => completedCycles;
    public int RequiredCycles => scrubCyclesRequired;
    public bool IsScrubbing => isTracking;
    public float CurrentProgress => Mathf.Clamp01(currentScrubDistance / minimumScrubDistance);
    public int CurrentDirection => currentDirection; // -1 = left, 1 = right, 0 = none

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!isTracking || targetTransform == null) return;

        if (Input.GetMouseButton(0))
        {
            TrackScrubbing();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            // Mouse released - reset current scrub progress but keep cycles
            ResetCurrentScrub();
        }
    }

    private void TrackScrubbing()
    {
        Vector2 mousePos = GetMouseWorldPosition();

        // Check distance to target - only count scrubbing if close enough
        float distanceToTarget = Vector2.Distance(mousePos, (Vector2)targetTransform.position);
        if (distanceToTarget > maxScrubDistance)
        {
            // Too far from target, don't count this movement
            // But don't reset - just ignore movement until they get closer
            return;
        }

        float deltaX = mousePos.x - lastPosition.x;

        // Determine movement direction
        int newDirection = 0;
        if (deltaX > 0.02f)
            newDirection = 1;  // Moving right
        else if (deltaX < -0.02f)
            newDirection = -1; // Moving left

        // Detect direction change (completing a scrub motion)
        if (newDirection != 0 && currentDirection != 0 && newDirection != currentDirection)
        {
            // Direction changed! Check if we moved far enough
            float distanceMoved = Mathf.Abs(mousePos.x - scrubStartPosition.x);

            if (distanceMoved >= minimumScrubDistance)
            {
                completedCycles++;
                OnScrubCycleComplete?.Invoke();

                if (completedCycles >= scrubCyclesRequired)
                {
                    OnAllScrubsComplete?.Invoke();
                    StopTracking();
                    return;
                }
            }

            // Reset for next scrub
            scrubStartPosition = mousePos;
            currentScrubDistance = 0f;
        }

        // Update direction
        if (newDirection != 0)
        {
            currentDirection = newDirection;
        }

        // Calculate and report progress
        currentScrubDistance = Mathf.Abs(mousePos.x - scrubStartPosition.x);
        OnScrubProgress?.Invoke(CurrentProgress);

        lastPosition = mousePos;
    }

    private void ResetCurrentScrub()
    {
        currentScrubDistance = 0f;
        currentDirection = 0;
        OnScrubProgress?.Invoke(0f);
    }

    /// <summary>
    /// Set the target transform to check distance from (usually the chicken)
    /// </summary>
    public void SetTarget(Transform target)
    {
        targetTransform = target;
    }

    /// <summary>
    /// Start tracking scrubbing gestures
    /// </summary>
    public void StartTracking()
    {
        isTracking = true;
        completedCycles = 0;
        currentDirection = 0;
        currentScrubDistance = 0f;

        Vector2 mousePos = GetMouseWorldPosition();
        lastPosition = mousePos;
        scrubStartPosition = mousePos;

        OnScrubProgress?.Invoke(0f);

        // Start scrubbing loop sound
        if (scrubbingLoopSound != null && SoundManager.Instance != null)
            scrubbingLoopEmitter = SoundManager.Instance.CreateSoundBuilder().Play(scrubbingLoopSound);
    }

    /// <summary>
    /// Stop tracking scrubbing gestures
    /// </summary>
    public void StopTracking()
    {
        isTracking = false;
        ResetCurrentScrub();

        // Stop scrubbing loop sound
        if (scrubbingLoopEmitter != null)
        {
            scrubbingLoopEmitter.FadeOutAndStop(0.15f);
            scrubbingLoopEmitter = null;
        }
    }

    /// <summary>
    /// Reset all progress (cycles and current scrub)
    /// </summary>
    public void ResetProgress()
    {
        completedCycles = 0;
        currentScrubDistance = 0f;
        currentDirection = 0;
        OnScrubProgress?.Invoke(0f);
    }

    /// <summary>
    /// Enable or disable gesture detection
    /// </summary>
    public void SetActive(bool active)
    {
        enabled = active;
        if (!active)
        {
            StopTracking();
        }
    }

    private Vector2 GetMouseWorldPosition()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(mainCamera.transform.position.z);
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);

        return new Vector2(worldPos.x, worldPos.y);
    }
}
