using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour, IControllable
{
    [Header("Передвижение")]
    public float walkSpeed = 5f;

    [Header("Проверка поверхности")]
    public float groundCheckDistance = 0.2f;
    public LayerMask groundMask = -1;

    [Header("Садим игрока")]
    public float mountDistance = 2f;
    public LayerMask transportLayerMask = -1;
    public float mountSpeed = 10f;

    private Rigidbody rb;
    private InputRouter inputRouter;
    private bool isMounting = false;

    // Debug
    private Vector3 debugRayStart;
    private float debugRayLength;
    private bool debugIsGrounded;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.linearDamping = 2f;
        inputRouter = FindObjectOfType<InputRouter>();
    }

    public void HandleMovement(Vector2 input)
    {
        if (isMounting || !HandleGroundCheck()) return;

        Vector3 move = (transform.right * input.x + transform.forward * input.y) * walkSpeed;
        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);
    }

    public void HandleRotation(Vector2 mouseDelta) { }
    public void HandleInteract(bool pressed) { }
    public void HandleFlare(bool pressed) { }

    public void HandleUseTool(bool pressed, bool held)
    {
        if (!pressed || isMounting) return;

        var hits = Physics.OverlapSphere(transform.position, mountDistance, transportLayerMask);
        TransportMovement nearest = null;
        float bestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            var transport = hit.GetComponent<TransportMovement>();
            if (transport != null)
            {
                float d = Vector3.Distance(transform.position, transport.transform.position);
                if (d < bestDist) { bestDist = d; nearest = transport; }
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

    private bool HandleGroundCheck()
    {
        var col = GetComponent<Collider>();
        if (!col) return false;

        var bounds = col.bounds;
        var origin = bounds.center + Vector3.up * (bounds.extents.y - 0.01f);
        float length = bounds.extents.y * 0.02f + groundCheckDistance;

        debugRayStart = origin;
        debugRayLength = length;
        debugIsGrounded = Physics.Raycast(origin, Vector3.down, length, groundMask, QueryTriggerInteraction.Ignore);
        return debugIsGrounded;
    }
}