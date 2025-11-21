using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class MineralScanner_InputJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Настройки джойстика")]
    [SerializeField] private RectTransform joystickBackground; // фон джойстика
    [SerializeField] private RectTransform joystickHandle;    // ручка, которая двигается
    [SerializeField] private float handleRange = 100f;        // радиус движения ручки в пикселях

    // Выходное значение от -1 до 1 по X и Y
    public Vector2 InputDirection { get; private set; } = Vector2.zero;

    private Vector2 startPos;
    private bool isDragging = false;
    private Canvas parentCanvas;

    private void Awake()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        if (joystickBackground == null) joystickBackground = GetComponent<RectTransform>();
        startPos = joystickHandle.anchoredPosition;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground, eventData.position, parentCanvas.worldCamera, out pos);

        pos = Vector2.ClampMagnitude(pos, handleRange);
        joystickHandle.anchoredPosition = pos;

        InputDirection = pos / handleRange; // нормализованное значение -1..1
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        joystickHandle.anchoredPosition = Vector2.zero;
        InputDirection = Vector2.zero;
    }

    // Для ПК: можно также управлять с клавиатуры (WASD / стрелки)
    private void Update()
    {
        if (!isDragging)
        {
            // Клавиатурный ввод (опционально)
            Vector2 keyboardInput = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            );

            if (keyboardInput.sqrMagnitude > 0.01f)
            {
                InputDirection = keyboardInput.normalized;
                joystickHandle.anchoredPosition = InputDirection * handleRange;
            }
            else if (keyboardInput == Vector2.zero && !isDragging)
            {
                InputDirection = Vector2.zero;
                joystickHandle.anchoredPosition = Vector2.zero;
            }
        }
    }
}