using UnityEngine;
using UnityEngine.EventSystems;

public class SimpleClickTest : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private void Start()
    {
        // Ensure we have a collider
        if (GetComponent<Collider2D>() == null)
        {
            gameObject.AddComponent<BoxCollider2D>();
            Debug.Log($"Added collider to {gameObject.name}");
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"CLICK WORKS ON {gameObject.name}! ✅");
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log($"RELEASE WORKS ON {gameObject.name}! ✅");
    }
    
    private void OnMouseDown()
    {
        Debug.Log($"OnMouseDown also works on {gameObject.name}!");
    }
}