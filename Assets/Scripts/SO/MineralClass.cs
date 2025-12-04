using UnityEngine;

[CreateAssetMenu(menuName = "Mineral Scanner/Mineral Class", fileName = "New Mineral Class")]
public class MineralClass : ScriptableObject
{
    public enum AgeUnit { Days, Millions }

    [Header("Локализация названия")]
    [Tooltip("Например: NAME_MINERAL_CLASS_1, NAME_MINERAL_CLASS_5, CLASS_ANOMALY")]
    public string localizationKey = "NAME_MINERAL_CLASS_1";

    public bool isAnomalyClass = false;

    [Space]
    public AgeUnit ageUnit = AgeUnit.Millions;
    [Min(0)] public float ageMin = 150f;
    [Min(0)] public float ageMax = 250f;
    public float ageError = 5f;

    [Min(0)] public float radioactivityMin = 0.5f;
    [Min(0)] public float radioactivityMax = 2.8f;
    public float radioactivityError = 0.05f;

    public MineralData.CrystalSystem crystalSystem = MineralData.CrystalSystem.Cubic;

    // ← Геттер для локализованного имени
    public string LocalizedName => LocalizationManager.Loc(localizationKey);
}