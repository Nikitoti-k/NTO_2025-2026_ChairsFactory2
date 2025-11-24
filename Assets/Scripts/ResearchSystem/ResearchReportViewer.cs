using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class ResearchReportViewer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject reportPanel;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject reportEntryPrefab;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI noReportsText;

    [Header("Кнопка закрытия")]
    [SerializeField] private Button closeButton;

    [Header("Взаимодействие")]
    [SerializeField] private float interactionDistance = 3f;

    private List<MineralResearchResult> yesterdayResults = new();
    private List<MineralResearchResult> currentDayResults = new();

    private struct MineralResearchResult
    {
        public string mineralName;
        public bool wasCorrect;
    }
    private static int globalSampleCounter = 1;           
    private static int currentDaySampleCounter = 1;      
    private static int lastSavedDay = -1;                   

    private void SaveYesterdayResults()
    {
        yesterdayResults = new List<MineralResearchResult>(currentDayResults);
        currentDayResults.Clear();

        
        currentDaySampleCounter = 1;

        Debug.Log($"<b>[ResearchReportViewer]</b> <color=lime>Сохранено {yesterdayResults.Count} результатов за вчера!</color>");
        UpdatePanelVisuals();
    }

    public static void LogResearchResult(string mineralName, bool correct)
    {
        var viewer = FindObjectsOfType<ResearchReportViewer>().FirstOrDefault();
        if (viewer != null)
        {
           
            int today = GameDayManager.Instance ? GameDayManager.Instance.CurrentDay : 1;

            
            if (today != lastSavedDay + 1)
                currentDaySampleCounter = 1;

            lastSavedDay = today;

            string displayName = $"Образец №{currentDaySampleCounter}";

            viewer.currentDayResults.Add(new MineralResearchResult
            {
                mineralName = displayName,
                wasCorrect = correct
            });

            currentDaySampleCounter++; 
        }
    }
    private void Awake()
    {
        if (reportPanel != null) reportPanel.SetActive(false);
        if (noReportsText != null) noReportsText.gameObject.SetActive(true);

        
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    private void OnEnable()
    {
        if (GameDayManager.Instance)
            GameDayManager.Instance.OnDayFullyCompleted.AddListener(SaveYesterdayResults);
    }

    private void OnDisable()
    {
        if (GameDayManager.Instance)
            GameDayManager.Instance.OnDayFullyCompleted.RemoveListener(SaveYesterdayResults);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePanel);
    }

   

   

    public void OpenPanel()
    {
        if (reportPanel == null) return;

        reportPanel.SetActive(true);
        CameraController.Instance.SetMode(CameraController.ControlMode.UI); 
        UpdatePanelVisuals();
        Debug.Log("<b>[ResearchReportViewer]</b> Панель открыта — UI режим включён");
    }

    public void ClosePanel()
    {
        if (reportPanel == null) return;

        reportPanel.SetActive(false);
        CameraController.Instance.SetMode(CameraController.ControlMode.FPS); 
        Debug.Log("<b>[ResearchReportViewer]</b> Панель закрыта — FPS режим восстановлен");
    }

    public void TogglePanel()
    {
        if (reportPanel.activeSelf)
            ClosePanel();
        else
            OpenPanel();
    }

    private void UpdatePanelVisuals()
    {
        if (contentParent != null)
        {
            foreach (Transform child in contentParent) Destroy(child.gameObject);
        }

        if (yesterdayResults.Count == 0)
        {
            if (noReportsText != null)
            {
                noReportsText.text = "Отчётов за вчера нет.\nЭто первый день экспедиции.";
                noReportsText.gameObject.SetActive(true);
            }
            if (titleText != null)
                titleText.text = "Результаты исследований";
            return;
        }

        if (noReportsText != null) noReportsText.gameObject.SetActive(false);
        if (titleText != null)
            titleText.text = $"Результаты за день {GameDayManager.Instance.CurrentDay}";

        foreach (var result in yesterdayResults)
        {
            var entry = Instantiate(reportEntryPrefab, contentParent);
            var img = entry.GetComponent<Image>();
            var text = entry.GetComponentInChildren<TextMeshProUGUI>();

            if (text != null) text.text = result.mineralName;
            if (img != null)
            {
                img.color = result.wasCorrect
                    ? new Color(0.1f, 0.9f, 0.1f, 0.95f)
                    : new Color(0.9f, 0.2f, 0.2f, 0.95f);
            }
        }
    }

   

#if UNITY_EDITOR
    [ContextMenu("Открыть панель (тест)")]
    private void TestOpen() => OpenPanel();
#endif

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}