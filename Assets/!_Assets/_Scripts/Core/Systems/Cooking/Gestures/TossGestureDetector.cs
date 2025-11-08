using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Detects fast upward drag gestures (toss motion) from mouse input.
/// Tracks mouse velocity and direction to detect quick upward flicks.
/// </summary>
public class TossGestureDetector : MonoBehaviour
{
    [Header("Gesture Settings")]
    [SerializeField] private float minimumVelocityThreshold = 5f; // Minimum upward speed to register as toss
    [SerializeField] private float minimumUpwardDistance = 0.5f; // Minimum distance moved upward
    [SerializeField] private float maxTossTime = 0.3f; // Maximum time for the drag to be considered a "quick" toss
    [SerializeField] private float angleToleranceDegrees = 45f; // How much deviation from straight up is allowed

    [Header("Visual Feedback")]
    [SerializeField] private Transform tossOrigin; // The pan handle position

    // Events
    public event Action OnTossComplete;
    public event Action<float> OnTossProgress; // Returns velocity progress 0-1 (based on threshold)

    // State
    private bool isTracking = false;
    private Vector2 dragStartPos;
    private float dragStartTime;
    private Camera mainCamera;

    // Progress
    private float currentVelocity = 0f;
    public float CurrentVelocity => currentVelocity;
    public bool IsTossing => isTracking;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // Start tracking when mouse button is pressed
        if (Input.GetMouseButtonDown(0))
        {
            StartTracking();
        }

        // Track mouse movement while button is held
        if (Input.GetMouseButton(0) && isTracking)
        {
            TrackMousePosition();
        }

        // Check for toss when button is released
        if (Input.GetMouseButtonUp(0))
        {
            CheckForToss();
            StopTracking();
        }
    }

    private void StartTracking()
    {
        isTracking = true;
        dragStartPos = GetMouseWorldPosition();
        dragStartTime = Time.time;
        currentVelocity = 0f;
    }

    private void TrackMousePosition()
    {
        Vector2 currentMousePos = GetMouseWorldPosition();
        Vector2 dragVector = currentMousePos - dragStartPos;

        // Calculate velocity (distance / time)
        float dragTime = Time.time - dragStartTime;
        if (dragTime > 0)
        {
            currentVelocity = dragVector.y / dragTime; // Only care about vertical velocity
        }

        // Emit progress event based on velocity
        float progress = Mathf.Clamp01(Mathf.Max(0, currentVelocity) / minimumVelocityThreshold);
        OnTossProgress?.Invoke(progress);
    }

    private void CheckForToss()
    {
        if (!isTracking) return;

        Vector2 currentMousePos = GetMouseWorldPosition();
        Vector2 dragVector = currentMousePos - dragStartPos;
        float dragTime = Time.time - dragStartTime;

        // Calculate velocity
        float velocity = dragVector.magnitude / dragTime;
        float upwardVelocity = dragVector.y / dragTime;

        // Check if movement was primarily upward
        float angleDegrees = Vector2.SignedAngle(Vector2.up, dragVector.normalized);
        bool isPrimarilyUpward = Mathf.Abs(angleDegrees) < angleToleranceDegrees;

        // Check all toss conditions
        bool isFastEnough = upwardVelocity >= minimumVelocityThreshold;
        bool isQuickEnough = dragTime <= maxTossTime;
        bool isLongEnough = dragVector.y >= minimumUpwardDistance;

        Debug.Log($"Toss check - Velocity: {upwardVelocity:F2}, Time: {dragTime:F2}, Distance: {dragVector.y:F2}, Upward: {isPrimarilyUpward}");

        if (isFastEnough && isQuickEnough && isLongEnough && isPrimarilyUpward)
        {
            CompleteToss();
        }
        else
        {
            Debug.Log($"Toss failed - Fast: {isFastEnough}, Quick: {isQuickEnough}, Long: {isLongEnough}, Upward: {isPrimarilyUpward}");
        }
    }

    private void CompleteToss()
    {
        Debug.Log("Toss detected!");
        OnTossComplete?.Invoke();

        // Reset
        currentVelocity = 0f;
        OnTossProgress?.Invoke(0f);
    }

    private void StopTracking()
    {
        isTracking = false;
        currentVelocity = 0f;
        OnTossProgress?.Invoke(0f);
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

    /// <summary>
    /// Set the origin point for gesture detection (usually the pan handle position)
    /// </summary>
    public void SetTossOrigin(Transform origin)
    {
        tossOrigin = origin;
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

    /// <summary>
    /// Reset the gesture detector state
    /// </summary>
    public void ResetGesture()
    {
        StopTracking();
    }

    /// <summary>
    /// Set the minimum velocity threshold for toss detection
    /// </summary>
    public void SetVelocityThreshold(float threshold)
    {
        minimumVelocityThreshold = threshold;
    }
}
