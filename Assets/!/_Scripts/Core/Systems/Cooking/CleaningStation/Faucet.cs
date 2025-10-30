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
    [SerializeField] private SoundData waterSound;
    [SerializeField] private float waterForce = 5f;
    
    [SerializeField] private bool isOn = false;
    private Tween handleTween;
    
    #region IInteractable Implementation
    public void OnInteract()
    {
        ToggleFaucet();
    }
    
    public bool CanInteract()
    {
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
        
        // Rotate handle
        RotateHandle();
        
        // Toggle water effects
        ToggleWaterEffects();
        
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
        
        // Animate handle rotation
        handleTween = faucetHandle.DORotate(targetRotation, rotationDuration)
            .SetEase(rotationEase);
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
    }
}