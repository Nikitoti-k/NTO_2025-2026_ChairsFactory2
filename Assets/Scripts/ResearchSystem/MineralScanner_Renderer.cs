using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(RectTransform))]
public class MineralScanner_Renderer : MonoBehaviour
{
    public static MineralScanner_Renderer Instance { get; private set; }

    private event Action<float> OnProximityChanged;
    public void SubscribeToProximity(Action<float> callback) => OnProximityChanged += callback;
    public void UnsubscribeFromProximity(Action<float> callback) => OnProximityChanged -= callback;

    [Header("UI Сканера")]
    [SerializeField] private RectTransform scanningPoint;
    [SerializeField] private Button recordButtonUI;
    [SerializeField] private Button reportButton;
    [SerializeField] private TextMeshProUGUI resultText;

    [Header("Панель отчёта")]
    [SerializeField] private GameObject reportPanel;
    [SerializeField] private MineralReportUI reportUI;

    [Header("Экран 'Нет соединения'")]
    [SerializeField] private GameObject noConnectionOverlay;   
    [SerializeField] private TextMeshProUGUI noConnectionText; 

    [Header("Границы и детект")]
    [SerializeField] private Vector2 boundsMin = new Vector2(-300f, -300f);
    [SerializeField] private Vector2 boundsMax = new Vector2(300f, 300f);
    [SerializeField] private float detectionRadius = 80f;
    [SerializeField] private float captureRadius = 30f;
    [SerializeField] private float scannerSpeed = 580f;
    public Camera renderCam;

    private RectTransform myRect;
    private MineralData currentMineral;
    private ScanPoint nearestPoint;
    private bool isReportSubmitted = false;

    
    private static HashSet<string> studiedMinerals = new HashSet<string>();

    private struct LastScan { public ScanPoint Point; public Vector2 ScannerPos; public string ResultLine; }
    private LastScan? lastSuccessfulScan;
    private Dictionary<MineralData, List<int>> crystalLetterOrder = new();

    private void Awake()
    {
        Instance = this;
        myRect = GetComponent<RectTransform>();

        if (scanningPoint == null || renderCam == null || resultText == null || recordButtonUI == null || reportButton == null || noConnectionOverlay == null)
        {
            Debug.LogError("Не назначены ссылки в MineralScanner_Renderer!");
            enabled = false;
            return;
        }

        
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

    private void OnMineralPlaced(GameObject mineralObj)
    {
        currentMineral = mineralObj.GetComponentInChildren<MineralData>();
        if (currentMineral == null)
        {
            Debug.LogError($"[MineralScanner] Нет MineralData на {mineralObj.name}");
            return;
        }

        string mineralID = mineralObj.name; 

       
        if (studiedMinerals.Contains(mineralID))
        {
            
            isReportSubmitted = true;
            reportButton.interactable = false;
            recordButtonUI.interactable = false;

            noConnectionOverlay.SetActive(true);
            noConnectionText.text = "ОБРАЗЕЦ УЖЕ ИЗУЧЕН\nОтчёт по нему отправлен.";

            resultText.text = "<color=#888888>Исследование завершено ранее</color>";
            return;
        }

        
        isReportSubmitted = false;
        reportButton.interactable = true;
        recordButtonUI.interactable = true;

        noConnectionOverlay.SetActive(false);

        
        scanningPoint.anchoredPosition = Vector2.zero;

        lastSuccessfulScan = null;
        crystalLetterOrder.Remove(currentMineral);
        ResetText();
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

        resultText.text = correct
            ? "<color=lime>ИССЛЕДОВАНИЕ ОКОНЧЕНО</color>\nПравильная классификация"
            : "<color=red>ИССЛЕДОВАНИЕ ОКОНЧЕНО</color>\nОшибка в классификации";

       
        if (currentMineral != null)
        {
            string id = currentMineral.transform.root.name; 
            studiedMinerals.Add(id);
        }
    }

    private void OnReportCancelled()
    {
        CameraController.Instance.SetMode(CameraController.ControlMode.FPS);
    }

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
            Vector2 movement = JoystickController.Instance.SmoothVelocity * scannerSpeed * Time.deltaTime;
            Vector2 targetPos = scanningPoint.anchoredPosition + movement;

            scanningPoint.anchoredPosition = Vector2.Lerp(
                scanningPoint.anchoredPosition,
                ClampToBounds(targetPos),
                20f * Time.deltaTime
            );
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
                if (d < bestDist)
                {
                    bestDist = d;
                    closest = point;
                }
            }
        }

        float proximity = (closest != null && bestDist <= detectionRadius)
            ? Mathf.InverseLerp(detectionRadius, captureRadius, bestDist)
            : 0f;

        nearestPoint = closest;
        OnProximityChanged?.Invoke(proximity);
    }

    private void GenerateCrystalLetterOrder(MineralData mineral)
    {
        if (crystalLetterOrder.ContainsKey(mineral)) return;

        string name = GetCrystalName(mineral.CrystalSystem_);
        List<int> order = new List<int>();
        for (int i = 0; i < name.Length; i++) order.Add(i);

        int seed = mineral.GetHashCode() + (int)mineral.AgeMya * 1000 + (int)(mineral.RadioactivityUsv * 10000);
        UnityEngine.Random.InitState(seed);
        Shuffle(order);
        crystalLetterOrder[mineral] = order;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
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

        if (lastSuccessfulScan.HasValue &&
            lastSuccessfulScan.Value.Point == nearestPoint &&
            Vector2.Distance(lastSuccessfulScan.Value.ScannerPos, scanningPoint.anchoredPosition) < 1f)
        {
            UpdateResultTextWithFixed(nearestPoint, lastSuccessfulScan.Value.ResultLine);
            return;
        }

        string newLine = GenerateResultLine(nearestPoint, accuracy);

        lastSuccessfulScan = new LastScan
        {
            Point = nearestPoint,
            ScannerPos = scanningPoint.anchoredPosition,
            ResultLine = newLine
        };

        UpdateResultTextWithFixed(nearestPoint, newLine);
    }

    private Vector2 GetPointLocalPos(ScanPoint point)
    {
        Vector3 screen = renderCam.WorldToScreenPoint(point.transform.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(myRect, screen, renderCam, out Vector2 local);
        return local;
    }

    private string GenerateResultLine(ScanPoint point, float accuracy)
    {
        if (point == currentMineral.AgePoint)
            return $"Возраст: {FormatAge(currentMineral.AgeMya, accuracy)} {currentMineral.AgeUnitText}";

        if (point == currentMineral.RadioactivityPoint)
            return $"Радиоактивность: {FormatRadioactivity(currentMineral.RadioactivityUsv, accuracy)} Бк";

        if (point == currentMineral.CrystalPoint)
            return $"Крист. решётка: {FormatCrystal(currentMineral.CrystalSystem_, accuracy)}";

        return "???";
    }

    private string FormatAge(float real, float acc)
    {
        if (acc >= 0.95f) return real.ToString("F1");
        float factor = Mathf.Lerp(0.2f, 1f, acc);
        return (real * factor).ToString("F1");
    }

    private string FormatRadioactivity(float real, float acc)
    {
        if (acc >= 0.95f) return real.ToString("F3");
        float factor = Mathf.Lerp(0.2f, 1f, acc);
        return (real * factor).ToString("F3");
    }

    private string FormatCrystal(MineralData.CrystalSystem system, float acc)
    {
        if (acc >= 0.95f) return GetCrystalName(system);

        string name = GetCrystalName(system);
        if (!crystalLetterOrder.TryGetValue(currentMineral, out List<int> order))
            return new string('?', name.Length);

        char[] result = new char[name.Length];
        for (int i = 0; i < name.Length; i++) result[i] = '?';

        int visibleCount = Mathf.RoundToInt(acc * name.Length);
        for (int i = 0; i < visibleCount; i++)
        {
            int letterIndex = order[i];
            result[letterIndex] = name[letterIndex];
        }
        return new string(result);
    }

    private string GetCrystalName(MineralData.CrystalSystem system)
    {
        return system switch
        {
            MineralData.CrystalSystem.Cubic => "кубическая",
            MineralData.CrystalSystem.Trigonal => "тригональная",
            MineralData.CrystalSystem.Monoclinic => "моноклинная",
            _ => "аморфная"
        };
    }

    private void UpdateResultTextWithFixed(ScanPoint point, string newLine)
    {
        string[] lines = resultText.text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (point == currentMineral.AgePoint && lines[i].StartsWith("Возраст"))
                lines[i] = newLine;
            else if (point == currentMineral.RadioactivityPoint && lines[i].StartsWith("Радиоактивность"))
                lines[i] = newLine;
            else if (point == currentMineral.CrystalPoint && lines[i].StartsWith("Крист"))
                lines[i] = newLine;
        }
        resultText.text = string.Join("\n", lines);
    }

    private void ResetText()
    {
        if (!isReportSubmitted)
            resultText.text = "Крист. решётка: ???\nВозраст: ???\nРадиоактивность: ???";
    }
}