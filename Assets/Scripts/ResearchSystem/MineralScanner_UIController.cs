using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class MineralScanner_UIController : MonoBehaviour
{
    [Header("UI элементы")]
    [SerializeField] private RectTransform joystickCircle;      // Круг, который двигается
    [SerializeField] private float circleRadius = 120f;         // Пиксели от центра

    [Header("Точки данных (визуальные маркеры)")]
    [SerializeField] private Image agePoint;
    [SerializeField] private Image crystalPoint;
    [SerializeField] private Image radioactivityPoint;

    [Header("Эффекты")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color activeColor = Color.cyan;
    [SerializeField] private float pulseSpeed = 3f;

    private Canvas canvas;
    private Vector2 currentInput;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        canvas.worldCamera = GetComponentInParent<Camera>(); // MineralViewCamera
    }

    public void SetJoystickPosition(Vector2 input)
    {
        currentInput = input;
        joystickCircle.anchoredPosition = input * circleRadius;

        // Проверяем близость к точкам
        CheckPointProximity(agePoint, 0);
        CheckPointProximity(crystalPoint, 1);
        CheckPointProximity(radioactivityPoint, 2);
    }

    private void CheckPointProximity(Image pointImage, int pointIndex)
    {
        if (pointImage == null) return;

        Vector2 pointPos = pointImage.rectTransform.anchoredPosition;
        float dist = Vector2.Distance(joystickCircle.anchoredPosition, pointPos);

        bool isClose = dist < 40f; // пиксели

        pointImage.color = Color.Lerp(pointImage.color, isClose ? activeColor : normalColor, Time.deltaTime * 10f);

        if (isClose)
        {
            // Пульсация
            float pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * pulseSpeed);
            pointImage.transform.localScale = Vector3.one * pulse;
        }
        else
        {
            pointImage.transform.localScale = Vector3.one;
        }

        // Здесь можно вызвать событие "точка активирована"
       /* if (isClose && Time.frameCount % 10 == 0) // чтобы не спамить
            MineralScannerManager.Instance?.OnScanPointActivated(pointIndex);*/
    }

#if UNITY_EDITOR
    private void Reset()
    {
        // Автоматическое создание UI при добавлении скрипта
        CreateDefaultUI();
    }

    [ContextMenu("Создать стандартный UI")]
    private void CreateDefaultUI()
    {
        // Создаёт круг + 3 точки — один раз, потом можно кастомизировать
        var circle = CreateImage("JoystickCircle", Color.cyan);
        circle.rectTransform.sizeDelta = new Vector2(40, 40);
        joystickCircle = circle.rectTransform;

        agePoint = CreatePoint("AgePoint", new Vector2(-100, 80), Color.cyan);
        crystalPoint = CreatePoint("CrystalPoint", new Vector2(100, -60), Color.magenta);
        radioactivityPoint = CreatePoint("RadioactivityPoint", new Vector2(0, -100), Color.yellow);
    }

    private Image CreateImage(string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        img.rectTransform.anchoredPosition = Vector2.zero;
        return img;
    }

    private Image CreatePoint(string name, Vector2 pos, Color col)
    {
        var img = CreateImage(name, col);
        img.rectTransform.anchoredPosition = pos;
        img.rectTransform.sizeDelta = new Vector2(30, 30);
        return img;
    }
#endif
}