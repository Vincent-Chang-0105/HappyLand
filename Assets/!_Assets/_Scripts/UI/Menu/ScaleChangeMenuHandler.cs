using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ScaleChangeMenuHandler : MenuEventSystemHandler
{
    [Header("Scale Settings")]
    [SerializeField] protected Vector3 _normalScale = Vector3.one;
    [SerializeField] protected Vector3 _selectedScale = new Vector3(1.2f, 1.2f, 1.2f);
    [SerializeField] protected float _scaleChangeDuration = 0.25f;
    [SerializeField] protected Ease _scaleEaseType = Ease.OutBack;

    protected Dictionary<Selectable, Vector3> _originalScales = new Dictionary<Selectable, Vector3>();
    protected Dictionary<Selectable, Tween> _scaleChangeTweens = new Dictionary<Selectable, Tween>();
    
    public override void Awake()
    {
        base.Awake();
        
        // Store original scales
        foreach (var selectable in Selectables)
        {
            _originalScales.Add(selectable, selectable.transform.localScale);
            _scaleChangeTweens.Add(selectable, null);
        }
    }
    
    protected override void HandleSelect(Selectable selectable)
    {
        ChangeScale(selectable, _selectedScale);
    }

    protected override void HandleDeselect(Selectable selectable)
    {
        ResetScale(selectable);
    }
    
    protected override void ResetToOriginalState(Selectable selectable)
    {
        // Reset scale to original without animation
        if (_originalScales.ContainsKey(selectable))
        {
            selectable.transform.localScale = _originalScales[selectable];
        }
        else
        {
            selectable.transform.localScale = _normalScale;
        }
    }
    
    protected void ChangeScale(Selectable selectable, Vector3 targetScale)
    {
        // Kill any existing scale tween for this specific selectable
        if (_scaleChangeTweens.ContainsKey(selectable) && _scaleChangeTweens[selectable] != null)
        {
            _scaleChangeTweens[selectable].Kill();
        }

        _scaleChangeTweens[selectable] = selectable.transform.DOScale(targetScale, _scaleChangeDuration)
            .SetEase(_scaleEaseType)
            .OnComplete(() =>
            {
                // Clear the tween reference when complete
                if (_scaleChangeTweens.ContainsKey(selectable))
                    _scaleChangeTweens[selectable] = null;
            })
            .SetUpdate(true);
    }
    
    protected void ResetScale(Selectable selectable)
    {
        if (_originalScales.ContainsKey(selectable))
        {
            ChangeScale(selectable, _originalScales[selectable]);
        }
        else
        {
            ChangeScale(selectable, _normalScale);
        }
    }
    
    protected override void CleanupTweens()
    {
        foreach (var tween in _scaleChangeTweens.Values)
        {
            if (tween != null)
                tween.Kill(true);
        }
        _scaleChangeTweens.Clear();
    }

    protected override void HandleClick(Selectable selectable)
    {
        // Optional: Add a quick scale punch effect on click
        // Uncomment the lines below if you want a click animation
        
        selectable.transform.DOPunchScale(Vector3.one * 0.1f, 0.1f, 1, 0.5f).SetUpdate(true);
        
    }
}