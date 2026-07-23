using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Flips the GameObject's X (or Y) scale on pointer hover.
/// Attach to any button whose hover sprite is mirrored and needs flipping.
/// </summary>
public class HoverFlipImage : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private bool flipX = true;
    [SerializeField] private bool flipY = false;

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Vector3 scale = originalScale;
        if (flipX) scale.x *= -1;
        if (flipY) scale.y *= -1;
        transform.localScale = scale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = originalScale;
    }
}
