using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class CanGrab : MonoBehaviour
{
    [Header("Основные настройки")]
    [SerializeField] private Transform grabPoint;
    [SerializeField] private float maxGrabDistance = 3.5f;
    [SerializeField] private LayerMask grabbableLayerMask = -1;
    [SerializeField] private float pullSpeed = 20f;
    [SerializeField] private float throwForce = 10f;

    [Header("Отпускание при столкновении")]
    [SerializeField] private bool releaseOnCollision = true;
    [SerializeField] private float minReleaseImpulse = 2f; 

    private Camera cam;
    private Rigidbody currentRB;
    private GrabbableItem currentItem;

    private bool isPulling = false;
    private bool isSnappingToZone = false;

    private void Awake()
    {
        cam = Camera.main;
        if (grabPoint == null) grabPoint = transform;
    }

    private void OnEnable() => GrabbableItem.OnGrabbedCollision += HandleCollision;
    private void OnDisable() => GrabbableItem.OnGrabbedCollision -= HandleCollision;

    public bool IsHoldingObject() => currentRB != null;
    public GrabbableItem GetGrabbedItem() => currentItem;
    public void ForceRelease() => ReleaseObject();

    public void HandlePhysicalInteract(bool pressed, bool held)
    {
        if (isSnappingToZone) return;

        if (held && currentRB == null)
            TryGrabObject();
        else if (!held && currentRB != null)
            ReleaseObject(throwForce); // можно бросать
    }

    public void StartSnappingToZone() => isSnappingToZone = true;
    public void EndSnappingToZoneComplete() => isSnappingToZone = false;

    private void TryGrabObject()
    {
        if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, maxGrabDistance, grabbableLayerMask))
            return;

        Rigidbody rb = hit.rigidbody;
        if (rb == null) return;

        var item = hit.collider.GetComponent<GrabbableItem>();
        item?.GetComponentInParent<SnapZone>()?.OnItemGrabbedFromZone();

        currentRB = rb;
        currentItem = item;
        isPulling = true;

        
    }

   
    private void ReleaseObject(float throwMultiplier = 0f)
    {
        if (currentRB == null) return;

        if (throwMultiplier > 0f)
        {
            Vector3 throwDir = cam.transform.forward + Vector3.up * 0.2f;
            currentRB.linearVelocity = throwDir * throwForce * throwMultiplier;
        }

        currentRB = null;
        currentItem = null;
        isPulling = false;
        isSnappingToZone = false;
    }

    
    private void FixedUpdate()
    {
        if (currentRB == null || isSnappingToZone) return;

        Vector3 targetPos = grabPoint.position;
        Quaternion targetRot = grabPoint.rotation;

        if (isPulling)
        {
            Vector3 direction = (targetPos - currentRB.position).normalized;
            float distance = Vector3.Distance(currentRB.position, targetPos);

            if (distance < 0.1f)
                isPulling = false;

            currentRB.linearVelocity = direction * pullSpeed;
        }
        else
        {
           
            Vector3 velocity = (targetPos - currentRB.position) * 20f;
            currentRB.linearVelocity = velocity;

            
            currentRB.angularVelocity = QuaternionToAngularVelocity(targetRot * Quaternion.Inverse(currentRB.rotation)) * 30f;
        }
    }

    private Vector3 QuaternionToAngularVelocity(Quaternion delta)
    {
        delta.ToAngleAxis(out float angleDeg, out Vector3 axis);
        if (angleDeg > 180) angleDeg -= 360;
        return axis * (angleDeg * Mathf.Deg2Rad);
    }

    
    private void HandleCollision(GrabbableItem item, Collision collision)
    {
        if (item != currentItem || !releaseOnCollision || isPulling || isSnappingToZone) return;

        float impulse = collision.impulse.magnitude;
        if (impulse > minReleaseImpulse)
        {
            ReleaseObject(); 
        }
    }
}