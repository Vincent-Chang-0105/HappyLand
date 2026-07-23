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
    [SerializeField] private float waterForce = 5f;
    [SerializeField] private float waterFadeDuration = 0.3f; // Duration of fade in/out

    [Header("Sounds")]
    [SerializeField] private SoundData faucetOnSound;
    [SerializeField] private SoundData faucetOffSound;
    [SerializeField] private SoundData waterLoopSound;

    [Header("Water Cost")]
    [SerializeField] private WaterCostTracker waterCostTracker;

    [SerializeField] private bool isOn = false;
    private Tween handleTween;
    private Tween waterFadeTween;
    private SpriteRenderer waterSpriteRenderer;
    private SoundEmitter waterLoopEmitter;
    
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

        //Debug.Log($"Faucet {(isOn ? "turned ON" : "turned OFF")}");
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

        // Play click sound immediately when handle starts turning
        if (SoundManager.Instance != null)
        {
            SoundData clickSound = isOn ? faucetOnSound : faucetOffSound;
            if (clickSound != null)
                SoundManager.Instance.CreateSoundBuilder().Play(clickSound);
        }

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

        // Toggle water loop sound
        if (isOn)
        {
            if (waterLoopSound != null && SoundManager.Instance != null)
                waterLoopEmitter = SoundManager.Instance.CreateSoundBuilder().Play(waterLoopSound);
        }
        else
        {
            if (waterLoopEmitter != null)
            {
                waterLoopEmitter.FadeOutAndStop(waterFadeDuration);
                waterLoopEmitter = null;
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
            handleTween.Kill();

        if (waterFadeTween != null && waterFadeTween.IsActive())
            waterFadeTween.Kill();

        // Cleanup water loop sound
        if (waterLoopEmitter != null)
        {
            waterLoopEmitter.Stop();
            waterLoopEmitter = null;
        }
    }
}