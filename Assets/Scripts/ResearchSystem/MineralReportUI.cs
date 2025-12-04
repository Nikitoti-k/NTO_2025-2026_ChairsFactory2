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
        [HideInInspector] public TextMeshProUGUI tmp; // Ссылка на текст кнопки
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

            // Находим TMP в кнопке
            link.tmp = link.button.GetComponentInChildren<TextMeshProUGUI>();
            if (link.tmp == null)
            {
                Debug.LogWarning($"Нет TextMeshProUGUI на кнопке {link.button.name}");
                continue;
            }

            // Делаем текст многострочным и красивым
            link.tmp.fontSize = 18; // Основной размер
            link.tmp.fontSizeMax = 18;
            link.tmp.enableWordWrapping = true;
            link.tmp.alignment = TextAlignmentOptions.TopLeft;

            // Первичное обновление
            UpdateButtonAppearance(link);

            // Клик
            MineralClass mc = link.mineralClass;
            link.button.onClick.RemoveAllListeners();
            link.button.onClick.AddListener(() => SelectClass(mc));

            // Подсказка при наведении (опционально — можно убрать)
            var trigger = link.button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var entry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
            entry.callback.AddListener((data) => UpdateClassDetails(mc));
            trigger.triggers.Add(entry);

            var exit = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
            exit.callback.AddListener((data) => { if (selectedClass == null) classDetailsText.text = LocalizationManager.Loc("REPORT_SELECT_CLASS"); });
            trigger.triggers.Add(exit);
        }

        Localize();
    }

    // Обновляет и название, и параметры прямо в кнопке
    private void UpdateButtonAppearance(ClassButtonLink link)
    {
        if (link.tmp == null || link.mineralClass == null) return;

        string name = link.mineralClass.LocalizedName;
        string details;

        if (link.mineralClass.isAnomalyClass)
        {
            details = $"<color=#FF2222>«АНОМАЛИЯ»</color>";
        }
        else
        {
            string ageRange = link.mineralClass.ageMin == link.mineralClass.ageMax
                ? $"{link.mineralClass.ageMin:F0}"
                : $"{link.mineralClass.ageMin:F0}–{link.mineralClass.ageMax:F0}";

            string ageUnit = link.mineralClass.ageUnit == MineralClass.AgeUnit.Days
                ? "дн."
                : "млн лет";

            string radRange = link.mineralClass.radioactivityMin == link.mineralClass.radioactivityMax
                ? $"{link.mineralClass.radioactivityMin:F3}"
                : $"{link.mineralClass.radioactivityMin:F3}–{link.mineralClass.radioactivityMax:F3}";

            string crystal = LocalizationManager.Loc(GetCrystalKey(link.mineralClass.crystalSystem));

            details = $"Возраст: {ageRange} {ageUnit}\nРад.: {radRange} µSv/h\nКристалл: {crystal}";
        }

        // ВСЁ ЧЁРНОЕ, один шрифт, только название чуть крупнее
        link.tmp.text =
            $"<size=25><b>{name}</b></size>\n" +
            $"<size=25><color=black>{details}</color></size>";

        // Принудительно чёрный цвет (на случай если где-то остался старый)
        link.tmp.color = Color.black;
    }

    public void Localize()
    {
        foreach (var link in classButtonLinks)
            UpdateButtonAppearance(link);
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
                if (link.tmp) link.tmp.color = isAnomalyClass ? Color.red : new Color(0.5f, 0.5f, 0.5f, 0.7f);
            }
            else
            {
                link.button.interactable = true;
                if (link.tmp) link.tmp.color = Color.white;
            }
        }
    }

    private void SelectClass(MineralClass mc)
    {
        selectedClass = mc;
        UpdateClassDetails(mc);
        statusText.text = string.Format(LocalizationManager.Loc("REPORT_SELECTED"), $"<color=yellow>{mc.LocalizedName}</color>");

        // Подсветка выбранной кнопки
        foreach (var link in classButtonLinks)
        {
            bool selected = link.mineralClass == mc;
            var colors = link.button.colors;
            colors.normalColor = selected ? new Color(0.2f, 0.8f, 1f) : Color.white;
            colors.highlightedColor = selected ? new Color(0.3f, 0.9f, 1f) : new Color(0.9f, 0.9f, 0.9f);
            link.button.colors = colors;
        }
    }

    // Это теперь дублирует то, что в кнопке — но можно оставить для детального отображения
    private void UpdateClassDetails(MineralClass mc)
    {
        if (mc.isAnomalyClass)
        {
            classDetailsText.text = string.Format(LocalizationManager.Loc("REPORT_CLASS_DETAILS_ANOMALY"), mc.LocalizedName);
            return;
        }

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

    // Остальное без изменений (Submit, ClosePanel, UpdateMeasuredData, GetCrystalKey...)

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

        measuredDataText.text = string.Format(
            LocalizationManager.Loc("REPORT_MEASURED_DATA"),
            ageValue,
            currentSample.AgeUnitText,
            radValue,
            crystal
        );
    }

    private string GetCrystalKey(MineralData.CrystalSystem system) => system switch
    {
        MineralData.CrystalSystem.Cubic => "CRYSTAL_CUBIC",
        MineralData.CrystalSystem.Molecular => "CRYSTAL_MOLECULAR",
        MineralData.CrystalSystem.Monoclinic => "CRYSTAL_MONOCLINIC",
        _ => "CRYSTAL_AMORPHOUS"
    };
}