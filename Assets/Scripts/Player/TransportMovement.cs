using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TransportMovement : MonoBehaviour, IControllable
{
    [Header("Скорость")]
    [SerializeField] private float forwardSpeed = 16f;
    [SerializeField] private float reverseSpeed = 10f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float brakeDeceleration = 30f;

    [Header("Поворот")]
    [SerializeField] private float turnSpeed = 100f;
    [SerializeField] private float lowSpeedTurnBonus = 3f;
    [SerializeField, Range(0.05f, 0.5f)] private float turnSmoothTime = 0.12f;
    [SerializeField] private float minTurnSpeed = 1.5f;

    [Header("Проходимость")]
    [SerializeField] private float raycastDistance = 3f;

    [Header("Посадка")]
    public Transform seatTransform;
    public Vector3 fallbackMountOffset = new Vector3(0f, 1.2f, 0.6f);

    private Rigidbody _rb;
    private Vector2 _input;
    private float _currentTurn;
    private float _targetForwardSpeed;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.freezeRotation = true;
        _rb.centerOfMass = new Vector3(0f, -1.1f, 0.3f);
        _rb.solverVelocityIterations = 16;
        _rb.solverIterations = 10;
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
        float forwardInput = _input.y;
        float turnInput = _input.x;

        Vector3 flatVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        float currentForwardSpeed = Vector3.Dot(flatVel, transform.forward);

        float desiredSpeed = 0f;
        if (forwardInput > 0.01f) desiredSpeed = forwardSpeed;
        else if (forwardInput < -0.01f) desiredSpeed = -reverseSpeed;

        _targetForwardSpeed = Mathf.MoveTowards(_targetForwardSpeed, desiredSpeed, acceleration * Time.fixedDeltaTime);

        bool isBraking = Mathf.Abs(currentForwardSpeed) > 1f &&
                         Mathf.Sign(currentForwardSpeed) != Mathf.Sign(forwardInput) &&
                         forwardInput != 0f;

        float effectiveAccel = isBraking ? brakeDeceleration : acceleration;

        Vector3 comWorld = transform.TransformPoint(_rb.centerOfMass);
        Vector3 surfaceNormal = Vector3.up;
        if (Physics.Raycast(comWorld, Vector3.down, out RaycastHit hit, raycastDistance))
            surfaceNormal = hit.normal;

        Vector3 currentVelOnPlane = Vector3.ProjectOnPlane(flatVel, surfaceNormal);
        Vector3 desiredDirOnPlane = Vector3.ProjectOnPlane(transform.forward, surfaceNormal).normalized;
        Vector3 desiredVelOnPlane = desiredDirOnPlane * _targetForwardSpeed;
        Vector3 velDelta = desiredVelOnPlane - currentVelOnPlane;
        Vector3 force = velDelta / Time.fixedDeltaTime;
        force = Vector3.ClampMagnitude(force, effectiveAccel);
        _rb.AddForce(force, ForceMode.Acceleration);

        _currentTurn = Mathf.MoveTowards(_currentTurn, turnInput, Time.fixedDeltaTime / turnSmoothTime);

        float absForwardSpeed = Mathf.Abs(currentForwardSpeed);
        float rotationThisFrame = 0f;
        if (absForwardSpeed >= minTurnSpeed)
        {
            float speedFactor = Mathf.InverseLerp(minTurnSpeed, forwardSpeed * 0.6f, absForwardSpeed);
            float turnBonus = Mathf.Lerp(lowSpeedTurnBonus, 1f, speedFactor);
            rotationThisFrame = _currentTurn * turnSpeed * turnBonus * Time.fixedDeltaTime;
        }

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
        prb.interpolation = RigidbodyInterpolation.Interpolate;

        var col = player.GetComponent<Collider>();
        col.enabled = true;
        col.isTrigger = false;

        Vector3 exitPos = transform.position + transform.forward * 2.8f + Vector3.up * 1.5f;
        player.transform.position = exitPos;
        prb.linearVelocity = _rb.linearVelocity + Vector3.up * 5f;

        router?.SetController(player);
    }
}