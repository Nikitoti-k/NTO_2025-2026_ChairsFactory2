using UnityEngine;
using System;

[RequireComponent(typeof(MineralPointSpawner))]
public class MineralData : MonoBehaviour
{
    public enum CrystalSystem { Cubic, Monoclinic, Molecular, Amorphous }

    [System.Serializable]
    public class MineralSaveData
    {
        public float realAge;
        public float realRadioactivity;
        public Vector3 agePointLocalPos;
        public Vector3 crystalPointLocalPos;
        public Vector3 radioactivityPointLocalPos;
        public bool isResearched;
        public bool isTutorialHighlighted;
        public bool isLastInTutorialQueue;
    }

    [Header("════ КЛАСС МИНЕРАЛА ════")]
    [SerializeField] private MineralClass mineralClass;

    [Header("════ ДЛЯ АНОМАЛИЙ — РУЧНАЯ НАСТРОЙКА ════")]
    [Tooltip("Ставь галку ТОЛЬКО у аномалий — тогда значения ниже переопределяют класс")]
    public bool isAnomalyOverride = false;

    [Space]
    [Tooltip("Ручной возраст (например: 0.001, -500, 999999)")]
    public float overrideAge = 0.001f;
    [Tooltip("Дни или миллионы лет?")]
    public MineralClass.AgeUnit overrideAgeUnit = MineralClass.AgeUnit.Days;
    [Tooltip("Ручная радиация (например: 99999.999)")]
    public float overrideRadioactivity = 99999f;
    [Tooltip("Ручная кристаллическая решётка")]
    public CrystalSystem overrideCrystalSystem = CrystalSystem.Molecular;

    [Header("════ ТОЧКИ СКАНИРОВАНИЯ ════")]
    public ScanPoint AgePoint;
    public ScanPoint CrystalPoint;
    public ScanPoint RadioactivityPoint;

    [Header("════ TUTORIAL ════")]
    [SerializeField] private Outline tutorialOutline;

    // ────────────────────────
    // УМНЫЕ СВОЙСТВА — что видит игрок
    // ────────────────────────

    public float AgeMya => isAnomalyOverride
        ? overrideAge
        : realAge + UnityEngine.Random.Range(-mineralClass.ageError, mineralClass.ageError);

    public float RadioactivityUsv => isAnomalyOverride
        ? overrideRadioactivity
        : realRadioactivity + UnityEngine.Random.Range(-mineralClass.radioactivityError, mineralClass.radioactivityError);

    public CrystalSystem CrystalSystem_ => isAnomalyOverride
        ? overrideCrystalSystem
        : mineralClass.crystalSystem;

    // ← КЛЮЧЕВОЕ ИСПРАВЛЕНИЕ: теперь единицы полностью локализованы!
    public string AgeUnitText
    {
        get
        {
            MineralClass.AgeUnit unit = isAnomalyOverride ? overrideAgeUnit : mineralClass.ageUnit;
            return unit == MineralClass.AgeUnit.Days
                ? LocalizationManager.Loc("REPORT_AGE_UNIT_DAYS")
                : LocalizationManager.Loc("REPORT_AGE_UNIT_MILLION");
        }
    }

    public string ClassName => mineralClass != null ? mineralClass.localizationKey : "Неизвестно";
    public MineralClass MineralClassSO => mineralClass;

    // Базовые значения (генерируются для обычных минералов)
    public float realAge;
    public float realRadioactivity;
    public string UniqueInstanceID;
    public bool isResearched = false;
    public bool isTutorialHighlighted = false;
    public bool isLastInTutorialQueue = false;

    [HideInInspector] public bool isAnomaly = false; // ставится при выборе класса «Аномалия»

    // Сохранённые строки сканирования (уже с префиксами и локализацией)
    [HideInInspector] public string savedAgeLine = "";
    [HideInInspector] public string savedCrystalLine = "";
    [HideInInspector] public string savedRadioactivityLine = "";

    private void Awake()
    {
        if (string.IsNullOrEmpty(UniqueInstanceID))
            UniqueInstanceID = Guid.NewGuid().ToString();

        if (tutorialOutline == null)
            tutorialOutline = GetComponentInChildren<Outline>();
    }

    public void EnableTutorialOutline(bool enable)
    {
        if (tutorialOutline == null) tutorialOutline = GetComponentInChildren<Outline>();
        if (tutorialOutline != null) tutorialOutline.enabled = enable;
    }

    public void SetAsLastInTutorialQueue(bool isLast) => isLastInTutorialQueue = isLast;

    public void GenerateData()
    {
        if (mineralClass == null || mineralClass.isAnomalyClass || isAnomalyOverride) return;

        realAge = UnityEngine.Random.Range(mineralClass.ageMin, mineralClass.ageMax);
        realRadioactivity = UnityEngine.Random.Range(mineralClass.radioactivityMin, mineralClass.radioactivityMax);
    }

    // Save / Load
    public MineralSaveData GetMineralSaveData() => new MineralSaveData
    {
        realAge = realAge,
        realRadioactivity = realRadioactivity,
        agePointLocalPos = AgePoint ? AgePoint.transform.localPosition : Vector3.zero,
        crystalPointLocalPos = CrystalPoint ? CrystalPoint.transform.localPosition : Vector3.zero,
        radioactivityPointLocalPos = RadioactivityPoint ? RadioactivityPoint.transform.localPosition : Vector3.zero,
        isResearched = isResearched,
        isTutorialHighlighted = isTutorialHighlighted,
        isLastInTutorialQueue = isLastInTutorialQueue
    };

    public void LoadMineralSaveData(MineralSaveData data)
    {
        realAge = data.realAge;
        realRadioactivity = data.realRadioactivity;
        isResearched = data.isResearched;
        isTutorialHighlighted = data.isTutorialHighlighted;
        isLastInTutorialQueue = data.isLastInTutorialQueue;

        if (AgePoint) AgePoint.transform.localPosition = data.agePointLocalPos;
        if (CrystalPoint) CrystalPoint.transform.localPosition = data.crystalPointLocalPos;
        if (RadioactivityPoint) RadioactivityPoint.transform.localPosition = data.radioactivityPointLocalPos;

        if (tutorialOutline == null) tutorialOutline = GetComponentInChildren<Outline>();
        if (tutorialOutline != null) tutorialOutline.enabled = isTutorialHighlighted;
    }

#if UNITY_EDITOR
    [ContextMenu("Сгенерировать данные")]
    private void EditorGenerate() => GenerateData();
#endif
}