using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class CanGrab : MonoBehaviour
{
    [SerializeField] private Transform grabPoint;
    [SerializeField] private float maxGrabDistance = 3.5f;
    [SerializeField] private LayerMask grabbableLayerMask = -1; //пока сделал через слой, потом поменяем на проверку компонента на обьекте
    [SerializeField] private float pullSpeed = 20f;

    private Camera cam;
    private Rigidbody currentRB;
    private Collider currentCol;
    private Transform currentTransform;
    private bool isPulling;
    private Vector3 lockedOffset;
    private Quaternion lockedRotation;
    private bool savedIsKinematic;
    private int savedLayer;

    private void Awake()
    {
        cam = Camera.main;
    }

    public void HandlePhysicalInteract(bool pressed, bool held)
    {
        if (held)
        {
            if (currentRB == null)
                TryGrabObject();
        }
        else
        {
            if (currentRB != null)
                ReleaseObject();
        }
    }

    private void TryGrabObject()
    {
        if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, maxGrabDistance, grabbableLayerMask))
            return;

        var rb = hit.rigidbody;
        if (rb == null || rb.isKinematic)
            return;

        currentRB = rb;
        currentCol = hit.collider;
        currentTransform = rb.transform;

        savedIsKinematic = rb.isKinematic;
        savedLayer = rb.gameObject.layer;

        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        Physics.IgnoreCollision(GetComponent<Collider>(), currentCol, true);

        isPulling = true;
    }

    private void ReleaseObject()
    {
        if (currentRB == null) return;

        currentRB.isKinematic = savedIsKinematic;
        currentRB.gameObject.layer = savedLayer;
        Physics.IgnoreCollision(GetComponent<Collider>(), currentCol, false);

        currentRB = null;
        currentCol = null;
        currentTransform = null;
        isPulling = false;
    }

    private void FixedUpdate()
    {
        if (currentRB == null) return;

        if (isPulling)
        {
            Vector3 targetPos = grabPoint.position;
            float dist = Vector3.Distance(currentTransform.position, targetPos);

            if (dist < 0.03f)
            {
                isPulling = false;
                lockedOffset = grabPoint.InverseTransformPoint(currentTransform.position);
                lockedRotation = Quaternion.Inverse(grabPoint.rotation) * currentTransform.rotation;
            }
            else
            {
                currentTransform.position = Vector3.MoveTowards(
                    currentTransform.position, targetPos, pullSpeed * Time.fixedDeltaTime);
            }
        }
        else
        {
            currentTransform.position = grabPoint.TransformPoint(lockedOffset);
            currentTransform.rotation = grabPoint.rotation * lockedRotation;
        }
    }
}