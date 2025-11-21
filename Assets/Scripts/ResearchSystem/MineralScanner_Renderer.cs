using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class MineralScanner_Renderer : MonoBehaviour
{
    [Header("=== Настройки сканирующей точки ===")]
    public RectTransform scanningPoint;                  // Точка, которую двигаем
    public Vector2 moveDirectionLocal = Vector2.up;      // Направление В ЛОКАЛЬНОМ пространстве сканера
    public float speed = 200f;                           // единиц/сек в локальном пространстве

    [Header("=== Границы В ЛОКАЛЬНЫХ координатах сканера ===")]
    public Vector2 boundsMin = new Vector2(-250, -250);
    public Vector2 boundsMax = new Vector2(250, 250);

    [Header("=== Поведение на границе ===")]
    public BounceMode bounceMode = BounceMode.Bounce;
    public bool wrapAround = false;                      // Зациклить (как в Asteroids)

    public enum BounceMode { Bounce, Stop, Wrap }

    private RectTransform myRect;        // RectTransform самого сканера (this)
    private Vector3 lastValidPosition;

    private void Awake()
    {
        myRect = GetComponent<RectTransform>();

        if (scanningPoint == null)
        {
            Debug.LogError("MineralScanner_Renderer: назначь scanningPoint!");
            enabled = false;
        }
        myRect = GetComponent<RectTransform>();

        if (scanningPoint == null)
        {
            Debug.LogError("MineralScanner_Renderer: назначь scanningPoint!");
            enabled = false;
        }

        joystick = FindObjectOfType<JoystickController>(); // или перетащи в инспекторе
    }
    private JoystickController joystick;

  

    private void LateUpdate()
    {
        if (scanningPoint == null || joystick == null) return;
        // ← ВОТ ГЛАВНОЕ ИЗМЕНЕНИЕ:
        Vector2 input = joystick.CurrentDirection; // от -1 до 1
        moveDirectionLocal = input.normalized;
        speed = input.magnitude > 0.01f ? 45f : 0f; // скорость зависит от силы наклона!

        // 1. Берём текущее локальное положение точки относительно этого сканера
        Vector3 currentLocalPos = myRect.InverseTransformPoint(scanningPoint.position);
        Vector2 pos2D = new Vector2(currentLocalPos.x, currentLocalPos.y);

        // 2. Считаем новое положение в локальном пространстве
        Vector2 delta = moveDirectionLocal.normalized * speed * Time.deltaTime;
        Vector2 desiredPos = pos2D + delta;

        // 3. Ограничиваем + обрабатываем отскок/зацикливание
        Vector2 finalPos = desiredPos;
        bool hitBoundary = false;

        if (desiredPos.x < boundsMin.x)
        {
            hitBoundary = true;
            if (bounceMode == BounceMode.Bounce) moveDirectionLocal.x *= -1;
            else if (bounceMode == BounceMode.Wrap && wrapAround) finalPos.x = boundsMax.x + (desiredPos.x - boundsMin.x);
            else finalPos.x = Mathf.Max(desiredPos.x, boundsMin.x);
        }
        else if (desiredPos.x > boundsMax.x)
        {
            hitBoundary = true;
            if (bounceMode == BounceMode.Bounce) moveDirectionLocal.x *= -1;
            else if (bounceMode == BounceMode.Wrap && wrapAround) finalPos.x = boundsMin.x + (desiredPos.x - boundsMax.x);
            else finalPos.x = Mathf.Min(desiredPos.x, boundsMax.x);
        }

        if (desiredPos.y < boundsMin.y)
        {
            hitBoundary = true;
            if (bounceMode == BounceMode.Bounce) moveDirectionLocal.y *= -1;
            else if (bounceMode == BounceMode.Wrap && wrapAround) finalPos.y = boundsMax.y + (desiredPos.y - boundsMin.y);
            else finalPos.y = Mathf.Max(desiredPos.y, boundsMin.y);
        }
        else if (desiredPos.y > boundsMax.y)
        {
            hitBoundary = true;
            if (bounceMode == BounceMode.Bounce) moveDirectionLocal.y *= -1;
            else if (bounceMode == BounceMode.Wrap && wrapAround) finalPos.y = boundsMin.y + (desiredPos.y - boundsMax.y);
            else finalPos.y = Mathf.Min(desiredPos.y, boundsMax.y);
        }

        // 4. Применяем итоговую локальную позицию → автоматически учтёт поворот, масштаб и т.д.
        Vector3 newWorldPos = myRect.TransformPoint(finalPos.x, finalPos.y, currentLocalPos.z);
        scanningPoint.position = newWorldPos;

        // Сохраняем для следующего кадра
        lastValidPosition = scanningPoint.position;
    }

    // Визуализация границ в редакторе (даже если канвас повёрнут!)
    private void OnDrawGizmosSelected()
    {
        if (myRect == null) return;

        Vector3[] corners = new Vector3[4];
        corners[0] = myRect.TransformPoint(boundsMin.x, boundsMin.y, 0);
        corners[1] = myRect.TransformPoint(boundsMax.x, boundsMin.y, 0);
        corners[2] = myRect.TransformPoint(boundsMax.x, boundsMax.y, 0);
        corners[3] = myRect.TransformPoint(boundsMin.x, boundsMax.y, 0);

        Gizmos.color = new Color(0, 1, 1, 0.4f);
        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
        }
        Gizmos.DrawLine(corners[0], corners[2]);
        Gizmos.DrawLine(corners[1], corners[3]);
    }
}