using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TransportMovement : MonoBehaviour, IControllable
{
    [Header("Скорость")]
    [SerializeField] private float forwardSpeed = 16f;
    [SerializeField] private float reverseSpeed = 10f;
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float brakeDeceleration = 50f;
    [Header("Поворот")]
    [SerializeField] private float turnSpeed = 100f;
    [SerializeField] private float lowSpeedTurnBonus = 2.2f;
    [SerializeField, Range(0.05f, 0.5f)] private float turnSmoothTime = 0.12f;
    [Header("Посадка")]
    public Transform seatTransform;
    public Vector3 fallbackMountOffset = new Vector3(0f, 1.2f, 0.6f);

    private Rigidbody _rb;
    private Vector2 _input;
    private float _currentSpeed;
    private float _currentTurn;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.freezeRotation = true;
        _rb.centerOfMass = new Vector3(0f, -0.6f, 0f);
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
        Drive();
    }

    private void Drive()
    {
        float forward = _input.y;
        float turn = _input.x;

        float targetSpeed = forward > 0.01f ? forwardSpeed :
                           forward < -0.01f ? -reverseSpeed : 0f;
        bool braking = Mathf.Abs(Vector3.Dot(_rb.linearVelocity, transform.forward)) > 1f &&
                       Mathf.Sign(forward) != Mathf.Sign(Vector3.Dot(_rb.linearVelocity, transform.forward));
        float accel = braking || forward == 0f ? brakeDeceleration : acceleration;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, accel * Time.fixedDeltaTime);
        Vector3 velocity = transform.forward * _currentSpeed;
        Vector3 horiz = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        horiz = Vector3.MoveTowards(horiz, velocity, accel * 2f * Time.fixedDeltaTime);
        _rb.linearVelocity = new Vector3(horiz.x, _rb.linearVelocity.y, horiz.z);

        _currentTurn = Mathf.MoveTowards(_currentTurn, turn, Time.fixedDeltaTime / turnSmoothTime);
        float speedFactor = Mathf.InverseLerp(0f, forwardSpeed * 0.6f, Mathf.Abs(Vector3.Dot(_rb.linearVelocity, transform.forward)));
        float turnBonus = Mathf.Lerp(lowSpeedTurnBonus, 1f, speedFactor);
        float rotationThisFrame = _currentTurn * turnSpeed * turnBonus * Time.fixedDeltaTime;
        if (Mathf.Abs(rotationThisFrame) > 0.001f)
            _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0f, rotationThisFrame, 0f));
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
        Vector3 exitPos = transform.position + transform.forward * 2.8f + Vector3.up * 1.5f;
        player.transform.position = exitPos;
        prb.linearVelocity = transform.forward * 6f + Vector3.up * 5f;
        router?.SetController(player);
    }
}