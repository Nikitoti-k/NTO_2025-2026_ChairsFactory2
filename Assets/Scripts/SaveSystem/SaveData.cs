using UnityEngine;
using System.Collections.Generic;
using System; // ← ЭТО ОБЯЗАТЕЛЬНО!

using System.Collections.Generic;

[System.Serializable]
public class SaveFile
{
    public const string CURRENT_VERSION = "2.0";
    public string version = CURRENT_VERSION;
    public string checksum;

    public GameStateBlock gameState = new GameStateBlock();
    public string globalReports = "";
    public Vector2 cameraLookDirection;
    public TutorialSaveData tutorialData = new TutorialSaveData();

    // ← НОВЫЕ ПОЛЯ ДЛЯ НАСТРОЕК
    public float masterVolume = 1f;
    public float sfxVolume = 1f;
    public float ambienceVolume = 1f;
    public string language = "RU"; // "RU" или "EN"

    public List<ObjectSaveData> objects = new List<ObjectSaveData>();
    public List<MineralSaveData> minerals = new List<MineralSaveData>();
    public List<DepositSaveData> deposits = new List<DepositSaveData>();

    public SaveFile GetCleanCopy()
    {
        return new SaveFile
        {
            gameState = this.gameState,
            globalReports = this.globalReports,
            cameraLookDirection = this.cameraLookDirection,
            tutorialData = this.tutorialData,
            masterVolume = this.masterVolume,
            sfxVolume = this.sfxVolume,
            ambienceVolume = this.ambienceVolume,
            language = this.language,
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

    // Монологи
    public bool hasPlayedIntroMonologue = false;
    public bool hasPlayedReturnMonologue = false;
    public bool hasPlayedFinalMonologue = false;
    public bool hasPlayedMorningDay2 = false;
    public bool hasPlayedMorningDay3 = false;

    // Ключевые флаги прогресса
    public bool flareHintActive = false;
    public bool flareThrown = false;
    public bool anomalyPlaced = false;
    public bool playerSlept = false;

    // === ОДНОРАЗОВЫЕ ПОДСКАЗКИ (НИКОГДА БОЛЬШЕ НЕ ПОКАЗЫВАТЬ) ===
    public bool hintShown_Look = false;           // 0
    public bool hintShown_Move = false;           // 1
    public bool hintShown_Door = false;           // 2
    public bool hintShown_Vehicle = false;        // 3
    public bool hintShown_Flare = false;          // 4
    public bool hintShown_Break = false;          // 5
    public bool hintShown_Carry = false;          // 6
    public bool hintShown_Return = false;         // 7
    public bool hintShown_Table = false;          // 8
    public bool hintShown_ScanMove = false;       // 9
    public bool hintShown_ScanClick = false;      // 10
    public bool hintShown_Accuracy = false;       // 11
    public bool hintShown_FindMore = false;       // 12
    public bool hintShown_Conclusion = false;     // 13 — ВАЖНО: только один раз!
    public bool hintShown_AnomalyPlace = false;   // 14
    public bool hintShown_GoToBed = false;        // 15
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