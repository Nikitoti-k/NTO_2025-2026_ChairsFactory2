using UnityEngine;

[DisallowMultipleComponent]
public class MineralData : MonoBehaviour
{
    public enum SampleType { Ore, Fossil, Anomaly }
    public enum CrystalSystem { None, Cubic, Trigonal, Monoclinic }

    [Header("Тип образца")]
    [SerializeField] private SampleType sampleType = SampleType.Ore;
    [SerializeField] private float ageMya = 150f;
    [SerializeField] private CrystalSystem crystalSystem = CrystalSystem.Cubic;
    [SerializeField] private float radioactivityUsv = 0.12f;

    [Header("Точки сканирования")]
    [SerializeField] private ScanPoint agePoint;
    [SerializeField] private ScanPoint crystalPoint;
    [SerializeField] private ScanPoint radioactivityPoint;

    public SampleType Type => sampleType;
    public float AgeMya => ageMya;
    public CrystalSystem CrystalSystem_ => crystalSystem;
    public float RadioactivityUsv => radioactivityUsv;

    public ScanPoint AgePoint => agePoint;
    public ScanPoint CrystalPoint => crystalPoint;
    public ScanPoint RadioactivityPoint => radioactivityPoint;

    public ScanPoint GetPoint(int index) => index switch
    {
        0 => agePoint,
        1 => crystalPoint,
        2 => radioactivityPoint,
        _ => null
    };
}