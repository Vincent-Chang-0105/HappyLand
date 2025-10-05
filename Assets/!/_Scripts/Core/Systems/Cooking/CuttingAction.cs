using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

public class CuttingAction : CookingAction
{
    [Header("UI Cutting Settings")]
    [SerializeField] private RectTransform cuttingBoard;
    [SerializeField] private Image[] cuttingGuides; // UI Images for dotted lines
    [SerializeField] private RectTransform knife;
    [SerializeField] private float cutAccuracy = 50f; // UI units
    
    [Header("Cut Objects")]
    [SerializeField] private RectTransform[] objectsToCut; // Chicken pieces, etc.
    [SerializeField] private Image[] cutPieces; // The result pieces after cutting
    
    private List<bool> cutsCompleted;
    private bool isCutting = false;
    private Vector2 cutStartPos;
    private Canvas parentCanvas;
    
    private void Start()
    {
        cutsCompleted = new List<bool>(new bool[cuttingGuides.Length]);
        parentCanvas = GetComponentInParent<Canvas>();
        
        // Hide cut pieces initially
        foreach (var piece in cutPieces)
        {
            piece.gameObject.SetActive(false);
        }
    }
    
    public override void StartAction()
    {
        OnActionStart?.Invoke();
        ShowCuttingGuides(true);
        EnableKnife(true);
    }
    
    public override void UpdateAction()
    {
        HandleUIInput();
        UpdateKnifePosition();
    }
    
    private void HandleUIInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 localPoint;
            Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                cuttingBoard, Input.mousePosition, cam, out localPoint);
            
            if (IsPointInCuttingArea(localPoint))
            {
                isCutting = true;
                cutStartPos = localPoint;
            }
        }
        else if (Input.GetMouseButtonUp(0) && isCutting)
        {
            isCutting = false;
            Vector2 localPoint;
            Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                cuttingBoard, Input.mousePosition, cam, out localPoint);
            
            CheckCutAccuracy(cutStartPos, localPoint);
        }
    }
    
    private void UpdateKnifePosition()
    {
        if (knife != null)
        {
            Vector2 localPoint;
            Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                cuttingBoard, Input.mousePosition, cam, out localPoint);
            
            knife.anchoredPosition = localPoint;
            
            // Rotate knife based on cutting direction
            if (isCutting)
            {
                Vector2 direction = (localPoint - cutStartPos).normalized;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                knife.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
            }
        }
    }
    
    private bool IsPointInCuttingArea(Vector2 point)
    {
        // Check if point is within cutting board bounds
        Rect rect = cuttingBoard.rect;
        return rect.Contains(point);
    }
    
    private void CheckCutAccuracy(Vector2 start, Vector2 end)
    {
        for (int i = 0; i < cuttingGuides.Length; i++)
        {
            if (cutsCompleted[i]) continue;
            
            RectTransform guide = cuttingGuides[i].rectTransform;
            
            if (IsSwipeAlongGuide(start, end, guide))
            {
                cutsCompleted[i] = true;
                CompleteCut(i);
                UpdateProgress(GetCompletionRatio());
                break;
            }
        }
    }
    
    private bool IsSwipeAlongGuide(Vector2 swipeStart, Vector2 swipeEnd, RectTransform guide)
    {
        // Get guide line positions
        Vector2 guideStart = guide.anchoredPosition - new Vector2(guide.rect.width / 2f, 0);
        Vector2 guideEnd = guide.anchoredPosition + new Vector2(guide.rect.width / 2f, 0);
        
        // Check if swipe is close to guide line
        float distanceToStart = Vector2.Distance(swipeStart, guideStart);
        float distanceToEnd = Vector2.Distance(swipeEnd, guideEnd);
        
        // Also check if swipe direction matches guide direction
        Vector2 swipeDirection = (swipeEnd - swipeStart).normalized;
        Vector2 guideDirection = (guideEnd - guideStart).normalized;
        float directionMatch = Vector2.Dot(swipeDirection, guideDirection);
        
        return distanceToStart < cutAccuracy && distanceToEnd < cutAccuracy && directionMatch > 0.7f;
    }
    
    private void CompleteCut(int cutIndex)
    {
        // Hide the cutting guide
        cuttingGuides[cutIndex].DOFade(0f, 0.3f).OnComplete(() => {
            cuttingGuides[cutIndex].gameObject.SetActive(false);
        });
        
        // Show cut effect
        CreateUICutEffect(cuttingGuides[cutIndex].rectTransform.anchoredPosition);
        
        // Animate object splitting
        if (cutIndex < objectsToCut.Length)
        {
            AnimateObjectCut(objectsToCut[cutIndex], cutIndex);
        }
        
        // Show cut pieces
        if (cutIndex < cutPieces.Length)
        {
            cutPieces[cutIndex].gameObject.SetActive(true);
            cutPieces[cutIndex].transform.localScale = Vector3.zero;
            cutPieces[cutIndex].transform.DOScale(1f, 0.5f).SetEase(Ease.OutBounce);
        }
    }
    
    private void AnimateObjectCut(RectTransform objectToCut, int cutIndex)
    {
        // Create cut line effect
        GameObject cutLine = new GameObject("CutLine");
        cutLine.transform.SetParent(cuttingBoard);
        
        Image lineImage = cutLine.AddComponent<Image>();
        lineImage.color = Color.white;
        
        RectTransform lineRect = cutLine.GetComponent<RectTransform>();
        lineRect.anchoredPosition = cuttingGuides[cutIndex].rectTransform.anchoredPosition;
        lineRect.sizeDelta = new Vector2(cuttingGuides[cutIndex].rectTransform.sizeDelta.x, 3f);
        
        // Flash the cut line
        lineImage.DOFade(0f, 0.5f).OnComplete(() => Destroy(cutLine));
        
        // Split the object visually
        objectToCut.DOPunchScale(Vector3.one * 0.1f, 0.3f);
    }
    
    private void CreateUICutEffect(Vector2 position)
    {
        // Create sparkle or slice effect at cut position
        GameObject effect = new GameObject("CutEffect");
        effect.transform.SetParent(cuttingBoard);
        
        Image effectImage = effect.AddComponent<Image>();
        effectImage.color = Color.yellow;
        
        RectTransform effectRect = effect.GetComponent<RectTransform>();
        effectRect.anchoredPosition = position;
        effectRect.sizeDelta = Vector2.one * 20f;
        
        // Animate effect
        effectRect.DOScale(2f, 0.3f).SetEase(Ease.OutQuad);
        effectImage.DOFade(0f, 0.3f).OnComplete(() => Destroy(effect));
        
        // Add sound effect here
        Debug.Log("Cut completed at position: " + position);
    }
    
    private float GetCompletionRatio()
    {
        int completedCuts = cutsCompleted.FindAll(x => x).Count;
        return (float)completedCuts / cuttingGuides.Length;
    }
    
    public override void CompleteAction()
    {
        isCompleted = true;
        ShowCuttingGuides(false);
        EnableKnife(false);
        OnActionComplete?.Invoke();
        
        // Show completion effect
        cuttingBoard.DOPunchScale(Vector3.one * 0.05f, 0.5f);
    }
    
    private void ShowCuttingGuides(bool show)
    {
        foreach (var guide in cuttingGuides)
        {
            guide.gameObject.SetActive(show);
            if (show)
            {
                // Animate guides appearing
                guide.color = new Color(1, 0, 0, 0.7f); // Red dotted lines
                guide.transform.DOScale(1.1f, 0.5f).SetLoops(-1, LoopType.Yoyo);
            }
        }
    }
    
    private void EnableKnife(bool enable)
    {
        if (knife != null)
        {
            knife.gameObject.SetActive(enable);
        }
    }
}