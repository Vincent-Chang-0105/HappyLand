using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[System.Serializable]
public class ScreenUIElement
{
    [Header("UI Element")]
    public GameObject uiElement;
    public string elementName; // For debugging
    
    [Header("Screen Visibility")]
    public List<string> visibleOnScreens = new List<string>(); // Screen names where this UI should be visible
    
    [Header("Animation (Optional)")]
    public bool useAnimation = true;
    public float animationDuration = 0.3f;
    public AnimationType animationType = AnimationType.Fade;
}

public enum AnimationType
{
    Fade,
    Scale,
    Slide
}

public class ScreenUIManager : MonoBehaviour
{
    [Header("UI Elements Configuration")]
    [SerializeField] private List<ScreenUIElement> screenUIElements = new List<ScreenUIElement>();
    
    [Header("References")]
    [SerializeField] private ScreenTransitionManager screenTransitionManager;
    
    private string currentScreenName = "";
    
    private void Start()
    {
        // Find ScreenTransitionManager if not assigned
        if (screenTransitionManager == null)
        {
            screenTransitionManager = FindObjectOfType<ScreenTransitionManager>();
        }
        
        // Initial UI setup
        if (screenTransitionManager != null)
        {
            currentScreenName = screenTransitionManager.GetCurrentScreenName();
            UpdateUIForScreen(currentScreenName, true); // Instant on start
        }
    }
    
    private void Update()
    {
        // Check if screen has changed
        if (screenTransitionManager != null)
        {
            string newScreenName = screenTransitionManager.GetCurrentScreenName();
            
            if (newScreenName != currentScreenName)
            {
                currentScreenName = newScreenName;
                UpdateUIForScreen(currentScreenName, false); // Animated during gameplay
            }
        }
    }
    
    private void UpdateUIForScreen(string screenName, bool instant = false)
    {
        Debug.Log($"Updating UI for screen: {screenName}");
        
        foreach (ScreenUIElement uiElement in screenUIElements)
        {
            if (uiElement.uiElement == null) continue;
            
            bool shouldBeVisible = uiElement.visibleOnScreens.Contains(screenName);
            bool isCurrentlyVisible = uiElement.uiElement.activeInHierarchy;
            
            // Only change visibility if needed
            if (shouldBeVisible != isCurrentlyVisible)
            {
                if (shouldBeVisible)
                {
                    ShowUIElement(uiElement, instant);
                }
                else
                {
                    HideUIElement(uiElement, instant);
                }
            }
        }
    }
    
    private void ShowUIElement(ScreenUIElement uiElement, bool instant)
    {
        if (instant || !uiElement.useAnimation)
        {
            uiElement.uiElement.SetActive(true);
            Debug.Log($"Instantly showed UI element: {uiElement.elementName}");
        }
        else
        {
            StartCoroutine(AnimateShowUI(uiElement));
        }
    }
    
    private void HideUIElement(ScreenUIElement uiElement, bool instant)
    {
        if (instant || !uiElement.useAnimation)
        {
            uiElement.uiElement.SetActive(false);
            Debug.Log($"Instantly hid UI element: {uiElement.elementName}");
        }
        else
        {
            StartCoroutine(AnimateHideUI(uiElement));
        }
    }
    
    private IEnumerator AnimateShowUI(ScreenUIElement uiElement)
    {
        uiElement.uiElement.SetActive(true);
        
        switch (uiElement.animationType)
        {
            case AnimationType.Fade:
                yield return StartCoroutine(FadeIn(uiElement));
                break;
            case AnimationType.Scale:
                yield return StartCoroutine(ScaleIn(uiElement));
                break;
            case AnimationType.Slide:
                yield return StartCoroutine(SlideIn(uiElement));
                break;
        }
        
        Debug.Log($"Animated show UI element: {uiElement.elementName}");
    }
    
    private IEnumerator AnimateHideUI(ScreenUIElement uiElement)
    {
        switch (uiElement.animationType)
        {
            case AnimationType.Fade:
                yield return StartCoroutine(FadeOut(uiElement));
                break;
            case AnimationType.Scale:
                yield return StartCoroutine(ScaleOut(uiElement));
                break;
            case AnimationType.Slide:
                yield return StartCoroutine(SlideOut(uiElement));
                break;
        }
        
        uiElement.uiElement.SetActive(false);
        Debug.Log($"Animated hide UI element: {uiElement.elementName}");
    }
    
    #region Animation Methods
    private IEnumerator FadeIn(ScreenUIElement uiElement)
    {
        CanvasGroup canvasGroup = uiElement.uiElement.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = uiElement.uiElement.AddComponent<CanvasGroup>();
        }
        
        canvasGroup.alpha = 0f;
        float elapsedTime = 0f;
        
        while (elapsedTime < uiElement.animationDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / uiElement.animationDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    private IEnumerator FadeOut(ScreenUIElement uiElement)
    {
        CanvasGroup canvasGroup = uiElement.uiElement.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = uiElement.uiElement.AddComponent<CanvasGroup>();
        }
        
        canvasGroup.alpha = 1f;
        float elapsedTime = 0f;
        
        while (elapsedTime < uiElement.animationDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / uiElement.animationDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
    }
    
    private IEnumerator ScaleIn(ScreenUIElement uiElement)
    {
        Transform transform = uiElement.uiElement.transform;
        transform.localScale = Vector3.zero;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < uiElement.animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float scale = Mathf.Lerp(0f, 1f, elapsedTime / uiElement.animationDuration);
            transform.localScale = Vector3.one * scale;
            yield return null;
        }
        
        transform.localScale = Vector3.one;
    }
    
    private IEnumerator ScaleOut(ScreenUIElement uiElement)
    {
        Transform transform = uiElement.uiElement.transform;
        transform.localScale = Vector3.one;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < uiElement.animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float scale = Mathf.Lerp(1f, 0f, elapsedTime / uiElement.animationDuration);
            transform.localScale = Vector3.one * scale;
            yield return null;
        }
        
        transform.localScale = Vector3.zero;
    }
    
    private IEnumerator SlideIn(ScreenUIElement uiElement)
    {
        RectTransform rectTransform = uiElement.uiElement.GetComponent<RectTransform>();
        if (rectTransform == null) yield break;
        
        Vector2 originalPosition = rectTransform.anchoredPosition;
        Vector2 startPosition = originalPosition + new Vector2(0, -200f); // Slide from bottom
        rectTransform.anchoredPosition = startPosition;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < uiElement.animationDuration)
        {
            elapsedTime += Time.deltaTime;
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, originalPosition, elapsedTime / uiElement.animationDuration);
            yield return null;
        }
        
        rectTransform.anchoredPosition = originalPosition;
    }
    
    private IEnumerator SlideOut(ScreenUIElement uiElement)
    {
        RectTransform rectTransform = uiElement.uiElement.GetComponent<RectTransform>();
        if (rectTransform == null) yield break;
        
        Vector2 originalPosition = rectTransform.anchoredPosition;
        Vector2 endPosition = originalPosition + new Vector2(0, -200f); // Slide to bottom
        
        float elapsedTime = 0f;
        
        while (elapsedTime < uiElement.animationDuration)
        {
            elapsedTime += Time.deltaTime;
            rectTransform.anchoredPosition = Vector2.Lerp(originalPosition, endPosition, elapsedTime / uiElement.animationDuration);
            yield return null;
        }
        
        rectTransform.anchoredPosition = endPosition;
    }
    #endregion
    
    #region Public Methods
    public void ForceUpdateUI()
    {
        if (screenTransitionManager != null)
        {
            currentScreenName = screenTransitionManager.GetCurrentScreenName();
            UpdateUIForScreen(currentScreenName, false);
        }
    }
    
    public void ShowUIElementByName(string elementName)
    {
        ScreenUIElement element = screenUIElements.Find(e => e.elementName == elementName);
        if (element != null)
        {
            ShowUIElement(element, false);
        }
    }
    
    public void HideUIElementByName(string elementName)
    {
        ScreenUIElement element = screenUIElements.Find(e => e.elementName == elementName);
        if (element != null)
        {
            HideUIElement(element, false);
        }
    }
    #endregion
}