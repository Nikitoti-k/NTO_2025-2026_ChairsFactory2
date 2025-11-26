using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class ResearchReportViewer : MonoBehaviour, ISaveable
{
    [Header("UI")]
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
        public string mineralClassName; // опционально, для красоты
    }

    [System.Serializable]
    private class DayReport
    {
        public int dayNumber;
        public List<MineralResearchResult> results = new();
    }

    // Все отчёты за все дни
    private readonly List<DayReport> allReports = new();
    private int currentViewedDayIndex = -1;

    private void Awake()
    {
        if (reportPanel) reportPanel.SetActive(false);
        if (noReportsText) noReportsText.gameObject.SetActive(true);
        if (closeButton) closeButton.onClick.AddListener(ClosePanel);
        if (prevDayButton) prevDayButton.onClick.AddListener(ShowPreviousDay);
        if (nextDayButton) nextDayButton.onClick.AddListener(ShowNextDay);
    }

    private void OnEnable()
    {
        GameDayManager.Instance?.OnDayFullyCompleted.AddListener(OnDayCompleted);
    }

    private void OnDisable()
    {
        GameDayManager.Instance?.OnDayFullyCompleted.RemoveListener(OnDayCompleted);
        closeButton?.onClick.RemoveListener(ClosePanel);
        prevDayButton?.onClick.RemoveListener(ShowPreviousDay);
        nextDayButton?.onClick.RemoveListener(ShowNextDay);
    }

    private void OnDayCompleted()
    {
        int today = GameDayManager.Instance.CurrentDay;
        var currentDayReport = allReports.Find(r => r.dayNumber == today);
        if (currentDayReport != null)
        {
            // День завершён — фиксируем результаты
            currentDayReport.results = currentDayReport.results.ToList(); // защита от изменений
        }
    }

    public static void LogResearchResult(string mineralName, bool correct, string className = "")
    {
        var viewer = FindObjectOfType<ResearchReportViewer>();
        if (!viewer || !GameDayManager.Instance) return;

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
            displayName = $"Образец №{sampleNum}",
            wasCorrect = correct,
            mineralClassName = className
        });
    }

    public void OpenPanel()
    {
        if (!reportPanel) return;

        reportPanel.SetActive(true);
        CameraController.Instance.SetMode(CameraController.ControlMode.UI);

        // Показываем последний завершённый день
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
            noReportsText.text = "Нет завершённых отчётов.\nИсследуй минералы и заверши день!";
            titleText.text = "Результаты исследований";
            dayCounterText.text = "";
            prevDayButton.interactable = nextDayButton.interactable = false;
            return;
        }

        var report = allReports[currentViewedDayIndex];
        noReportsText.gameObject.SetActive(false);
        titleText.text = $"День {report.dayNumber}: Результаты";
        dayCounterText.text = $"{currentViewedDayIndex + 1} / {allReports.Count}";

        prevDayButton.interactable = currentViewedDayIndex > 0;
        nextDayButton.interactable = currentViewedDayIndex < allReports.Count - 1;

        foreach (var result in report.results)
        {
            var entry = Instantiate(reportEntryPrefab, contentParent);
            var text = entry.GetComponentInChildren<TextMeshProUGUI>();
            var img = entry.GetComponent<Image>();

            if (text) text.text = $"{result.displayName}\n<size=70%>{result.mineralClassName}</size>";
            if (img)
            {
                img.color = result.wasCorrect
                    ? new Color(0.1f, 0.8f, 0.1f, 0.95f)
                    : new Color(0.8f, 0.2f, 0.2f, 0.95f);
            }
        }
    }

    // ISaveable
    public string GetUniqueID() => "RESEARCH_REPORT_VIEWER";
    public SaveData GetSaveData()
    {
        var data = new SaveData
        {
            uniqueID = "RESEARCH_REPORT_VIEWER",
            prefabIdentifier = "ResearchSystem",
            customString1 = string.Join("|",
                allReports.Select(day => $"{day.dayNumber}:{string.Join(";", day.results.Select(r => $"{r.displayName}¬{r.mineralClassName}¬{(r.wasCorrect ? 1 : 0)}"))}"))
        };
        return data;
    }

    public void LoadFromSaveData(SaveData data)
    {
        allReports.Clear();
        if (string.IsNullOrEmpty(data.customString1)) return;

        var dayEntries = data.customString1.Split('|');
        foreach (var entry in dayEntries)
        {
            if (string.IsNullOrEmpty(entry)) continue;
            var parts = entry.Split(':');
            if (parts.Length != 2) continue;

            if (!int.TryParse(parts[0], out int dayNum)) continue;

            var dayReport = new DayReport { dayNumber = dayNum };
            var resultStrings = parts[1].Split(';');
            foreach (var r in resultStrings)
            {
                if (string.IsNullOrEmpty(r)) continue;
                var sub = r.Split('¬');
                if (sub.Length >= 2)
                {
                    dayReport.results.Add(new MineralResearchResult
                    {
                        displayName = sub[0],
                        mineralClassName = sub[1],
                        wasCorrect = sub.Length > 2 && sub[2] == "1"
                    });
                }
            }
            allReports.Add(dayReport);
        }

        allReports.Sort((a, b) => a.dayNumber.CompareTo(b.dayNumber));
    }

#if UNITY_EDITOR
    [ContextMenu("Открыть панель (тест)")]
    private void TestOpen() => OpenPanel();

    [ContextMenu("Очистить все отчёты (тест)")]
    private void ClearAll() => allReports.Clear();
#endif
}