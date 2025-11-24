using UnityEngine;

[RequireComponent(typeof(MineralPointSpawner))]
public class MineralData : MonoBehaviour
{
    public enum CrystalSystem { Cubic, Trigonal, Monoclinic, Tetragonal, Hexagonal, Orthorhombic, Triclinic }

    
    private float realAge;
    private float realRadioactivity;

    [Header("Точки сканирования (авто или вручную)")]
    public ScanPoint AgePoint;
    public ScanPoint CrystalPoint;
    public ScanPoint RadioactivityPoint;

    [Header("КЛАСС — перетащи нужный .asset сюда!")]
    [SerializeField] private MineralClass mineralClass;

   
    public float AgeMya => realAge + Random.Range(-mineralClass.ageError, mineralClass.ageError);
    public float RadioactivityUsv => realRadioactivity + Random.Range(-mineralClass.radioactivityError, mineralClass.radioactivityError);
    public CrystalSystem CrystalSystem_ => mineralClass.crystalSystem;

    public string ClassName => mineralClass.className;
    public string AgeUnitText => mineralClass.ageUnit == MineralClass.AgeUnit.Days ? "дней" : "млн лет";
    public MineralClass MineralClassSO => mineralClass;
    [HideInInspector] public string UniqueInstanceID;

    private void Awake()
    {
        // Генерируем уникальный ID при первом запуске
        if (string.IsNullOrEmpty(UniqueInstanceID))
        {
            UniqueInstanceID = System.Guid.NewGuid().ToString();
            Debug.Log($"[MineralData] Назначен уникальный ID: {UniqueInstanceID}");
        }
    }
    public void GenerateData()
    {
        if (mineralClass == null)
        {
            Debug.LogError("[MineralData] Не назначен MineralClass SO!");
            return;
        }

        realAge = Random.Range(mineralClass.ageMin, mineralClass.ageMax);
        realRadioactivity = Random.Range(mineralClass.radioactivityMin, mineralClass.radioactivityMax);

        Debug.Log($"Сгенерирован минерал: <color=cyan>{mineralClass.className}</color>\n" +
                  $"Возраст: {realAge:F1} ±{mineralClass.ageError} {AgeUnitText}\n" +
                  $"Радиация: {realRadioactivity:F3} ±{mineralClass.radioactivityError} Бк");
    }

#if UNITY_EDITOR
    [ContextMenu("Сгенерировать данные (тест)")]
    private void EditorGenerate() => GenerateData();
#endif
}