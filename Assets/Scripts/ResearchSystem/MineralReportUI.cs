using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class MineralReportUI : MonoBehaviour
{
    public event Action<bool> OnReportSubmitted;
    public event Action OnReportCancelled;

    [Header("UI Элементы")]
    [SerializeField] private Button[] classButtons = new Button[7];
    [SerializeField] private Button submitButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Тексты")]
    [SerializeField] private TextMeshProUGUI measuredDataText;     
    [SerializeField] private TextMeshProUGUI classDetailsText;   

    [Header("Все классы (7 SO)")]
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
                btnText.text = mc.className;

            classButtons[i].onClick.AddListener(() => SelectClass(mc));
        }
    }

    public void StartReport(MineralData mineral)
    {
        currentSample = mineral;
        correctClass = mineral.MineralClassSO;
        selectedClass = null;

        UpdateMeasuredData();
        classDetailsText.text = "Выберите класс для просмотра характеристик";
        statusText.text = "Выберите правильный класс";
        ResetButtonHighlights();
    }

    private void SelectClass(MineralClass mc)
    {
        selectedClass = mc;
        UpdateClassDetails(mc);
        statusText.text = $"Выбран: <color=yellow>{mc.className}</color>\nНажмите «Отправить отчёт»";
        HighlightSelectedButton(mc);
    }

    
    private void UpdateMeasuredData()
    {
        if (currentSample == null) return;

        string ageStr = currentSample.AgeMya.ToString("F1");
        string radStr = currentSample.RadioactivityUsv.ToString("F3");
        string crystal = GetCrystalName(currentSample.CrystalSystem_);

        measuredDataText.text =
            $"<b>ИЗМЕРЕННЫЕ ДАННЫЕ:</b>\n\n" +
            $"Возраст: <color=#FFD700>{ageStr}</color> {currentSample.AgeUnitText}\n" +
            $"Радиация: <color=#FF6666>{radStr}</color> Бк\n" +
            $"Решётка: <color=#CC66FF>{crystal}</color>";
    }

    
    private void UpdateClassDetails(MineralClass mc)
    {
        string ageRange = mc.ageMin == mc.ageMax
            ? $"{mc.ageMin:F0}"
            : $"{mc.ageMin:F0}–{mc.ageMax:F0}";

        string radRange = mc.radioactivityMin == mc.radioactivityMax
            ? $"{mc.radioactivityMin:F3}"
            : $"{mc.radioactivityMin:F3}–{mc.radioactivityMax:F3}";

        string unit = mc.ageUnit == MineralClass.AgeUnit.Days ? "дней" : "млн лет";
        string crystal = GetCrystalName(mc.crystalSystem);

        classDetailsText.text =
            $"<b><size=120%>{mc.className}</size></b>\n\n" +
            $"Возраст: <color=#66CCFF>{ageRange} {unit}</color>\n" +
            $"Радиация: <color=#66CCFF>{radRange} Бк</color>\n" +
            $"Решётка: <color=#CC66FF>{crystal}</color>";
    }

    private void Submit()
    {
        if (selectedClass == null)
        {
            statusText.text = "<color=red>Сначала выберите класс!</color>";
            return;
        }

        bool correct = selectedClass == correctClass;
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
            var colors = classButtons[i].colors;
           
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

    private string GetCrystalName(MineralData.CrystalSystem system)
    {
        return system switch
        {
            MineralData.CrystalSystem.Cubic => "кубическая",
            MineralData.CrystalSystem.Trigonal => "тригональная",
            MineralData.CrystalSystem.Monoclinic => "моноклинная",
            
          
            _ => "неизвестная"
        };
    }
}