using UnityEngine;

public interface ISaveable
{
    string GetUniqueID();
    SaveData GetSaveData();
    void LoadFromSaveData(SaveData data);
}