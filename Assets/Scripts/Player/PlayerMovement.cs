using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour, IControllable
{
    [Header("Настройки движения")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField, Range(1f, 50f)] private float acceleration = 20f;
    [SerializeField, Range(1f, 50f)] private float deceleration = 30f;
    [SerializeField, Range(1f, 50f)] private float airAcceleration = 8f;
    [SerializeField, Range(0f, 1f)] private float stopThreshold = 0.1f;

    [Header("Проверка земли")]
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundMask = -1;

    [Header("Разное")]
    [SerializeField] private float mountDistance = 2f;
    [SerializeField] private LayerMask transportLayerMask = -1;
    [SerializeField] private float mountSpeed = 10f;

    [Header("Добыча льда")]
    [SerializeField] private float miningRange = 3f;
    [SerializeField] private LayerMask miningLayerMask = -1;
    [SerializeField] private Transform miningRayOrigin;

    [Header("Захват объектов")]
    [SerializeField] private CanGrab objectGrabber;

    private Rigidbody rb;
    private InputRouter inputRouter;
    private bool isMounting = false;
    private bool isGrounded = true;

    private Vector2 currentMovementInput;
    private Vector2 currentInput;

    private void Awake()
    {
        // Инициализация компонентов
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        inputRouter = FindFirstObjectByType<InputRouter>();
        if (objectGrabber == null)
            objectGrabber = FindFirstObjectByType<CanGrab>();
        if (miningRayOrigin == null)
            miningRayOrigin = Camera.main ? Camera.main.transform : transform;
    }

    public void HandleMovement(Vector2 input)
    {
        // Сохранение ввода
        currentMovementInput = input;
    }

    private void FixedUpdate()
    {
        if (inputRouter?.CurrentController != (object)this || isMounting) return;

        
        Vector2 input = currentMovementInput;
        HandleMovementPhysics(input);
    }

    private void HandleMovementPhysics(Vector2 input)
    {
        // Проверка земли
        isGrounded = IsGrounded();

        float targetSpeed = input.magnitude;
        if (targetSpeed < stopThreshold)
            input = Vector2.zero;

        float currentAccel = isGrounded ? acceleration : airAcceleration;
        float currentDecel = isGrounded ? deceleration : airAcceleration;

        if (input.sqrMagnitude > 0.01f)
        {
            currentInput = Vector2.MoveTowards(currentInput, input, currentAccel * Time.fixedDeltaTime);
        }
        else
        {
            currentInput = Vector2.MoveTowards(currentInput, Vector2.zero, currentDecel * Time.fixedDeltaTime);
        }

        Vector3 moveDirection = (transform.right * currentInput.x + transform.forward * currentInput.y).normalized;
        Vector3 desiredVelocity = moveDirection * walkSpeed;

        Vector3 currentHorizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 newHorizontalVel = Vector3.MoveTowards(currentHorizontalVel, desiredVelocity,
            (isGrounded ? acceleration : airAcceleration) * Time.fixedDeltaTime * 10f);

        rb.linearVelocity = new Vector3(newHorizontalVel.x, rb.linearVelocity.y, newHorizontalVel.z);
    }

    private bool IsGrounded()
    {
        // Проверка сферой
        Collider col = GetComponent<Collider>();
        if (!col) return false;

        Bounds bounds = col.bounds;
        Vector3 origin = bounds.center;
        float radius = bounds.extents.x * 0.9f;
        float distance = bounds.extents.y - radius + groundCheckDistance;

        return Physics.SphereCast(origin, radius, Vector3.down, out _, distance, groundMask, QueryTriggerInteraction.Ignore);
    }

    public void HandlePhysicalInteract(bool pressed, bool held)
    {
        // Взаимодействие с захватом или добычей
        objectGrabber?.HandlePhysicalInteract(pressed, held);
        if (pressed) TryMineIceDeposit();
    }

    private void TryMineIceDeposit()
    {
        // Рейкаст для льда
        Ray ray = new Ray(miningRayOrigin.position, miningRayOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, miningRange, miningLayerMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.TryGetComponent(out IceDeposit deposit))
                deposit.Hit();
        }
    }

    public void HandleRotation(Vector2 mouseDelta) { }

    public void HandleFlare(bool pressed)
    {
        // Бросок факела
        GetComponent<FlareController>()?.ThrowFlare(pressed);
    }

    public void HandleInteract(bool pressed)
    {
        if (!pressed || isMounting) return;

        // Попытка snap если держим объект
        if (objectGrabber != null && objectGrabber.IsHoldingObject())
        {
            GrabbableItem grabbedItem = objectGrabber.GetGrabbedItem();
            if (grabbedItem != null)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, 2.5f);
                foreach (Collider col in hits)
                {
                    if (col.TryGetComponent<SnapZone>(out SnapZone snapZone) && snapZone.CanSnap(grabbedItem))
                    {
                        snapZone.Snap(grabbedItem);
                        return;
                    }
                }
            }

            Debug.Log("Нельзя сесть на снегоход — у тебя предмет в руках!");
            return;
        }

        // Посадка на транспорт
        Collider[] transportHits = Physics.OverlapSphere(transform.position, mountDistance, transportLayerMask);
        TransportMovement nearest = null;
        float bestDist = float.MaxValue;

        foreach (Collider col in transportHits)
        {
            if (col.TryGetComponent<TransportMovement>(out TransportMovement transport))
            {
                float d = Vector3.Distance(transform.position, transport.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    nearest = transport;
                }
            }
        }

        if (nearest != null)
        {
            isMounting = true;
            inputRouter.DisableInput();
            StartCoroutine(MountToTransport(nearest));
        }
    }

    private IEnumerator MountToTransport(TransportMovement transport)
    {
        // Анимация посадки
        transform.SetParent(transport.transform);
        rb.isKinematic = true;
        rb.useGravity = false;
        GetComponent<Collider>().isTrigger = true;

        Vector3 targetPos = transport.GetMountPosition();
        while (Vector3.Distance(transform.localPosition, targetPos) > 0.01f)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, mountSpeed * Time.deltaTime);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.identity, mountSpeed * Time.deltaTime);
            yield return null;
        }

        transform.localPosition = targetPos;
        transform.localRotation = Quaternion.identity;
        inputRouter.SetController(transport);
        isMounting = false;
    }
}