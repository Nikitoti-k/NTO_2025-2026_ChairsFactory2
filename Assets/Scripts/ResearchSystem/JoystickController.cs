using UnityEngine;
using UnityEngine.InputSystem;

public class JoystickController : MonoBehaviour
{
    public static JoystickController Instance { get; private set; }

    [Header("Visuals")]
    [SerializeField] private Transform handle;
    [SerializeField] private float maxAngle = 45f;
    [SerializeField, Range(1f, 30f)] private float returnSpeed = 18f;

    [Header("Interaction")]
    [SerializeField] private float grabRadius = 0.15f;
    [SerializeField] private LayerMask joystickLayer = -1;
    [SerializeField] private bool invertX = true;
    [SerializeField] private bool invertY = true;

    [Header("Sensitivity")]
    [SerializeField] private float mouseSensitivity = 1.5f;
    [SerializeField] private float deadZone = 0.1f;

    public Vector2 CurrentDirection { get; private set; } = Vector2.zero;
    public bool IsGrabbed { get; private set; } = false;

    private Quaternion initialRotation;
    private Vector2 currentTilt = Vector2.zero;
    private Vector2 targetTilt = Vector2.zero;
    private Camera _mainCamera;
    private Vector2 _lastMousePos;
    private bool _isActive = false;

    private void Awake()
    {
        Instance = this;
        if (handle == null && transform.childCount > 0)
            handle = transform.GetChild(0);

        initialRotation = handle != null ? handle.localRotation : Quaternion.identity;
        _mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        _isActive = true;
    }

    private void OnDisable()
    {
        _isActive = false;
        IsGrabbed = false;
        ResetJoystick();
    }

    public bool TryGrab(Vector2 screenPos)
    {
        if (!_isActive || IsGrabbed) return false;

        Ray ray = _mainCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 3f, joystickLayer))
        {
            if (hit.collider.transform == handle ||
                hit.collider.transform.IsChildOf(transform) ||
                Vector3.Distance(hit.point, handle.position) <= grabRadius)
            {
                IsGrabbed = true;
                _lastMousePos = screenPos;
                return true;
            }
        }

        return false;
    }

    public void UpdateDrag(Vector2 currentScreenPos)
    {
        if (!_isActive || !IsGrabbed) return;

        Vector2 mouseDelta = currentScreenPos - _lastMousePos;

        float screenScale = Mathf.Min(Screen.width, Screen.height);
        float normalizedSensitivity = mouseSensitivity / screenScale * 1000f;

        Vector2 newTilt = targetTilt + mouseDelta * normalizedSensitivity;

        if (newTilt.magnitude > maxAngle)
        {
            newTilt = newTilt.normalized * maxAngle;
        }

        targetTilt = newTilt;
        _lastMousePos = currentScreenPos;

        UpdateOutput();
    }

    public void Release()
    {
        if (!_isActive) return;

        IsGrabbed = false;
        targetTilt = Vector2.zero;
    }

    private void Update()
    {
        if (!_isActive) return;

        if (!IsGrabbed)
        {
            targetTilt = Vector2.Lerp(targetTilt, Vector2.zero, returnSpeed * Time.deltaTime);
        }

        UpdateVisuals();
        UpdateOutput();
    }

    private void UpdateVisuals()
    {
        currentTilt = Vector2.Lerp(currentTilt, targetTilt, returnSpeed * Time.deltaTime);

        if (handle != null)
        {
            float rotX = currentTilt.x * (invertX ? -1f : 1f);
            float rotY = currentTilt.y * (invertY ? -1f : 1f);
            handle.localRotation = initialRotation * Quaternion.Euler(rotY, 0f, rotX);
        }
    }

    private void UpdateOutput()
    {
        Vector2 normalizedTilt = targetTilt / maxAngle;

        if (normalizedTilt.magnitude < deadZone)
        {
            CurrentDirection = Vector2.zero;
        }
        else
        {
            float magnitude = (normalizedTilt.magnitude - deadZone) / (1f - deadZone);
            CurrentDirection = normalizedTilt.normalized * Mathf.Clamp01(magnitude);
        }
    }

    public void ResetJoystick()
    {
        IsGrabbed = false;
        targetTilt = Vector2.zero;
        currentTilt = Vector2.zero;
        CurrentDirection = Vector2.zero;

        if (handle != null)
        {
            handle.localRotation = initialRotation;
        }
    }

    public void SetActive(bool active)
    {
        _isActive = active;
        if (!active)
        {
            ResetJoystick();
        }
    }
}