using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TransportMovement : MonoBehaviour, IControllable
{
    [Header("=== Движение ===")]
    [SerializeField] private float maxForwardSpeed = 18f;
    [SerializeField] private float maxReverseSpeed = 10f;
    [SerializeField] private float accelerationForce = 320f;      // сила тяги вперёд/назад
    [SerializeField] private float brakeForce = 500f;            // сила торможения (очень мощная)
    [SerializeField] private float dragOnGround = 1.2f;          // сопротивление при движении
    [SerializeField] private float airDrag = 0.1f;               // сопротивление в воздухе

    [Header("=== Поворот ===")]
    [SerializeField] private float maxTurnRate = 80f;            // градусов в секунду на максимальной скорости
    [SerializeField] private float minTurnRate = 180f;           // градусов в секунду почти на месте
    [SerializeField] private float turnSpeedThreshold = 4f;     // ниже этой скорости поворот становится резко лучше
    [SerializeField] private float turnSmoothTime = 0.15f;      // инерция поворота руля

    [Header("=== Посадка ===")]
    public Transform seatTransform;
    public Vector3 fallbackMountOffset = new Vector3(0f, 1.2f, 0.6f);

    private Rigidbody _rb;
    private Vector2 _input;
    private float _currentTurnInput;     // сглаженный поворот (имитация руля)

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.centerOfMass = new Vector3(0f, -0.8f, 0f); // ниже — стабильнее
        _rb.linearDamping = airDrag;
        _rb.angularDamping = 5f;
    }

    public void HandleMovement(Vector2 input) => _input = input;

    public void HandleInteract(bool pressed)
    {
        if (pressed) Dismount();
    }

    public void HandlePhysicalInteract(bool pressed, bool held) { }
    public void HandleFlare(bool pressed) { }

    private void FixedUpdate()
    {
        if (InputRouter.Instance?.CurrentController != this) return;

        HandleDrag();
        HandleThrustAndBrake();
        HandleSteering();
    }

    private void HandleDrag()
    {
        // Если транспорт на земле — добавляем дополнительное сопротивление (имитация трения о грунт)
        bool onGround = Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, 1.2f);
        _rb.linearDamping = onGround ? dragOnGround : airDrag;
    }

    private void HandleThrustAndBrake()
    {
        Vector3 forward = transform.forward;
        float currentForwardSpeed = Vector3.Dot(_rb.linearVelocity, forward);

        float inputY = _input.y;

        // Определяем, едем ли мы в противоположную сторону от ввода (нужен тормоз)
        bool isBraking = inputY != 0 && Mathf.Sign(inputY) != Mathf.Sign(currentForwardSpeed) && Mathf.Abs(currentForwardSpeed) > 0.5f;
        // Или игрок отпустил газ — тоже тормозим
        bool neutralBrake = inputY == 0 && Mathf.Abs(currentForwardSpeed) > 0.5f;

        if (isBraking || neutralBrake)
        {
            // Тормозим против направления движения
            Vector3 brakeDirection = -_rb.linearVelocity.normalized;
            float brakePower = brakeForce * (isBraking ? 1.3f : 0.8f); // ручной тормоз чуть сильнее
            _rb.AddForce(brakeDirection * brakePower * Time.fixedDeltaTime, ForceMode.Acceleration);
        }
        else if (Mathf.Abs(inputY) > 0.01f)
        {
            // Тяга вперёд или назад
            float targetMax = inputY > 0 ? maxForwardSpeed : maxReverseSpeed;
            float speedLimit = targetMax * Mathf.Abs(inputY);

            // Если уже почти на максимуме — не даём разгоняться дальше
            if (Mathf.Abs(currentForwardSpeed) < speedLimit || Mathf.Sign(currentForwardSpeed) != Mathf.Sign(inputY))
            {
                Vector3 thrust = forward * inputY * accelerationForce * Time.fixedDeltaTime;
                _rb.AddForce(thrust, ForceMode.Acceleration);
            }
        }
    }

    private void HandleSteering()
    {
        float speed = _rb.linearVelocity.magnitude;

        // Сглаживаем ввод поворота — руль не поворачивается мгновенно
        float targetTurn = _input.x;
        _currentTurnInput = Mathf.MoveTowards(_currentTurnInput, targetTurn, Time.fixedDeltaTime / turnSmoothTime);

        if (Mathf.Abs(_currentTurnInput) < 0.01f) return;

        // Поворот зависит от скорости: чем быстрее — тем хуже поворачивает
        float speedFactor = Mathf.InverseLerp(0f, turnSpeedThreshold, speed);
        float currentTurnRate = Mathf.Lerp(minTurnRate, maxTurnRate, speedFactor);

        float rotationThisFrame = _currentTurnInput * currentTurnRate * Time.fixedDeltaTime;

        // Критически важно: не даём поворачивать, если скорость слишком мала и мы почти стоим
        // (убираем разворот на месте)
        float forwardSpeed = Vector3.Dot(_rb.linearVelocity, transform.forward);
        if (Mathf.Abs(forwardSpeed) < 1.5f && speed < 3f)
        {
            // На очень малой скорости можно поворачивать, но только если есть хоть какой-то импульс
            rotationThisFrame *= Mathf.InverseLerp(0f, 3f, speed);
        }

        if (Mathf.Abs(rotationThisFrame) > 0.001f)
        {
            Quaternion deltaRot = Quaternion.Euler(0f, rotationThisFrame, 0f);
            _rb.MoveRotation(_rb.rotation * deltaRot);
        }
    }

    private void Dismount()
    {
        var player = FindFirstObjectByType<PlayerMovement>();
        if (!player) return;

        var router = InputRouter.Instance;
        player.transform.SetParent(null);

        var prb = player.GetComponent<Rigidbody>();
        prb.isKinematic = false;
        prb.useGravity = true;

        var col = player.GetComponent<Collider>();
        col.enabled = true;
        col.isTrigger = false;

        Vector3 exitPos = transform.position + transform.right * 1.5f + transform.forward * -2f + Vector3.up * 1.5f;
        player.transform.position = exitPos;

        // Выбрасываем игрока с учётом скорости транспорта
        Vector3 inheritVel = _rb.linearVelocity * 0.7f;
        prb.linearVelocity = inheritVel + Vector3.up * 6f + transform.right * 4f;

        router?.SetController(player);
    }

    // Визуализация в редакторе
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, Vector3.down * 1.2f);
    }
}