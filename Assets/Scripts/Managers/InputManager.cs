using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    // === Чтение ввода (доступно всем) ===
    public Vector2 Move => _move;
    public Vector2 Look => _look;
    public bool Interact => _interact;
    public bool Physical => _physical;
    public bool PhysicalHeld => _physicalHeld;
    public bool Flare => _flare;

    // === Приватные поля (внутри только мы меняем) ===
    private Vector2 _move;
    private Vector2 _look;
    private bool _interact;
    private bool _physical;
    private bool _physicalHeld;
    private bool _flare;

    // ==== Публичные методы для принудительного сброса ====
    public void ClearMovementInput() => _move = Vector2.zero;
    public void ClearLookInput() => _look = Vector2.zero;
    public void ClearAllInput()
    {
        _move = Vector2.zero;
        _look = Vector2.zero;
        _interact = false;
        _physical = false;
        _flare = false;
    }
   

    // Статическая обёртка — чтобы можно было писать InputManager.ClearLook()
    public static void ClearLook() => Instance?.ClearLookInput();
    // Удобная статическая обёртка (чтобы не писать Instance? каждый раз)
    public static void ClearMovement() => Instance?.ClearMovementInput();
    public static void ClearAll() => Instance?.ClearAllInput();

    private PlayerControls _actions;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _actions = new PlayerControls();
        _actions.Player.Enable();

        // === Подписки ===
        _actions.Player.Move.performed += ctx => _move = ctx.ReadValue<Vector2>();
        _actions.Player.Move.canceled += _ => _move = Vector2.zero;

        _actions.Player.Look.performed += ctx => _look = ctx.ReadValue<Vector2>();
        _actions.Player.Look.canceled += _ => _look = Vector2.zero;

        _actions.Player.InteractButton.performed += _ => _interact = true;

        _actions.Player.Physical_Interact_Button.performed += _ =>
        {
            _physical = true;
            _physicalHeld = true;
        };
        _actions.Player.Physical_Interact_Button.canceled += _ => _physicalHeld = false;

        _actions.Player.FlareButton.performed += _ => _flare = true;
    }

    private void LateUpdate()
    {
        _interact = false;
        _physical = false;
        _flare = false;
    }

    private void OnEnable() => _actions?.Player.Enable();
    private void OnDisable() => _actions?.Player.Disable();
    private void OnDestroy() => Instance = null;
}