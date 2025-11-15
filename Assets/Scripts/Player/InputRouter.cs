using UnityEngine;
public interface IControllable
{
    void HandleMovement(Vector2 movementInput);
    void HandleRotation(Vector2 mouseDelta);
    void HandleInteract(bool pressed);
    void HandleUseTool(bool pressed, bool held);
    void HandleFlare(bool pressed);
}



public class InputRouter : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool showLogs = true;

    public IControllable CurrentController { get; private set; }

    // ¬вод отключЄн только во врем€ анимации посадки/высадки и т.п.
    private bool inputEnabled = true;

    private void Awake()
    {
        
        inputEnabled = true;
    }

    private void Start()
    {
        
        var player = FindObjectOfType<PlayerMovement>();
        if (player != null)
            SetController(player);
    }

    private void Update()
    {
        if (CurrentController == null || !inputEnabled || InputManager.Instance == null) return;

        CurrentController.HandleInteract(InputManager.Instance.InteractPressed);
        CurrentController.HandleUseTool(InputManager.Instance.UseToolPressed, InputManager.Instance.UseToolHeld);
        CurrentController.HandleFlare(InputManager.Instance.FlarePressed);
    }

    private void FixedUpdate()
    {
        if (CurrentController == null || !inputEnabled || InputManager.Instance == null) return;

        CurrentController.HandleMovement(InputManager.Instance.Move);
    }

   
    public void SetController(IControllable controller)
    {
        CurrentController = controller;

        
        inputEnabled = true;

        if (showLogs && controller != null)
            Debug.Log($"[InputRouter] ”правление передано: {controller.GetType().Name}");
    }

   
    public void DisableInput()
    {
        inputEnabled = false;
        if (showLogs) Debug.Log("[InputRouter] ¬вод отключЄн (переход)");
    }

    public void EnableInput()
    {
        inputEnabled = true;
        if (showLogs) Debug.Log("[InputRouter] ¬вод включЄн");
    }
}