using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CleaningArea : MonoBehaviour
{
    [Header("VFX")]
    [SerializeField] private ParticleSystem splashVFX;

    [Header("Scrubbing")]
    [SerializeField] private ScrubGestureDetector gestureDetector;
    [SerializeField] private ScrubPrompt scrubPrompt;
    [SerializeField] private int requiredScrubs = 4;

    [Header("Scrub Movement")]
    [SerializeField] private float scrubWiggleAmount = 0.08f;
    [SerializeField] private float scrubWiggleSpeed = 30f;
    [SerializeField] private float scrubRotationAmount = 15f;
    [SerializeField] private float rotationSmoothSpeed = 10f;

    // Track all washables in the area
    private class WashableData
    {
        public IWashable washable;
        public Collider2D collider;
        public Vector3 originalPosition;
        public Quaternion originalRotation;
        public int scrubCount;
        public float currentRotation;
    }

    private Dictionary<Collider2D, WashableData> washablesInArea = new Dictionary<Collider2D, WashableData>();
    private WashableData currentTarget;
    private bool isTrackingActive = false;
    private Coroutine waitForReleaseCoroutine;
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Start()
    {
        if (gestureDetector != null)
        {
            gestureDetector.OnScrubProgress += OnScrubProgress;
            gestureDetector.OnScrubCycleComplete += OnScrubCycleComplete;
            gestureDetector.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (gestureDetector != null)
        {
            gestureDetector.OnScrubProgress -= OnScrubProgress;
            gestureDetector.OnScrubCycleComplete -= OnScrubCycleComplete;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        IWashable washable = other.GetComponent<IWashable>();
        if (washable != null && washable.CanBeWashed())
        {
            Debug.Log($"Object {other.gameObject.name} entered cleaning area.");

            // Add to tracking list
            var data = new WashableData
            {
                washable = washable,
                collider = other,
                originalPosition = other.transform.position,
                originalRotation = other.transform.rotation,
                scrubCount = 0,
                currentRotation = 0f
            };
            washablesInArea[other] = data;

            // Notify washable
            washable.StartWashing();

            // Start tracking if this is the first washable
            if (washablesInArea.Count == 1)
            {
                StartTrackingArea();
            }

            UpdatePrompt();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (washablesInArea.TryGetValue(other, out WashableData data))
        {
            Debug.Log($"Object {other.gameObject.name} left cleaning area.");

            // Reset position/rotation
            other.transform.SetPositionAndRotation(data.originalPosition, data.originalRotation);

            // Notify washable
            data.washable?.StopWashing();

            // Remove from tracking
            washablesInArea.Remove(other);

            // Clear current target if it was this one
            if (currentTarget == data)
            {
                currentTarget = null;
            }

            // Stop tracking if no more washables
            if (washablesInArea.Count == 0)
            {
                StopTrackingArea();
            }

            UpdatePrompt();
        }
    }

    private void StartTrackingArea()
    {
        // Wait for mouse release before starting (to not conflict with drag)
        waitForReleaseCoroutine = StartCoroutine(WaitForMouseReleaseAndStartTracking());
    }

    private IEnumerator WaitForMouseReleaseAndStartTracking()
    {
        while (Input.GetMouseButton(0))
        {
            yield return null;
        }

        // Update original positions to where chickens were dropped
        foreach (var kvp in washablesInArea)
        {
            kvp.Value.originalPosition = kvp.Value.collider.transform.position;
            kvp.Value.originalRotation = kvp.Value.collider.transform.rotation;
        }

        if (washablesInArea.Count > 0 && gestureDetector != null)
        {
            isTrackingActive = true;
            gestureDetector.SetActive(true);
            gestureDetector.StartTracking();
        }

        waitForReleaseCoroutine = null;
    }

    private void StopTrackingArea()
    {
        if (waitForReleaseCoroutine != null)
        {
            StopCoroutine(waitForReleaseCoroutine);
            waitForReleaseCoroutine = null;
        }

        if (gestureDetector != null)
        {
            gestureDetector.StopTracking();
            gestureDetector.SetActive(false);
        }

        isTrackingActive = false;
        currentTarget = null;

        if (scrubPrompt != null)
        {
            scrubPrompt.HidePrompt();
        }
    }

    private void UpdatePrompt()
    {
        if (scrubPrompt == null) return;

        if (washablesInArea.Count > 0)
        {
            // Show prompt with current target's progress, or 0 if no target
            int currentScrubs = currentTarget?.scrubCount ?? 0;
            scrubPrompt.ShowPrompt(currentScrubs, requiredScrubs);
        }
        else
        {
            scrubPrompt.HidePrompt();
        }
    }

    private WashableData FindNearestWashable()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(mainCamera.transform.position.z);
        Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(mousePos);

        WashableData nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var kvp in washablesInArea)
        {
            float dist = Vector2.Distance(mouseWorldPos, (Vector2)kvp.Value.collider.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = kvp.Value;
            }
        }

        return nearest;
    }

    private void OnScrubProgress(float progress)
    {
        if (!isTrackingActive || washablesInArea.Count == 0) return;

        // Find the nearest washable to scrub
        WashableData nearest = FindNearestWashable();

        // Switch target if needed
        if (nearest != currentTarget)
        {
            // Reset previous target's rotation only (keep position where it is)
            if (currentTarget != null && currentTarget.collider != null)
            {
                currentTarget.collider.transform.rotation = currentTarget.originalRotation;
                currentTarget.currentRotation = 0f;
                // Update original position to current position so it stays in place
                currentTarget.originalPosition = currentTarget.collider.transform.position;
            }

            currentTarget = nearest;

            // Update the new target's original position to where it currently is
            if (currentTarget != null && currentTarget.collider != null)
            {
                currentTarget.originalPosition = currentTarget.collider.transform.position;
                currentTarget.originalRotation = currentTarget.collider.transform.rotation;
            }

            // Update gesture detector target
            if (gestureDetector != null && currentTarget != null)
            {
                gestureDetector.SetTarget(currentTarget.collider.transform);
            }

            UpdatePrompt();
        }

        if (currentTarget == null || currentTarget.collider == null) return;

        // Update prompt progress
        if (scrubPrompt != null)
        {
            scrubPrompt.UpdateProgress(progress);
        }

        // Wiggle and rotate the current target
        if (progress > 0)
        {
            float wiggleX = Mathf.Sin(Time.time * scrubWiggleSpeed) * scrubWiggleAmount;
            float wiggleY = Mathf.Cos(Time.time * scrubWiggleSpeed * 0.8f) * scrubWiggleAmount * 0.3f;

            Vector3 newPos = currentTarget.originalPosition;
            newPos.x += wiggleX;
            newPos.y += wiggleY;
            currentTarget.collider.transform.position = newPos;

            // Rotate based on scrub direction
            int direction = gestureDetector.CurrentDirection;
            float targetRotation = direction * scrubRotationAmount * progress;
            currentTarget.currentRotation = Mathf.Lerp(currentTarget.currentRotation, targetRotation, Time.deltaTime * rotationSmoothSpeed);
            currentTarget.collider.transform.rotation = currentTarget.originalRotation * Quaternion.Euler(0, 0, currentTarget.currentRotation);
        }
        else
        {
            // Return to original rotation
            currentTarget.currentRotation = Mathf.Lerp(currentTarget.currentRotation, 0f, Time.deltaTime * rotationSmoothSpeed);
            currentTarget.collider.transform.rotation = currentTarget.originalRotation * Quaternion.Euler(0, 0, currentTarget.currentRotation);
        }
    }

    private void OnScrubCycleComplete()
    {
        if (currentTarget == null) return;

        Debug.Log($"Scrub cycle complete on {currentTarget.collider.gameObject.name}!");

        // Increment this chicken's scrub count
        currentTarget.scrubCount++;

        if (scrubPrompt != null)
        {
            scrubPrompt.ShowScrubSuccess();
        }

        // Play splash VFX
        if (splashVFX != null && currentTarget.collider != null)
        {
            Vector3 vfxPos = currentTarget.collider.transform.position;
            vfxPos.z = -2f;
            splashVFX.transform.position = vfxPos;
            splashVFX.Stop();
            splashVFX.Play();
        }

        // Check if this chicken is fully washed
        if (currentTarget.scrubCount >= requiredScrubs)
        {
            CompleteWashing(currentTarget);
        }
        else
        {
            UpdatePrompt();
        }

        // Reset gesture detector for next cycle
        if (gestureDetector != null)
        {
            gestureDetector.ResetProgress();
        }
    }

    private void CompleteWashing(WashableData data)
    {
        Debug.Log($"All scrubs complete! {data.collider.gameObject.name} washed.");

        // Reset position/rotation
        data.collider.transform.SetPositionAndRotation(data.originalPosition, data.originalRotation);

        // Complete the washing
        data.washable?.CompleteWashing();

        // Remove from tracking
        washablesInArea.Remove(data.collider);

        // Clear current target if it was this one
        if (currentTarget == data)
        {
            currentTarget = null;
        }

        // Stop tracking if no more washables
        if (washablesInArea.Count == 0)
        {
            StopTrackingArea();
        }
        else
        {
            UpdatePrompt();
        }
    }
}
