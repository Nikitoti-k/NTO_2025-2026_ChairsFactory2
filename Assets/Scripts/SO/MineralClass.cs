using UnityEngine;

[CreateAssetMenu(menuName = "Mineral Scanner/Mineral Class", fileName = "New Mineral Class")]
public class MineralClass : ScriptableObject
{
    public enum AgeUnit { Days, Millions }

    [Header("Название класса")]
    public string className = "Окаменелая флора";

    [Header("Это класс «Аномалия»?")]
    [Tooltip("Если включено — все диапазоны и погрешности игнорируются")]
    public bool isAnomalyClass = false;

    // === Эти поля используются ТОЛЬКО если isAnomalyClass == false ===
    [Space(10)]
    [Header("=== Параметры для ОБЫЧНЫХ классов (скрыты у Аномалий) ===")]

    public AgeUnit ageUnit = AgeUnit.Millions;

    [Min(0)] public float ageMin = 150f;
    [Min(0)] public float ageMax = 250f;

    // ← ВОЗВРАЩАЕМ! Нужны для MineralData и других скриптов
    [Tooltip("Погрешность возраста при сканировании (±)")]
    public float ageError = 5f;

    [Space]
    [Min(0)] public float radioactivityMin = 0.5f;
    [Min(0)] public float radioactivityMax = 2.8f;

    // ← ВОЗВРАЩАЕМ!
    [Tooltip("Погрешность радиации при сканировании (±)")]
    public float radioactivityError = 0.05f;

    public MineralData.CrystalSystem crystalSystem = MineralData.CrystalSystem.Cubic;
}