using UnityEngine;
using UnityEngine.EventSystems;

public class InteractionManager : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private LayerMask interactableLayer = -1;
    [SerializeField] private float interactionRange = 1f;
    
    private Camera mainCamera;
    
    private void Start()
    {
        mainCamera = Camera.main;
    }
    
    private void Update()
    {
        HandleInput();
    }
    
    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;
            
            // Check for interactable objects
            Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos, interactableLayer);
            
            if (hit != null)
            {
                IInteractable interactable = hit.GetComponent<IInteractable>();
                if (interactable != null && interactable.CanInteract())
                {
                    interactable.OnInteract();
                }
            }
        }
    }
}