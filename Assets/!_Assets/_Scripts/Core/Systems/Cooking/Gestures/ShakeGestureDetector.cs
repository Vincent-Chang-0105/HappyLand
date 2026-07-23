using UnityEngine;
using System;

/// <summary>
/// Detects rapid side-to-side mouse movement (shaking motion) for sinigang cooking.
/// Tracks horizontal velocity and direction changes to detect quick left-right shaking.
/// </summary>
public class ShakeGestureDetector : MonoBehaviour
{
    [Header("Gesture Settings")]
    [SerializeField] private float minimumShakeDistance = 0.5f; // Min horizontal distance for one direction
    [SerializeField] private float minimumVelocityThreshold = 3f; // Min horizontal speed to count
    [SerializeField] private int requiredShakeCycles = 3; // Number of back-and-forth cycles needed
    [SerializeField] private float maxShakeDistance = 3f; // Max distance from shake origin to register

    [Header("Visual Feedback")]
    [SerializeField] private Transform shakeOrigin; // The pan position

    // Events
    public event Action OnShakeComplete; // One full shake cycle done
    public event Action<float> OnShakeProgress; // Returns progress 0-1 within current shake
    public event Action OnAllShakesComplete; // All required shakes done

    // State
    private bool isTracking = false;
    private Vector2 lastMousePos;
    private float currentDirectionDistance = 0f;
    private int currentDirection = 0; // -1 = left, 0 = none, 1 = right
    private int completedDirectionChanges = 0;
    private int completedCycles = 0;
    private Camera mainCamera;

    // Progress
    public bool IsShaking => isTracking;
    public int CompletedCycles => completedCycles;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!isTracking) return;

        if (Input.GetMouseButtonDown(0))
        {
            lastMousePos = GetMouseWorldPosition();
            currentDirectionDistance = 0f;
            currentDirection = 0;
        }

        if (Input.GetMouseButton(0))
        {
            TrackShaking();
        }

        if (Input.GetMouseButtonUp(0))
        {
            currentDirectionDistance = 0f;
            currentDirection = 0;
        }
    }

    private void TrackShaking()
    {
        Vector2 mousePos = GetMouseWorldPosition();

        // Check distance from shake origin
        if (shakeOrigin != null)
        {
            float distanceToOrigin = Vector2.Distance(mousePos, (Vector2)shakeOrigin.position);
            if (distanceToOrigin > maxShakeDistance)
            {
                lastMousePos = mousePos;
                return; // Too far from pan, don't count
            }
        }

        float deltaX = mousePos.x - lastMousePos.x;
        lastMousePos = mousePos;

        if (Mathf.Abs(deltaX) < 0.001f) return;

        int newDirection = deltaX > 0 ? 1 : -1;

        // Direction changed
        if (currentDirection != 0 && newDirection != currentDirection)
        {
            if (currentDirectionDistance >= minimumShakeDistance)
            {
                completedDirectionChanges++;

                // Every 2 direction changes = 1 full cycle (left-right or right-left)
                if (completedDirectionChanges % 2 == 0)
                {
                    completedCycles++;
                    OnShakeComplete?.Invoke();

                    if (completedCycles >= requiredShakeCycles)
                    {
                        OnAllShakesComplete?.Invoke();
                        StopTracking();
                        return;
                    }
                }
            }
            currentDirectionDistance = 0f;
        }

        currentDirection = newDirection;
        currentDirectionDistance += Mathf.Abs(deltaX);

        // Emit progress
        float cycleProgress = (float)completedCycles / requiredShakeCycles;
        float withinCycleProgress = Mathf.Clamp01(currentDirectionDistance / minimumShakeDistance);
        float totalProgress = cycleProgress + (withinCycleProgress / requiredShakeCycles);
        OnShakeProgress?.Invoke(Mathf.Clamp01(totalProgress));
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

    public void SetShakeOrigin(Transform origin)
    {
        shakeOrigin = origin;
    }

    public void SetActive(bool active)
    {
        enabled = active;
        if (!active)
        {
            StopTracking();
        }
    }

    public void StartTracking()
    {
        isTracking = true;
        completedCycles = 0;
        completedDirectionChanges = 0;
        currentDirectionDistance = 0f;
        currentDirection = 0;
    }

    public void StopTracking()
    {
        isTracking = false;
        currentDirectionDistance = 0f;
        currentDirection = 0;
    }

    public void ResetGesture()
    {
        StopTracking();
        completedCycles = 0;
        completedDirectionChanges = 0;
    }
}
