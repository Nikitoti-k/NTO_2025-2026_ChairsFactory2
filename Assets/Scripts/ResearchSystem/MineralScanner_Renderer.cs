using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class MineralScanner_Renderer : MonoBehaviour
{
    public static MineralScanner_Renderer Instance { get; private set; }

    // === СОБЫТИЯ ===
    private event System.Action<float> OnProximityChanged;
    public void SubscribeToProximity(System.Action<float> c) => OnProximityChanged += c;
    public void UnsubscribeFromProximity(System.Action<float> c) => OnProximityChanged -= c;

    // ← НОВОЕ СОБЫТИЕ: когда все 3 значения получены
    private event System.Action OnAllThreeValuesScanned;
    public void SubscribeToAllThreeScanned(System.Action callback) => OnAllThreeValuesScanned += callback;
    public void UnsubscribeFromAllThreeScanned(System.Action callback) => OnAllThreeValuesScanned -= callback;

    [SerializeField] private RectTransform scanningPoint;
    [SerializeField] private Button recordButtonUI;
    [SerializeField] private Button reportButton;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private GameObject reportPanel;
    [SerializeField] private MineralReportUI reportUI;
    [SerializeField] private GameObject noConnectionOverlay;
    [SerializeField] private TextMeshProUGUI noConnectionText;

    [SerializeField] private Vector2 boundsMin = new(-300f, -300f);
    [SerializeField] private Vector2 boundsMax = new(300f, 300f);
    [SerializeField] private float detectionRadius = 80f;
    [SerializeField] private float captureRadius = 30f;
    [SerializeField] private float scannerSpeed = 850f;
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

    public MineralData GetCurrentMineral() => currentMineral;

    public void OnMineralPlaced(GameObject obj)
    {
        currentMineral = obj.GetComponentInChildren<MineralData>();
        if (currentMineral == null) return;

        isReportSubmitted = currentMineral.isResearched;
        reportButton.interactable = false;
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

        if (HasSavedData())
        {
            RestoreSavedDataToText();
        }
        else ResetText();

        GenerateCrystalLetterOrder(currentMineral);
        UpdateReportButtonAndNotify();
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
        OnAllThreeValuesScanned?.Invoke(); // ← сбрасываем подсказку
    }

    private bool AreAllThreeValuesScanned()
    {
        if (currentMineral == null) return false;
        return !string.IsNullOrEmpty(currentMineral.savedAgeLine) &&
               !string.IsNullOrEmpty(currentMineral.savedRadioactivityLine) &&
               !string.IsNullOrEmpty(currentMineral.savedCrystalLine);
    }

    private void UpdateReportButtonAndNotify()
    {
        if (currentMineral != null && !currentMineral.isResearched)
        {
            bool allScanned = AreAllThreeValuesScanned();
            reportButton.interactable = allScanned;
            if (allScanned) OnAllThreeValuesScanned?.Invoke();
        }
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
            GameDayManager.Instance.RegisterMineralResearched(currentMineral);
        }

        // ← УБИРАЕМ ПОДСКАЗКУ ПОСЛЕ ОТПРАВКИ ОТЧЁТА
        OnAllThreeValuesScanned?.Invoke();
    }

    private void OnReportCancelled() => CameraController.Instance.SetMode(CameraController.ControlMode.FPS);

    public void OpenReportPanel()
    {
        if (currentMineral == null || isReportSubmitted || !AreAllThreeValuesScanned()) return;
        reportPanel.SetActive(true);
        CameraController.Instance.SetMode(CameraController.ControlMode.UI);
        reportUI.StartReport(currentMineral);
    }

    private void LateUpdate()
    {
        if (JoystickController.Instance == null) return;
        Vector2 delta = JoystickController.Instance.CurrentDirection * scannerSpeed * Time.deltaTime;
        Vector2 newPos = scanningPoint.anchoredPosition + delta;
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

        TutorialManager.Instance?.OnRecordButtonPressed();

        string line = GenerateResultLine(nearestPoint, accuracy);
        lastSuccessfulScan = new LastScan { Point = nearestPoint, ScannerPos = scanningPoint.anchoredPosition, ResultLine = line };
        UpdateResultTextWithFixed(nearestPoint, line);

        // ← ВЫЗЫВАЕМ СОБЫТИЕ ПОСЛЕ КАЖДОЙ ЗАПИСИ
        UpdateReportButtonAndNotify();
    }

    // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===
    private bool HasSavedData() => currentMineral != null &&
        (!string.IsNullOrEmpty(currentMineral.savedAgeLine) ||
         !string.IsNullOrEmpty(currentMineral.savedRadioactivityLine) ||
         !string.IsNullOrEmpty(currentMineral.savedCrystalLine));

    private void RestoreSavedDataToText()
    {
        resultText.text = $"{(string.IsNullOrEmpty(currentMineral.savedCrystalLine) ? "Крист. решётка: ???" : currentMineral.savedCrystalLine)}\n" +
                          $"{(string.IsNullOrEmpty(currentMineral.savedAgeLine) ? "Возраст: ???" : currentMineral.savedAgeLine)}\n" +
                          $"{(string.IsNullOrEmpty(currentMineral.savedRadioactivityLine) ? "Радиоактивность: ???" : currentMineral.savedRadioactivityLine)}";
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
        MineralData.CrystalSystem.Molecular => "молекулярная",
        MineralData.CrystalSystem.Monoclinic => "моноклинная",
        _ => "аморфная"
    };

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

    private void ResetText() => resultText.text = "Крист. решётка: ???\nВозраст: ???\nРадиоактивность: ???";
}