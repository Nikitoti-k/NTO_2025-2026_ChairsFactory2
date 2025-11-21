using UnityEngine;
using System;

/// <summary>
/// Основные данные минерала. Вешаем на корень префаба минерала.
/// </summary>
[DisallowMultipleComponent]
public class MineralData : MonoBehaviour
{
    public enum SampleType
    {
        Ore,          // Руда
        Fossil,       // Окаменелость  
        Anomaly       // Аномалия (космическая/неизвестная)
    }

    public enum CrystalSystem
    {
        None,
        Cubic,        // Кубическая
        Tetragonal,
        Orthorhombic,
        Hexagonal,
        Trigonal,     // Тригональная
        Monoclinic,   // Моноклинная
        Triclinic
    }

    [Header("≡ Основные параметры образца")]
    [SerializeField] private SampleType sampleType = SampleType.Ore;
    [SerializeField, Tooltip("В миллионах лет (Млн лет)")]
    private float ageMya = 150f;
    [SerializeField] private CrystalSystem crystalSystem = CrystalSystem.Cubic;
    [SerializeField, Range(0f, 1000f), Tooltip("В микрозивертах в час (µSv/h)")]
    private float radioactivityUsv = 0.12f;

    [Header("≡ Точки данных для джойстика (3 шт.)")]
    [SerializeField] private ScanPoint agePoint;
    [SerializeField] private ScanPoint crystalPoint;
    [SerializeField] private ScanPoint radioactivityPoint;

    // ───────────────────────────────────────
    // Публичные геттеры (чтобы MineralScannerManager мог читать)
    // ───────────────────────────────────────
    public SampleType Type => sampleType;
    public float AgeMya => ageMya;
    public CrystalSystem CrystalSystem_ => crystalSystem;
    public float RadioactivityUsv => radioactivityUsv;

    public ScanPoint AgePoint => agePoint;
    public ScanPoint CrystalPoint => crystalPoint;
    public ScanPoint RadioactivityPoint => radioactivityPoint;

    // Быстрый доступ по индексу (0=возраст, 1=решётка, 2=радиация)
    public ScanPoint GetPoint(int index) => index switch
    {
        0 => agePoint,
        1 => crystalPoint,
        2 => radioactivityPoint,
        _ => null
    };

    private void OnValidate()
    {
        // Автоматически заполняем названия точек в редакторе — чтобы не забыть
        if (agePoint != null) agePoint.pointName = "Возраст";
        if (crystalPoint != null) crystalPoint.pointName = "Кристаллическая решётка";
        if (radioactivityPoint != null) radioactivityPoint.pointName = "Уровень радиоактивности";
    }

#if UNITY_EDITOR
    private void Reset()
    {
        // При добавлении скрипта — автоматически создаём 3 пустых ребёнка и ScanPoint-ы
        CreateScanPointsIfNeeded();
    }

    [ContextMenu("Создать/пересоздать 3 точки сканирования")]
    private void CreateScanPointsIfNeeded()
    {
        // Удаляем старые, если есть
        foreach (Transform child in transform)
            if (child.name.Contains("ScanPoint")) DestroyImmediate(child.gameObject);

        agePoint = CreatePoint("ScanPoint_Age", new Vector3(0.15f, 0.1f, 0f), Color.cyan);
        crystalPoint = CreatePoint("ScanPoint_CrystalSystem", new Vector3(-0.12f, -0.1f, 0.1f), Color.magenta);
        radioactivityPoint = CreatePoint("ScanPoint_Radioactivity", new Vector3(0f, 0f, -0.15f), Color.yellow);

        UnityEditor.EditorUtility.SetDirty(this);
    }

    private ScanPoint CreatePoint(string name, Vector3 localPos, Color gizmoColor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;

        var point = go.AddComponent<ScanPoint>();
        point.gizmoColor = gizmoColor;
        return point;
    }
#endif
}
