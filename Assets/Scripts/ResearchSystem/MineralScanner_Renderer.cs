using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

[RequireComponent(typeof(RectTransform))]
public class MineralScanner_Renderer : MonoBehaviour
{
    public static MineralScanner_Renderer Instance { get; private set; }

    // Событие закрыто — подписка только через методы
    private event Action<float> OnProximityChanged;

    public void SubscribeToProximity(Action<float> callback) => OnProximityChanged += callback;
    public void UnsubscribeFromProximity(Action<float> callback) => OnProximityChanged -= callback;

    [Header("UI")]
    [SerializeField] private RectTransform scanningPoint;
    [SerializeField] private Button recordButtonUI;
    [SerializeField] private TextMeshProUGUI resultText;

    [Header("Границы")]
    [SerializeField] private Vector2 boundsMin = new Vector2(-300f, -300f);
    [SerializeField] private Vector2 boundsMax = new Vector2(300f, 300f);

    [Header("Детект")]
    [SerializeField] private float detectionRadius = 80f;
    [SerializeField] private float captureRadius = 30f;
    [SerializeField] private float scannerSpeed = 580f;

    public Camera renderCam;
    private Vector2 velocity; // для разгона точки
    private RectTransform myRect;
    private MineralData currentMineral;
    private ScanPoint nearestPoint;
    private Vector2 currentDirection;

    private void Awake()
    {
        Instance = this;
        myRect = GetComponent<RectTransform>();

        if (scanningPoint == null || renderCam == null || resultText == null)
        {
            enabled = false;
            return;
        }

        resultText.text = "Крист. решётка: ???\nВозраст: ???\nРадиоактивность: ???";
        recordButtonUI?.onClick.AddListener(TryRecordData);
    }

    private void OnDestroy()
    {
        Instance = null;
        recordButtonUI?.onClick.RemoveListener(TryRecordData);
    }

   

    private void LateUpdate()
    {
        if (JoystickController.Instance == null) return;

        Vector2 joyInput = JoystickController.Instance.CurrentDirection;

        
        if (!JoystickController.Instance.IsGrabbed)
        {
            CheckScanPoints(scanningPoint.anchoredPosition);
            return; 
        }

        // Двигаем только когда держим — плавно и точно
        Vector2 movement = joyInput * scannerSpeed * Time.deltaTime;
        Vector2 newPos = scanningPoint.anchoredPosition + movement;

        scanningPoint.anchoredPosition = ClampToBounds(newPos);
        CheckScanPoints(scanningPoint.anchoredPosition);
    }
    private Vector2 ClampToBounds(Vector2 pos)
    {
        pos.x = Mathf.Clamp(pos.x, boundsMin.x, boundsMax.x);
        pos.y = Mathf.Clamp(pos.y, boundsMin.y, boundsMax.y);
        return pos;
    }

    private void CheckScanPoints(Vector2 scannerPos)
    {
        MineralData mineral = MineralScannerManager.Instance?.CurrentMineral;

        if (mineral == null)
        {
            if (currentMineral != null)
            {
                currentMineral = null;
                nearestPoint = null;
                OnProximityChanged?.Invoke(0f);
                ResetText();
            }
            return;
        }

        if (mineral != currentMineral)
        {
            currentMineral = mineral;
            ResetText();
        }

        float bestDist = float.MaxValue;
        ScanPoint closest = null;

        foreach (var point in new[] { mineral.AgePoint, mineral.CrystalPoint, mineral.RadioactivityPoint })
        {
            if (point == null) continue;

            Vector3 screen = renderCam.WorldToScreenPoint(point.transform.position);
            if (screen.z < 0) continue;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(myRect, screen, renderCam, out Vector2 local))
            {
                float d = Vector2.Distance(scannerPos, local);
                if (d < bestDist)
                {
                    bestDist = d;
                    closest = point;
                }
            }
        }

        float proximity = closest != null && bestDist <= detectionRadius
            ? Mathf.InverseLerp(detectionRadius, captureRadius, bestDist)
            : 0f;

        nearestPoint = closest;
        OnProximityChanged?.Invoke(proximity);
    }

    public void TryRecordData()
    {
        if (currentMineral == null || nearestPoint == null)
        {
            resultText.text = "<color=red>Нет сигнала!</color>\nКрист. решётка: ???\nВозраст: ???\nРадиоактивность: ???";
            return;
        }

        Vector2 pointPos = GetPointLocalPos(nearestPoint);
        float dist = Vector2.Distance(scanningPoint.anchoredPosition, pointPos);
        float accuracy = Mathf.InverseLerp(detectionRadius, captureRadius, dist);

        UpdateResultText(nearestPoint, accuracy);
    }

    private Vector2 GetPointLocalPos(ScanPoint point)
    {
        Vector3 screen = renderCam.WorldToScreenPoint(point.transform.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(myRect, screen, renderCam, out Vector2 local);
        return local;
    }

    private void UpdateResultText(ScanPoint point, float accuracy)
    {
        string[] lines = resultText.text.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            if (point == currentMineral.AgePoint && lines[i].Contains("Возраст"))
                lines[i] = $"Возраст: {FormatAge(currentMineral.AgeMya, accuracy)} млн лет";
            else if (point == currentMineral.RadioactivityPoint && lines[i].Contains("Радиоактивность"))
                lines[i] = $"Радиоактивность: {FormatRadioactivity(currentMineral.RadioactivityUsv, accuracy)} Бк";
            else if (point == currentMineral.CrystalPoint && lines[i].Contains("Крист."))
                lines[i] = $"Крист. решётка: {ScrambleCrystalName(currentMineral.CrystalSystem_, accuracy)}";
        }

        resultText.text = string.Join("\n", lines);
    }

    private void ResetText()
    {
        resultText.text = "Крист. решётка: ???\nВозраст: ???\nРадиоактивность: ???";
    }

    private string FormatAge(float real, float acc) =>
        acc >= 0.9f ? real.ToString("F1") : (real + UnityEngine.Random.Range(-20f, 50f) * (1f - acc)).ToString("F1");

    private string FormatRadioactivity(float real, float acc) =>
        acc >= 0.9f ? real.ToString("F3") : (real + UnityEngine.Random.Range(-0.1f, 0.4f) * (1f - acc)).ToString("F3");

    private string ScrambleCrystalName(MineralData.CrystalSystem system, float acc)
    {
        string name = system switch
        {
            MineralData.CrystalSystem.Cubic => "кубическая",
            MineralData.CrystalSystem.Trigonal => "тригональная",
            MineralData.CrystalSystem.Monoclinic => "моноклинная",
            _ => "неизвестно"
        };

        if (acc >= 0.9f) return name;

        char[] c = name.ToCharArray();
        int errors = Mathf.CeilToInt((1f - acc) * c.Length * 2.2f);
        for (int i = 0; i < errors; i++)
        {
            int idx = UnityEngine.Random.Range(0, c.Length);
            if (c[idx] != '?') c[idx] = '?';
        }
        return new string(c);
    }
}