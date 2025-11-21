using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

[RequireComponent(typeof(ConfigurableJoint))]
[RequireComponent(typeof(Rigidbody))]
public class JoystickController : MonoBehaviour
{
    [Header("Выход: направление от -1 до 1")]
    public Vector2 CurrentDirection { get; private set; } = Vector2.zero;

    [Header("Настройки джойстика")]
    [SerializeField] private Transform handle;                    // Ручка (то, что хватают)
    [SerializeField] private float maxAngle = 45f;
    [SerializeField] private float mouseSensitivity = 120f;      // градусов за секунду от мыши
    [SerializeField] private float gamepadSensitivity = 90f;      // градусов за секунду от стика
    [SerializeField] private bool smoothReturnOnRelease = true;
    [SerializeField] private float returnDuration = 0.35f;
    [SerializeField] private AnimationCurve returnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private ConfigurableJoint joint;
    private Rigidbody baseRigidbody;
    private Rigidbody handleRigidbody;
    private Quaternion initialLocalRotation;

    // Виртуальный курсор в углах наклона
    private Vector2 virtualTilt = Vector2.zero;

    private Coroutine returnCoroutine;
    private bool isGrabbed = false;
    private bool isVirtualControlActive = false;

    public static JoystickController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        joint = GetComponent<ConfigurableJoint>();
        baseRigidbody = GetComponent<Rigidbody>();
        initialLocalRotation = transform.localRotation;

        if (handle != null)
            handleRigidbody = handle.GetComponent<Rigidbody>();

        if (handle == null)
            handle = transform;
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

    private void OnGrabbed(CanGrab grabber, Rigidbody grabbedRb)
    {
        // Проверяем, что схватили именно ручку этого джойстика
        if (handleRigidbody == null || grabbedRb != handleRigidbody) return;

        isGrabbed = true;
        isVirtualControlActive = true;

        // Делаем ручку кинематической — CanGrab больше не тянет её
        handleRigidbody.isKinematic = true;
        handleRigidbody.useGravity = false;

        // Синхронизируем виртуальный курсор с текущим положением
        Quaternion delta = transform.localRotation * Quaternion.Inverse(initialLocalRotation);
        Vector3 euler = delta.eulerAngles;
        float tiltX = (euler.x > 180f) ? euler.x - 360f : euler.x;
        float tiltZ = (euler.z > 180f) ? euler.z - 360f : euler.z;

        virtualTilt = new Vector2(tiltZ, -tiltX); // X = Z-ось, Y = X-ось (инвертируем Y)

        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }
    }

    private void OnReleased(CanGrab grabber, Rigidbody grabbedRb)
    {
        if (handleRigidbody == null || grabbedRb != handleRigidbody) return;

        isGrabbed = false;
        isVirtualControlActive = false;

        // Восстанавливаем физику
        handleRigidbody.isKinematic = false;
        handleRigidbody.useGravity = true;

        if (smoothReturnOnRelease)
            returnCoroutine = StartCoroutine(ReturnToCenterSmooth());
        else
            transform.localRotation = initialLocalRotation;
    }

    private void Update()
    {
        if (!isVirtualControlActive) return;

        Vector2 input = Vector2.zero;

        // Геймпад — правый стик
        if (Gamepad.current != null)
        {
            input = Gamepad.current.rightStick.ReadValue();
            virtualTilt += input * gamepadSensitivity * Time.deltaTime;
        }
        // Мышь — используем дельту из InputManager (твой Look)
        else if (InputManager.Instance != null)
        {
            Vector2 lookDelta = InputManager.Instance.Look;
            input = lookDelta * 0.1f; // Look уже в "пикселях", переводим в "угол"
            virtualTilt += input * mouseSensitivity * Time.deltaTime;
        }

        // Ограничиваем наклон
        virtualTilt.x = Mathf.Clamp(virtualTilt.x, -maxAngle, maxAngle);
        virtualTilt.y = Mathf.Clamp(virtualTilt.y, -maxAngle, maxAngle);

        // Применяем вращение
        Quaternion targetRotation = initialLocalRotation * Quaternion.Euler(virtualTilt.y, 0f, virtualTilt.x);
        transform.localRotation = targetRotation;
    }

    private void FixedUpdate()
    {
        // Расчёт CurrentDirection — всегда
        Quaternion delta = transform.localRotation * Quaternion.Inverse(initialLocalRotation);
        Vector3 euler = delta.eulerAngles;

        float tiltX = (euler.x > 180f) ? euler.x - 360f : euler.x;
        float tiltZ = (euler.z > 180f) ? euler.z - 360f : euler.z;

        float inputX = Mathf.Clamp(tiltZ / maxAngle, -1f, 1f);
        float inputY = Mathf.Clamp(-tiltX / maxAngle, -1f, 1f);

        CurrentDirection = new Vector2(inputX, inputY);

        // Принудительный возврат, если не схвачен
        if (!isGrabbed && returnCoroutine == null && !isVirtualControlActive)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, initialLocalRotation, 20f * Time.fixedDeltaTime);
            if (baseRigidbody != null)
                baseRigidbody.angularVelocity *= 0.9f;
        }
    }

    private IEnumerator ReturnToCenterSmooth()
    {
        Quaternion startRot = transform.localRotation;
        float elapsed = 0f;

        while (elapsed < returnDuration)
        {
            if (isGrabbed) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / returnDuration);
            float curveT = returnCurve.Evaluate(t);

            transform.localRotation = Quaternion.SlerpUnclamped(startRot, initialLocalRotation, curveT);
            yield return null;
        }

        transform.localRotation = initialLocalRotation;
        returnCoroutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.forward * 0.5f);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.right * 0.4f * CurrentDirection.x);

        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.up * 0.4f * CurrentDirection.y);
    }
}