using UnityEngine;
using DG.Tweening;
using AudioSystem;

public class Faucet : MonoBehaviour, IInteractable
{
    [Header("Faucet Settings")]
    [SerializeField] private Transform faucetHandle;
    [SerializeField] private BoxCollider2D cleaningAreaCollider;
    [SerializeField] private float handleRotationAngle = 90f;
    [SerializeField] private float rotationDuration = 0.5f;
    [SerializeField] private Ease rotationEase = Ease.OutQuart;
    
    [Header("Water Effects")]
    [SerializeField] private ParticleSystem waterEffect;
    [SerializeField] private GameObject waterSpriteObject; // The GameObject with SpriteRenderer + Animator
    [SerializeField] private SoundData waterSound;
    [SerializeField] private float waterForce = 5f;
    [SerializeField] private float waterFadeDuration = 0.3f; // Duration of fade in/out

    [Header("Water Cost")]
    [SerializeField] private WaterCostTracker waterCostTracker;

    [SerializeField] private bool isOn = false;
    private Tween handleTween;
    private Tween waterFadeTween;
    private SpriteRenderer waterSpriteRenderer;
    
    #region IInteractable Implementation
    public void OnInteract()
    {
        ToggleFaucet();
    }
    
    public bool CanInteract()
    {
        // Always allow turning OFF
        if (isOn) return true;

        // If off, check if player can afford to turn on
        if (MoneyManager.Instance != null && !MoneyManager.Instance.CanAfford(1))
            return false;

        return true;
    }
    #endregion
    
    public void ToggleFaucet()
    {
        isOn = !isOn;

        if (cleaningAreaCollider != null)
        {
            cleaningAreaCollider.enabled = isOn;
        }

        // Rotate handle - water effects will be triggered after animation completes
        RotateHandle();

        // Tutorial Event
        if (isOn)
            TutorialEvents.FaucetOpened();
        else
            TutorialEvents.FaucetClosed();

        Debug.Log($"Faucet {(isOn ? "turned ON" : "turned OFF")}");
    }
    
    private void RotateHandle()
    {
        if (faucetHandle == null) return;

        // Kill existing tween
        if (handleTween != null && handleTween.IsActive())
        {
            handleTween.Kill();
        }

        // Calculate target rotation
        float targetZ = isOn ? handleRotationAngle : 0f;
        Vector3 targetRotation = new Vector3(0, 0, targetZ);

        // If turning OFF, stop water immediately
        if (!isOn)
        {
            ToggleWaterEffects();
            if (waterCostTracker != null)
            {
                waterCostTracker.OnFaucetTurnedOff();
            }
        }

        // Animate handle rotation
        handleTween = faucetHandle.DORotate(targetRotation, rotationDuration)
            .SetEase(rotationEase)
            .OnComplete(() =>
            {
                // Only start water effects after animation when turning ON
                if (isOn)
                {
                    ToggleWaterEffects();
                    if (waterCostTracker != null)
                    {
                        waterCostTracker.OnFaucetTurnedOn();
                    }
                }
            });
    }
    
    private void ToggleWaterEffects()
    {
        // Toggle particle effect
        if (waterEffect != null)
        {
            if (isOn)
            {
                waterEffect.Play();
            }
            else
            {
                waterEffect.Stop();
            }
        }

        // Toggle water sprite animation with fade
        if (waterSpriteObject != null)
        {
            // Get SpriteRenderer reference if not cached
            if (waterSpriteRenderer == null)
            {
                waterSpriteRenderer = waterSpriteObject.GetComponent<SpriteRenderer>();
            }

            if (waterSpriteRenderer != null)
            {
                // Kill any existing fade tween
                if (waterFadeTween != null && waterFadeTween.IsActive())
                {
                    waterFadeTween.Kill();
                }

                if (isOn)
                {
                    // Fade in
                    waterSpriteObject.SetActive(true);
                    waterSpriteRenderer.color = new Color(waterSpriteRenderer.color.r, waterSpriteRenderer.color.g, waterSpriteRenderer.color.b, 0f);
                    waterFadeTween = waterSpriteRenderer.DOFade(1f, waterFadeDuration).SetEase(Ease.OutQuad);
                }
                else
                {
                    // Fade out
                    waterFadeTween = waterSpriteRenderer.DOFade(0f, waterFadeDuration)
                        .SetEase(Ease.InQuad)
                        .OnComplete(() => waterSpriteObject.SetActive(false));
                }
            }
        }

        // Toggle water sound
        if (waterSound != null)
        {
            if (isOn)
            {
                //aterSound.Play();
            }
            else
            {
                //waterSound.Stop();
            }
        }
    }
    
    public bool IsOn()
    {
        return isOn;
    }
    
    private void OnDestroy()
    {
        // Cleanup tweens
        if (handleTween != null && handleTween.IsActive())
        {
            handleTween.Kill();
        }

        if (waterFadeTween != null && waterFadeTween.IsActive())
        {
            waterFadeTween.Kill();
        }
    }
}