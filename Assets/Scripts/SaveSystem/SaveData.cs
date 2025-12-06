using UnityEngine;
using System.Collections.Generic;
using System; // ← ЭТО ОБЯЗАТЕЛЬНО!

using System.Collections.Generic;

[System.Serializable]
public class SaveFile
{
    public const string CURRENT_VERSION = "2.0";

    public string version = CURRENT_VERSION;
    public string checksum;                  // заполняется автоматически перед сохранением

    public GameStateBlock gameState = new GameStateBlock();
    public string globalReports = "";

    public Vector2 cameraLookDirection;

    // ← ТОЛЬКО ОДИН источник правды про туториал!
    public TutorialSaveData tutorialData = new TutorialSaveData();

    public List<ObjectSaveData> objects = new List<ObjectSaveData>();
    public List<MineralSaveData> minerals = new List<MineralSaveData>();
    public List<DepositSaveData> deposits = new List<DepositSaveData>();

    // КРИТИЧЕСКИ ВАЖНЫЙ МЕТОД — возвращает копию без checksum и version (чтобы хеш был стабильным)
    public SaveFile GetCleanCopy()
    {
        return new SaveFile
        {
            gameState = this.gameState,
            globalReports = this.globalReports,
            cameraLookDirection = this.cameraLookDirection,
            tutorialData = this.tutorialData,
            objects = this.objects,
            minerals = this.minerals,
            deposits = this.deposits
        };
    }
}
[System.Serializable]
public class TutorialSaveData
{
    public int step = 0;
    public int researchedCount = 0;

    public bool hasPlayedIntroMonologue = false;     // ← обязательно true после первого монолога
    public bool hasPlayedReturnMonologue = false;
    public bool hasPlayedFinalMonologue = false;

    public bool anomalyPlaced = false;
    public bool playerSlept = false;
    public bool flareHintWasShown = false;

    // Добавляй сюда новые флаги по мере развития туториала — всё будет сохраняться автоматически
}
public interface IHasTutorialData
{
    TutorialSaveData GetTutorialSaveData();
    void LoadTutorialSaveData(TutorialSaveData data);
}



[System.Serializable]
public class ObjectSaveData
{
    public string uniqueID;
    public string prefabIdentifier;

    public Vector3 position;
    public Quaternion rotation;

    public Vector3 velocity;
    public Vector3 angularVelocity;

    public bool isActive = true;
    public string parentPath = "";

    public bool isTrigger;
    public bool useGravity = true;
    public int constraints;
    public bool isKinematic;

    public string seatedInTransportID = "";
    public string controllingTransportID = "";
    public string snappedZoneID = "";
    public int snapPointIndex = -1;
    public bool wasInScannerZone;
}

[System.Serializable]
public class GameStateBlock
{
    public int currentDay;
    public float currentTimeInMinutes;
    public int depositsBrokenToday;
    public int mineralsResearchedToday;
    // ← Единственное, что сохраняем от DayActivation
  
}

[System.Serializable]
public class MineralSaveData
{
    public string uniqueID;

    public float realAge;
    public float realRadioactivity;

    public Vector3 agePointLocalPos;
    public Vector3 crystalPointLocalPos;
    public Vector3 radioactivityPointLocalPos;

    public bool isResearched;
    public string savedAgeLine = "";
    public string savedRadioactivityLine = "";
    public string savedCrystalLine = "";
}

[System.Serializable]
public class DepositSaveData
{
    public string uniqueID;
    public int currentHits;
}



[System.Serializable]
public class SaveSlotMeta
{
    public string slotName = "Сохранение";
    public DateTime saveTime = DateTime.Now;

    // Для удобного отображения в UI
    public string GetFormattedTime() => saveTime.ToString("dd MMMM yyyy, HH:mm");
}


[System.Serializable]
public class SaveSlotInfo
{
    public int slotIndex;
    public string slotName;
    public string saveTime;         // уже отформатированная строка
    public Texture2D previewTexture;
    public bool hasData;

    public void DestroyTexture()
    {
        if (previewTexture != null && previewTexture != null)
            UnityEngine.Object.Destroy(previewTexture);
    }
}