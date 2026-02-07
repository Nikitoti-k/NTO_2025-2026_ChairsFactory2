using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CameraController : MonoBehaviour
{
    public enum ControlMode { UI, FPS, Focus }

    public float eyeHeight = 1.6f;
    public float mouseSensitivityX = 1.8f;
    public float mouseSensitivityY = 1.8f;
    public float toolSensitivityMultiplier = 0.4f;
    public float pitchMin = -80f;
    public float pitchMax = 80f;
    public float rotationSmoothTime = 0.15f;
    public float vehicleYawLimit = 70f;
    public float toolPointMoveSpeed = 0.002f;
    public float focusTransitionTime = 0.8f;

    [SerializeField] private LayerMask uiLayer = 1;
    [SerializeField] private LayerMask focusClickLayer = -1;
    [SerializeField] private float uiRayDistance = 4f;
    [SerializeField] private float focusClickDistance = 10f;

    private Transform _target;
    private Rigidbody _targetRb;
    private CanGrab _playerGrabber;
    private InputRouter _router;
    private float _yaw, _pitch;
    private float _smoothYaw, _smoothPitch;
    private float _yawVel, _pitchVel;
    private float _sensX, _sensY;
    private TransportMovement _currentVehicle;
    private bool _inVehicle;
    private FocusPoint _currentFocusPoint;
    private float _focusTimer;
    private Vector3 _focusStartPos;
    private Quaternion _focusStartRot;
    private Vector3 _focusTargetPos;
    private Quaternion _focusTargetRot;
    private Vector3 _originalPlayerPosition;
    private Quaternion _originalPlayerRotation;
    private bool _playerColliderWasEnabled;
    private float _originalYaw;
    private float _originalPitch;
    private ControlMode _modeBeforeFocus;

    public static CameraController Instance { get; private set; }
    private ControlMode currentMode = ControlMode.UI;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        var player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
        {
            _target = player.transform;
            _targetRb = player.GetComponent<Rigidbody>();
            _playerGrabber = player.GetComponent<CanGrab>();
            player.SetCameraController(this);
        }
        _router = FindFirstObjectByType<InputRouter>();

        _yaw = _smoothYaw = 0f;
        _pitch = _smoothPitch = 0f;
        _originalYaw = _yaw;
        _originalPitch = _pitch;
        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

        UpdateSensitivity(false);

        CanGrab.OnGrabbed += OnPlayerGrabbed;
        CanGrab.OnReleased += OnPlayerReleased;
    }

    private void OnDestroy()
    {
        CanGrab.OnGrabbed -= OnPlayerGrabbed;
        CanGrab.OnReleased -= OnPlayerReleased;
    }

    private void OnPlayerGrabbed(CanGrab grabber, Rigidbody rb)
    {
        var item = rb.GetComponent<GrabbableItem>();
        UpdateSensitivity(item != null && item.ItemType == GrabbableType.Tool);
    }

    private void OnPlayerReleased(CanGrab grabber, Rigidbody rb)
    {
        UpdateSensitivity(false);
    }

    public void SetMode(ControlMode mode)
    {
        if (currentMode == mode) return;
        currentMode = mode;
        if (mode == ControlMode.FPS)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            WarpCursorToCenter();
            InputManager.ClearLook();
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void WarpCursorToCenter()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            Mouse.current.WarpCursorPosition(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
#endif
    }

    public void LoadCameraDirection(Vector2 yawPitch)
    {
        float yaw = yawPitch.x;
        float pitch = UnwrapAngle(yawPitch.y);
        _yaw = _smoothYaw = yaw;
        _pitch = _smoothPitch = pitch;
        _originalYaw = _yaw;
        _originalPitch = _pitch;
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    public void ForceCameraSync()
    {
        if (_target == null) return;

        transform.position = _target.position + Vector3.up * eyeHeight;
        transform.rotation = Quaternion.Euler(_smoothPitch, _smoothYaw, 0f);

        var playerRb = _target.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.MoveRotation(Quaternion.Euler(0f, _smoothYaw, 0f));
        }

        _yawVel = _pitchVel = 0f;
    }

    public void LoadCameraDirectionAndSyncPlayer(Vector2 yawPitch)
    {
        float yaw = yawPitch.x;
        float pitch = UnwrapAngle(yawPitch.y);

        _yaw = _smoothYaw = yaw;
        _pitch = _smoothPitch = pitch;
        _originalYaw = _yaw;
        _originalPitch = _pitch;

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        var playerRb = _target?.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.MoveRotation(Quaternion.Euler(0f, yaw, 0f));
            playerRb.angularVelocity = Vector3.zero;
        }

        _yawVel = 0f;
        _pitchVel = 0f;
    }

    public void FocusOnPoint(FocusPoint focusPoint)
    {
        if (currentMode == ControlMode.Focus || focusPoint == null) return;

        _modeBeforeFocus = currentMode;
        _currentFocusPoint = focusPoint;

        _originalPlayerPosition = _target.position;
        _originalPlayerRotation = _target.rotation;
        _originalYaw = _yaw;
        _originalPitch = _pitch;

        _focusStartPos = transform.position;
        _focusStartRot = transform.rotation;

        Transform cameraPoint = focusPoint.cameraPoint;
        Transform playerPoint = focusPoint.playerPoint;

        _focusTargetPos = cameraPoint.position;
        _focusTargetRot = cameraPoint.rotation;

        _focusTimer = 0f;

        MovePlayerToPoint(playerPoint);
        SetMode(ControlMode.Focus);
    }

    private void MovePlayerToPoint(Transform playerPoint)
    {
        var col = _target.GetComponent<Collider>();
        _playerColliderWasEnabled = col != null && col.enabled;
        if (col != null) col.enabled = false;

        if (_targetRb != null)
        {
            _targetRb.isKinematic = true;
            _targetRb.linearVelocity = Vector3.zero;
            _targetRb.angularVelocity = Vector3.zero;
        }

        _target.position = playerPoint.position;
        _target.rotation = playerPoint.rotation;

        var playerMovement = _target.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.SetFocusState(true, playerPoint.position, playerPoint.rotation);
        }
    }

    private void ReturnPlayerToOriginal()
    {
        var playerMovement = _target.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.SetFocusState(false, _originalPlayerPosition, _originalPlayerRotation);
        }

        _target.position = _originalPlayerPosition;
        _target.rotation = _originalPlayerRotation;

        if (_targetRb != null)
        {
            _targetRb.isKinematic = false;
            _targetRb.linearVelocity = Vector3.zero;
            _targetRb.angularVelocity = Vector3.zero;
        }

        var col = _target.GetComponent<Collider>();
        if (col != null) col.enabled = _playerColliderWasEnabled;

        _yaw = _originalYaw;
        _pitch = _originalPitch;
        _smoothYaw = _originalYaw;
        _smoothPitch = _originalPitch;
        _yawVel = 0f;
        _pitchVel = 0f;

        ForceCameraSync();
    }

    public void ReleaseFocus()
    {
        if (currentMode != ControlMode.Focus) return;

        ReturnPlayerToOriginal();

        SetMode(_modeBeforeFocus);

        _currentFocusPoint = null;
    }

    private void Update()
    {
        HandleFocusClick();
        HandleFocusExit();

        if (currentMode == ControlMode.Focus)
        {
            UpdateFocus();
            return;
        }

        if (currentMode != ControlMode.FPS || _target == null || _router == null)
        {
            HandleFPS_UIRaycast();
            return;
        }

        Vector2 look = InputManager.Instance.Look;

        bool newInVehicle = _router.CurrentController is TransportMovement;
        if (newInVehicle != _inVehicle)
        {
            _inVehicle = newInVehicle;
            if (_inVehicle) { _currentVehicle = (TransportMovement)_router.CurrentController; _yaw = _currentVehicle.transform.eulerAngles.y; _pitch = 0f; }
            else _currentVehicle = null;
        }

        _yaw += look.x * _sensX;
        _pitch -= look.y * _sensY;
        _pitch = Mathf.Clamp(_pitch, pitchMin, pitchMax);
        _yaw = NormalizeAngle(_yaw);

        if (_inVehicle && _currentVehicle != null)
        {
            float vehicleYaw = _currentVehicle.transform.eulerAngles.y;
            float rel = Mathf.Clamp(NormalizeAngle(_yaw - vehicleYaw), -vehicleYawLimit, vehicleYawLimit);
            _yaw = vehicleYaw + rel;
        }

        _smoothYaw = Mathf.SmoothDampAngle(_smoothYaw, _yaw, ref _yawVel, rotationSmoothTime);
        _smoothPitch = Mathf.SmoothDampAngle(_smoothPitch, _pitch, ref _pitchVel, rotationSmoothTime);
        transform.rotation = Quaternion.Euler(_smoothPitch, _smoothYaw, 0f);

        HandleFPS_UIRaycast();
    }

    private void HandleFocusClick()
    {
        if (currentMode == ControlMode.Focus) return;
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, focusClickDistance, focusClickLayer))
        {
            var focusPoint = hit.collider.GetComponent<FocusPoint>();
            if (focusPoint != null)
            {
                FocusOnPoint(focusPoint);
            }
        }
    }

    private void HandleFocusExit()
    {
        if (currentMode == ControlMode.Focus && Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            ReleaseFocus();
        }
    }

    private void UpdateFocus()
    {
        if (_currentFocusPoint == null)
        {
            ReleaseFocus();
            return;
        }

        _focusTimer += Time.deltaTime;
        float t = Mathf.Clamp01(_focusTimer / focusTransitionTime);

        transform.position = Vector3.Lerp(_focusStartPos, _focusTargetPos, t);
        transform.rotation = Quaternion.Slerp(_focusStartRot, _focusTargetRot, t);

        if (t >= 1f)
        {
            transform.position = _focusTargetPos;
            transform.rotation = _focusTargetRot;
        }
    }

    private void FixedUpdate()
    {
        if (_targetRb && currentMode == ControlMode.FPS && _router.CurrentController is PlayerMovement)
            _targetRb.MoveRotation(Quaternion.Euler(0f, _yaw, 0f));
    }

    private void LateUpdate()
    {
        if (currentMode == ControlMode.Focus) return;
        if (_target) transform.position = _target.position + Vector3.up * eyeHeight;
    }

    private void HandleFPS_UIRaycast()
    {
        if (currentMode != ControlMode.FPS) return;
        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, uiRayDistance, uiLayer) && Mouse.current.leftButton.wasPressedThisFrame)
        {
            var btn = hit.collider.GetComponent<Button>();
            btn?.onClick.Invoke();
        }
    }

    private void UpdateSensitivity(bool holdingTool)
    {
        float m = holdingTool ? toolSensitivityMultiplier : 1f;
        _sensX = mouseSensitivityX * m;
        _sensY = mouseSensitivityY * m;
    }

    private static float NormalizeAngle(float a)
    {
        while (a > 180f) a -= 360f;
        while (a < -180f) a += 360f;
        return a;
    }

    private static float UnwrapAngle(float a) => a > 180f ? a - 360f : a;
}