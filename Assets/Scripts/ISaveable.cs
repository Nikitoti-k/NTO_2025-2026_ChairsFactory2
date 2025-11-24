using UnityEngine;

public interface ISaveable
{
    string GetUniqueID();  // Уникальный ID объекта (например, имя сцены + имя объекта)
    SaveData GetSaveData();  // Получить данные для сохранения
    void LoadFromSaveData(SaveData data);  // Загрузить данные
}