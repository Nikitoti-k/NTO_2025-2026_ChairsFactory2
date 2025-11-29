// CanGrab.cs — РАБОЧАЯ ВЕРСИЯ
using UnityEngine;
[RequireComponent(typeof(PlayerMovement))]
public class CanGrab : MonoBehaviour
{
    [SerializeField] private Transform grabPoint;
    [SerializeField] private float maxGrabDistance = 3.5f;
    [SerializeField] public Transform toolGrabPoint;
    [SerializeField] private float toolMaxGrabDistance = 1.8f;
    [SerializeField] private LayerMask grabbableMask = -1;
    [SerializeField] private LayerMask collisionReleaseMask = -1;

    // VotV-физика минералов
    [Header("Mineral Physics")]
    [SerializeField] private float mineralHoldDistance = 2.5f;
    [SerializeField] private float mineralPullForce = 38f;
    [SerializeField] private float mineralDrag = 10f;
    [SerializeField] private float mineralAngularDrag = 15f;
    [SerializeField] private float mineralMaxVelocity = 12f;
    [SerializeField] private float mineralBreakDistance = 6f;

    [SerializeField] private float pullSpeed = 20f;
    [SerializeField] private bool toolsHaveGravity = false;
    [SerializeField] private bool mineralsHaveGravity = true;

    [SerializeField]private Camera cam;
    private Rigidbody heldRb;
    private Transform heldTransform;
    private GrabbableItem heldItem;
    private Transform activePoint;
    private Vector3 lockedOffset;
    private Quaternion lockedRotation;
    private bool isPulling = false;
    private bool wasTriggerBeforeGrab = false;
    private bool wasKinematicBeforeGrab = false;

    // VotV
    private ConfigurableJoint mineralJoint;
    private float originalDrag, originalAngularDrag;
    private bool isSnapping = false;

    public static System.Action<CanGrab, Rigidbody> OnGrabbed;
    public static System.Action<CanGrab, Rigidbody> OnReleased;

    private void Start()
    {
       // cam = Camera.main;
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
            GrabOldStyle(item, toolGrabPoint);
            return;
        }
        if (TryGrabAt(grabPoint, maxGrabDistance, out item))
        {
            if (item.ItemType == GrabbableType.Mineral)
                GrabMineralVotV(item);
            else
                GrabOldStyle(item, grabPoint);
        }
    }

    private bool TryGrabAt(Transform point, float dist, out GrabbableItem item)
    {
        item = null;
        if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, dist, grabbableMask)) return false;
        item = hit.collider.GetComponent<GrabbableItem>();
        if (item == null) return false;
        if (point == toolGrabPoint && item.ItemType != GrabbableType.Tool) return false;
        if (point == grabPoint && item.ItemType == GrabbableType.Tool) return false;
        var rb = item.GetComponent<Rigidbody>() ?? hit.collider.attachedRigidbody;
        return rb != null;
    }

    private void GrabMineralVotV(GrabbableItem item)
    {
        var rb = item.GetComponent<Rigidbody>();
        var snapZone = item.GetComponentInParent<SnapZone>();
        snapZone?.OnItemGrabbedFromZone(item);

        heldRb = rb;
        heldTransform = rb.transform;
        heldItem = item;
        activePoint = grabPoint;

        originalDrag = rb.linearDamping;
        originalAngularDrag = rb.angularDamping;
        rb.linearDamping = mineralDrag;
        rb.angularDamping = mineralAngularDrag;

        mineralJoint = rb.gameObject.AddComponent<ConfigurableJoint>();
        mineralJoint.connectedBody = null;
        mineralJoint.autoConfigureConnectedAnchor = false;
        mineralJoint.connectedAnchor = cam.transform.position + cam.transform.forward * mineralHoldDistance;
        mineralJoint.xMotion = mineralJoint.yMotion = mineralJoint.zMotion = ConfigurableJointMotion.Limited;
        mineralJoint.angularXMotion = mineralJoint.angularYMotion = mineralJoint.angularZMotion = ConfigurableJointMotion.Locked;

        var drive = new JointDrive { positionSpring = mineralPullForce * 100f, positionDamper = mineralPullForce * 10f, maximumForce = 1e8f };
        mineralJoint.xDrive = mineralJoint.yDrive = mineralJoint.zDrive = drive;

        rb.useGravity = mineralsHaveGravity;
        rb.isKinematic = false;

        OnGrabbed?.Invoke(this, heldRb);
    }

    private void GrabOldStyle(GrabbableItem item, Transform point)
    {
        var rb = item.GetComponent<Rigidbody>() ?? item.GetComponentInParent<Rigidbody>();
        var snapZone = item.GetComponentInParent<SnapZone>();
        snapZone?.OnItemGrabbedFromZone(item);

        heldRb = rb;
        heldTransform = rb.transform;
        heldItem = item;
        activePoint = point;

        wasKinematicBeforeGrab = rb.isKinematic;
        var col = item.GetComponent<Collider>();
        if (col) { wasTriggerBeforeGrab = col.isTrigger; col.isTrigger = true; }

        rb.isKinematic = false;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        OnGrabbed?.Invoke(this, heldRb);
        isPulling = true;
    }

    private void Release(bool force = false)
    {
        if (heldRb == null) return;
        OnReleased?.Invoke(this, heldRb);

        if (heldItem.ItemType == GrabbableType.Mineral && mineralJoint != null)
        {
            heldRb.linearDamping = originalDrag;
            heldRb.angularDamping = originalAngularDrag;
            Destroy(mineralJoint);
            mineralJoint = null;
        }
        else if (!force && !isSnapping)
        {
            var col = heldItem.GetComponent<Collider>();
            if (col) col.isTrigger = wasTriggerBeforeGrab;

            bool hasGravity = heldItem.ItemType == GrabbableType.Tool ? toolsHaveGravity : mineralsHaveGravity;
            heldRb.useGravity = hasGravity;
            heldRb.isKinematic = wasKinematicBeforeGrab;
            heldRb.linearVelocity *= 0.3f;
            heldRb.angularVelocity *= 0.3f;
        }

        heldRb = null;
        heldTransform = null;
        heldItem = null;
        activePoint = null;
        isPulling = false;
    }

    private void FixedUpdate()
    {
        if (heldRb == null || isSnapping || activePoint == null) return;

        if (heldItem.ItemType == GrabbableType.Mineral && mineralJoint != null)
        {
            mineralJoint.connectedAnchor = cam.transform.position + cam.transform.forward * mineralHoldDistance;
            if (Vector3.Distance(heldTransform.position, cam.transform.position) > mineralBreakDistance)
                Release();
            if (heldRb.linearVelocity.sqrMagnitude > mineralMaxVelocity * mineralMaxVelocity)
                heldRb.linearVelocity = heldRb.linearVelocity.normalized * mineralMaxVelocity;
        }
        else if (isPulling)
        {
            Vector3 dir = activePoint.position - heldTransform.position;
            if (dir.sqrMagnitude < 0.02f)
            {
                isPulling = false;
                lockedOffset = activePoint.InverseTransformPoint(heldTransform.position);
                lockedRotation = Quaternion.Inverse(activePoint.rotation) * heldTransform.rotation;
                heldRb.linearVelocity = Vector3.zero;
            }
            else
            {
                heldRb.linearVelocity = dir.normalized * pullSpeed;
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
        if (heldItem.ItemType == GrabbableType.Mineral) return;

        heldTransform.position = activePoint.TransformPoint(lockedOffset);
        heldTransform.rotation = activePoint.rotation * lockedRotation;
    }
}