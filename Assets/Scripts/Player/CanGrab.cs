using UnityEngine;
[RequireComponent(typeof(PlayerMovement))]
public class CanGrab : MonoBehaviour
{
    [Header("Основные настройки")]
    [SerializeField] private Transform grabPoint;                    // обычный grab point (для минералов и т.д.)
    [SerializeField] private float maxGrabDistance = 3.5f;           // дальность для обычных предметов

    [Header("Настройки для инструментов")]
    [SerializeField] public Transform toolGrabPoint;                // отдельная точка захвата инструмента (обычно в руке)
    [SerializeField] private float toolMaxGrabDistance = 1.8f;       // инструменты берём ближе и точнее

    [SerializeField] private LayerMask grabbableLayerMask = -1;
    [SerializeField] private float pullSpeed = 20f;

    [Header("Отпускание при столкновении")]
    [SerializeField] private bool releaseOnCollision = true;
    [SerializeField] private LayerMask collisionReleaseMask = -1;

    [Header("Физика при отпускании")]
    [SerializeField] private bool mineralsHaveGravity = true;
    [SerializeField] private bool toolsHaveGravity = false;

    private Camera cam;
    private Rigidbody currentRB;
    private Transform currentTransform;
    private GrabbableItem currentItem;
    private Vector3 lockedOffset;
    private Quaternion lockedRotation;
    private bool isPulling = false;
    private bool isSnappingToZone = false;

    // Текущие активные точки — будут переопределяться при захвате
    private Transform activeGrabPoint;
    private float activeMaxGrabDistance;

    public delegate void GrabEvent(CanGrab grabber, Rigidbody rb);
    public static event GrabEvent OnGrabbed;
    public static event GrabEvent OnReleased;

    private void Awake()
    {
        cam = Camera.main;
        if (grabPoint == null) grabPoint = transform;
        if (toolGrabPoint == null) toolGrabPoint = grabPoint; // fallback на обычный
    }

    private void OnEnable()
    {
        GrabbableItem.OnGrabbedCollision += HandleGrabbedObjectCollision;
    }

    private void OnDisable()
    {
        GrabbableItem.OnGrabbedCollision -= HandleGrabbedObjectCollision;
    }

    public bool IsHoldingObject() => currentRB != null;
    public GrabbableItem GetGrabbedItem() => currentItem;

    public void StartSnappingToZone() => isSnappingToZone = true;
    public void EndSnappingToZoneComplete() => isSnappingToZone = false;

    public void HandlePhysicalInteract(bool pressed, bool held)
    {
        if (isSnappingToZone) return;

        if (held && currentRB == null)
            TryGrabObject();
        else if (!held && currentRB != null)
            ReleaseObject();
    }

    public void ForceRelease() => ReleaseObject(true);

    private void TryGrabObject()
    {
        if (isSnappingToZone) return;

        // Сначала пробуем инструмент — у него приоритет (меньше дистанция, но проверяем первым)
        if (TryGrabWithSettings(toolGrabPoint, toolMaxGrabDistance, out GrabbableItem toolItem))
        {
            GrabItem(toolItem, toolGrabPoint);
            return;
        }

        // Если инструмент не найден в радиусе — пробуем обычные предметы
        if (TryGrabWithSettings(grabPoint, maxGrabDistance, out GrabbableItem regularItem))
        {
            GrabItem(regularItem, grabPoint);
        }
    }

    private bool TryGrabWithSettings(Transform point, float distance, out GrabbableItem foundItem)
    {
        foundItem = null;

        if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, distance, grabbableLayerMask))
            return false;

        GrabbableItem item = hit.collider.GetComponent<GrabbableItem>();
        if (item == null) return false;

        // Для toolGrabPoint разрешаем только инструменты
        if (point == toolGrabPoint && item.ItemType != GrabbableType.Tool)
            return false;

        // Для обычного grabPoint — всё кроме инструментов (или можно разрешить, если хочешь)
        // Если хочешь, чтобы обычный grabPoint тоже мог хватать инструменты с дальней дистанции — убери эту строку:
        if (point == grabPoint && item.ItemType == GrabbableType.Tool)
            return false;

        Rigidbody rb = item.GetComponent<Rigidbody>() ?? hit.collider.attachedRigidbody;
        if (rb == null) return false;

        foundItem = item;
        return true;
    }

    private void GrabItem(GrabbableItem item, Transform usedGrabPoint)
    {
        Rigidbody rb = item.GetComponent<Rigidbody>() ?? item.GetComponentInParent<Rigidbody>();

        item.GetComponentInParent<SnapZone>()?.OnItemGrabbedFromZone();

        currentRB = rb;
        currentTransform = rb.transform;
        currentItem = item;

        // Сохраняем, какой grab point используется сейчас
        activeGrabPoint = usedGrabPoint;
        activeMaxGrabDistance = usedGrabPoint == toolGrabPoint ? toolMaxGrabDistance : maxGrabDistance;

        currentRB.useGravity = false;
        OnGrabbed?.Invoke(this, currentRB);
        isPulling = true;
    }

    private void ReleaseObject(bool force = false)
    {
        if (currentRB == null) return;

        OnReleased?.Invoke(this, currentRB);

        if (!force && !isSnappingToZone)
        {
            bool shouldHaveGravity = currentItem.ItemType switch
            {
                GrabbableType.Mineral => mineralsHaveGravity,
                GrabbableType.Tool => toolsHaveGravity,
                GrabbableType.Resource => mineralsHaveGravity,
                GrabbableType.Junk => mineralsHaveGravity,
                _ => true
            };

            currentRB.useGravity = shouldHaveGravity;

            if (shouldHaveGravity)
            {
                Vector3 releaseVelocity = (activeGrabPoint.position - currentTransform.position) / Time.fixedDeltaTime * 0.5f;
                currentRB.linearVelocity = releaseVelocity;
            }
            else
            {
                currentRB.linearVelocity = Vector3.zero;
            }
        }

        currentRB = null;
        currentTransform = null;
        currentItem = null;
        activeGrabPoint = null;
        isPulling = false;
    }

    private void FixedUpdate()
    {
        if (!isPulling || currentRB == null || isSnappingToZone || activeGrabPoint == null) return;

        Vector3 targetPos = activeGrabPoint.position;
        Vector3 direction = targetPos - currentTransform.position;
        float distance = direction.magnitude;

        if (distance < 0.15f)
        {
            isPulling = false;
            lockedOffset = activeGrabPoint.InverseTransformPoint(currentTransform.position);
            lockedRotation = Quaternion.Inverse(activeGrabPoint.rotation) * currentTransform.rotation;
        }
        else
        {
            currentRB.linearVelocity = direction.normalized * pullSpeed;
        }
    }

    private void LateUpdate()
    {
        if (currentRB == null || isPulling || isSnappingToZone || activeGrabPoint == null) return;
        if (currentRB.GetComponent<ConfigurableJoint>() != null) return;

        currentTransform.position = activeGrabPoint.TransformPoint(lockedOffset);
        currentTransform.rotation = activeGrabPoint.rotation * lockedRotation;
    }

    private void HandleGrabbedObjectCollision(GrabbableItem item, Collision collision)
    {
        if (item != currentItem || !releaseOnCollision || isPulling || isSnappingToZone) return;
        if (((1 << collision.collider.gameObject.layer) & collisionReleaseMask) == 0) return;

        ReleaseObject();
    }
}