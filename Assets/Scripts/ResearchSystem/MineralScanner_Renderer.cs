using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class MineralScanner_Renderer : MonoBehaviour
{
    public static MineralScanner_Renderer Instance { get; private set; }
    private event System.Action<float> OnProximityChanged;
    public void SubscribeToProximity(System.Action<float> c) => OnProximityChanged += c;
    public void UnsubscribeFromProximity(System.Action<float> c) => OnProximityChanged -= c;

    [Header("UI")]
    [SerializeField] private RectTransform scanningPoint;
    [SerializeField] private Button recordButtonUI;
    [SerializeField] private Button reportButton;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private GameObject reportPanel;
    [SerializeField] private MineralReportUI reportUI;
    [SerializeField] private GameObject noConnectionOverlay;
    [SerializeField] private TextMeshProUGUI noConnectionText;

    [Header("Настройки")]
    [SerializeField] private Vector2 boundsMin = new(-300f, -300f);
    [SerializeField] private Vector2 boundsMax = new(300f, 300f);
    [SerializeField] private float detectionRadius = 80f;
    [SerializeField] private float captureRadius = 30f;
    [SerializeField] private float scannerSpeed = 580f;
    public Camera renderCam;

    private RectTransform myRect;
    private MineralData currentMineral;
    private ScanPoint nearestPoint;
    private bool isReportSubmitted;

    private struct LastScan { public ScanPoint Point; public Vector2 ScannerPos; public string ResultLine; }
    private LastScan? lastSuccessfulScan;
    private readonly Dictionary<MineralData, List<int>> crystalLetterOrder = new();

    private void Awake()
    {
        Instance = this;
        myRect = GetComponent<RectTransform>();

        resultText.text = "Крист. решётка: ???\nВозраст: ???\nРадиоактивность: ???";
        noConnectionOverlay.SetActive(true);
        noConnectionText.text = "НЕТ СОЕДИНЕНИЯ";

        recordButtonUI.onClick.RemoveAllListeners();
        recordButtonUI.onClick.AddListener(TryRecordData);
        reportButton.onClick.RemoveAllListeners();
        reportButton.onClick.AddListener(OpenReportPanel);

        if (reportUI != null)
        {
            reportUI.OnReportSubmitted += OnReportSubmitted;
            reportUI.OnReportCancelled += OnReportCancelled;
        }
    }

    private void OnDestroy() => Instance = null;

    private void Start()
    {
        reportPanel.SetActive(false);
        MineralScannerManager.Instance.OnMineralScanned.AddListener(OnMineralPlaced);
        MineralScannerManager.Instance.OnMineralRemoved.AddListener(OnMineralRemoved);
    }

    public void OnMineralPlaced(GameObject obj)
    {
        currentMineral = obj.GetComponentInChildren<MineralData>();
        if (currentMineral == null) return;

        isReportSubmitted = currentMineral.isResearched;
        reportButton.interactable = !isReportSubmitted;
        recordButtonUI.interactable = !isReportSubmitted;
        noConnectionOverlay.SetActive(isReportSubmitted);

        if (isReportSubmitted)
        {
            noConnectionText.text = "ОБРАЗЕЦ УЖЕ ИЗУЧЕН\nОтчёт по нему отправлен.";
            resultText.text = "<color=#888888>Исследование завершено ранее</color>";
            return;
        }

        noConnectionOverlay.SetActive(false);
        scanningPoint.anchoredPosition = Vector2.zero;
        lastSuccessfulScan = null;
        crystalLetterOrder.Remove(currentMineral);

        if (!string.IsNullOrEmpty(currentMineral.savedAgeLine) ||
            !string.IsNullOrEmpty(currentMineral.savedRadioactivityLine) ||
            !string.IsNullOrEmpty(currentMineral.savedCrystalLine))
        {
            resultText.text = $"{(string.IsNullOrEmpty(currentMineral.savedCrystalLine) ? "Крист. решётка: ???" : currentMineral.savedCrystalLine)}\n" +
                              $"{(string.IsNullOrEmpty(currentMineral.savedAgeLine) ? "Возраст: ???" : currentMineral.savedAgeLine)}\n" +
                              $"{(string.IsNullOrEmpty(currentMineral.savedRadioactivityLine) ? "Радиоактивность: ???" : currentMineral.savedRadioactivityLine)}";
        }
        else ResetText();

        GenerateCrystalLetterOrder(currentMineral);
    }

    private void OnMineralRemoved()
    {
        currentMineral = null;
        isReportSubmitted = false;
        reportButton.interactable = false;
        recordButtonUI.interactable = true;
        noConnectionOverlay.SetActive(true);
        noConnectionText.text = "НЕТ СОЕДИНЕНИЯ";
        reportPanel.SetActive(false);
        crystalLetterOrder.Clear();
        ResetText();
    }

    private void OnReportSubmitted(bool correct)
    {
        isReportSubmitted = true;
        reportButton.interactable = false;
        recordButtonUI.interactable = false;
        CameraController.Instance.SetMode(CameraController.ControlMode.FPS);

        if (currentMineral != null)
        {
            currentMineral.isResearched = true;
            string name = currentMineral.transform.name.Replace("(Clone)", "").Trim();
            ResearchReportViewer.LogResearchResult(name, correct);

            // ИСПРАВЛЕНО: было mineral, стало currentMineral.UniqueInstanceID
            GameDayManager.Instance.RegisterMineralResearched(currentMineral);
        }
    }

    private void OnReportCancelled() => CameraController.Instance.SetMode(CameraController.ControlMode.FPS);

    public void OpenReportPanel()
    {
        if (currentMineral == null || isReportSubmitted) return;
        reportPanel.SetActive(true);
        CameraController.Instance.SetMode(CameraController.ControlMode.UI);
        reportUI.StartReport(currentMineral);
    }

    private void LateUpdate()
    {
        if (JoystickController.Instance == null) return;

        if (JoystickController.Instance.IsGrabbed || !JoystickController.Instance.SmoothVelocity.Equals(Vector2.zero))
        {
            Vector2 delta = JoystickController.Instance.SmoothVelocity * scannerSpeed * Time.deltaTime;
            Vector2 target = ClampToBounds(scanningPoint.anchoredPosition + delta);
            scanningPoint.anchoredPosition = Vector2.Lerp(scanningPoint.anchoredPosition, target, 20f * Time.deltaTime);
        }

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
            if (currentMineral != null) { currentMineral = null; nearestPoint = null; lastSuccessfulScan = null; OnProximityChanged?.Invoke(0f); ResetText(); }
            return;
        }

        if (mineral != currentMineral)
        {
            currentMineral = mineral;
            lastSuccessfulScan = null;
            ResetText();
            GenerateCrystalLetterOrder(mineral);
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
                if (d < bestDist) { bestDist = d; closest = point; }
            }
        }

        float proximity = (closest != null && bestDist <= detectionRadius)
            ? Mathf.InverseLerp(detectionRadius, captureRadius, bestDist) : 0f;

        nearestPoint = closest;
        OnProximityChanged?.Invoke(proximity);
    }

    private void GenerateCrystalLetterOrder(MineralData mineral)
    {
        if (crystalLetterOrder.ContainsKey(mineral)) return;
        string name = GetCrystalName(mineral.CrystalSystem_);
        var order = Enumerable.Range(0, name.Length).ToList();
        int seed = mineral.GetHashCode() + (int)(mineral.AgeMya * 1000) + (int)(mineral.RadioactivityUsv * 10000);
        UnityEngine.Random.InitState(seed);
        for (int i = order.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }
        crystalLetterOrder[mineral] = order;
    }

    public void TryRecordData()
    {
        if (currentMineral == null || nearestPoint == null || isReportSubmitted)
        {
            resultText.text = "<color=red>Нет сигнала или исследование завершено!</color>\nКрист. решётка: ???\nВозраст: ???\nРадиоактивность: ???";
            return;
        }

        Vector2 pointPos = GetPointLocalPos(nearestPoint);
        float dist = Vector2.Distance(scanningPoint.anchoredPosition, pointPos);
        float accuracy = Mathf.InverseLerp(detectionRadius, captureRadius, dist);

        if (lastSuccessfulScan.HasValue && lastSuccessfulScan.Value.Point == nearestPoint &&
            Vector2.Distance(lastSuccessfulScan.Value.ScannerPos, scanningPoint.anchoredPosition) < 1f)
        {
            UpdateResultTextWithFixed(nearestPoint, lastSuccessfulScan.Value.ResultLine);
            return;
        }

        string line = GenerateResultLine(nearestPoint, accuracy);
        lastSuccessfulScan = new LastScan { Point = nearestPoint, ScannerPos = scanningPoint.anchoredPosition, ResultLine = line };
        UpdateResultTextWithFixed(nearestPoint, line);
    }

    private Vector2 GetPointLocalPos(ScanPoint p)
    {
        Vector3 screen = renderCam.WorldToScreenPoint(p.transform.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(myRect, screen, renderCam, out Vector2 local);
        return local;
    }

    private string GenerateResultLine(ScanPoint p, float acc) => p switch
    {
        var x when x == currentMineral.AgePoint => $"Возраст: {FormatAge(currentMineral.AgeMya, acc)} {currentMineral.AgeUnitText}",
        var x when x == currentMineral.RadioactivityPoint => $"Радиоактивность: {FormatRadioactivity(currentMineral.RadioactivityUsv, acc)} Бк",
        var x when x == currentMineral.CrystalPoint => $"Крист. решётка: {FormatCrystal(currentMineral.CrystalSystem_, acc)}",
        _ => "???"
    };

    private string FormatAge(float v, float a) => (a >= 0.95f ? v : v * Mathf.Lerp(0.2f, 1f, a)).ToString("F1");
    private string FormatRadioactivity(float v, float a) => (a >= 0.95f ? v : v * Mathf.Lerp(0.2f, 1f, a)).ToString("F3");

    private string FormatCrystal(MineralData.CrystalSystem sys, float acc)
    {
        if (acc >= 0.95f) return GetCrystalName(sys);
        string name = GetCrystalName(sys);
        if (!crystalLetterOrder.TryGetValue(currentMineral, out var order)) return new string('?', name.Length);

        char[] result = new char[name.Length];
        for (int i = 0; i < result.Length; i++) result[i] = '?';
        int visible = Mathf.RoundToInt(acc * name.Length);
        for (int i = 0; i < visible; i++) result[order[i]] = name[order[i]];
        return new string(result);
    }

    private string GetCrystalName(MineralData.CrystalSystem s) => s switch
    {
        MineralData.CrystalSystem.Cubic => "кубическая",
        MineralData.CrystalSystem.Trigonal => "тригональная",
        MineralData.CrystalSystem.Monoclinic => "моноклинная",
        _ => "аморфная"
    };

    private void UpdateResultTextWithFixed(ScanPoint point, string line)
    {
        if (point == currentMineral.AgePoint) currentMineral.savedAgeLine = line;
        else if (point == currentMineral.RadioactivityPoint) currentMineral.savedRadioactivityLine = line;
        else if (point == currentMineral.CrystalPoint) currentMineral.savedCrystalLine = line;

        string[] lines = resultText.text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (point == currentMineral.AgePoint && lines[i].StartsWith("Возраст")) lines[i] = line;
            else if (point == currentMineral.RadioactivityPoint && lines[i].StartsWith("Радиоактивность")) lines[i] = line;
            else if (point == currentMineral.CrystalPoint && lines[i].StartsWith("Крист")) lines[i] = line;
        }
        resultText.text = string.Join("\n", lines);
    }

    private void ResetText() => resultText.text = !isReportSubmitted
        ? "Крист. решётка: ???\nВозраст: ???\nРадиоактивность: ???"
        : resultText.text;
}