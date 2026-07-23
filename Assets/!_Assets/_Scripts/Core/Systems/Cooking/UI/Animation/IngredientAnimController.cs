using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class IngredientAnimController : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float frameDuration = 0.05f; // Time per frame
    [SerializeField] private bool loopAnimation = false;
    
    private Image imageComponent;
    private Sprite[] currentAnimationFrames;
    private int currentFrameIndex = 0;
    private bool isPlaying = false;
    
    private void Awake()
    {
        imageComponent = GetComponent<Image>();
    }
    
    public void PlayAnimation(Sprite[] frames, bool loop = false, System.Action onComplete = null)
    {
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning("No animation frames provided!");
            onComplete?.Invoke();
            return;
        }
        
        currentAnimationFrames = frames;
        loopAnimation = loop;
        
        StartCoroutine(AnimateFrames(onComplete));
    }
    
    // Coroutine that cycles through animation frames
    private IEnumerator AnimateFrames(System.Action onComplete)
    {
        isPlaying = true;
        currentFrameIndex = 0;
        
        do
        {
            for (int i = 0; i < currentAnimationFrames.Length; i++)
            {
                if (imageComponent != null && currentAnimationFrames[i] != null)
                {
                    imageComponent.sprite = currentAnimationFrames[i];
                    currentFrameIndex = i;
                }
                
                yield return new WaitForSeconds(frameDuration);
            }
            
        } while (loopAnimation && isPlaying);
        
        isPlaying = false;
        onComplete?.Invoke();
    }
    
    // Stop the animation
    public void StopAnimation()
    {
        isPlaying = false;
        StopAllCoroutines();
    }
    
    // Get the total animation duration
    public float GetAnimationDuration()
    {
        if (currentAnimationFrames == null) return 0f;
        return currentAnimationFrames.Length * frameDuration;
    }
}