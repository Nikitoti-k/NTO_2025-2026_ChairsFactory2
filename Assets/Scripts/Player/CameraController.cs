using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CameraController : MonoBehaviour
{
    public enum ControlMode { UI, FPS }

    public float eyeHeight = 1.6f;
    public float mouseSensitivityX = 1.8f;
    public float mouseSensitivityY = 1.8f;
    public float toolSensitivityMultiplier = 0.4f;
    public float pitchMin = -80f;
    public float pitchMax = 80f;
    public float rotationSmoothTime = 0.15f;
    public float vehicleYawLimit = 70f;
    public float toolPointMoveSpeed = 0.002f;

    [SerializeField] private LayerMask uiLayer = 1;
    [SerializeField] private float uiRayDistance = 4f;

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
        }
        _router = FindFirstObjectByType<InputRouter>();

        _yaw = _smoothYaw = 0f;
        _pitch = _smoothPitch = 0f;
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
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    public void ForceCameraSync()
    {
        if (_target == null) return;
        transform.position = _target.position + Vector3.up * eyeHeight;
        transform.rotation = Quaternion.Euler(_smoothPitch, _smoothYaw, 0f);
        _yawVel = _pitchVel = 0f;
    }

    private void Update()
    {
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

    private void FixedUpdate()
    {
        if (_targetRb && currentMode == ControlMode.FPS && _router.CurrentController is PlayerMovement)
            _targetRb.MoveRotation(Quaternion.Euler(0f, _yaw, 0f));
    }

    private void LateUpdate()
    {
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