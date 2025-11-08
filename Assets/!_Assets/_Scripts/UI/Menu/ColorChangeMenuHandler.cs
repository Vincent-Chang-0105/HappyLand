using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class ColorChangeMenuHandler : MenuEventSystemHandler
{
    [Header("Text Color Settings")]
    [SerializeField] protected Color _normalTextColor = Color.white;
    [SerializeField] protected Color _selectedTextColor = Color.yellow;
    [SerializeField] protected float _colorChangeDuration = 0.25f;

    protected Dictionary<Selectable, Color> _textColors = new Dictionary<Selectable, Color>();
    protected Tween _colorChangeTween;
    
    public override void Awake()
    {
        base.Awake();
        
        // Store original text colors
        foreach (var selectable in Selectables)
        {
            Text legacyText = selectable.GetComponentInChildren<Text>();
            TextMeshProUGUI tmpText = selectable.GetComponentInChildren<TextMeshProUGUI>();
            
            if (legacyText != null)
            {
                _textColors.Add(selectable, legacyText.color);
            }
            else if (tmpText != null)
            {
                _textColors.Add(selectable, tmpText.color);
            }
        }
    }
    
    protected override void HandleSelect(Selectable selectable)
    {
        ChangeTextColor(selectable, _selectedTextColor);
    }

    protected override void HandleDeselect(Selectable selectable)
    {
        ResetTextColor(selectable);
    }
    
    protected override void ResetToOriginalState(Selectable selectable)
    {     
        // Reset text color
        ResetTextColor(selectable);
    }
    
    protected void ChangeTextColor(Selectable selectable, Color targetColor)
    {
        // Check for Legacy UI Text
        Text legacyText = selectable.GetComponentInChildren<Text>();
        if (legacyText != null)
        {
            _colorChangeTween = DOTween.To(() => legacyText.color,
                x => legacyText.color = x,
                targetColor,
                _colorChangeDuration)
                .SetUpdate(true);
            return;
        }
        
        // Check for TextMeshPro
        TextMeshProUGUI tmpText = selectable.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText != null)
        {
            _colorChangeTween = DOTween.To(() => tmpText.color,
                x => tmpText.color = x,
                targetColor,
                _colorChangeDuration)
                .SetUpdate(true);
        }
    }
    
    protected void ResetTextColor(Selectable selectable)
    {
        if (_textColors.ContainsKey(selectable))
        {
            ChangeTextColor(selectable, _textColors[selectable]);
        }
        else
        {
            ChangeTextColor(selectable, _normalTextColor);
        }
    }
    
    protected override void CleanupTweens()
    {
        if (_colorChangeTween != null)
            _colorChangeTween.Kill(true);
    }

    protected override void HandleClick(Selectable selectable)
    {
        
    }
}