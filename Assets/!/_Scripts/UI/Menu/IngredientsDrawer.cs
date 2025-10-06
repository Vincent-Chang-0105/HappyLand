using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.UI;

public class IngredientsDrawer : MonoBehaviour
{
    [SerializeField] private Button HandleButton;
    [SerializeField] private RectTransform DrawerPanel;
    [SerializeField] private float AnimationDuration = 0.5f;
    [SerializeField] private Vector2 OpenPosition;
    [SerializeField] private Vector2 ClosedPosition;

    private bool isOpen = false;

    private void Start()
    {
        HandleButton.onClick.AddListener(ToggleDrawer);
        DrawerPanel.anchoredPosition = ClosedPosition;

    }

    private void ToggleDrawer()
    {
        isOpen = !isOpen;
        
        Vector2 targetPosition = isOpen ? OpenPosition : ClosedPosition;
        DrawerPanel.DOAnchorPos(targetPosition, AnimationDuration).SetEase(Ease.OutQuart);
    }


}
