using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public enum DirectionButton
{
    Up,
    Down,
    Left,
    Right
}

[System.Serializable]
public class NavigationButton
{
    [Header("Button Configuration")]
    public DirectionButton buttonDirection; // Which button to modify (up/down/left/right)
    public Sprite buttonSprite; // The image to set for this button
    
    [Header("Navigation Target")]
    public Vector2 targetGridPosition;
    public string targetScreenName; // Alternative to grid position
}

[System.Serializable]
public class ScreenData
{
    public string screenName;
    public RectTransform screenTransform;
    public Vector2 gridPosition; // Grid coordinates (x, y)
    public Vector2 worldPosition; // Actual UI position
    
    [Header("Screen-Specific Navigation")]
    public List<NavigationButton> navigationButtons = new List<NavigationButton>();
}

public class ScreenTransitionManager : MonoBehaviour
{
    [Header("Screen Grid Setup")]
    [SerializeField] private List<ScreenData> screens = new List<ScreenData>();
    [SerializeField] private Vector2 screenSpacing = new Vector2(1920, 1080); // Distance between screens
    
    [Header("Navigation")]
    [SerializeField] private Vector2 currentGridPosition = new Vector2(0, 0); // Current active screen grid position
    
    [Header("Animation")]
    [SerializeField] private float animationDuration = 0.7f;
    [SerializeField] private Ease slideEase = Ease.OutCubic;

    [Header("Optional Global Navigation Buttons")]
    [SerializeField] private Button upButton;
    [SerializeField] private Button downButton;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    
    private bool isTransitioning = false;
    private ScreenData currentScreen;
    
    void Start()
    {
        SetupButtonListeners();
        InitializeScreenPositions();
        SetActiveScreen(currentGridPosition);
    }
    
    private void SetupButtonListeners()
    {
        // Setup global navigation buttons (optional)
        if (upButton != null)
            upButton.onClick.AddListener(() => MoveToScreen(Vector2.up));
        if (downButton != null)
            downButton.onClick.AddListener(() => MoveToScreen(Vector2.down));
        if (leftButton != null)
            leftButton.onClick.AddListener(() => MoveToScreen(Vector2.left));
        if (rightButton != null)
            rightButton.onClick.AddListener(() => MoveToScreen(Vector2.right));
            
    }
    
    private void InitializeScreenPositions()
    {
        foreach (ScreenData screen in screens)
        {
            // Calculate world position based on grid position
            screen.worldPosition = screen.gridPosition * screenSpacing;
            
            // Set initial screen positions
            if (screen.screenTransform != null)
            {
                screen.screenTransform.anchoredPosition = screen.worldPosition;
            }
        }
    }
    
    public void MoveToScreen(Vector2 direction)
    {
        if (isTransitioning) return;
        
        Vector2 targetGridPos = currentGridPosition + direction;
        ScreenData targetScreen = GetScreenAtGridPosition(targetGridPos);
        
        if (targetScreen != null)
        {
            StartCoroutine(TransitionToScreen(targetScreen));
        }
    }
    
    public void MoveToScreenDirect(Vector2 gridPosition)
    {
        if (isTransitioning) return;
        
        ScreenData targetScreen = GetScreenAtGridPosition(gridPosition);
        if (targetScreen != null)
        {
            StartCoroutine(TransitionToScreen(targetScreen));
        }
    }
    
    public void MoveToScreenByName(string screenName)
    {
        if (isTransitioning) return;
        
        ScreenData targetScreen = screens.Find(s => s.screenName == screenName);
        if (targetScreen != null)
        {
            StartCoroutine(TransitionToScreen(targetScreen));
        }
    }
    
    private IEnumerator TransitionToScreen(ScreenData targetScreen)
    {
        isTransitioning = true;

        // Hide all navigation buttons during transition
        HideAllNavigationButtons();
        
        Vector2 offset = currentGridPosition * screenSpacing - targetScreen.gridPosition * screenSpacing;
        
        // Move all screens simultaneously
        List<Tween> moveTweens = new List<Tween>();
        
        foreach (ScreenData screen in screens)
        {
            if (screen.screenTransform != null)
            {
                Vector2 newPosition = screen.screenTransform.anchoredPosition + offset;
                Tween moveTween = screen.screenTransform.DOAnchorPos(newPosition, animationDuration).SetEase(slideEase);
                moveTweens.Add(moveTween);
            }
        }
        
        // Wait for all tweens to complete
        yield return DOTween.Sequence().Append(moveTweens[0]).WaitForCompletion();
        
        currentGridPosition = targetScreen.gridPosition;
        currentScreen = targetScreen;

        UpdateButtonStates();

        isTransitioning = false;
    }
    
    private void SetActiveScreen(Vector2 gridPosition)
    {
        currentScreen = GetScreenAtGridPosition(gridPosition);
        UpdateButtonStates();
    }
    
    private ScreenData GetScreenAtGridPosition(Vector2 gridPos)
    {
        return screens.Find(screen => screen.gridPosition == gridPos);
    }
    
    private void UpdateButtonStates()
    {
        // Enable/disable global navigation buttons based on available screens
        if (upButton != null)
            // Show upButton when in bottom row (y = 0) to go to serving screen (y = 1)
            upButton.gameObject.SetActive(currentGridPosition.y == 0);
        if (downButton != null)
            downButton.interactable = GetScreenAtGridPosition(currentGridPosition + Vector2.down) != null;
        if (leftButton != null)
            leftButton.interactable = GetScreenAtGridPosition(currentGridPosition + Vector2.left) != null;
        if (rightButton != null)
            rightButton.interactable = GetScreenAtGridPosition(currentGridPosition + Vector2.right) != null;
            
        // Update screen-specific button states
        UpdateScreenSpecificButtonStates();
    }
    
    private void UpdateScreenSpecificButtonStates()
    {
        // First, hide all directional buttons
        if (upButton != null) upButton.gameObject.SetActive(false);
        if (downButton != null) downButton.gameObject.SetActive(false);
        if (leftButton != null) leftButton.gameObject.SetActive(false);
        if (rightButton != null) rightButton.gameObject.SetActive(false);
        
        // Find current screen configuration
        ScreenData currentScreenData = screens.Find(screen => screen.gridPosition == currentGridPosition);
        
        if (currentScreenData != null)
        {
            foreach (NavigationButton navButton in currentScreenData.navigationButtons)
            {
                Button targetButton = GetDirectionalButton(navButton.buttonDirection);
                
                if (targetButton != null)
                {
                    // Show and configure the button
                    targetButton.gameObject.SetActive(true);
                    
                    // Update button image
                    Image buttonImage = targetButton.GetComponent<Image>();
                    if (buttonImage != null && navButton.buttonSprite != null)
                    {
                        buttonImage.sprite = navButton.buttonSprite;
                    }
                    
                    // Verify target exists and enable button
                    bool targetExists = false;
                    if (!string.IsNullOrEmpty(navButton.targetScreenName))
                    {
                        targetExists = screens.Find(s => s.screenName == navButton.targetScreenName) != null;
                    }
                    else
                    {
                        targetExists = GetScreenAtGridPosition(navButton.targetGridPosition) != null;
                    }
                    
                    targetButton.interactable = targetExists;
                }
            }
        }
    }

private Button GetDirectionalButton(DirectionButton direction)
{
    switch (direction)
    {
        case DirectionButton.Up:
            return upButton;
        case DirectionButton.Down:
            return downButton;
        case DirectionButton.Left:
            return leftButton;
        case DirectionButton.Right:
            return rightButton;
        default:
            return null;
    }
}

private void HideAllNavigationButtons()
{
    if (upButton != null) upButton.gameObject.SetActive(false);
    if (downButton != null) downButton.gameObject.SetActive(false);
    if (leftButton != null) leftButton.gameObject.SetActive(false);
    if (rightButton != null) rightButton.gameObject.SetActive(false);
}
    
    // Public methods for external scripts
    public string GetCurrentScreenName()
    {
        return currentScreen?.screenName ?? "Unknown";
    }
    
    public Vector2 GetCurrentGridPosition()
    {
        return currentGridPosition;
    }
    
    public List<string> GetAvailableScreenNames()
    {
        List<string> names = new List<string>();
        foreach (ScreenData screen in screens)
        {
            names.Add(screen.screenName);
        }
        return names;
    }
}
