using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }

    public bool InteractPressed { get; private set; }
    public bool Physical_Interact_Button_Pressed { get; private set; }   // одноразовое нажатие
    public bool Physical_Interact_Button_Held { get; private set; }      // удержание
    public bool FlarePressed { get; private set; }

    private PlayerControls actions;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        actions = new PlayerControls();
        actions.Player.Enable();

        // ƒвижение и взгл€д
        actions.Player.Move.performed += ctx => Move = ctx.ReadValue<Vector2>();
        actions.Player.Move.canceled += ctx => Move = Vector2.zero;

        actions.Player.Look.performed += ctx => Look = ctx.ReadValue<Vector2>();
        actions.Player.Look.canceled += ctx => Look = Vector2.zero;

        
        actions.Player.InteractButton.performed += _ => InteractPressed = true;

       
        actions.Player.Physical_Interact_Button.performed += _ =>
        {
            Physical_Interact_Button_Pressed = true;
            Physical_Interact_Button_Held = true;
        };
        actions.Player.Physical_Interact_Button.canceled += _ =>
        {
            Physical_Interact_Button_Pressed = false;
            Physical_Interact_Button_Held = false;
        };

        
        actions.Player.FlareButton.performed += _ => FlarePressed = true;
    }

    private void LateUpdate()
    {
        InteractPressed = false;
        Physical_Interact_Button_Pressed = false;
        FlarePressed = false;
    }

    public void ResetLook() => Look = Vector2.zero;

    private void OnEnable() => actions?.Player.Enable();
    private void OnDisable() => actions?.Player.Disable();

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}