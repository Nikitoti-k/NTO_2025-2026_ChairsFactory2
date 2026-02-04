using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class ResearchReportViewer : MonoBehaviour, ILocalizable
{
    [Header("UI References")]
    [SerializeField] private GameObject reportPanel;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject reportEntryPrefab;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI noReportsText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button prevDayButton;
    [SerializeField] private Button nextDayButton;
    [SerializeField] private TextMeshProUGUI dayCounterText;

    [System.Serializable]
    private class MineralResearchResult
    {
        public string displayName;
        public bool wasCorrect;
        public string mineralClassName;
    }

    [System.Serializable]
    private class DayReport
    {
        public int dayNumber;
        public List<MineralResearchResult> results = new();
    }

    private readonly List<DayReport> allReports = new();
    private int currentViewedDayIndex = -1;

    private void Awake()
    {
        if (reportPanel) reportPanel.SetActive(false);
        if (noReportsText) noReportsText.gameObject.SetActive(true);

        closeButton?.onClick.AddListener(ClosePanel);
        prevDayButton?.onClick.AddListener(ShowPreviousDay);
        nextDayButton?.onClick.AddListener(ShowNextDay);

        LocalizationManager.Register(this);
    }

    private void OnDestroy()
    {
        LocalizationManager.Unregister(this);

        closeButton?.onClick.RemoveListener(ClosePanel);
        prevDayButton?.onClick.RemoveListener(ShowPreviousDay);
        nextDayButton?.onClick.RemoveListener(ShowNextDay);
    }

    private void OnEnable()
    {
        if (GameDayManager.Instance != null)
            GameDayManager.Instance.OnDayFullyCompleted.AddListener(OnDayCompleted);
    }

    private void OnDisable()
    {
        if (GameDayManager.Instance != null)
            GameDayManager.Instance.OnDayFullyCompleted.RemoveListener(OnDayCompleted);
    }

    public void Localize()
    {
        UpdatePanelVisuals(); 
    }

    private void OnDayCompleted()
    {
        int today = GameDayManager.Instance.CurrentDay;
        var currentDayReport = allReports.Find(r => r.dayNumber == today);
        if (currentDayReport != null)
        {
            currentDayReport.results = new List<MineralResearchResult>(currentDayReport.results);
        }
    }

    public static void LogResearchResult(string mineralName, bool correct, string className = "")
    {
        var viewer = FindObjectOfType<ResearchReportViewer>();
        if (viewer == null || GameDayManager.Instance == null) return;

        int today = GameDayManager.Instance.CurrentDay;
        var dayReport = viewer.allReports.Find(r => r.dayNumber == today);
        if (dayReport == null)
        {
            dayReport = new DayReport { dayNumber = today };
            viewer.allReports.Add(dayReport);
        }

        int sampleNum = dayReport.results.Count + 1;
        dayReport.results.Add(new MineralResearchResult
        {
            displayName = LocalizationManager.Loc("REPORT_VIEWER_SAMPLE", sampleNum),
            wasCorrect = correct,
            mineralClassName = className
        });
    }

    public void OpenPanel()
    {
        if (!reportPanel) return;
        reportPanel.SetActive(true);
        CameraController.Instance.SetMode(CameraController.ControlMode.UI);

        if (allReports.Count > 0)
        {
            currentViewedDayIndex = allReports.FindLastIndex(r => r.dayNumber < GameDayManager.Instance.CurrentDay);
            if (currentViewedDayIndex == -1) currentViewedDayIndex = allReports.Count - 1;
        }
        else
        {
            currentViewedDayIndex = -1;
        }

        UpdatePanelVisuals();
    }

    public void ClosePanel()
    {
        reportPanel.SetActive(false);
        CameraController.Instance.SetMode(CameraController.ControlMode.FPS);
    }

    private void ShowPreviousDay()
    {
        if (currentViewedDayIndex <= 0) return;
        currentViewedDayIndex--;
        UpdatePanelVisuals();
    }

    private void ShowNextDay()
    {
        if (currentViewedDayIndex >= allReports.Count - 1) return;
        currentViewedDayIndex++;
        UpdatePanelVisuals();
    }

    private void UpdatePanelVisuals()
    {
        
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        if (currentViewedDayIndex < 0 || currentViewedDayIndex >= allReports.Count)
        {
            noReportsText.gameObject.SetActive(true);
            noReportsText.text = LocalizationManager.Loc("REPORT_VIEWER_NO_REPORTS");
            titleText.text = LocalizationManager.Loc("REPORT_VIEWER_TITLE");
            dayCounterText.text = "";
            prevDayButton.interactable = nextDayButton.interactable = false;
            return;
        }

        var report = allReports[currentViewedDayIndex];

        noReportsText.gameObject.SetActive(false);
        titleText.text = LocalizationManager.Loc("REPORT_VIEWER_DAY_TITLE", report.dayNumber);
        dayCounterText.text = LocalizationManager.Loc("REPORT_VIEWER_DAY_COUNTER", currentViewedDayIndex + 1, allReports.Count);

        prevDayButton.interactable = currentViewedDayIndex > 0;
        nextDayButton.interactable = currentViewedDayIndex < allReports.Count - 1;

        foreach (var result in report.results)
        {
            var entry = Instantiate(reportEntryPrefab, contentParent);
            var text = entry.GetComponentInChildren<TextMeshProUGUI>();
            var img = entry.GetComponent<Image>();

            if (text)
            {
                string classText = string.IsNullOrEmpty(result.mineralClassName)
                    ? ""
                    : "\n" + LocalizationManager.Loc("REPORT_VIEWER_CLASS_FORMAT", result.mineralClassName);

                text.text = result.displayName + classText;
            }

            if (img)
            {
                img.color = result.wasCorrect
                    ? new Color(0.1f, 0.8f, 0.1f, 0.95f) 
                    : new Color(0.8f, 0.2f, 0.2f, 0.95f);
            }
        }
    }

  

   
  

   


    // СЕРИАЛИЗАЦИЯ / ДЕСЕРИАЛИЗАЦИЯ — используется SaveManager'ом
    public string SerializeReports()
    {
        if (allReports.Count == 0) return "";

        var dayStrings = new List<string>();

        foreach (var day in allReports)
        {
            var resultStrings = new List<string>();
            foreach (var r in day.results)
            {
                // Просто и надёжно: используем символы, которые НИКОГДА не будут в именах
                resultStrings.Add($"{r.displayName}|{r.mineralClassName}|{(r.wasCorrect ? 1 : 0)}");
            }
            dayStrings.Add($"{day.dayNumber}:{string.Join(";", resultStrings)}");
        }

        return string.Join("|", dayStrings);
    }

    public void DeserializeReports(string data)
    {
        allReports.Clear();
        if (string.IsNullOrEmpty(data)) return;

        var dayEntries = data.Split('|');
        foreach (var entry in dayEntries)
        {
            if (string.IsNullOrEmpty(entry)) continue;

            var colonParts = entry.Split(new[] { ':' }, 2);
            if (colonParts.Length != 2 || !int.TryParse(colonParts[0], out int dayNum)) continue;

            var dayReport = new DayReport { dayNumber = dayNum };
            var resultStrings = colonParts[1].Split(';');

            foreach (var r in resultStrings)
            {
                if (string.IsNullOrEmpty(r)) continue;

                var parts = r.Split('|');
                if (parts.Length < 2) continue;

                dayReport.results.Add(new MineralResearchResult
                {
                    displayName = parts[0],
                    mineralClassName = parts[1],
                    wasCorrect = parts.Length > 2 && parts[2] == "1"
                });
            }

            allReports.Add(dayReport);
        }

        allReports.Sort((a, b) => a.dayNumber.CompareTo(b.dayNumber));
    }

  
// Защита от спецсимволов (на всякий случай)
private string Escape(string s) => s.Replace("¬", "&#xAC;").Replace("|", "&#x7C;").Replace(":", "&#x3A;");
private string Unescape(string s) => s.Replace("&#xAC;", "¬").Replace("&#x7C;", "|").Replace("&#x3A;", ":");

#if UNITY_EDITOR
[ContextMenu("Открыть панель (тест)")]
private void TestOpen() => OpenPanel();

[ContextMenu("Очистить все отчёты (тест)")]
private void ClearAll() => allReports.Clear();
#endif
}