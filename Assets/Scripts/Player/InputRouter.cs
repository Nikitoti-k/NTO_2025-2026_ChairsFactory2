using UnityEngine;

public interface IControllable
{
    void HandleMovement(Vector2 input);
    void HandleInteract(bool pressed);
    void HandlePhysicalInteract(bool pressed, bool held);
    void HandleFlare(bool pressed);
}


public class InputRouter : MonoBehaviour
{
    public static InputRouter Instance { get; private set; }

    [SerializeField] private bool showLogs = true;
    public IControllable CurrentController { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        var player = FindFirstObjectByType<PlayerMovement>();
        if (player != null) SetController(player);
    }

  
   private void Update()
    {
        if (CurrentController == null || InputManager.Instance == null) return;

        var im = InputManager.Instance;

        // УБИРАЕМ ПРОВЕРКУ НА ФАКЕЛ ТОЛЬКО ДЛЯ ИГРОКА!
        // Теперь проверяем: если текущий контроллер — игрок И он держит факел → тогда бросаем
        if (CurrentController is PlayerMovement playerMovement)
        {
            var flareCtrl = playerMovement.GetComponent<FlareController>();
            if (flareCtrl != null && flareCtrl.IsHoldingFlare)
            {
                CurrentController.HandleFlare(im.Flare);
                // НЕ return! Пусть остальные действия тоже пройдут (например, выход из машины)
            }
        }

        // Нормальная передача ввода — всегда!
        CurrentController.HandleInteract(im.Interact);
        CurrentController.HandlePhysicalInteract(im.Physical, im.PhysicalHeld);
        CurrentController.HandleFlare(im.Flare);
    }

    private void FixedUpdate()
    {
        if (CurrentController == null || InputManager.Instance == null) return;
        CurrentController.HandleMovement(InputManager.Instance.Move);
    }

    public void SetController(IControllable controller)
    {
        CurrentController = controller;
        if (showLogs && controller != null)
            Debug.Log($"[InputRouter] Control → {controller.GetType().Name}");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}