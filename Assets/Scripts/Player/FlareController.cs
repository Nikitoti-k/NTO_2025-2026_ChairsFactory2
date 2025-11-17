using UnityEngine;
[RequireComponent(typeof(PlayerMovement))]
public class FlareController : MonoBehaviour
{
    [Header("Настройки для бросания факелов")]
    [SerializeField] private Transform handHoldPoint;
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float maxThrowDistance = 50f;
    [SerializeField] private LayerMask flareTargetMask = -1;
    [SerializeField] private float scatterAmount = 1.5f;
    [SerializeField] private float cooldownTime = 1.5f;
    
    private Camera cam;
    private PlayerMovement playerMovement;
    private FlarePool flarePool;
    private FlareObject heldFlare;
    private float cooldownTimer = 0f;
    private bool isHolding = false;
    private bool isCooldown = false;
    void Awake()
    {
        cam = Camera.main;
        playerMovement = GetComponent<PlayerMovement>();
        flarePool = FlarePool.Instance;
        if (handHoldPoint == null)
            handHoldPoint = transform;
        if (throwPoint == null)
            throwPoint = cam.transform;
    }
    void Update()
    {
        UpdateCooldown();
    }
    public void ThrowFlare(bool pressed)
    {
        if (pressed && !isCooldown && !isHolding)
        {
            SpawnHeldFlare();
        }
        else if (pressed && isHolding)
        {
            ThrowHeldFlare();
        }
    }
    private void SpawnHeldFlare()
    {
        FlareObject flare = flarePool.GetFlare(handHoldPoint.position);
        if (flare == null) return;
        heldFlare = flare;
        flare.transform.SetParent(handHoldPoint);
        flare.transform.localPosition = Vector3.zero;
        flare.transform.localRotation = Quaternion.identity;
        heldFlare.GetComponent<Rigidbody>().isKinematic = true;
        isHolding = true;
    }
    private void ThrowHeldFlare()
    {
        heldFlare.transform.SetParent(null);
        heldFlare.GetComponent<Rigidbody>().isKinematic = false;


        if (Physics.Raycast(cam.transform.position, cam.transform.forward,
        out RaycastHit hit, maxThrowDistance, flareTargetMask))
        {
            Vector3 targetPoint = hit.point + hit.normal * 0.1f;
            ThrowToPoint(targetPoint);
        }
        else
        {
            Vector3 farPoint = cam.transform.position + cam.transform.forward * maxThrowDistance;
            ThrowToPoint(farPoint);
        }
        heldFlare = null;
        isHolding = false;
        cooldownTimer = cooldownTime;
        isCooldown = true;
    }
    private void ThrowToPoint(Vector3 worldTarget)
    {
        Vector3 spawnPos = throwPoint.position;
        Vector3 throwDirection = (worldTarget - spawnPos).normalized;
        heldFlare.Initialize(throwDirection, scatterAmount);
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
    public bool CanSpawnFlare => !isCooldown && !isHolding;
    public bool IsHoldingFlare => isHolding;
    public float CooldownProgress => isCooldown ? (cooldownTime - cooldownTimer) / cooldownTime : 0f;
}