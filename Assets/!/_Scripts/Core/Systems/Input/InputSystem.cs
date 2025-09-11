using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InputSystem : PersistentSingleton<InputSystem> {
    public enum ActionMap
    {
        Player,
        UI
    }

    //Debug Mode
    [SerializeField] private bool isDebug = false;
    //Unity Action Events
    public event UnityAction<Vector2> MoveEvent;
    public event UnityAction<Vector2> LookEvent;
    public event UnityAction<bool> SprintEvent;
    public event UnityAction EscapeKeyEvent;
    public event UnityAction SpaceBarKeyEvent;        // Keep this for backward compatibility if needed
    public event UnityAction SpaceBarKeyDownEvent;    // New event for key press down
    public event UnityAction SpaceBarKeyUpEvent;      // New event for key release
    public event UnityAction EKeyEvent;

    private ActionMap _currentActionMap = ActionMap.Player;
    public ActionMap CurrentActionMap => _currentActionMap;

    // Cursor settings
    [Header("Cursor Settings")]
    public int cursorState;

    // Other settings
    [Header("Settings")]
    public bool enableLookInput = true;
    public bool enableMoveInput = true; 

    private void Start()
    {
    
    }

    private void SetCursorState(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;

        cursorState = (int)Cursor.lockState;
    }

    public void OnMove(InputValue value)
	{
        if (enableMoveInput)
        {
            MoveEvent?.Invoke(value.Get<Vector2>());
        }
	}

    public void OnLook(InputValue value)
	{
		if(enableLookInput)
		{
			LookEvent?.Invoke(value.Get<Vector2>());
		}
	}

    public void OnSprint(InputValue value)
    {
        SprintEvent?.Invoke(value.isPressed);
    }

    public void OnEscapeKey(InputValue value)
    {
        if(value.isPressed)
        {
            EscapeKeyEvent?.Invoke();
        }
    }

    public void OnSpaceBarKey(InputValue value)
    {
        if (value.isPressed)
        {
            // Spacebar was pressed down
            SpaceBarKeyEvent?.Invoke();        // Keep existing functionality
            SpaceBarKeyDownEvent?.Invoke();    // New down event
        }
        else
        {
            // Spacebar was released
            SpaceBarKeyUpEvent?.Invoke();      // New up event
        }
    }

    public void OnEKey(InputValue value)
    {
        if(value.isPressed)
        {
            EKeyEvent?.Invoke();
        }
    }

    public void SetInputState(bool value)
    {
        SetCursorState(value);
        enableLookInput = value;
        enableMoveInput = value;

        // Reset look input to zero when disabling input
        if (!value)
        {
            LookEvent?.Invoke(Vector2.zero);
        }

        // Reset move input to zero when disabling input
        if (!value)
        {
            MoveEvent?.Invoke(Vector2.zero);
        }
    }

    public void DisableMovementInputs()
    {
        enableMoveInput = false;
        MoveEvent?.Invoke(Vector2.zero);
    }

    public void EnableMovementInputs()
    {
        enableMoveInput = true;
    }

    public void DisableLookInputs()
    {
        enableLookInput = false;
        LookEvent?.Invoke(Vector2.zero);
    }

    public void EnableLookInputs()
    {
        enableLookInput = true;
    }

    #region Debug

    void OnEnable()
    {
        // var actionMap = playerInput.actions.FindActionMap("Player");

        // if(isDebug)
        // {
        //     foreach (var action in actionMap.actions)
        //     {
        //         action.performed += ctx => Debug.Log($"Performed: {action.name} Value: {ctx.ReadValueAsObject()}");
        //         action.started += ctx => Debug.Log($"Started: {action.name}");
        //         action.canceled += ctx => Debug.Log($"Canceled: {action.name}");
        //     }
        // }
    }

    void OnDisable()
    {
        // var actionMap = playerInput.actions.FindActionMap("Player");

        // if(isDebug)
        // {
        //     foreach (var action in actionMap.actions)
        //     {
        //         action.performed -= ctx => Debug.Log($"Performed: {action.name} Value: {ctx.ReadValueAsObject()}");
        //         action.started -= ctx => Debug.Log($"Started: {action.name}");
        //         action.canceled -= ctx => Debug.Log($"Canceled: {action.name}");
        //     }
        // }

    }
    #endregion
}