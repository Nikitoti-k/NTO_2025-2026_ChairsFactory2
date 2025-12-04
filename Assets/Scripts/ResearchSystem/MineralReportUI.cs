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
    [SerializeField] private Button[] classButtons = new Button[5];
    [SerializeField] private Button submitButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Тексты")]
    [SerializeField] private TextMeshProUGUI measuredDataText;
    [SerializeField] private TextMeshProUGUI classDetailsText;

    [Header("Все классы")]
    [SerializeField] private List<MineralClass> allClasses = new List<MineralClass>(7);

    private MineralData currentSample;
    private MineralClass selectedClass;
    private MineralClass correctClass;

    private void Awake()
    {
        closeButton.onClick.AddListener(ClosePanel);
        submitButton.onClick.AddListener(Submit);

        for (int i = 0; i < classButtons.Length && i < allClasses.Count; i++)
        {
            int idx = i;
            MineralClass mc = allClasses[i];

            TextMeshProUGUI btnText = classButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
                btnText.text = mc.LocalizedName; // ← Только твои NAME_MINERAL_CLASS_*

            classButtons[i].onClick.RemoveAllListeners();
            classButtons[i].onClick.AddListener(() => SelectClass(mc));
        }

        Localize();
    }

    public void Localize()
    {
        for (int i = 0; i < classButtons.Length && i < allClasses.Count; i++)
        {
            TextMeshProUGUI btnText = classButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
                btnText.text = allClasses[i].LocalizedName;
        }
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

        bool shouldLockNormal = isAnomaly && isFirstDay;

        for (int i = 0; i < classButtons.Length && i < allClasses.Count; i++)
        {
            Button btn = classButtons[i];
            MineralClass mc = allClasses[i];
            TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();

            if (shouldLockNormal)
            {
                if (mc.isAnomalyClass)
                {
                    btn.interactable = true;
                    if (txt) txt.color = Color.red;
                }
                else
                {
                    btn.interactable = false;
                    if (txt) txt.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
                }
            }
            else
            {
                btn.interactable = true;
                if (txt) txt.color = Color.white;
            }
        }
    }

    private void SelectClass(MineralClass mc)
    {
        selectedClass = mc;
        UpdateClassDetails(mc);
        statusText.text = string.Format(LocalizationManager.Loc("REPORT_SELECTED"), $"<color=yellow>{mc.LocalizedName}</color>");
        HighlightSelectedButton(mc);
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
            currentSample.AgeUnitText,  // Это уже локализовано в MineralData, или используй ниже
            radValue,
            crystal
        );
    }

    private void UpdateClassDetails(MineralClass mc)
    {
        if (mc.isAnomalyClass)
        {
            classDetailsText.text = string.Format(
                LocalizationManager.Loc("REPORT_CLASS_DETAILS_ANOMALY"),
                mc.LocalizedName
            );
            return;
        }

        string ageRange = mc.ageMin == mc.ageMax
            ? $"{mc.ageMin:F0}"
            : $"{mc.ageMin:F0}–{mc.ageMax:F0}";

        string radRange = mc.radioactivityMin == mc.radioactivityMax
            ? $"{mc.radioactivityMin:F3}"
            : $"{mc.radioactivityMin:F3}–{mc.radioactivityMax:F3}";

        string unitKey = mc.ageUnit == MineralClass.AgeUnit.Days
            ? "REPORT_AGE_UNIT_DAYS"
            : "REPORT_AGE_UNIT_MILLION";

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

    private void HighlightSelectedButton(MineralClass mc)
    {
        for (int i = 0; i < classButtons.Length; i++)
        {
            if (i >= allClasses.Count) break;
            bool isSelected = allClasses[i] == mc;
            var colors = classButtons[i].colors;
            colors.normalColor = isSelected ? new Color(0.2f, 0.8f, 1f) : Color.white;
            colors.highlightedColor = isSelected ? new Color(0.3f, 0.9f, 1f) : new Color(0.9f, 0.9f, 0.9f);
            classButtons[i].colors = colors;
        }
    }

    private void ResetButtonHighlights()
    {
        foreach (var btn in classButtons)
        {
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
            btn.colors = colors;
        }
    }

    private string GetCrystalKey(MineralData.CrystalSystem system) => system switch
    {
        MineralData.CrystalSystem.Cubic => "CRYSTAL_CUBIC",
        MineralData.CrystalSystem.Molecular => "CRYSTAL_MOLECULAR",
        MineralData.CrystalSystem.Monoclinic => "CRYSTAL_MONOCLINIC",
        _ => "CRYSTAL_AMORPHOUS"
    };
}