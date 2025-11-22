using UnityEngine;

[RequireComponent(typeof(ConfigurableJoint))]
[RequireComponent(typeof(Rigidbody))]
public class JoystickController : MonoBehaviour
{
    public Vector2 CurrentDirection { get; private set; } = Vector2.zero;

    [Header("Настройки рычага")]
    [SerializeField] private Transform handle;  // сам рычаг
    [SerializeField] private float maxAngle = 45f;
    [SerializeField] private float mouseSensitivity = 140f;

    private Rigidbody handleRb;
    private Quaternion initialLocalRotation;

    private Vector2 virtualTilt = Vector2.zero;
    private bool isGrabbed = false;

    public static JoystickController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        if (handle == null)
            handle = transform;

        handleRb = handle.GetComponent<Rigidbody>();
        initialLocalRotation = handle.localRotation;
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
        if (rb != handleRb) return;

        isGrabbed = true;

        // Взят — делаем кинематическим, управление только наше
        handleRb.isKinematic = true;
        handleRb.useGravity = false;

        SyncTiltFromRotation();
    }

    private void OnReleased(CanGrab grabber, Rigidbody rb)
    {
        if (rb != handleRb) return;

        isGrabbed = false;

        // МГНОВЕННЫЙ сброс в ноль
        ResetToCenter();
    }

    private void Update()
    {
        if (!isGrabbed || InputManager.Instance == null) return;

        Vector2 look = InputManager.Instance.Look;
        Vector2 input = new Vector2(look.x, -look.y);

        virtualTilt += input * mouseSensitivity * Time.deltaTime;

        virtualTilt.x = Mathf.Clamp(virtualTilt.x, -maxAngle, maxAngle);
        virtualTilt.y = Mathf.Clamp(virtualTilt.y, -maxAngle, maxAngle);

        Quaternion target = Quaternion.Euler(virtualTilt.y, 0f, virtualTilt.x);
        handle.localRotation = target;
    }

    private void FixedUpdate()
    {
        Quaternion delta = handle.localRotation * Quaternion.Inverse(initialLocalRotation);
        Vector3 e = delta.eulerAngles;

        float x = e.x > 180f ? e.x - 360f : e.x;
        float z = e.z > 180f ? e.z - 360f : e.z;

        CurrentDirection = new Vector2(
            Mathf.Clamp(z / maxAngle, -1f, 1f),
            Mathf.Clamp(-x / maxAngle, -1f, 1f)
        );
    }

    private void ResetToCenter()
    {
        // ставим ровно в ноль
        handle.localRotation = initialLocalRotation;

        // возвращаем физику
        handleRb.isKinematic = false;
        handleRb.useGravity = true;

        // обнуляем virtualTilt
        virtualTilt = Vector2.zero;
    }

    private void SyncTiltFromRotation()
    {
        Quaternion delta = handle.localRotation * Quaternion.Inverse(initialLocalRotation);
        Vector3 e = delta.eulerAngles;

        float x = e.x > 180 ? e.x - 360 : e.x;
        float z = e.z > 180 ? e.z - 360 : e.z;

        virtualTilt = new Vector2(z, -x);
    }
}
