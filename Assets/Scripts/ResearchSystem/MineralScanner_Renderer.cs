using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class MineralScanner_Renderer : MonoBehaviour, ILocalizable
{
    public static MineralScanner_Renderer Instance { get; private set; }

   
    private event System.Action<float> OnProximityChanged;
    public void SubscribeToProximity(System.Action<float> c) => OnProximityChanged += c;
    public void UnsubscribeFromProximity(System.Action<float> c) => OnProximityChanged -= c;

    private event System.Action OnAllThreeValuesScanned;
    public void SubscribeToAllThreeScanned(System.Action callback) => OnAllThreeValuesScanned += callback;
    public void UnsubscribeFromAllThreeScanned(System.Action callback) => OnAllThreeValuesScanned -= callback;

    [Header("UI Elements")]
    [SerializeField] private RectTransform scanningPoint;
    [SerializeField] private Button recordButtonUI;
    [SerializeField] private Button reportButton;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private GameObject reportPanel;
    [SerializeField] private MineralReportUI reportUI;
    [SerializeField] private GameObject noConnectionOverlay;
    [SerializeField] private TextMeshProUGUI noConnectionText;

    [Header("Scanner Settings")]
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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        myRect = GetComponent<RectTransform>();

        Localize(); 
        noConnectionOverlay.SetActive(true);

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

    private void OnDestroy()
    {
        Instance = null;
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

    private void OnEnable()
    {
        LocalizationManager.Register(this);
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
    }

    private void OnDisable()
    {
        LocalizationManager.Unregister(this);
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

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

        if (isReportSubmitted)
        {
            noConnectionOverlay.SetActive(true);
            noConnectionText.text = LocalizationManager.Loc("SCANNER_ALREADY_RESEARCHED");
            resultText.text = LocalizationManager.Loc("SCANNER_ALREADY_DONE");
            return;
        }

        noConnectionOverlay.SetActive(false);
        scanningPoint.anchoredPosition = Vector2.zero;
        lastSuccessfulScan = null;
        crystalLetterOrder.Remove(currentMineral);

        if (HasSavedData())
            RestoreSavedDataToText();
        else
            ResetText();

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
        noConnectionText.text = LocalizationManager.Loc("SCANNER_NO_CONNECTION");
        reportPanel.SetActive(false);
        crystalLetterOrder.Clear();
        ResetText();
        OnAllThreeValuesScanned?.Invoke();
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

        OnAllThreeValuesScanned?.Invoke();
    }

    private void OnReportCancelled()
    {
        CameraController.Instance.SetMode(CameraController.ControlMode.FPS);
    }

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
            if (currentMineral != null)
            {
                currentMineral = null;
                nearestPoint = null;
                lastSuccessfulScan = null;
                OnProximityChanged?.Invoke(0f);
                ResetText();
            }
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
        Debug.Log("нажимаем кнопку!");
        if (currentMineral == null || nearestPoint == null || isReportSubmitted)
        {
            resultText.text = LocalizationManager.Loc("SCANNER_NO_SIGNAL");
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
        UpdateReportButtonAndNotify();
    }

    
    public void Localize()
    {
        if (noConnectionOverlay.activeSelf)
        {
            if (isReportSubmitted)
            {
                noConnectionText.text = LocalizationManager.Loc("SCANNER_ALREADY_RESEARCHED");
                resultText.text = LocalizationManager.Loc("SCANNER_ALREADY_DONE");
            }
            else
            {
                noConnectionText.text = LocalizationManager.Loc("SCANNER_NO_CONNECTION");
            }
        }

        if (currentMineral == null || isReportSubmitted)
        {
            ResetText();
        }
        else if (HasSavedData())
        {
            RestoreSavedDataToText();
        }
        else
        {
            ResetText();
        }
    }

    private void OnLanguageChanged(LocalizationManager.Language lang) => Localize();

    
    private bool HasSavedData() => currentMineral != null &&
        (!string.IsNullOrEmpty(currentMineral.savedAgeLine) ||
         !string.IsNullOrEmpty(currentMineral.savedRadioactivityLine) ||
         !string.IsNullOrEmpty(currentMineral.savedCrystalLine));

    private void RestoreSavedDataToText()
    {
        resultText.text = string.Format(
            LocalizationManager.Loc("SCANNER_DEFAULT"),
            currentMineral.savedCrystalLine ?? "???",
            currentMineral.savedAgeLine ?? "???",
            currentMineral.savedRadioactivityLine ?? "???"
        );
    }

    private Vector2 GetPointLocalPos(ScanPoint p)
    {
        Vector3 screen = renderCam.WorldToScreenPoint(p.transform.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(myRect, screen, renderCam, out Vector2 local);
        return local;
    }

    private string GenerateResultLine(ScanPoint p, float acc) => p switch
    {
        var x when x == currentMineral.AgePoint =>
            string.Format(
                LocalizationManager.Loc("AGE_LABEL"),
                FormatAge(currentMineral.AgeMya, acc),
                currentMineral.AgeUnitText
            ),

        var x when x == currentMineral.RadioactivityPoint =>
            string.Format(
                LocalizationManager.Loc("RAD_LABEL"),
                FormatRadioactivity(currentMineral.RadioactivityUsv, acc)
            ),

        var x when x == currentMineral.CrystalPoint =>
            string.Format(
                LocalizationManager.Loc("CRYSTAL_LABEL"),
                FormatCrystal(currentMineral.CrystalSystem_, acc)
            ),

        _ => "???"
    };

    private string FormatAge(float v, float a) => (a >= 0.95f ? v : v * Mathf.Lerp(0.2f, 1f, a)).ToString("F1");
    private string FormatRadioactivity(float v, float a) => (a >= 0.95f ? v : v * Mathf.Lerp(0.2f, 1f, a)).ToString("F3");

    private string FormatCrystal(MineralData.CrystalSystem sys, float acc)
    {
        string fullName = GetCrystalName(sys);
        if (acc >= 0.95f) return fullName;

        if (!crystalLetterOrder.TryGetValue(currentMineral, out var order))
            return new string('?', fullName.Length);

        char[] result = new char[fullName.Length];
        for (int i = 0; i < result.Length; i++) result[i] = '?';

        int visible = Mathf.RoundToInt(acc * fullName.Length);
        for (int i = 0; i < visible && i < order.Count; i++)
            result[order[i]] = fullName[order[i]];

        return new string(result);
    }

    private string GetCrystalName(MineralData.CrystalSystem s) => s switch
    {
        MineralData.CrystalSystem.Cubic => LocalizationManager.Loc("CRYSTAL_CUBIC"),
        MineralData.CrystalSystem.Molecular => LocalizationManager.Loc("CRYSTAL_MOLECULAR"),
        MineralData.CrystalSystem.Monoclinic => LocalizationManager.Loc("CRYSTAL_MONOCLINIC"),
        _ => LocalizationManager.Loc("CRYSTAL_AMORPHOUS")
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

    private void UpdateResultTextWithFixed(ScanPoint point, string fullLineWithPrefix)
    {
        
        if (point == currentMineral.AgePoint) currentMineral.savedAgeLine = fullLineWithPrefix;
        else if (point == currentMineral.RadioactivityPoint) currentMineral.savedRadioactivityLine = fullLineWithPrefix;
        else if (point == currentMineral.CrystalPoint) currentMineral.savedCrystalLine = fullLineWithPrefix;

        
        resultText.text = string.Format(
            LocalizationManager.Loc("SCANNER_DEFAULT"),
            currentMineral.savedCrystalLine ?? "???",
            currentMineral.savedAgeLine ?? "???",
            currentMineral.savedRadioactivityLine ?? "???"
        );
    }
    private void ResetText()
    {
        resultText.text = string.Format(
            LocalizationManager.Loc("SCANNER_DEFAULT"),
            "???", "???", "???"
        );
    }
    private string GetSavedOrPlaceholder(ref string saved)
    {
        return !string.IsNullOrEmpty(saved) ? saved : "???";
    }
}