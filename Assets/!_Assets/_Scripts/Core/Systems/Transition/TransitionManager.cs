using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Unity.VisualScripting;

public class TransitionManager : PersistentSingleton<TransitionManager>
{
    [Header("Transition Settings")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeSpeed = 2f;
    [SerializeField] private Color fadeColor = Color.black;

    private bool isTransitioning = false;
    
    private void Start()
    {
        // Make sure fade starts transparent
        if (fadeImage != null)
        {
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
            fadeImage.gameObject.SetActive(false);
        }
    }
    
    public void TransitionToPosition(Vector3 targetPosition, Transform player, bool faceRight)
    {
        if (!isTransitioning)
        {
            StartCoroutine(SmoothTransition(targetPosition, player, faceRight));
        }
    }

    private IEnumerator SmoothTransition(Vector3 targetPosition, Transform player, bool faceRight)
    {
        isTransitioning = true;
        
        // Disable player input
        if (InputSystem.Instance != null)
        {
            InputSystem.Instance.DisablePlayerInputs();
        }
        
        // Fade out
        yield return StartCoroutine(FadeOut());

        // Move player during fade
        if (player != null)
        {
            player.position = targetPosition;

            // Stop any movement
            Rigidbody playerRb = player.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector3.zero;
            }
            // Flip player sprite based on direction
            SpriteRenderer playerSprite = player.GetComponentInChildren<SpriteRenderer>();
            if (playerSprite != null)
            {
                playerSprite.flipX = faceRight;
            }
        }
        
        // Small delay for positioning
        yield return new WaitForSeconds(0.5f);
        
        // Fade in
        yield return StartCoroutine(FadeIn());
        
        // Re-enable player input
        if (InputSystem.Instance != null)
        {
            InputSystem.Instance.EnablePlayerInputs();
        }
        
        isTransitioning = false;
    }

    private IEnumerator FadeOut()
    {
        if (fadeImage == null) yield break;

        fadeImage.gameObject.SetActive(true);
        float alpha = 0f;
        while (alpha < 1f)
        {
            alpha += fadeSpeed * Time.deltaTime;
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
            yield return null;
        }

        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
    }

    private IEnumerator FadeIn()
    {
        if (fadeImage == null) yield break;
        
        float alpha = 1f;
        while (alpha > 0f)
        {
            alpha -= fadeSpeed * Time.deltaTime;
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
            yield return null;
        }

        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        fadeImage.gameObject.SetActive(false);
    }
}