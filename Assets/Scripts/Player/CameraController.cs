using UnityEngine;

public class CameraController : MonoBehaviour
{
    public enum ControlMode
    {
        UI,
        FPS
    }

    [Header("Высота глаз")]
    public float eyeHeight = 1.6f;

    [Header("Чувствительность мыши")]
    public float mouseSensitivityX = 2f;
    public float mouseSensitivityY = 2f;
    public float pitchMin = -80f;
    public float pitchMax = 80f;

    [Header("Сглаживание только поворота камеры")]
    [Range(0.01f, 0.5f)] public float rotationSmoothTime = 0.12f;

    [Header("Текущий режим")]
    [SerializeField] private ControlMode currentMode = ControlMode.FPS;

    
    private float targetYaw = 0f;
    private float targetPitch = 0f;

    
    private float smoothYaw = 0f;
    private float smoothPitch = 0f;

    private float yawVelocity = 0f;
    private float pitchVelocity = 0f;

    private Transform target;
    private InputRouter inputRouter;
    private bool justEnteredFPS = false;

    private void Start()
    {
        var playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
            target = playerMovement.transform;
        else
            Debug.LogError("CameraController: PlayerMovement не найден!");

        inputRouter = FindFirstObjectByType<InputRouter>();

        
        targetYaw = smoothYaw = transform.eulerAngles.y;
        targetPitch = smoothPitch = UnwrapAngle(transform.eulerAngles.x);

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

        
        if (justEnteredFPS)
        {
            mouseDelta = Vector2.zero;
            justEnteredFPS = false;
        }

        
        targetYaw += mouseDelta.x * mouseSensitivityX;
        targetPitch -= mouseDelta.y * mouseSensitivityY;
        targetPitch = Mathf.Clamp(targetPitch, pitchMin, pitchMax);

        
        targetYaw = NormalizeAngle(targetYaw);

        
        smoothYaw = Mathf.SmoothDampAngle(smoothYaw, targetYaw, ref yawVelocity, rotationSmoothTime);
        smoothPitch = Mathf.SmoothDampAngle(smoothPitch, targetPitch, ref pitchVelocity, rotationSmoothTime);

        
        transform.rotation = Quaternion.Euler(smoothPitch, smoothYaw, 0f);

       
        if (inputRouter != null && inputRouter.CurrentController is PlayerMovement)
            target.rotation = Quaternion.Euler(0f, smoothYaw, 0f);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        
        transform.position = target.position + Vector3.up * eyeHeight;
    }

    
    private static float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    private static float UnwrapAngle(float angle)
    {
        if (angle > 180f) angle -= 360f;
        return angle;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
            ApplyMode(currentMode);
    }
#endif
}