using UnityEngine;

[RequireComponent(typeof(MineralPointSpawner))]
public class MineralData : MonoBehaviour
{
    public enum CrystalSystem { Cubic, Trigonal, Monoclinic, Tetragonal, Hexagonal, Orthorhombic, Triclinic }

    [System.Serializable]
    public class MineralSaveData
    {
        public float realAge;
        public float realRadioactivity;
        public Vector3 agePointLocalPos;
        public Vector3 crystalPointLocalPos;
        public Vector3 radioactivityPointLocalPos;
        public bool isResearched;
    }

    [Header("Точки сканирования")]
    public ScanPoint AgePoint;
    public ScanPoint CrystalPoint;
    public ScanPoint RadioactivityPoint;

    [Header("Класс минерала")]
    [SerializeField] private MineralClass mineralClass;

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

    [HideInInspector] public string savedAgeLine = "";
    [HideInInspector] public string savedRadioactivityLine = "";
    [HideInInspector] public string savedCrystalLine = "";

    private void Awake()
    {
        if (string.IsNullOrEmpty(UniqueInstanceID))
            UniqueInstanceID = System.Guid.NewGuid().ToString();
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
            isResearched = isResearched
        };
    }

    public void LoadMineralSaveData(MineralSaveData data)
    {
        realAge = data.realAge;
        realRadioactivity = data.realRadioactivity;
        isResearched = data.isResearched;

        if (AgePoint) AgePoint.transform.localPosition = data.agePointLocalPos;
        if (CrystalPoint) CrystalPoint.transform.localPosition = data.crystalPointLocalPos;
        if (RadioactivityPoint) RadioactivityPoint.transform.localPosition = data.radioactivityPointLocalPos;
    }

#if UNITY_EDITOR
    [ContextMenu("Сгенерировать данные")]
    private void EditorGenerate() => GenerateData();
#endif
}