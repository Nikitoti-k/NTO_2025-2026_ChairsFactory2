using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class FlareController : MonoBehaviour
{
    [Header("Настройки броска факелов")]
    [SerializeField] private Transform handHoldPoint;
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float maxThrowDistance = 50f;
    [SerializeField] private LayerMask flareTargetMask = -1;
    [SerializeField] private float scatterAmount = 1.5f;
    [SerializeField] private float cooldownTime = 1.5f;

    private Camera cam;
    private PlayerMovement playerMovement;
    private CanGrab canGrab;
    private FlarePool flarePool;
    private FlareObject heldFlare;
    private float cooldownTimer = 0f;
    private bool isHolding = false;
    private bool isCooldown = false;

    void Awake()
    {
        // Инициализация
        cam = Camera.main;
        playerMovement = GetComponent<PlayerMovement>();
        canGrab = GetComponent<CanGrab>();
        flarePool = FlarePool.Instance;

        if (handHoldPoint == null)
            handHoldPoint = transform;
        if (throwPoint == null)
            throwPoint = cam.transform;
    }

    void Update()
    {
        // Обновление кулдауна
        UpdateCooldown();
    }

    public void ThrowFlare(bool pressed)
    {
        if (!pressed) return;

        if (isHolding)
        {
            ThrowHeldFlare();
            return;
        }

        if (!isCooldown && !canGrab.IsHoldingObject())
        {
            SpawnHeldFlare();
        }
    }

    private void SpawnHeldFlare()
    {
        // Спавн в руке
        FlareObject flare = flarePool.GetFlare(handHoldPoint.position);
        if (flare == null) return;

        heldFlare = flare;
        flare.transform.SetParent(handHoldPoint);
        flare.transform.localPosition = Vector3.zero;
        flare.transform.localRotation = Quaternion.identity;

        Rigidbody rb = heldFlare.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        isHolding = true;
    }

    private void ThrowHeldFlare()
    {
        if (heldFlare == null) return;

        // Бросок
        heldFlare.transform.SetParent(null);

        Rigidbody rb = heldFlare.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = false;

        Vector3 targetPoint;
        if (Physics.Raycast(cam.transform.position, cam.transform.forward,
            out RaycastHit hit, maxThrowDistance, flareTargetMask))
        {
            targetPoint = hit.point + hit.normal * 0.1f;
        }
        else
        {
            targetPoint = cam.transform.position + cam.transform.forward * maxThrowDistance;
        }

        Vector3 spawnPos = throwPoint.position;
        Vector3 throwDirection = (targetPoint - spawnPos).normalized;
        heldFlare.Initialize(throwDirection, scatterAmount);

        heldFlare = null;
        isHolding = false;
        cooldownTimer = cooldownTime;
        isCooldown = true;
    }

    private void UpdateCooldown()
    {
        if (isCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                isCooldown = false;
            }
        }
    }

    public bool CanSpawnFlare => !isCooldown && !isHolding && !canGrab.IsHoldingObject();
    public bool IsHoldingFlare => isHolding;
    public float CooldownProgress => isCooldown ? (cooldownTime - cooldownTimer) / cooldownTime : 0f;

    public void ForceDropFlare()
    {
        if (isHolding)
        {
            ThrowHeldFlare();
        }
    }
}