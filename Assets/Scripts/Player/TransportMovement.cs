using UnityEngine;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TransportMovement : MonoBehaviour, IControllable
{
    [Header("Movement")]
    public float moveSpeed = 15f;
    public float turnSpeed = 90f;

    [Header("Ground Check")]
    public float groundCheckDistance = 0.2f;
    public LayerMask groundMask = -1;

    [Header("Dismount")]
    public Vector3 dismountOffset = new Vector3(2f, 0f, 0f);

    [Header("Mount Point")]
    public Transform seatTransform;
    public Vector3 fallbackMountOffset = new Vector3(0, 1f, 0);

    private Rigidbody rb;
    private InputRouter inputRouter;
    private Transform playerTransform;
    private PlayerMovement playerMovement;

    // Debug
    private Vector3 debugRayStart;
    private float debugRayLength;
    private bool debugIsGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.useGravity = true;
        rb.linearDamping = 2f;
        rb.angularDamping = 5f;

        inputRouter = FindObjectOfType<InputRouter>();
        playerMovement = FindObjectOfType<PlayerMovement>();
        playerTransform = playerMovement?.transform;

        if (playerTransform == null)
            Debug.LogError("TransportMovement: Player не найден на сцене!");
    }

    public void HandleMovement(Vector2 input)
    {
        HandleGroundCheck();
        if (!isGrounded) return;

        Vector3 move = transform.forward * input.y * moveSpeed;
        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);

        if (Mathf.Abs(input.x) > 0.1f)
            transform.Rotate(0, input.x * turnSpeed * Time.fixedDeltaTime, 0);
    }

    public void HandleRotation(Vector2 mouseDelta) { }
    public void HandlePhysicalInteract(bool pressed, bool held){}
    public void HandleFlare(bool pressed) { }

    public void HandleInteract(bool pressed)
    {
        if (!pressed || playerTransform == null) return;

        // Высадка игрока
        playerTransform.SetParent(null);
        playerTransform.position = transform.position + transform.TransformDirection(dismountOffset);
        playerTransform.rotation = transform.rotation;

        var playerRb = playerTransform.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.isKinematic = false;
            playerRb.useGravity = true;
        }

        var col = playerTransform.GetComponent<Collider>();
        if (col != null) col.isTrigger = false;

        
        inputRouter.SetController(playerMovement);

        Debug.Log("Высадка");
    }

    public Vector3 GetMountPosition()
    {
        return seatTransform ? seatTransform.localPosition : fallbackMountOffset;
    }

    private void HandleGroundCheck()
    {
        var col = GetComponent<Collider>();
        if (!col)
        {
            isGrounded = false;
            return;
        }

        var bounds = col.bounds;
        var origin = bounds.center + Vector3.up * (bounds.extents.y - 0.01f);
        float length = bounds.extents.y * 0.01f + groundCheckDistance;

        debugRayStart = origin;
        debugRayLength = length;
        debugIsGrounded = Physics.Raycast(origin, Vector3.down, length, groundMask, QueryTriggerInteraction.Ignore);
        isGrounded = debugIsGrounded;
    }

    private bool isGrounded;

   
}