using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour, IControllable
{
    [Header("Движение")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField, Range(1f, 50f)] private float acceleration = 20f;
    [SerializeField, Range(1f, 50f)] private float deceleration = 30f;
    [SerializeField, Range(1f, 50f)] private float airAcceleration = 8f;
    [SerializeField, Range(0f, 1f)] private float stopThreshold = 0.1f;
    [Header("Земля")]
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundMask = -1;
    [Header("Посадка в транспорт")]
    [SerializeField] private float mountDistance = 2f;
    [SerializeField] private LayerMask transportMask = -1;
    [Header("Добыча")]
    [SerializeField] private float miningRange = 3f;
    [SerializeField] private LayerMask miningMask = -1;
    [SerializeField] private Transform miningRayOrigin;
    [Header("Компоненты")]
    [SerializeField] private CanGrab objectGrabber;

    private Rigidbody _rb;
    private InputRouter _router;
    private Vector2 _input;
    private Vector2 _smoothedInput;
    private bool _isMounting;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.freezeRotation = true;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _router = FindFirstObjectByType<InputRouter>();
        if (objectGrabber == null) objectGrabber = GetComponent<CanGrab>();
        if (miningRayOrigin == null) miningRayOrigin = Camera.main ? Camera.main.transform : transform;
    }

    public void HandleMovement(Vector2 input) => _input = input;

    private void FixedUpdate()
    {
        if (_router?.CurrentController != this || _isMounting) return;
        Move(_input);
    }

    private void Move(Vector2 input)
    {
        bool grounded = IsGrounded();
        if (input.sqrMagnitude < stopThreshold * stopThreshold) input = Vector2.zero;
        float accel = grounded ? acceleration : airAcceleration;
        float decel = grounded ? deceleration : airAcceleration;
        _smoothedInput = input.sqrMagnitude > 0.01f
            ? Vector2.MoveTowards(_smoothedInput, input, accel * Time.fixedDeltaTime)
            : Vector2.MoveTowards(_smoothedInput, Vector2.zero, decel * Time.fixedDeltaTime);
        Vector3 dir = (transform.right * _smoothedInput.x + transform.forward * _smoothedInput.y).normalized;
        Vector3 target = dir * walkSpeed;
        Vector3 horiz = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        horiz = Vector3.MoveTowards(horiz, target, accel * 10f * Time.fixedDeltaTime);
        _rb.linearVelocity = new Vector3(horiz.x, _rb.linearVelocity.y, horiz.z);
    }

    private bool IsGrounded()
    {
        var col = GetComponent<Collider>();
        if (!col) return false;
        var b = col.bounds;
        float r = b.extents.x * 0.9f;
        float d = b.extents.y - r + groundCheckDistance;
        return Physics.SphereCast(b.center, r, Vector3.down, out _, d, groundMask);
    }

    public void HandlePhysicalInteract(bool pressed, bool held)
    {
        objectGrabber?.HandlePhysicalInteract(pressed, held);
        if (pressed) MineIce();
    }

    private void MineIce()
    {
        if (Physics.Raycast(miningRayOrigin.position, miningRayOrigin.forward, out RaycastHit hit, miningRange, miningMask))
            if (hit.collider.TryGetComponent<IceDeposit>(out var deposit))
                deposit.Hit();
    }

    public void HandleInteract(bool pressed)
    {
        if (!pressed || _isMounting) return;
        if (objectGrabber?.IsHoldingObject() == true)
        {
            TrySnapObject();
            return;
        }
        TryMountTransport();
    }

    private void TrySnapObject()
    {
        var item = objectGrabber.GetGrabbedItem();
        if (item == null) return;
        var zone = Physics.OverlapSphere(transform.position, 2.5f)
            .Select(c => c.GetComponent<SnapZone>())
            .FirstOrDefault(z => z != null && z.CanSnap(item));
        zone?.Snap(item);
    }

    private void TryMountTransport()
    {
        var nearest = Physics.OverlapSphere(transform.position, mountDistance, transportMask)
            .Select(c => c.GetComponent<TransportMovement>())
            .Where(t => t != null)
            .OrderBy(t => Vector3.Distance(transform.position, t.transform.position))
            .FirstOrDefault();

        if (nearest != null)
        {
            _isMounting = true;
            Mount(nearest);
        }
    }

    private void Mount(TransportMovement transport)
    {
        _rb.isKinematic = true;
        _rb.useGravity = false;
        _rb.linearVelocity = Vector3.zero;
        var col = GetComponent<Collider>();
        col.enabled = false;
        Transform seat = transport.seatTransform;
        if (seat == null)
        {
            seat = new GameObject("PlayerSeat").transform;
            seat.SetParent(transport.transform, false);
            seat.localPosition = transport.fallbackMountOffset;
        }
        transform.SetParent(seat);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        _router.SetController(transport);
        _isMounting = false;
    }

    public void HandleFlare(bool pressed)
        => GetComponent<FlareController>()?.ThrowFlare(pressed);

    public void HandleRotation(Vector2 mouseDelta) { }
}