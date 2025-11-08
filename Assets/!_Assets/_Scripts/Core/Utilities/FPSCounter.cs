using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI fpsText; // Optional UI Text to display FPS
    private int frameCount = 0; // Count of frames in the interval
    private float elapsedTime = 0f; // Time passed in the interval
    private float updateInterval = 0.02f; // Time interval to calculate FPS

    private void Update()
    {
        frameCount++;
        elapsedTime += Time.deltaTime;

        if (elapsedTime >= updateInterval)
        {
            float fps = frameCount / elapsedTime; // Calculate FPS
            DisplayFPS(fps);

            // Reset counters
            frameCount = 0;
            elapsedTime = 0f;

        }
    }

    private void DisplayFPS(float fps)
    {
        // If a UI Text is assigned, display FPS on it
        if (fpsText != null)
        {
            fpsText.text = $"FPS: {Mathf.RoundToInt(fps)}";
        }

    }
}
