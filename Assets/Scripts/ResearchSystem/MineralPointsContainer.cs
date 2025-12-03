using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[RequireComponent(typeof(MineralPointSpawner))]
public class MineralData : MonoBehaviour
{
    public enum CrystalSystem { Cubic, Monoclinic, Molecular }

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

    [Header("Точки сканирования")]
    public ScanPoint AgePoint;
    public ScanPoint CrystalPoint;
    public ScanPoint RadioactivityPoint;

    [Header("Класс минерала")]
    [SerializeField] private MineralClass mineralClass;

    [Header("Tutorial Outline — назначай на дочерний объект с Outline!")]
    [SerializeField] private Outline tutorialOutline;

    public float AgeMya => realAge + Random.Range(-mineralClass.ageError, mineralClass.ageError);
    public float RadioactivityUsv => realRadioactivity + Random.Range(-mineralClass.radioactivityError, mineralClass.radioactivityError);
    public CrystalSystem CrystalSystem_ => mineralClass.crystalSystem;
    public string ClassName => mineralClass.className;
    public string AgeUnitText => mineralClass.ageUnit == MineralClass.AgeUnit.Days ? "дней" : "млн лет";
    public MineralClass MineralClassSO => mineralClass;

    public float realAge;
    public float realRadioactivity;
    public string UniqueInstanceID;
    public bool isResearched = false;
    public bool isTutorialHighlighted = false;
    public bool isLastInTutorialQueue = false;

    [HideInInspector] public string savedAgeLine = "";
    [HideInInspector] public string savedCrystalLine = "";
    [HideInInspector] public string savedRadioactivityLine = "";

    private void Awake()
    {
        if (string.IsNullOrEmpty(UniqueInstanceID))
            UniqueInstanceID = System.Guid.NewGuid().ToString();

        // ← ВАЖНО: находим Outline при старте — даже если он на дочернем объекте
        if (tutorialOutline == null)
            tutorialOutline = GetComponentInChildren<Outline>();

        if (tutorialOutline != null)
            tutorialOutline.enabled = false;
    }

    public void EnableTutorialOutline(bool enable)
    {
        if (tutorialOutline == null)
            tutorialOutline = GetComponentInChildren<Outline>();

        if (tutorialOutline != null)
            tutorialOutline.enabled = enable;
    }

    public void SetAsLastInTutorialQueue(bool isLast)
    {
        isLastInTutorialQueue = isLast;
    }

    public void GenerateData()
    {
        if (mineralClass == null) return;
        realAge = Random.Range(mineralClass.ageMin, mineralClass.ageMax);
        realRadioactivity = Random.Range(mineralClass.radioactivityMin, mineralClass.radioactivityMax);
    }

    public MineralSaveData GetMineralSaveData()
    {
        return new MineralSaveData
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
    }

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

        if (tutorialOutline == null)
            tutorialOutline = GetComponentInChildren<Outline>();

        if (tutorialOutline != null)
            tutorialOutline.enabled = isTutorialHighlighted;
    }

#if UNITY_EDITOR
    [ContextMenu("Сгенерировать данные")]
    private void EditorGenerate() => GenerateData();
#endif
}