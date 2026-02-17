using UnityEngine;

[System.Serializable]
public class TutorialStep
{
    [Header("Step Info")]
    [HideInInspector] public int stepId; // Auto-assigned from array index via OnValidate
    public string stepName;
    [TextArea(3, 5)]
    public string instructionText;
    public bool showUI = true; // Set false to wait for event without showing UI

    [Header("Target Object")]
    public TutorialTargetType targetType;
    public string targetObjectTag;
    public string targetObjectName;

    [Header("Completion Condition")]
    public TutorialCompletionType completionType;
    public float autoCompleteDelay = 0f;

    [Header("Timing")]
    public float showDelay = 0f; // Delay before showing this step's UI (in seconds)

    [Header("Arrow Settings")]
    public bool showArrow = true;
    public Vector3 arrowOffset = new Vector3(0, 1f, 0);

    [Header("Action On Complete")]
    public TutorialActionType actionOnComplete = TutorialActionType.None;
    public string actionParameter; // e.g., "Fried Chicken" for SpawnCustomerWithOrder

    [Header("Navigation Control (Optional)")]
    [Tooltip("If true, tutorial will control which navigation buttons are enabled")]
    public bool controlNavigation = false;
    [Tooltip("Only these directions will be enabled (if controlNavigation is true)")]
    public System.Collections.Generic.List<DirectionButton> allowedDirections = new System.Collections.Generic.List<DirectionButton>();

    [Header("State")]
    [HideInInspector] public bool isCompleted = false;
}

public enum TutorialTargetType
{
    None,
    Chicken,
    Faucet,
    BoilBowl,
    FryBowl,
    Pot,
    Pan,
    OilBottle,
    SinigangMix,
    PlatedDish,
    PlatingStation,
    Customer,
    ServingArea,
    OrderSlot,
    SaltShaker
}

public enum TutorialCompletionType
{
    ButtonPress,
    ChickenPickedUp,
    ChickenWashed,
    ChickenInBoilBowl,
    BowlOnPot,
    StirComplete,
    AllStirsComplete,
    OilAdded,
    ChickenInFryBowl,
    BowlOnPan,
    TossComplete,
    AllTossesComplete,
    SinigangMixAdded,
    SinigangComplete,
    DishPlated,
    DishServed,
    CustomerArrived,
    CustomerWaiting,  // Customer is at slot and ready for order to be taken
    OrderSlotClicked,  // Player clicked on an order slot to take the order
    WashScreenEntered,
    FaucetOpened,
    CookScreenEntered,
    ServeScreenEntered,
    BoilScreenEntered,
    CutScreenEntered,
    ThreeChickensWashed,
    FaucetClosed,
    ChickenTransferred,
    SaltAdded,
    ChickenAddedToPlate,     // Individual chicken placed on plating station
    FriedChickenCompleted    // Fried chicken dish fully assembled
}

public enum TutorialActionType
{
    None,
    SpawnCustomerWithOrder,  // Spawns a customer with specific order (use actionParameter)
    ResumeGameOnly,          // Just resumes the game without waiting
    StopCustomerGeneration,  // Stops auto customer spawning
    StartCustomerGeneration  // Starts auto customer spawning
}
