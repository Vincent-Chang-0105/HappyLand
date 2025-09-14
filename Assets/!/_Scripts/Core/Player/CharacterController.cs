using UnityEngine;

public class CharacterController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float groundDistance = 0.55f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask terrainMask = 1;

    [Header("Components")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private SpriteRenderer sr;

    private Vector2 moveInput;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Subscribe to the InputSystem move event
        if (InputSystem.Instance != null)
        {
            InputSystem.Instance.MoveEvent += OnMoveInput;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (InputSystem.Instance != null)
        {
            InputSystem.Instance.MoveEvent -= OnMoveInput;
        }
    }

    private void OnMoveInput(Vector2 input)
    {
        moveInput = input;
    }

    // Update is called once per frame

    void FixedUpdate()
    {
        HandleGroundAlignment();
        HandleMovement();
        HandleSpriteFlipping(); 
    }
    void Update()
    {

    }
        
    private void HandleGroundAlignment()
    {
        Vector3 raycastOrigin = transform.position + Vector3.up;
        
        if (Physics.Raycast(raycastOrigin, Vector3.down, out RaycastHit hit, Mathf.Infinity, terrainMask))
        {
            Vector3 targetPosition = transform.position;
            targetPosition.y = hit.point.y + groundDistance;
            transform.position = targetPosition;
        }
    }

    private void HandleMovement()
    {
        Vector3 moveDirection = new Vector3(moveInput.x, 0, moveInput.y);
        rb.linearVelocity = moveDirection * moveSpeed;
    }

    private void HandleSpriteFlipping()
    {
        if (moveInput.x != 0)
        {
            sr.flipX = moveInput.x > 0;
        }
    }
}