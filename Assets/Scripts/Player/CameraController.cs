using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public enum ControlMode { UI, FPS }

    [Header("Eye & Sensitivity")]
    public float eyeHeight = 1.6f;
    public float mouseSensitivityX = 1.8f;
    public float mouseSensitivityY = 1.8f;
    public float toolSensitivityMultiplier = 0.4f;
    public float pitchMin = -80f;
    public float pitchMax = 80f;

    [Header("Smoothing")]
    [Range(0.08f, 0.3f)] public float rotationSmoothTime = 0.15f;

    [Header("Vehicle Look Limits")]
    [Range(30f, 90f)] public float vehicleYawLimit = 70f;  

    [Header("Tool Grab Point")]
    public float toolPointMoveSpeed = 0.002f;

    [SerializeField] private ControlMode currentMode = ControlMode.FPS;

    private Transform _target;
    private Rigidbody _targetRb;
    private CanGrab _playerGrabber;
    private InputRouter _router;  // ← НОВОЕ

    private float _yaw, _pitch;
    private float _smoothYaw, _smoothPitch;
    private float _yawVel, _pitchVel;
    private float _sensX, _sensY;
    private bool _justEnteredFps;

    
    private TransportMovement _currentVehicle;
    private bool _inVehicle;

    private void Start()
    {
        var player = FindFirstObjectByType<PlayerMovement>();
        _target = player.transform;
        _targetRb = player.GetComponent<Rigidbody>();
        _playerGrabber = player.GetComponent<CanGrab>();

        _router = FindFirstObjectByType<InputRouter>(); 

        _yaw = _smoothYaw = transform.eulerAngles.y;
        _pitch = _smoothPitch = UnwrapAngle(transform.eulerAngles.x);

        UpdateSensitivity();
        ApplyMode(currentMode);

        if (_playerGrabber != null)
        {
            CanGrab.OnGrabbed += OnGrabbed;
            CanGrab.OnReleased += OnReleased;
        }
    }

    private void OnDestroy()
    {
        if (_playerGrabber != null)
        {
            CanGrab.OnGrabbed -= OnGrabbed;
            CanGrab.OnReleased -= OnReleased;
        }
    }

    private void OnGrabbed(CanGrab grabber, Rigidbody rb)
    {
        if (grabber != _playerGrabber) return;
        if (grabber.GetGrabbedItem()?.ItemType == GrabbableType.Tool)
            UpdateSensitivity(true);
    }

    private void OnReleased(CanGrab grabber, Rigidbody rb)
    {
        if (grabber != _playerGrabber) return;
        UpdateSensitivity(false);
    }

    private void UpdateSensitivity(bool holdingTool = false)
    {
        float mult = holdingTool ? toolSensitivityMultiplier : 1f;
        _sensX = mouseSensitivityX * mult;
        _sensY = mouseSensitivityY * mult;
    }

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
                WarpCursorToCenter();
                InputManager.ClearLook();
                _justEnteredFps = true;
                break;
            case ControlMode.UI:
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                _justEnteredFps = false;
                UpdateSensitivity(false);
                break;
        }
    }

    private void WarpCursorToCenter()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            Mouse.current.WarpCursorPosition(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
#endif
    }

    private void Update()
    {
        if (currentMode != ControlMode.FPS || _target == null || _router == null) return;

        Vector2 look = InputManager.Instance.Look;
        if (_justEnteredFps)
        {
            look = Vector2.zero;
            _justEnteredFps = false;
        }

        
        bool newInVehicle = _router.CurrentController is TransportMovement;
        if (newInVehicle != _inVehicle)
        {
            if (newInVehicle)
            {
               
                _currentVehicle = (TransportMovement)_router.CurrentController;
                float vehicleYaw = _currentVehicle.transform.eulerAngles.y;
                _yaw = NormalizeAngle(vehicleYaw);
                _smoothYaw = _yaw;
                _pitch = 0f;
                _smoothPitch = _pitch;
            }
            else
            {
                
                _currentVehicle = null;
            }
            _inVehicle = newInVehicle;
        }

       
        _yaw += look.x * _sensX;
        _pitch -= look.y * _sensY;
        _pitch = Mathf.Clamp(_pitch, pitchMin, pitchMax);
        _yaw = NormalizeAngle(_yaw);

       
        if (_inVehicle && _currentVehicle != null)
        {
            float vehicleYaw = _currentVehicle.transform.eulerAngles.y;
            float relativeYaw = NormalizeAngle(_yaw - vehicleYaw);
            relativeYaw = Mathf.Clamp(relativeYaw, -vehicleYawLimit, vehicleYawLimit);
            _yaw = NormalizeAngle(vehicleYaw + relativeYaw);
        }

        
        _smoothYaw = Mathf.SmoothDampAngle(_smoothYaw, _yaw, ref _yawVel, rotationSmoothTime);
        _smoothPitch = Mathf.SmoothDampAngle(_smoothPitch, _pitch, ref _pitchVel, rotationSmoothTime);

        
        transform.rotation = Quaternion.Euler(_smoothPitch, _smoothYaw, 0f);

        
        if (_playerGrabber?.IsHoldingObject() == true &&
            _playerGrabber.GetGrabbedItem()?.ItemType == GrabbableType.Tool)
        {
            var tgp = _playerGrabber.toolGrabPoint;
            if (tgp != null)
            {
                Vector3 move = new Vector3(look.x, 0f, look.y) * toolPointMoveSpeed;
                Vector3 worldMove = transform.TransformDirection(move);
                worldMove.y = 0f;
                tgp.position += worldMove;
            }
        }
    }

    private void FixedUpdate()
    {
        if (_targetRb == null || currentMode != ControlMode.FPS) return;

       
        if (_router != null && _router.CurrentController is PlayerMovement)
        {
            _targetRb.MoveRotation(Quaternion.Euler(0f, _yaw, 0f));
        }
    }

    private void LateUpdate()
    {
        if (_target != null)
            transform.position = _target.position + Vector3.up * eyeHeight;
    }

    private static float NormalizeAngle(float a)
    {
        while (a > 180f) a -= 360f;
        while (a < -180f) a += 360f;
        return a;
    }

    private static float UnwrapAngle(float a) => a > 180f ? a - 360f : a;

#if UNITY_EDITOR
    private void OnValidate() => ApplyMode(currentMode);
#endif
}