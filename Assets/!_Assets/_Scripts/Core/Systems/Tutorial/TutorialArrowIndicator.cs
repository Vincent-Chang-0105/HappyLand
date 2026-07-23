using UnityEngine;
using DG.Tweening;

public class TutorialArrowIndicator : MonoBehaviour
{
    [Header("Arrow Settings")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float bounceHeight = 0.3f;
    [SerializeField] private float bounceDuration = 0.5f;
    [SerializeField] private Color arrowColor = Color.yellow;
    [SerializeField] private int sortingOrder = 1000;

    private GameObject activeArrow;
    private Transform currentTarget;
    private Vector3 currentOffset;
    private Tween bounceTween;
    private Vector3 basePosition;

    public void ShowArrow(Transform target, Vector3 offset)
    {
        HideArrow();

        if (target == null) return;

        currentTarget = target;
        currentOffset = offset;

        // Create arrow if no prefab
        if (arrowPrefab != null)
        {
            activeArrow = Instantiate(arrowPrefab);
        }
        else
        {
            activeArrow = CreateDefaultArrow();
        }

        // Initial position
        UpdateArrowPosition();
        basePosition = activeArrow.transform.position;

        // Start bounce animation
        StartBounceAnimation();
    }

    public void HideArrow()
    {
        if (bounceTween != null)
        {
            bounceTween.Kill();
            bounceTween = null;
        }

        if (activeArrow != null)
        {
            Destroy(activeArrow);
            activeArrow = null;
        }

        currentTarget = null;
    }

    private void Update()
    {
        if (activeArrow != null && currentTarget != null)
        {
            UpdateArrowPosition();
        }
    }

    private void UpdateArrowPosition()
    {
        if (activeArrow == null || currentTarget == null) return;
        
        basePosition = currentTarget.position + currentOffset;
        // The bounce tween will handle the actual Y offset from basePosition
    }

    private void StartBounceAnimation()
    {
        if (activeArrow == null) return;

        bounceTween?.Kill();

        // Bounce animation - move up and down continuously
        bounceTween = DOTween.To(
            () => 0f,
            (float t) =>
            {
                if (activeArrow != null)
                {
                    float yOffset = Mathf.Sin(t * Mathf.PI) * bounceHeight;
                    activeArrow.transform.position = basePosition + new Vector3(0, yOffset, 0);
                }
            },
            1f,
            bounceDuration
        )
        .SetEase(Ease.Linear)
        .SetLoops(-1, LoopType.Restart)
        .SetUpdate(true); // Run during pause
    }

    private GameObject CreateDefaultArrow()
    {
        // Create a simple arrow pointing downward
        GameObject arrow = new GameObject("TutorialArrow");

        // Create arrow shape using a sprite renderer
        SpriteRenderer sr = arrow.AddComponent<SpriteRenderer>();
        sr.sprite = CreateArrowSprite();
        sr.color = arrowColor;
        sr.sortingOrder = sortingOrder;

        // Point downward
        arrow.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        return arrow;
    }

    private Sprite CreateArrowSprite()
    {
        // Create a simple triangle texture for the arrow
        int size = 64;
        Texture2D texture = new Texture2D(size, size);

        // Fill with transparent
        Color[] colors = new Color[size * size];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = Color.clear;

        // Draw a simple downward-pointing triangle
        int halfWidth = size / 2;
        for (int y = 0; y < size; y++)
        {
            int rowWidth = (size - y) * halfWidth / size;
            int startX = halfWidth - rowWidth;
            int endX = halfWidth + rowWidth;

            for (int x = startX; x < endX; x++)
            {
                if (x >= 0 && x < size)
                    colors[y * size + x] = Color.white;
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private void OnDestroy()
    {
        HideArrow();
    }
}
