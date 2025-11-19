using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class CanGrab : MonoBehaviour
{
    [Header("Основные настройки")]
    [SerializeField] private Transform grabPoint;
    [SerializeField] private float maxGrabDistance = 3.5f;
    [SerializeField] private LayerMask grabbableLayerMask = -1;
    [SerializeField] private float pullSpeed = 20f;

    [Header("Отпускание при столкновении")]
    [SerializeField] private bool releaseOnCollision = true;
    [SerializeField] private LayerMask collisionReleaseMask = -1;

    private Camera cam;
    private Rigidbody currentRB;
    private Transform currentTransform;
    private GrabbableItem currentItem;
    private Vector3 lockedOffset;
    private Quaternion lockedRotation;

    private bool isPulling = false;
    private bool isSnappingToZone = false;

    private void Awake()
    {
        cam = Camera.main;
        if (grabPoint == null) grabPoint = transform;
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
    public void ForceRelease() => ReleaseObject(true);

    
    public void HandlePhysicalInteract(bool pressed, bool held)
    {
        if (isSnappingToZone) return;

        if (held && currentRB == null)
            TryGrabObject();
        else if (!held && currentRB != null)
            ReleaseObject(false);
    }

    public void StartSnappingToZone() => isSnappingToZone = true;
    public void EndSnappingToZoneComplete() => isSnappingToZone = false;

   
    private void TryGrabObject()
    {
        if (isSnappingToZone) return;

        if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, maxGrabDistance, grabbableLayerMask))
            return;

        Rigidbody rb = hit.rigidbody;
        if (rb == null || rb.isKinematic) return;

        GrabbableItem item = hit.collider.GetComponent<GrabbableItem>();
        item?.GetComponentInParent<SnapZone>()?.OnItemGrabbedFromZone();

        // Захват
        currentRB = rb;
        currentTransform = rb.transform;
        currentItem = item;

        rb.isKinematic = true;
        isPulling = true;
    }

    private void ReleaseObject(bool force = false)
    {
        if (currentRB == null) return;

        currentRB.isKinematic = false;

        currentRB = null;
        currentTransform = null;
        currentItem = null;
        isPulling = false;
        isSnappingToZone = false;
    }

    private void FixedUpdate()
    {
        if (!isPulling || currentTransform == null) return;

        Vector3 target = grabPoint.position;
        float dist = Vector3.Distance(currentTransform.position, target);

        if (dist < 0.05f)
        {
            isPulling = false;
            lockedOffset = grabPoint.InverseTransformPoint(currentTransform.position);
            lockedRotation = Quaternion.Inverse(grabPoint.rotation) * currentTransform.rotation;
        }
        else
        {
            Vector3 newPos = Vector3.MoveTowards(currentTransform.position, target, pullSpeed * Time.fixedDeltaTime);
            currentTransform.position = newPos;
        }
    }

    private void LateUpdate()
    {
        if (currentRB == null || isPulling || isSnappingToZone) return;

        currentTransform.position = grabPoint.TransformPoint(lockedOffset);
        currentTransform.rotation = grabPoint.rotation * lockedRotation;
    }

    private void HandleGrabbedObjectCollision(GrabbableItem item, Collision collision)
    {
        if (item != currentItem || !releaseOnCollision || isPulling || isSnappingToZone) return;
        if (((1 << collision.collider.gameObject.layer) & collisionReleaseMask) == 0) return;

        ReleaseObject();
    }
}