using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))] // чтобы можно было двигать CanGrab-ом
public class MineralScanner_Joystick : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private Transform joystickHandle;          // Ручка джойстика (визуал)
    [SerializeField] private float maxRadius = 0.3f;            // Радиус движения от центра
    [SerializeField] private float returnSpeed = 8f;            // Как быстро возвращается в центр

    [Header("Вывод на экран")]
    [SerializeField] private MineralScanner_UIController uiController; // Скрипт на канвасе

    public UnityEvent<Vector2> OnJoystickMoved;                 // Для внешних систем (анимации, звук)
    public UnityEvent OnJoystickReleased;                       // Когда отпустили

    private Vector3 startPos;
    private bool isHeld = false;
    private CanGrab playerGrabber;
    [Header("Настройка")]
    [SerializeField] private Transform handle;           // верхняя часть джойстика (сфера/ручка)
    [SerializeField] private float maxDistance = 0.12f;   // на каком расстоянии от центра ручка упирается в лимит (подбери под себя)

    // ←←←←←←←←←←←←←←← ВНЕШНИЙ ДОСТУП К ЗНАЧЕНИЯМ ←←←←←←←←←←←←←←←
    public Vector2 Value { get; private set; }   // от -1 до +1
    public float X => Value.x;
    public float Z => Value.y;
    // →→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→

    private Vector3 startLocalPos;

    private void Awake()
    {
        if (handle == null) handle = transform.GetChild(0); // или просто перетяни в инспекторе

        // Запоминаем начальную локальную позицию ручки
        startLocalPos = handle.localPosition;
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var grabbable = gameObject.AddComponent<GrabbableItem>();
    }

    private void LateUpdate()
    {
        // Текущее смещение ручки от начального положения (в локальных координатах основания)
        Vector3 offset = handle.localPosition - startLocalPos;

        // Убираем Y — нам он не нужен (ручка не должна двигаться по высоте)
        offset.y = 0f;

        // Нормализуем по максимальному разрешённому расстоянию
        float distance = offset.magnitude;

        if (distance > maxDistance)
            offset = offset.normalized * maxDistance;

        // Получаем -1..1 по осям
        Value = new Vector2(
            offset.x / maxDistance,
            offset.z / maxDistance
        );
    }

    // Дебаг в сцене
    private void OnDrawGizmosSelected()
    {
        if (handle != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + startLocalPos, maxDistance);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position + startLocalPos, handle.position);

           // Handles.Label(transform.position + Vector3.up * 0.2f, $"Joystick: {Value.x:F2}, {Value.y:F2}");
        }
    }
    

    private void Update()
    {
        if (!isHeld)
        {
            // Плавный возврат в центр
            joystickHandle.localPosition = Vector3.Lerp(joystickHandle.localPosition, startPos, returnSpeed * Time.deltaTime);
            SendInput(Vector2.zero);
            return;
        }

        // Ограничиваем радиус
        Vector3 offset = joystickHandle.localPosition - startPos;
        if (offset.magnitude > maxRadius)
        {
            offset = offset.normalized * maxRadius;
            joystickHandle.localPosition = startPos + offset;
        }

        // Преобразуем в -1..1
        Vector2 input = new Vector2(offset.x / maxRadius, offset.z / maxRadius);
        SendInput(input);
    }

    private void SendInput(Vector2 input)
    {
        uiController?.SetJoystickPosition(input);
        OnJoystickMoved?.Invoke(input);
    }

    // Вызывается CanGrab при захвате/отпускании
    public void OnGrabStart(CanGrab grabber)
    {
        isHeld = true;
        playerGrabber = grabber;
    }

    public void OnGrabEnd()
    {
        isHeld = false;
        playerGrabber = null;
        OnJoystickReleased?.Invoke();
    }
}