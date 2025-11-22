using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class CanGrab : MonoBehaviour
{
    [Header("Основные")]
    [SerializeField] private Transform grabPoint;
    [SerializeField] private float maxGrabDistance = 3.5f;

    [Header("Инструменты")]
    [SerializeField] public Transform toolGrabPoint;
    [SerializeField] private float toolMaxGrabDistance = 1.8f;

    [Header("Физика")]
    [SerializeField] private LayerMask grabbableMask = -1;
    [SerializeField] private float pullSpeed = 20f;
    [SerializeField] private bool releaseOnCollision = true;
    [SerializeField] private LayerMask collisionReleaseMask = -1;

    [Header("Гравитация")]
    [SerializeField] private bool mineralsHaveGravity = true;
    [SerializeField] private bool toolsHaveGravity = false;

    private Camera cam;
    private Rigidbody heldRb;
    private Transform heldTransform;
    private GrabbableItem heldItem;
    private Transform activePoint;
    private Vector3 lockedOffset;
    private Quaternion lockedRotation;
    private bool isPulling = false;
    private bool isSnapping = false;

    public static System.Action<CanGrab, Rigidbody> OnGrabbed;
    public static System.Action<CanGrab, Rigidbody> OnReleased;

    private void Awake()
    {
        cam = Camera.main;
        if (grabPoint == null) grabPoint = transform;
        if (toolGrabPoint == null) toolGrabPoint = grabPoint;
    }

    //private void OnEnable() => GrabbableItem.OnGrabbedCollision += OnHeldCollision;
    //private void OnDisable() => GrabbableItem.OnGrabbedCollision -= OnHeldCollision;

    public bool IsHoldingObject() => heldRb != null;
    public GrabbableItem GetGrabbedItem() => heldItem;

    public void StartSnappingToZone() => isSnapping = true;
    public void EndSnappingToZoneComplete() => isSnapping = false;

    public void HandlePhysicalInteract(bool pressed, bool held)
    {
        if (isSnapping) return;
        if (held && heldRb == null) TryGrab();
        else if (!held && heldRb != null) Release();
    }

    public void ForceRelease() => Release(true);

    private void TryGrab()
    {
        if (TryGrabAt(toolGrabPoint, toolMaxGrabDistance, out var item) && item.ItemType == GrabbableType.Tool)
        {
            Grab(item, toolGrabPoint);
            return;
        }

        if (TryGrabAt(grabPoint, maxGrabDistance, out item))
            Grab(item, grabPoint);
    }

    private bool TryGrabAt(Transform point, float dist, out GrabbableItem item)
    {
        item = null;
        if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, dist, grabbableMask))
            return false;

        item = hit.collider.GetComponent<GrabbableItem>();
        if (item == null) return false;

        if (point == toolGrabPoint && item.ItemType != GrabbableType.Tool) return false;
        if (point == grabPoint && item.ItemType == GrabbableType.Tool) return false;

        var rb = item.GetComponent<Rigidbody>() ?? hit.collider.attachedRigidbody;
        return rb != null;
    }

    private void Grab(GrabbableItem item, Transform point)
    {
        var rb = item.GetComponent<Rigidbody>() ?? item.GetComponentInParent<Rigidbody>();
        item.GetComponentInParent<SnapZone>()?.OnItemGrabbedFromZone();

        heldRb = rb;
        heldTransform = rb.transform;
        heldItem = item;
        activePoint = point;

        heldRb.useGravity = false;
        heldRb.linearVelocity = Vector3.zero;
        heldRb.angularVelocity = Vector3.zero;

        OnGrabbed?.Invoke(this, heldRb);
        isPulling = true;

        if (item.ItemType == GrabbableType.Door)
            item.GetComponent<Door>()?.OnGrabbed();
    }

    private void Release(bool force = false)
    {
        if (heldRb == null) return;

        OnReleased?.Invoke(this, heldRb);

        if (heldItem.ItemType == GrabbableType.Door)
            heldItem.GetComponent<Door>()?.OnReleased();

        if (!force && !isSnapping)
        {
            bool hasGravity = heldItem.ItemType switch
            {
                GrabbableType.Tool => toolsHaveGravity,
                _ => mineralsHaveGravity
            };

            heldRb.useGravity = hasGravity;

            if (isPulling)
                heldRb.linearVelocity *= 0.3f;
        }

        heldRb = null;
        heldTransform = null;
        heldItem = null;
        activePoint = null;
        isPulling = false;
    }

    private void FixedUpdate()
    {
        if (!isPulling || heldRb == null || isSnapping || activePoint == null) return;

        Vector3 dir = activePoint.position - heldTransform.position;
        if (dir.magnitude < 0.15f)
        {
            isPulling = false;
            lockedOffset = activePoint.InverseTransformPoint(heldTransform.position);
            lockedRotation = Quaternion.Inverse(activePoint.rotation) * heldTransform.rotation;
        }
        else
        {
            heldRb.linearVelocity = dir.normalized * pullSpeed;
        }
    }

    private void LateUpdate()
    {
        if (heldRb == null || isPulling || isSnapping || activePoint == null) return;
        if (heldRb.GetComponent<ConfigurableJoint>()) return;
        if (heldItem.ItemType == GrabbableType.Door) return;

        heldTransform.position = activePoint.TransformPoint(lockedOffset);
        heldTransform.rotation = activePoint.rotation * lockedRotation;
    }

    private void OnHeldCollision(GrabbableItem item, Collision col)
    {
        if (item != heldItem || !releaseOnCollision || isPulling || isSnapping) return;
        if (((1 << col.collider.gameObject.layer) & collisionReleaseMask) == 0) return;
        Release();
    }
}