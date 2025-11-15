using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }
    public bool InteractPressed { get; private set; }
    public bool UseToolPressed { get; private set; }
    public bool UseToolHeld { get; private set; }
    public bool FlarePressed { get; private set; }

    private PlayerControls actions;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        actions = new PlayerControls();
        actions.Player.Enable();
        actions.Player.Move.performed += ctx => Move = ctx.ReadValue<Vector2>();
        actions.Player.Move.canceled += ctx => Move = Vector2.zero;
        actions.Player.Look.performed += ctx => Look = ctx.ReadValue<Vector2>();
        actions.Player.Look.canceled += ctx => Look = Vector2.zero;
        actions.Player.InteractButton.performed += ctx => InteractPressed = true;
        actions.Player.InteractButton.canceled += ctx => InteractPressed = false;
        actions.Player.UseToolButton.performed += ctx => { UseToolPressed = true; UseToolHeld = true; };
        actions.Player.UseToolButton.canceled += ctx => { UseToolPressed = false; UseToolHeld = false; };
        actions.Player.FlareButton.performed += ctx => FlarePressed = true;
        actions.Player.FlareButton.canceled += ctx => FlarePressed = false;
    }
    public void ResetLook()
    {
        Look = Vector2.zero;
    }
    void LateUpdate()
    {
        InteractPressed = false;
        UseToolPressed = false;
        FlarePressed = false;
    }
}