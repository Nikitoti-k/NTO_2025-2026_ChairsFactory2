using UnityEngine;

public class MineralSaveableObject : SaveableObject, IHasMineralData
{
    protected override void Awake()
    {
        base.Awake();
        if (string.IsNullOrEmpty(uniqueID))
            uniqueID = System.Guid.NewGuid().ToString();
    }

    public MineralSaveData GetMineralSaveData()
    {
        var mineral = GetComponent<MineralData>();
        if (mineral == null) return new MineralSaveData { uniqueID = uniqueID };

        return new MineralSaveData
        {
            uniqueID = uniqueID,
            realAge = mineral.realAge,
            realRadioactivity = mineral.realRadioactivity,
            agePointLocalPos = mineral.AgePoint ? mineral.AgePoint.transform.localPosition : Vector3.zero,
            crystalPointLocalPos = mineral.CrystalPoint ? mineral.CrystalPoint.transform.localPosition : Vector3.zero,
            radioactivityPointLocalPos = mineral.RadioactivityPoint ? mineral.RadioactivityPoint.transform.localPosition : Vector3.zero,
            isResearched = mineral.isResearched,
            savedAgeLine = mineral.savedAgeLine ?? "",
            savedRadioactivityLine = mineral.savedRadioactivityLine ?? "",
            savedCrystalLine = mineral.savedCrystalLine ?? ""
        };
    }

    public void LoadMineralData(MineralSaveData data)
    {
        var mineral = GetComponent<MineralData>();
        if (mineral == null) return;

        mineral.realAge = data.realAge;
        mineral.realRadioactivity = data.realRadioactivity;
        mineral.isResearched = data.isResearched;

        mineral.savedAgeLine = data.savedAgeLine;
        mineral.savedRadioactivityLine = data.savedRadioactivityLine;
        mineral.savedCrystalLine = data.savedCrystalLine;

        if (mineral.AgePoint) mineral.AgePoint.transform.localPosition = data.agePointLocalPos;
        if (mineral.CrystalPoint) mineral.CrystalPoint.transform.localPosition = data.crystalPointLocalPos;
        if (mineral.RadioactivityPoint) mineral.RadioactivityPoint.transform.localPosition = data.radioactivityPointLocalPos;

        GetComponent<MineralPointSpawner>()?.RestorePointsFromSaveData(
            data.agePointLocalPos,
            data.crystalPointLocalPos,
            data.radioactivityPointLocalPos);
    }
}