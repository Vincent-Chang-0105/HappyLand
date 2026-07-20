using UnityEngine;
using DG.Tweening;

/// <summary>
/// Owns all visual side-effects on the Pan (oil, liquid/broth sprite, color transitions).
/// Extracted from Pan.cs so Pan itself has zero visual bookkeeping.
/// </summary>
public class PanVisualController : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private GameObject liquidVisual;    // used for water, broth, adobo sauce
    [SerializeField] private GameObject oilVisual;
    [SerializeField] private ParticleSystem oilEffect;
    [SerializeField] private SpriteRenderer panSprite;

    [Header("Colors")]
    [SerializeField] private Color normalPanColor = Color.white;

    private SpriteRenderer liquidSprite;

    private void Awake()
    {
        if (liquidVisual != null)
            liquidSprite = liquidVisual.GetComponent<SpriteRenderer>();
    }

    // --- Oil ---

    public void ShowOil()
    {
        if (oilVisual != null) oilVisual.SetActive(true);
        if (oilEffect  != null) oilEffect.Play();
    }

    private void HideOil()
    {
        if (oilVisual != null) oilVisual.SetActive(false);
        if (oilEffect  != null) oilEffect.Stop();
        if (panSprite  != null) panSprite.color = normalPanColor;
    }

    // --- Liquid / Broth ---

    /// <summary>Show the liquid visual, animated in, and tinted to brothColor.</summary>
    public void ShowLiquid(Color brothColor)
    {
        if (liquidVisual == null) return;

        liquidVisual.SetActive(true);
        liquidVisual.transform.localScale = Vector3.zero;
        liquidVisual.transform.DOScale(0.85f, 0.4f).SetEase(Ease.OutBack);

        if (liquidSprite != null)
            liquidSprite.color = Color.white;

        TintLiquid(brothColor);
    }

    /// <summary>Smoothly change the liquid tint (e.g. catsup → mechado color).</summary>
    public void TintLiquid(Color targetColor)
    {
        if (liquidSprite != null)
            liquidSprite.DOColor(targetColor, 0.4f);
    }

    private void HideLiquid()
    {
        if (liquidVisual == null) return;

        liquidVisual.transform.DOKill();
        liquidVisual.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                liquidVisual.SetActive(false);
                if (liquidSprite != null) liquidSprite.color = Color.white;
            });
    }

    // --- Role-driven visuals called by Pan ---

    /// <summary>
    /// Called by Pan.AddIngredient() so the visual reacts to each role addition.
    /// activeMode may be null if the mode isn't fully unlocked yet.
    /// </summary>
    public void OnRoleAdded(IngredientRole role, CookingModeSO activeMode)
    {
        switch (role)
        {
            case IngredientRole.FryingOil:
                ShowOil();
                break;

            case IngredientRole.CookingWater:
                // Show liquid immediately with plain water color; mode SO will tint later
                ShowLiquid(Color.white);
                break;

            case IngredientRole.AdoboOnionGarlic:
                // Adobo uses the oil visual area but tints it to adobo broth color
                if (liquidVisual != null)
                {
                    liquidVisual.SetActive(true);
                }
                if (activeMode != null) TintLiquid(activeMode.brothColor);
                break;

            case IngredientRole.SinigangMix:
            case IngredientRole.MechadoCatsup:
            case IngredientRole.MechadoPowderedMilk:
                if (activeMode != null) TintLiquid(activeMode.brothColor);
                break;
        }
    }

    // --- Reset ---

    public void Reset()
    {
        HideOil();

        // Force-hide liquid even if it was shown without setting hasWater (e.g. adobo)
        if (liquidVisual != null && liquidVisual.activeSelf)
        {
            liquidVisual.transform.DOKill();
            liquidVisual.SetActive(false);
            if (liquidSprite != null) liquidSprite.color = Color.white;
        }

        if (panSprite != null) panSprite.color = normalPanColor;
    }
}
