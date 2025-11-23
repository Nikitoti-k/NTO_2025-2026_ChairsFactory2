using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class CanGrab : MonoBehaviour
{
    [SerializeField] private Transform grabPoint;
    [SerializeField] private float maxGrabDistance = 3.5f;
    [SerializeField] public Transform toolGrabPoint;
    [SerializeField] private float toolMaxGrabDistance = 1.8f;
    [SerializeField] private LayerMask grabbableMask = -1;
    [SerializeField] private float pullSpeed = 20f;
    [SerializeField] private bool releaseOnCollision = true;
    [SerializeField] private LayerMask collisionReleaseMask = -1;
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
    private bool wasTriggerBeforeGrab = false;

    public static System.Action<CanGrab, Rigidbody> OnGrabbed;
    public static System.Action<CanGrab, Rigidbody> OnReleased;

    private void Awake()
    {
        cam = Camera.main;
        if (grabPoint == null) grabPoint = transform;
        if (toolGrabPoint == null) toolGrabPoint = grabPoint;
    }

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

        var col = item.GetComponent<Collider>();
        if (col)
        {
            wasTriggerBeforeGrab = col.isTrigger;
            col.isTrigger = true;
        }

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

        var col = heldItem.GetComponent<Collider>();

        if (!force && !isSnapping)
        {
            
            if (col)
                col.isTrigger = wasTriggerBeforeGrab;

            bool hasGravity = heldItem.ItemType == GrabbableType.Tool ? toolsHaveGravity : mineralsHaveGravity;
            heldRb.useGravity = hasGravity;
            heldRb.linearVelocity *= 0.3f;
            heldRb.angularVelocity *= 0.3f;
        }
        else if (force && isSnapping)
        {
           
        }

        heldRb = null;
        heldTransform = null;
        heldItem = null;
        activePoint = null;
        isPulling = false;
        wasTriggerBeforeGrab = false;
    }

    private void FixedUpdate()
    {
        if (heldRb == null || isSnapping || activePoint == null) return;

        Vector3 targetPos = activePoint.position;
        Vector3 currentPos = heldTransform.position;
        Vector3 direction = targetPos - currentPos;

        if (isPulling)
        {
            if (direction.sqrMagnitude < 0.15f * 0.15f)
            {
                heldRb.linearVelocity = Vector3.zero;
                isPulling = false;
                lockedOffset = activePoint.InverseTransformPoint(currentPos);
                lockedRotation = Quaternion.Inverse(activePoint.rotation) * heldTransform.rotation;
            }
            else
            {
                heldRb.linearVelocity = direction.normalized * pullSpeed;
            }
        }
        else
        {
            heldRb.linearVelocity = Vector3.zero;
        }
    }

    private void LateUpdate()
    {
        if (heldRb == null || isPulling || isSnapping || activePoint == null) return;
        if (heldItem.ItemType == GrabbableType.Door) return;

        heldTransform.position = activePoint.TransformPoint(lockedOffset);
        heldTransform.rotation = activePoint.rotation * lockedRotation;
    }
}