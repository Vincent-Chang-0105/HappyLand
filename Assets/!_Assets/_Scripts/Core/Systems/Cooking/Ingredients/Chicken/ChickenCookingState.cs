using UnityEngine;

public class ChickenCookingState : MonoBehaviour
{
    [Header("Cooking Sprites")]
    [SerializeField] private Sprite washedVersionSprite;
    [SerializeField] private Sprite boiledVersionSprite;
    [SerializeField] private Sprite friedVersionSprite;
    [SerializeField] private Sprite sinigangVersionSprite;

    [Header("Auto Bowl Settings")]
    [SerializeField] private bool autoTeleportToBowl = true;
    [SerializeField] private string boilBowlTag = "BoilBowl";
    [SerializeField] private string fryBowlTag = "FryBowl";

    private SpriteRenderer spriteRenderer;

    // State flags
    private bool isWashed = false;
    private bool isBeingWashed = false;
    private bool isBoiled = false;
    private bool isBeingBoiled = false;
    private bool isFried = false;
    private bool isBeingFried = false;
    private bool isSiniganged = false;
    private bool isBeingSiniganged = false;

    // Events
    public System.Action OnWashComplete;
    public System.Action OnBoilComplete;
    public System.Action OnFryComplete;
    public System.Action OnSinigangComplete;

    public bool IsWashed => isWashed;
    public bool IsBoiled => isBoiled;
    public bool IsFried => isFried;
    public bool IsBeingWashed => isBeingWashed;
    public bool IsBeingBoiled => isBeingBoiled;
    public bool IsBeingFried => isBeingFried;
    public bool IsSiniganged => isSiniganged;
    public bool IsBeingSiniganged => isBeingSiniganged;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    #region Washing
    public void StartWashing()
    {
        if (isWashed || isBeingWashed) return;

        isBeingWashed = true;
        Debug.Log("ChickenCookingState: Started washing");
    }

    public void StopWashing()
    {
        if (!isBeingWashed) return;

        isBeingWashed = false;
        ResetAlpha();
        Debug.Log("ChickenCookingState: Stopped washing (cancelled)");
    }

    public void CompleteWashing()
    {
        if (!isBeingWashed) return;

        isBeingWashed = false;
        isWashed = true;

        if (washedVersionSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = washedVersionSprite;
        }

        ResetAlpha();
        OnWashComplete?.Invoke();

        // Tutorial event
        TutorialEvents.ChickenWashed();

        Debug.Log("ChickenCookingState: Washing complete!");
    }

    public bool CanBeWashed()
    {
        return !isWashed && !isBeingWashed;
    }
    #endregion

    #region Boiling
    public void StartBoiling()
    {
        if (isBoiled || isBeingBoiled || !isWashed) return;

        isBeingBoiled = true;
    }

    public void StopBoiling()
    {
        if (!isBeingBoiled) return;

        isBeingBoiled = false;
    }

    public void CompleteBoiling()
    {
        if (!isBeingBoiled) return;

        isBeingBoiled = false;
        isBoiled = true;

        if (boiledVersionSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = boiledVersionSprite;
        }

        OnBoilComplete?.Invoke();
    }

    public bool CanBeBoiled()
    {
        return isWashed && !isBoiled && !isBeingBoiled;
    }
    #endregion

    #region Frying
    public void StartFrying()
    {
        if (isFried || isBeingFried || !isWashed) return;

        isBeingFried = true;
    }

    public void StopFrying()
    {
        if (!isBeingFried) return;

        isBeingFried = false;
    }

    public void CompleteFrying()
    {
        if (!isBeingFried) return;

        isBeingFried = false;
        isFried = true;

        if (friedVersionSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = friedVersionSprite;
        }

        OnFryComplete?.Invoke();
    }

    public bool CanBeFried()
    {
        bool canFry = isWashed && isBoiled && !isFried && !isBeingFried;
        return canFry;
    }
    #endregion

    #region Siniganging
    public void StartSiniganging()
    {
        if (isSiniganged || isBeingSiniganged || !isWashed || !isBoiled) return;

        isBeingSiniganged = true;
    }

    public void StopSiniganging()
    {
        if (!isBeingSiniganged) return;

        isBeingSiniganged = false;
    }

    public void CompleteSiniganging()
    {
        if (!isBeingSiniganged) return;

        isBeingSiniganged = false;
        isSiniganged = true;

        if (sinigangVersionSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = sinigangVersionSprite;
        }

        OnSinigangComplete?.Invoke();
    }

    public bool CanBeSiniganged()
    {
        return isWashed && isBoiled && !isSiniganged && !isBeingSiniganged;
    }
    #endregion

    private void ResetAlpha()
    {
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;
        }
    }

    public string GetBoilBowlTag() => boilBowlTag;
    public string GetFryBowlTag() => fryBowlTag;
    public bool ShouldAutoTeleport() => autoTeleportToBowl;

    #region DevMode Helpers

    /// <summary>
    /// [DevMode] Instantly sets chicken to fully cooked fried state
    /// </summary>
    public void DevSetAsFried()
    {
        isWashed = true;
        isBoiled = true;
        isFried = true;
        isBeingWashed = false;
        isBeingBoiled = false;
        isBeingFried = false;

        if (friedVersionSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = friedVersionSprite;
        }

        Debug.Log("[DevMode] Chicken set to fried state");
    }

    /// <summary>
    /// [DevMode] Instantly sets chicken to fully cooked sinigang state
    /// </summary>
    public void DevSetAsSinigang()
    {
        isWashed = true;
        isBoiled = true;
        isSiniganged = true;
        isBeingWashed = false;
        isBeingBoiled = false;
        isBeingSiniganged = false;

        if (sinigangVersionSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = sinigangVersionSprite;
        }

        Debug.Log("[DevMode] Chicken set to sinigang state");
    }

    #endregion
}
