using UnityEngine;

[CreateAssetMenu(menuName = "Mineral Scanner/Mineral Class", fileName = "New Mineral Class")]
public class MineralClass : ScriptableObject
{
    public enum AgeUnit { Days, Millions }

    [Header("Название класса")]
    public string className = "Окаменелая флора";

    [Header("Возраст")]
    public AgeUnit ageUnit = AgeUnit.Millions;
    public float ageMin = 150f;
    public float ageMax = 250f;
    public float ageError = 1f;                

    [Header("Радиация (Бк)")]
    public float radioactivityMin = 0.5f;
    public float radioactivityMax = 2.8f;
    public float radioactivityError = 0.001f;   

    [Header("Кристаллическая решётка")]
    public MineralData.CrystalSystem crystalSystem = MineralData.CrystalSystem.Cubic;
}