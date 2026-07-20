using UnityEngine;
using DG.Tweening;
using System.Collections;
using AudioSystem;

public enum CustomerState
{
    Moving,
    Ordering,
    Waiting,
    Receiving,
    Leaving,
    Angry // when order takes too long
}

public class Customer : MonoBehaviour
{
    [Header("Customer Info")]
    [SerializeField] private string customerName;
    [SerializeField] private Order currentOrder;
    [SerializeField] private CustomerState currentState = CustomerState.Moving;

    [Header("Facial Expressions")]
    [SerializeField] private FacialExpressionSet expressionSet;
    [SerializeField] private CustomerEmotion currentEmotion = CustomerEmotion.Default;
    
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float rotationSpeed = 5f;
    
    [Header("Patience")]
    [SerializeField] private float maxPatienceTime = 120f; // 2 minutes
    [SerializeField] private float currentPatience;

    [Header("Effects")]
    [SerializeField] private GameObject MoneyEffect;

    [Header("Audio")]
    [SerializeField] private SoundData orderCompleteSound;
    
    // Movement points
    private Transform spawnPoint;
    private Transform orderingPoint;
    private Transform exitPoint;
    
    // References
    private CustomerGenerator generator;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    
    // Order tracking
    private bool orderTaken = false;

    // Animation state
    private Tween idleBobTween;
    private Vector3 originalLocalPos;

    // Cached yield instructions
    private static readonly WaitForSeconds waitForOrderDelay = new(0.5f);
    private static readonly WaitForSeconds waitAfterReaction = new(1.25f);
    private static readonly WaitForSeconds waitForExitAnim = new(0.25f);

    // Properties
    public string CustomerName => customerName;
    public Order CurrentOrder => currentOrder;
    public CustomerState CurrentState => currentState;
    public float PatiencePercentage => currentPatience / maxPatienceTime;
    private Transform assignedOrderingPoint;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }
    
    void Update()
    {
        if (currentState == CustomerState.Waiting || currentState == CustomerState.Ordering)
        {
            UpdatePatience();
        }
    }
    
    #region Setup

    public void SetupCustomer(string name, Order order, CustomerGenerator gen, FacialExpressionSet expressions)
    {
        customerName = name;
        currentOrder = order;
        generator = gen;
        expressionSet = expressions;
        currentPatience = maxPatienceTime;

        // Set name on GameObject for easy identification
        gameObject.name = customerName;

        // Apply default facial expression
        SetEmotion(CustomerEmotion.Default);
    }

    public void SetMovementPoints(Transform spawn, Transform ordering, Transform exit)
    {
        spawnPoint = spawn;
        orderingPoint = ordering;
        exitPoint = exit;
        assignedOrderingPoint = ordering;

        // Start moving to ordering point
        StartCoroutine(MoveToOrderingPoint());
    }
    
    public Transform GetAssignedOrderingPoint()
    {
        return assignedOrderingPoint;
    }
    
    #endregion
    
    #region Movement
    
    private IEnumerator MoveToOrderingPoint()
    {
        currentState = CustomerState.Moving;
        yield return StartCoroutine(MoveToPoint(orderingPoint));

        // Arrival bounce
        transform.DOPunchScale(Vector3.one * 0.2f, 0.45f, 5, 0.3f);

        currentState = CustomerState.Ordering;
        StartOrdering();
    }

    private IEnumerator MoveToPoint(Transform target, bool isLeaving = false)
    {
        float distance = Vector3.Distance(transform.position, target.position);
        if (distance < 0.01f) yield break;

        transform.DOKill();
        float duration = distance / moveSpeed;
        Ease ease = isLeaving ? Ease.InSine : Ease.OutSine;
        yield return transform.DOMove(target.position, duration)
            .SetEase(ease)
            .WaitForCompletion();
    }
    
    #endregion
    
    #region Ordering Behavior
    
    private void StartOrdering()
    {
        currentState = CustomerState.Ordering;
        
        // Show order UI or speech bubble here
        DisplayOrder();
        
        // Wait a moment then start waiting
        StartCoroutine(WaitForOrder());
    }
    
    private void DisplayOrder()
    {
        // This would show the order UI/speech bubble
        //Debug.Log($"{customerName} wants: {currentOrder.orderName}");
    }
    
    private IEnumerator WaitForOrder()
    {
        yield return waitForOrderDelay;
        currentState = CustomerState.Waiting;
        StartIdleBob();

        // Tutorial event - customer is now waiting for order
        TutorialEvents.CustomerWaiting();
    }

    private void StartIdleBob()
    {
        originalLocalPos = transform.localPosition;
        idleBobTween = transform.DOLocalMoveY(originalLocalPos.y + 0.07f, 0.75f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopIdleBob()
    {
        idleBobTween?.Kill();
        idleBobTween = null;
        transform.localPosition = new Vector3(transform.localPosition.x, originalLocalPos.y, transform.localPosition.z);
    }
    
    #endregion
    
    #region Order Handling
    
    public bool ReceiveOrder(GameObject deliveredFood)
    {
        if (currentState != CustomerState.Waiting)
        {
            Debug.LogWarning($"Customer {name} is not waiting for order (current state: {currentState})");
            return false;
        }

        currentState = CustomerState.Receiving;

        // Check if order is correct
        bool isCorrect = ValidateOrder(deliveredFood);

        // Tutorial event
        if (isCorrect)
        {
            TutorialEvents.DishServed();
            if (orderCompleteSound != null && SoundManager.Instance != null)
                SoundManager.Instance.CreateSoundBuilder().Play(orderCompleteSound);
        }

        // Notify generator
        generator.OnCustomerOrderReceived(this, isCorrect);

        // Show satisfaction/dissatisfaction
        StartCoroutine(ReactToOrder(isCorrect));

        return isCorrect;
    }

    private bool ValidateOrder(GameObject deliveredFood)
    {
        if (deliveredFood == null)
        {
            Debug.LogWarning("Customer received null food!");
            return false;
        }

        // Check if it's a plated dish
        PlatedDish plate = deliveredFood.GetComponent<PlatedDish>();
        if (plate != null)
        {
            // Validate the plated dish
            //Debug.Log($"Customer received plated dish with {plate.ChickenCount} ingredients");

            // Check if plate is complete and valid
            if (!plate.ValidateDish())
            {
                Debug.LogWarning("Customer rejected incomplete or invalid plate!");
                return false;
            }

            DishType expected = ParseOrderToDishType(currentOrder.orderName);
            if (expected == DishType.Unknown)
            {
                // Fallback for any order names not in the enum map
                bool match = plate.DishName.ToLower() == currentOrder.orderName.ToLower();
                if (!match)
                    Debug.LogWarning($"❌ Dish '{plate.DishName}' doesn't match order '{currentOrder.orderName}'");
                return match;
            }

            if (plate.Dish != expected)
            {
                Debug.LogWarning($"❌ Dish '{plate.Dish}' is wrong type for order '{currentOrder.orderName}'");
                return false;
            }
            return true;
        }

        // Fallback: Original name-based check for other food items
        bool matches = deliveredFood.name.Contains(currentOrder.orderName);
        return matches;
    }

    private static DishType ParseOrderToDishType(string orderName) =>
        orderName.ToLower().Replace(" ", "") switch
        {
            "friedchicken" => DishType.FriedChicken,
            "adobo"        => DishType.Adobo,
            "mechado"      => DishType.Mechado,
            "sinigang"     => DishType.Sinigang,
            "noodlechicken" or "chickennoodle" => DishType.NoodleChicken,
            _              => DishType.Unknown,
        };
    
    private IEnumerator ReactToOrder(bool wasCorrect)
    {
        // Play animation/show reaction and change facial expression
        if (wasCorrect)
        {
            SetEmotion(CustomerEmotion.Happy);
            MoneyEffect.SetActive(true);
            // Happy bounce
            transform.DOKill();
            transform.DOPunchScale(Vector3.one * 0.35f, 0.5f, 5, 0.4f);
        }
        else
        {
            SetEmotion(CustomerEmotion.Angry);
            // Angry shake
            transform.DOKill();
            transform.DOShakePosition(0.4f, new Vector3(0.12f, 0f, 0f), 18, 0f);
        }

        yield return waitAfterReaction;

        // Leave restaurant
        StartLeaving();
    }
    
    /// <summary>
    /// Mark that the player has taken this customer's order (clicked the order button)
    /// </summary>
    public void TakeOrder()
    {
        orderTaken = true;
        // Excited little pop when order is taken
        transform.DOPunchScale(Vector3.one * 0.18f, 0.3f, 4, 0.5f);
    }

    /// <summary>
    /// Check if customer is currently waiting for their order AND the order has been taken
    /// </summary>
    public bool IsWaitingForOrder()
    {
        return currentState == CustomerState.Waiting && orderTaken;
    }

    #endregion

    #region Patience System
    
    private void UpdatePatience()
    {
        currentPatience -= Time.deltaTime;
        
        if (currentPatience <= 0)
        {
            BecomeAngry();
        }
    }
    
    private void BecomeAngry()
    {
        currentState = CustomerState.Angry;
        SetEmotion(CustomerEmotion.Angry);
        //Debug.Log($"{customerName} got angry and left!");

        // Notify generator of failed order
        generator.OnCustomerOrderReceived(this, false);

        StartLeaving();
    }
    
    #endregion
    
    #region Leaving
    
    private void StartLeaving()
    {
        StopIdleBob();
        currentState = CustomerState.Leaving;
        generator.OnCustomerLeaving(this);
        StartCoroutine(LeaveRestaurant());
    }
    
    private IEnumerator LeaveRestaurant()
    {
        yield return StartCoroutine(MoveToPoint(exitPoint, isLeaving: true));

        // Pop out: shrink and fade before destroying
        transform.DOScale(Vector3.zero, 0.22f).SetEase(Ease.InBack);
        if (spriteRenderer != null)
            spriteRenderer.DOFade(0f, 0.22f);

        yield return waitForExitAnim;
        Destroy(gameObject);
    }

    #endregion

    #region Facial Expressions

    /// <summary>
    /// Changes the customer's facial expression
    /// </summary>
    public void SetEmotion(CustomerEmotion emotion)
    {
        if (expressionSet == null)
        {
            Debug.LogWarning($"Customer {customerName} has no expression set assigned!");
            return;
        }

        if (spriteRenderer == null)
        {
            Debug.LogWarning($"Customer {customerName} has no SpriteRenderer!");
            return;
        }

        currentEmotion = emotion;
        Sprite newSprite = expressionSet.GetExpression(emotion);

        if (newSprite != null)
        {
            spriteRenderer.sprite = newSprite;
            //Debug.Log($"Customer {customerName} changed expression to: {emotion}");
        }
        else
        {
            Debug.LogWarning($"Expression {emotion} not found in expression set for customer {customerName}");
        }
    }

    public FacialExpressionSet GetExpressionSet()
    {
        return expressionSet;
    }

    /// <summary>
    /// Gets the current facial expression
    /// </summary>
    public CustomerEmotion GetCurrentEmotion()
    {
        return currentEmotion;
    }

    #endregion
}