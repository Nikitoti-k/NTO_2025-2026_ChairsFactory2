using UnityEngine;

public class CameraController : MonoBehaviour
{
    public enum ControlMode
    {
        UI,  
        FPS  
    }

    [Header("Высота камер (глаз)")]
    public float eyeHeight = 1.6f;

    [Header("Чувствительность мыши (для FPS)")]
    public float mouseSensitivityX = 2f;
    public float mouseSensitivityY = 2f;
    public float pitchMin = -80f;
    public float pitchMax = 80f;

    [Header("Текущий режим")]
    [SerializeField] private ControlMode currentMode = ControlMode.FPS;

    private float yaw = 0f;
    private float pitch = 0f;
    private Transform target;
    private InputRouter inputRouter;
    private bool justEnteredFPS = false; 

    private void Start()
    {
        var playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerMovement != null)
            target = playerMovement.transform;
        else
            Debug.LogError("CameraController: PlayerMovement не найден!");

        inputRouter = FindObjectOfType<InputRouter>();
        ApplyMode(currentMode);
    }

    public void EnterFPSMode() => SetMode(ControlMode.FPS);
    public void EnterUIMode() => SetMode(ControlMode.UI);

    public void SetMode(ControlMode mode)
    {
        if (currentMode == mode) return;
        currentMode = mode;
        ApplyMode(mode);
    }

    private void ApplyMode(ControlMode mode)
    {
        switch (mode)
        {
            case ControlMode.FPS:
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                ResetMouseDeltaAndCenter();
                InputManager.Instance?.ResetLook();
                justEnteredFPS = true;
                break;

            case ControlMode.UI:
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                justEnteredFPS = false;
                break;
        }
    }

    private void ResetMouseDeltaAndCenter()
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null)
        {
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            mouse.WarpCursorPosition(center);
        }
#endif
    }

    private void Update()
    {
        if (currentMode != ControlMode.FPS || target == null || InputManager.Instance == null) return;

        Vector2 mouseDelta = InputManager.Instance.Look;

        // Игнорируем накопленную дельту в первый кадр после возврата в FPS
        if (justEnteredFPS)
        {
            mouseDelta = Vector2.zero;
            justEnteredFPS = false;
        }

        yaw += mouseDelta.x * mouseSensitivityX;
        pitch -= mouseDelta.y * mouseSensitivityY;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        if (inputRouter != null && inputRouter.CurrentController is PlayerMovement)
            target.rotation = Quaternion.Euler(0f, yaw, 0f);

      
    }

    private void LateUpdate()
    {
        if (target == null) return;
        transform.position = target.position + Vector3.up * eyeHeight;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
            ApplyMode(currentMode);
    }
#endif
}