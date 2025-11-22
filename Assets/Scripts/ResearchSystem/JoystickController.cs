using UnityEngine;

public class JoystickController : MonoBehaviour
{
    public static JoystickController Instance { get; private set; }

    [Header("Визуальный рычаг")]
    [SerializeField] private Transform handle;
    [SerializeField] private float maxAngle = 45f;

    [Header("Плавность")]
    [SerializeField, Range(1f, 30f)] private float moveSmooth = 18f;      
    [SerializeField, Range(1f, 30f)] private float returnSmooth = 12f;    
    [Header("Инверсия (настраивай в инспекторе)")]
    [SerializeField] private bool invertX = true;
    [SerializeField] private bool invertY = true;

    public Vector2 CurrentDirection { get; private set; } = Vector2.zero;
    public bool IsGrabbed { get; private set; } = false;

    private Quaternion initialRotation;
    private Vector2 targetTilt;     
    private Vector2 currentTilt;     

    private void Awake()
    {
        Instance = this;
        if (handle == null) handle = transform.GetChild(0);
        initialRotation = handle.localRotation;
    }

    private void OnEnable()
    {
        CanGrab.OnGrabbed += OnGrabbed;
        CanGrab.OnReleased += OnReleased;
    }

    private void OnDisable()
    {
        CanGrab.OnGrabbed -= OnGrabbed;
        CanGrab.OnReleased -= OnReleased;
    }

    private void OnGrabbed(CanGrab grabber, Rigidbody rb)
    {
        if (rb != null && (rb.transform.IsChildOf(transform) || rb.transform == transform))
            IsGrabbed = true;
    }

    private void OnReleased(CanGrab grabber, Rigidbody rb)
    {
        if (rb != null && (rb.transform.IsChildOf(transform) || rb.transform == transform))
            IsGrabbed = false;
    }

    private void Update()
    {
        // 1. Определяем целевой наклон
        if (IsGrabbed)
        {
            Vector2 input = InputManager.Instance.Look;
            targetTilt += input * 180f * Time.deltaTime; 
            targetTilt.x = Mathf.Clamp(targetTilt.x, -maxAngle, maxAngle);
            targetTilt.y = Mathf.Clamp(targetTilt.y, -maxAngle, maxAngle);
        }
        else
        {
            targetTilt = Vector2.zero;
        }

        
        float smooth = IsGrabbed ? moveSmooth : returnSmooth;
        currentTilt = Vector2.Lerp(currentTilt, targetTilt, smooth * Time.deltaTime);

        
        float rotX = currentTilt.x * (invertX ? -1f : 1f);
        float rotY = currentTilt.y * (invertY ? -1f : 1f);

        handle.localRotation = initialRotation * Quaternion.Euler(rotY, 0f, rotX);

        
        CurrentDirection = currentTilt / maxAngle;
    }
}