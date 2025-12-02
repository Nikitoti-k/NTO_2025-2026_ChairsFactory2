using UnityEngine;

public class JoystickController : MonoBehaviour
{
    public static JoystickController Instance { get; private set; }
    [SerializeField] private Transform handle;
    [SerializeField] private float maxAngle = 45f;
    [SerializeField, Range(1f, 30f)] private float tiltSmooth = 18f;
    [SerializeField, Range(1f, 30f)] private float returnSmooth = 12f;
    [SerializeField, Range(0.1f, 10f)] private float acceleration = 3.5f;
    [SerializeField, Range(0.1f, 10f)] private float deceleration = 6f;
    [SerializeField] private bool invertX = true;
    [SerializeField] private bool invertY = true;
    [SerializeField] private float joystickSensitivity = 120f;

    public Vector2 CurrentDirection { get; private set; } = Vector2.zero;
    public Vector2 SmoothVelocity { get; private set; } = Vector2.zero;
    public bool IsGrabbed { get; private set; } = false;

    private Quaternion initialRotation;
    private Vector2 targetTilt;
    private Vector2 currentTilt;

    private void Awake()
    {
        Instance = this;
        if (handle == null) handle = transform.GetChild(0);
        initialRotation = handle.localRotation;
    }

    private void OnEnable()
    {
        CanGrab.OnGrabbed += OnGrabbed;
        CanGrab.OnReleased += OnReleased;
    }

    private void OnDisable()
    {
        CanGrab.OnGrabbed -= OnGrabbed;
        CanGrab.OnReleased -= OnReleased;
    }

    private void OnGrabbed(CanGrab grabber, Rigidbody rb)
    {
        if (rb != null && (rb.transform.IsChildOf(transform) || rb.transform == transform))
            IsGrabbed = true;
    }

    private void OnReleased(CanGrab grabber, Rigidbody rb)
    {
        if (rb != null && (rb.transform.IsChildOf(transform) || rb.transform == transform))
            IsGrabbed = false;
    }

    private void Update()
    {
        if (IsGrabbed)
        {
            Vector2 input = InputManager.Instance.Look;
            targetTilt += input * joystickSensitivity * Time.deltaTime;
            targetTilt = Vector2.ClampMagnitude(targetTilt, maxAngle);
        }
        else
        {
            targetTilt = Vector2.zero;
        }

        float smooth = IsGrabbed ? tiltSmooth : returnSmooth;
        currentTilt = Vector2.Lerp(currentTilt, targetTilt, smooth * Time.deltaTime);

        float rotX = currentTilt.x * (invertX ? -1f : 1f);
        float rotY = currentTilt.y * (invertY ? -1f : 1f);
        handle.localRotation = initialRotation * Quaternion.Euler(rotY, 0f, rotX);

        Vector2 targetDir = currentTilt / maxAngle;
        if (IsGrabbed)
            SmoothVelocity = Vector2.MoveTowards(SmoothVelocity, targetDir, acceleration * Time.deltaTime);
        else
            SmoothVelocity = Vector2.MoveTowards(SmoothVelocity, Vector2.zero, deceleration * Time.deltaTime);

        CurrentDirection = SmoothVelocity;
    }
}