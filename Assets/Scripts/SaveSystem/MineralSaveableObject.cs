using UnityEngine;

public class MineralSaveableObject : SaveableObject
{
    protected override void Awake()
    {
        base.Awake();
        if (string.IsNullOrEmpty(uniqueID))
        {
            uniqueID = System.Guid.NewGuid().ToString();
            // Debug.Log($"[PooledSaveable] Новый объект — сгенерирован ID: {uniqueID}");
        }
        // Additional for minerals
    }

    public override SaveData GetSaveData()
    {
        var data = base.GetSaveData();

        var mineral = GetComponent<MineralData>();
        if (mineral != null)
        {
            if (GetComponentInParent<SnapZone>() == MineralScannerManager.Instance?.targetSnapZone)
                data.wasInScannerZone = true;

            var m = mineral.GetMineralSaveData();
            data.mineral = new SaveData.MineralBlock
            {
                realAge = m.realAge,
                realRadioactivity = m.realRadioactivity,
                agePointLocalPos = m.agePointLocalPos,
                crystalPointLocalPos = m.crystalPointLocalPos,
                radioactivityPointLocalPos = m.radioactivityPointLocalPos,
                isResearched = m.isResearched,
                savedAgeLine = mineral.savedAgeLine,
                savedRadioactivityLine = mineral.savedRadioactivityLine,
                savedCrystalLine = mineral.savedCrystalLine
            };
        }

        return data;
    }

    public override void LoadFromSaveData(SaveData data)
    {
        uniqueID = data.uniqueID;  // ← ФИКС

        base.LoadFromSaveData(data);

        if (data.mineral == null) return;

        var mineral = GetComponent<MineralData>();
        if (mineral != null)
        {
            mineral.LoadMineralSaveData(new MineralData.MineralSaveData
            {
                realAge = data.mineral.realAge,
                realRadioactivity = data.mineral.realRadioactivity,
                agePointLocalPos = data.mineral.agePointLocalPos,
                crystalPointLocalPos = data.mineral.crystalPointLocalPos,
                radioactivityPointLocalPos = data.mineral.radioactivityPointLocalPos,
                isResearched = data.mineral.isResearched
            });

            mineral.savedAgeLine = data.mineral.savedAgeLine ?? "";
            mineral.savedRadioactivityLine = data.mineral.savedRadioactivityLine ?? "";
            mineral.savedCrystalLine = data.mineral.savedCrystalLine ?? "";

            GetComponent<MineralPointSpawner>()?.RestorePointsFromSaveData(
                data.mineral.agePointLocalPos, data.mineral.crystalPointLocalPos, data.mineral.radioactivityPointLocalPos);
        }
    }
}