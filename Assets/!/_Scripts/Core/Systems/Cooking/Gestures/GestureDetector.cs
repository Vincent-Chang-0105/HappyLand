using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Detects circular stirring gestures from mouse input.
/// Tracks mouse position while button is held and calculates cumulative angle changes.
/// </summary>
public class GestureDetector : MonoBehaviour
{
    [Header("Gesture Settings")]
    [SerializeField] private float minimumCircleRadius = 0.5f;
    [SerializeField] private float angleThreshold = 330f; // Degrees needed for a complete stir
    [SerializeField] private int minimumSamples = 10; // Minimum mouse positions to register gesture

    [Header("Visual Feedback")]
    [SerializeField] private Transform stirCenter; // The center point for stirring (pot position)

    // Events
    public event Action OnStirComplete;
    public event Action<float> OnStirProgress; // Returns progress 0-1

    // State
    private bool isTracking = false;
    private List<Vector2> mousePositions = new List<Vector2>();
    private float cumulativeAngle = 0f;
    private Vector2 lastMousePos;
    private Camera mainCamera;

    // Progress
    public float CurrentProgress => Mathf.Clamp01(Mathf.Abs(cumulativeAngle) / angleThreshold);
    public bool IsStirring => isTracking;

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

        // Stop tracking when button is released
        if (Input.GetMouseButtonUp(0))
        {
            StopTracking();
        }
    }

    private void StartTracking()
    {
        isTracking = true;
        mousePositions.Clear();
        cumulativeAngle = 0f;

        Vector2 worldPos = GetMouseWorldPosition();
        mousePositions.Add(worldPos);
        lastMousePos = worldPos;
    }

    private void TrackMousePosition()
    {
        Vector2 currentMousePos = GetMouseWorldPosition();

        // Only track if mouse has moved significantly
        if (Vector2.Distance(currentMousePos, lastMousePos) < 0.05f)
            return;

        mousePositions.Add(currentMousePos);

        // Calculate angle if we have enough positions
        if (mousePositions.Count >= 3)
        {
            CalculateAngleChange(currentMousePos);
        }

        lastMousePos = currentMousePos;

        // Emit progress event
        OnStirProgress?.Invoke(CurrentProgress);

        // Check if stir is complete
        if (Mathf.Abs(cumulativeAngle) >= angleThreshold && mousePositions.Count >= minimumSamples)
        {
            CompleteStir();
        }
    }

    private void StopTracking()
    {
        isTracking = false;

        // Reset if stir wasn't completed
        mousePositions.Clear();
        cumulativeAngle = 0f;
        OnStirProgress?.Invoke(0f);
    }

    private void CalculateAngleChange(Vector2 currentPos)
    {
        if (stirCenter == null)
            return;

        Vector2 center = stirCenter.position;
        Vector2 prevPos = mousePositions[mousePositions.Count - 2];

        // Calculate vectors from center to previous and current positions
        Vector2 prevVector = prevPos - center;
        Vector2 currentVector = currentPos - center;

        // Check if positions are far enough from center
        if (prevVector.magnitude < minimumCircleRadius || currentVector.magnitude < minimumCircleRadius)
            return;

        // Calculate signed angle between vectors
        float angle = Vector2.SignedAngle(prevVector, currentVector);

        // Only add angle if it's reasonable (not a huge jump)
        if (Mathf.Abs(angle) < 90f)
        {
            cumulativeAngle += angle;
        }
    }

    private void CompleteStir()
    {
        OnStirComplete?.Invoke();

        // Reset for next stir
        mousePositions.Clear();
        cumulativeAngle = 0f;
        isTracking = false;

        OnStirProgress?.Invoke(0f);
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
    /// Set the center point for gesture detection (usually the pot's position)
    /// </summary>
    public void SetStirCenter(Transform center)
    {
        stirCenter = center;
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
}
