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

    [Header("Чувствительность мыши — обычная")]
    public float mouseSensitivityX = 1.8f;
    public float mouseSensitivityY = 1.8f;

    [Header("Чувствительность при держании инструмента")]
    public float toolSensitivityMultiplier = 0.4f; // 40% от обычной — точное прицеливание

    public float pitchMin = -80f;
    public float pitchMax = 80f;

    [Header("Сглаживание поворота")]
    [Range(0.08f, 0.3f)] public float rotationSmoothTime = 0.15f;

    [Header("Текущий режим")]
    [SerializeField] private ControlMode currentMode = ControlMode.FPS;

    private float targetYaw = 0f;
    private float targetPitch = 0f;
    private float smoothYaw = 0f;
    private float smoothPitch = 0f;
    private float yawVelocity = 0f;
    private float pitchVelocity = 0f;

    private Transform target;
    private Rigidbody targetRB;
    private InputRouter inputRouter;
    private bool justEnteredFPS = false;

    private float currentSensitivityX;
    private float currentSensitivityY;

    private CanGrab playerGrabber;

    // ===== НОВОЕ =====
    [Header("Перемещение toolGrabPoint при удержании инструмента")]
    public float toolPointMoveSpeed = 0.002f; // скорость "руки" по миру XZ

    private void Start()
    {
        var playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            target = playerMovement.transform;
            targetRB = playerMovement.GetComponent<Rigidbody>();
            playerGrabber = playerMovement.GetComponent<CanGrab>();
        }
        else
        {
            Debug.LogError("CameraController: PlayerMovement не найден!");
        }

        inputRouter = FindFirstObjectByType<InputRouter>();

        targetYaw = smoothYaw = transform.eulerAngles.y;
        targetPitch = smoothPitch = UnwrapAngle(transform.eulerAngles.x);

        UpdateCurrentSensitivity();

        ApplyMode(currentMode);

        if (playerGrabber != null)
        {
            CanGrab.OnGrabbed += OnObjectGrabbed;
            CanGrab.OnReleased += OnObjectReleased;
        }
    }

    private void OnDestroy()
    {
        if (playerGrabber != null)
        {
            CanGrab.OnGrabbed -= OnObjectGrabbed;
            CanGrab.OnReleased -= OnObjectReleased;
        }
    }

    private void OnObjectGrabbed(CanGrab grabber, Rigidbody rb)
    {
        if (grabber != playerGrabber) return;

        if (grabber.GetGrabbedItem()?.ItemType == GrabbableType.Tool)
            UpdateCurrentSensitivity(isHoldingTool: true);
    }

    private void OnObjectReleased(CanGrab grabber, Rigidbody rb)
    {
        if (grabber != playerGrabber) return;
        UpdateCurrentSensitivity(false);
    }

    private void UpdateCurrentSensitivity(bool isHoldingTool = false)
    {
        float mult = isHoldingTool ? toolSensitivityMultiplier : 1f;

        currentSensitivityX = mouseSensitivityX * mult;
        currentSensitivityY = mouseSensitivityY * mult;
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
                UpdateCurrentSensitivity(false);
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
        if (currentMode != ControlMode.FPS || target == null || InputManager.Instance == null)
            return;

        Vector2 mouseDelta = InputManager.Instance.Look;

        if (justEnteredFPS)
        {
            mouseDelta = Vector2.zero;
            justEnteredFPS = false;
        }

        // ==== вращение камеры ====
        targetYaw += mouseDelta.x * currentSensitivityX;
        targetPitch -= mouseDelta.y * currentSensitivityY;

        targetPitch = Mathf.Clamp(targetPitch, pitchMin, pitchMax);
        targetYaw = NormalizeAngle(targetYaw);

        smoothYaw = Mathf.SmoothDampAngle(smoothYaw, targetYaw, ref yawVelocity, rotationSmoothTime);
        smoothPitch = Mathf.SmoothDampAngle(smoothPitch, targetPitch, ref pitchVelocity, rotationSmoothTime);

        transform.rotation = Quaternion.Euler(smoothPitch, smoothYaw, 0f);

        // ====================================================================
        //                 НОВОЕ: ДВИГАЕМ toolGrabPoint ПО МИРУ
        // ====================================================================
        if (playerGrabber != null &&
            playerGrabber.IsHoldingObject() &&
            playerGrabber.GetGrabbedItem()?.ItemType == GrabbableType.Tool)
        {
            Transform tgp = playerGrabber.toolGrabPoint;
            if (tgp != null)
            {
                // Используем ту же дельту мышки!
                Vector2 moveDelta = mouseDelta;

                // создаём движение в плоскости XZ
                Vector3 move = new Vector3(moveDelta.x, 0f, moveDelta.y) * toolPointMoveSpeed;

                // перевод в мировые координаты относительно камеры
                Vector3 worldMove = transform.TransformDirection(move);
                worldMove.y = 0f; // строго по XZ!

                tgp.position += worldMove;
            }
        }
    }

    private void FixedUpdate()
    {
        if (targetRB == null || currentMode != ControlMode.FPS) return;

        Quaternion targetPlayerRot = Quaternion.Euler(0f, targetYaw, 0f);
        targetRB.MoveRotation(targetPlayerRot);
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
