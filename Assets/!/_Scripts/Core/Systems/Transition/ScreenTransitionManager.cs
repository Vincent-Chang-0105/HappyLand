using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class ScreenTransitionManager : MonoBehaviour
{
    [Header("Screens")]
    [SerializeField] private RectTransform cookingScreen;
    [SerializeField] private RectTransform servingScreen;
    
    [Header("Positions")]
    [SerializeField] private Vector2 centerPosition = Vector2.zero;
    [SerializeField] private Vector2 topPosition = new Vector2(0, 1080);    // Off-screen top
    [SerializeField] private Vector2 bottomPosition = new Vector2(0, -1080); // Off-screen bottom
    
    [Header("Animation")]
    [SerializeField] private float animationDuration = 0.7f;
    [SerializeField] private Ease slideEase = Ease.OutCubic;

    [Header("Buttons")]
    [SerializeField] private Button toggleButton;
    
    private bool isTransitioning = false;
    
    void Start()
    {
        // Setup button listeners
        toggleButton.onClick.AddListener(() =>
        {
            if (cookingScreen.anchoredPosition == centerPosition)
                SwitchToServing();
            else
                SwitchToCooking();
        });

        // Initialize positions
        cookingScreen.anchoredPosition = centerPosition;
        servingScreen.anchoredPosition = topPosition;
    }
    
    public void SwitchToCooking()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        
        // Cooking slides down to center, Serving slides down to bottom
        cookingScreen.DOAnchorPos(centerPosition, animationDuration).SetEase(slideEase);
        servingScreen.DOAnchorPos(topPosition, animationDuration).SetEase(slideEase)
            .OnComplete(() => isTransitioning = false);
    }
    
    public void SwitchToServing()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        
        // Serving slides up to center, Cooking slides up to top
        servingScreen.DOAnchorPos(centerPosition, animationDuration).SetEase(slideEase);
        cookingScreen.DOAnchorPos(bottomPosition, animationDuration).SetEase(slideEase)
            .OnComplete(() => isTransitioning = false);
    }
}
