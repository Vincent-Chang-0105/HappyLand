using UnityEngine;

public class ChickenCookingState : MonoBehaviour
{
    [Header("Cooking Sprites")]
    [SerializeField] private Sprite washedVersionSprite;
    [SerializeField] private Sprite boiledVersionSprite;
    [SerializeField] private Sprite friedVersionSprite;
    [SerializeField] private Sprite sinigangVersionSprite;

    [Header("Washing Settings")]
    [SerializeField] private float washDuration = 2f;

    [Header("Auto Bowl Settings")]
    [SerializeField] private bool autoTeleportToBowl = true;
    [SerializeField] private string boilBowlTag = "BoilBowl";
    [SerializeField] private string fryBowlTag = "FryBowl";

    private SpriteRenderer spriteRenderer;
    private float washTimer = 0f;

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

    private void Update()
    {
        if (isBeingWashed)
        {
            UpdateWashing();
        }
    }

    #region Washing
    public void StartWashing()
    {
        if (isWashed || isBeingWashed) return;

        isBeingWashed = true;
        washTimer = 0f;

    }

    public void StopWashing()
    {
        if (!isBeingWashed) return;

        isBeingWashed = false;
        washTimer = 0f;

        ResetAlpha();
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
    }

    public bool CanBeWashed()
    {
        return !isWashed && !isBeingWashed;
    }

    private void UpdateWashing()
    {
        washTimer += Time.deltaTime;

        if (washTimer >= washDuration)
        {
            CompleteWashing();
            return;
        }

        // Visual feedback - washing animation
        if (spriteRenderer != null)
        {
            float alpha = Mathf.Lerp(0.7f, 1f, Mathf.PingPong(washTimer * 3f, 1f));
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
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
}
