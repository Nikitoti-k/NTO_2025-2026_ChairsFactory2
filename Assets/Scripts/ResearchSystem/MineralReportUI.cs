using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class MineralReportUI : MonoBehaviour, ILocalizable
{
    public event Action<bool> OnReportSubmitted;
    public event Action OnReportCancelled;

    [Header("UI Элементы")]
    [SerializeField] private Button submitButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI measuredDataText;
    [SerializeField] private TextMeshProUGUI classDetailsText;

    [Header("Кнопки с классами")]
    [SerializeField] private List<ClassButtonLink> classButtonLinks = new List<ClassButtonLink>(5);

    [System.Serializable]
    public class ClassButtonLink
    {
        public Button button;
        public MineralClass mineralClass;
        [HideInInspector] public TextMeshProUGUI tmp;
    }

    private MineralData currentSample;
    private MineralClass selectedClass;
    private MineralClass correctClass;

    private void Awake()
    {
        closeButton.onClick.AddListener(ClosePanel);
        submitButton.onClick.AddListener(Submit);

        foreach (var link in classButtonLinks)
        {
            if (link.button == null || link.mineralClass == null) continue;

            link.tmp = link.button.GetComponentInChildren<TextMeshProUGUI>();
            if (link.tmp == null)
            {
                Debug.LogWarning($"Нет TextMeshProUGUI на кнопке {link.button.name}");
                continue;
            }

            link.tmp.fontSize = 19;
            link.tmp.fontSizeMax = 19;
            link.tmp.enableWordWrapping = true;
            link.tmp.alignment = TextAlignmentOptions.TopLeft;
            link.tmp.color = Color.black;

            MineralClass mc = link.mineralClass;
            link.button.onClick.RemoveAllListeners();
            link.button.onClick.AddListener(() => SelectClass(mc));

            var trigger = link.button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var enter = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
            enter.callback.AddListener((data) => UpdateClassDetails(mc));
            trigger.triggers.Add(enter);

            var exit = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
            exit.callback.AddListener((data) => { if (selectedClass == null) classDetailsText.text = LocalizationManager.Loc("REPORT_SELECT_CLASS"); });
            trigger.triggers.Add(exit);
        }
    }

    private void Start()
    {
        Localize();
    }

    private void UpdateButtonAppearance(ClassButtonLink link)
    {
        if (link.tmp == null || link.mineralClass == null) return;

        string name = link.mineralClass.LocalizedName;
        string details;

        if (link.mineralClass.isAnomalyClass)
        {
            details = LocalizationManager.Loc("REPORT_UNKNOWN");
        }
        else
        {
            string ageRange = link.mineralClass.ageMin == link.mineralClass.ageMax
                ? $"{link.mineralClass.ageMin:F0}"
                : $"{link.mineralClass.ageMin:F0}–{link.mineralClass.ageMax:F0}";

            string ageUnitKey = link.mineralClass.ageUnit == MineralClass.AgeUnit.Days
                ? "REPORT_AGE_UNIT_DAYS"
                : "REPORT_AGE_UNIT_MILLION";

            string radRange = link.mineralClass.radioactivityMin == link.mineralClass.radioactivityMax
                ? $"{link.mineralClass.radioactivityMin:F3}"
                : $"{link.mineralClass.radioactivityMin:F3}–{link.mineralClass.radioactivityMax:F3}";

            string crystal = LocalizationManager.Loc(GetCrystalKey(link.mineralClass.crystalSystem));

            details =
                $"{LocalizationManager.Loc("BUTTON_AGE")}: {ageRange} {LocalizationManager.Loc(ageUnitKey)}\n" +
                $"{LocalizationManager.Loc("BUTTON_RAD")}: {radRange} µSv/h\n" +
                $"{LocalizationManager.Loc("BUTTON_CRYSTAL")}: {crystal}";
        }

        link.tmp.text = $"<size=28><b>{name}</b></size>\n<size=19>{details}</size>";
        link.tmp.color = Color.black;
    }

    public void Localize()
    {
        foreach (var link in classButtonLinks)
            UpdateButtonAppearance(link);

        if (selectedClass != null)
            UpdateClassDetails(selectedClass);
        else
            classDetailsText.text = LocalizationManager.Loc("REPORT_SELECT_CLASS");

        if (currentSample != null)
            UpdateMeasuredData();

        statusText.text = selectedClass == null
            ? LocalizationManager.Loc("REPORT_CHOOSE_CORRECT")
            : string.Format(LocalizationManager.Loc("REPORT_SELECTED"), $"<color=yellow>{selectedClass.LocalizedName}</color>");
    }

    private void OnEnable()
    {
        LocalizationManager.Register(this);
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
        Localize();
    }

    private void OnDisable()
    {
        LocalizationManager.Unregister(this);
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(LocalizationManager.Language lang) => Localize();

    public void StartReport(MineralData mineral)
    {
        currentSample = mineral;
        correctClass = mineral.MineralClassSO;
        selectedClass = null;

        UpdateMeasuredData();
        classDetailsText.text = LocalizationManager.Loc("REPORT_SELECT_CLASS");
        statusText.text = LocalizationManager.Loc("REPORT_CHOOSE_CORRECT");
        ResetButtonHighlights();
        UpdateClassButtonsState();
        Localize(); // Гарантированное обновление кнопок
    }

    private void UpdateClassButtonsState()
    {
        bool isAnomaly = correctClass != null && correctClass.isAnomalyClass;
        bool isFirstDay = GameDayManager.Instance != null && GameDayManager.Instance.CurrentDay == 1;
        bool lockNormal = isAnomaly && isFirstDay;

        foreach (var link in classButtonLinks)
        {
            if (link.button == null || link.mineralClass == null) continue;

            bool isAnomalyClass = link.mineralClass.isAnomalyClass;

            if (lockNormal)
            {
                link.button.interactable = isAnomalyClass;
                if (link.tmp)
                    link.tmp.color = isAnomalyClass ? Color.black : new Color(0.3f, 0.3f, 0.3f, 0.6f);
            }
            else
            {
                link.button.interactable = true;
                if (link.tmp)
                    link.tmp.color = Color.black;
            }
        }
    }

    private void SelectClass(MineralClass mc)
    {
        selectedClass = mc;
        UpdateClassDetails(mc);
        statusText.text = string.Format(LocalizationManager.Loc("REPORT_SELECTED"), $"<color=yellow>{mc.LocalizedName}</color>");

        foreach (var link in classButtonLinks)
        {
            bool selected = link.mineralClass == mc;
            var colors = link.button.colors;
            colors.normalColor = selected ? new Color(0.2f, 0.8f, 1f) : Color.white;
            colors.highlightedColor = selected ? new Color(0.3f, 0.9f, 1f) : new Color(0.9f, 0.9f, 0.9f);
            link.button.colors = colors;
        }
    }

    private void UpdateClassDetails(MineralClass mc)
    {
        if (mc.isAnomalyClass)
        {
            classDetailsText.text = string.Format(LocalizationManager.Loc("REPORT_CLASS_DETAILS_ANOMALY"), mc.LocalizedName);
        }
        else
        {
            string ageRange = mc.ageMin == mc.ageMax ? $"{mc.ageMin:F0}" : $"{mc.ageMin:F0}–{mc.ageMax:F0}";
            string radRange = mc.radioactivityMin == mc.radioactivityMax
                ? $"{mc.radioactivityMin:F3}"
                : $"{mc.radioactivityMin:F3}–{mc.radioactivityMax:F3}";

            string unitKey = mc.ageUnit == MineralClass.AgeUnit.Days ? "REPORT_AGE_UNIT_DAYS" : "REPORT_AGE_UNIT_MILLION";
            string crystal = LocalizationManager.Loc(GetCrystalKey(mc.crystalSystem));

            classDetailsText.text = string.Format(
                LocalizationManager.Loc("REPORT_CLASS_DETAILS"),
                mc.LocalizedName,
                ageRange,
                LocalizationManager.Loc(unitKey),
                radRange,
                crystal
            );
        }
    }

    private void Submit()
    {
        if (selectedClass == null)
        {
            statusText.text = LocalizationManager.Loc("REPORT_SELECT_FIRST");
            return;
        }

        bool correct = selectedClass == correctClass;
        if (selectedClass.isAnomalyClass)
            currentSample.isAnomaly = true;

        if (selectedClass.isAnomalyClass && TutorialManager.Instance != null)
            TutorialManager.Instance.OnAnomalyReportSubmitted();

        OnReportSubmitted?.Invoke(correct);
        gameObject.SetActive(false);
    }

    private void ClosePanel()
    {
        OnReportCancelled?.Invoke();
        gameObject.SetActive(false);
    }

    private void ResetButtonHighlights()
    {
        foreach (var link in classButtonLinks)
        {
            if (link.button == null) continue;
            var colors = link.button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
            link.button.colors = colors;
        }
    }

    private void UpdateMeasuredData()
    {
        if (currentSample == null) return;

        string ageValue = currentSample.AgeMya.ToString("F1");
        string radValue = currentSample.RadioactivityUsv.ToString("F3");
        string crystal = LocalizationManager.Loc(GetCrystalKey(currentSample.CrystalSystem_));

        // Полностью чёрный текст, без цветных вставок
        measuredDataText.text =
            $"<b>MEASURED DATA:</b>\n\n" +
            $"Age: {ageValue} {currentSample.AgeUnitText}\n" +
            $"Radiation: {radValue} Bq\n" +
            $"Crystal system: {crystal}";

        measuredDataText.color = Color.black;
    }

    private string GetCrystalKey(MineralData.CrystalSystem system) => system switch
    {
        MineralData.CrystalSystem.Cubic => "CRYSTAL_CUBIC",
        MineralData.CrystalSystem.Molecular => "CRYSTAL_MOLECULAR",
        MineralData.CrystalSystem.Monoclinic => "CRYSTAL_MONOCLINIC",
        _ => "CRYSTAL_AMORPHOUS"
    };
}