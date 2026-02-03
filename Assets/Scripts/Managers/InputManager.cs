using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public Vector2 Move => _move;
    public Vector2 Look => _look;
    public bool Interact => _interact;
    public bool Physical => _physical;
    public bool PhysicalHeld => _physicalHeld;
    public bool Flare => _flare;
    public bool RadioNext => _radioNext;
    public bool EscapePressed { get; private set; }

    private Vector2 _move, _look;
    private bool _interact, _physical, _physicalHeld, _flare, _radioNext;

    private PlayerControls _actions;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
       

        _actions = new PlayerControls();
        _actions.Player.Enable();

        _actions.Player.Move.performed += ctx => _move = ctx.ReadValue<Vector2>();
        _actions.Player.Move.canceled += _ => _move = Vector2.zero;

        _actions.Player.Look.performed += ctx => _look = ctx.ReadValue<Vector2>();
        _actions.Player.Look.canceled += _ => _look = Vector2.zero;

        _actions.Player.InteractButton.performed += _ => _interact = true;
        _actions.Player.Physical_Interact_Button.performed += _ => { _physical = true; _physicalHeld = true; };
        _actions.Player.Physical_Interact_Button.canceled += _ => _physicalHeld = false;
        _actions.Player.FlareButton.performed += _ => _flare = true;
        _actions.Player.DialogueButton.performed += _ => _radioNext = true; // ← НОВАЯ
        _actions.Player.PauseButton.performed += _ => EscapePressed = true;
    }

    private void LateUpdate()
    {
        _interact = false;
        _physical = false;
        _flare = false;
        _radioNext = false;
        EscapePressed = false;
    }

    public void ClearLookInput() => _look = Vector2.zero;
    public void ClearMovementInput() => _move = Vector2.zero;
    public void ClearAllInput() => _move = _look = Vector2.zero;

    public static void ClearLook() => Instance?.ClearLookInput();
    public static void ClearMovement() => Instance?.ClearMovementInput();
    public static void ClearAll() => Instance?.ClearAllInput();

    private void OnEnable() => _actions?.Player.Enable();
    private void OnDisable() => _actions?.Player.Disable();
    private void OnDestroy() => Instance = null;
}