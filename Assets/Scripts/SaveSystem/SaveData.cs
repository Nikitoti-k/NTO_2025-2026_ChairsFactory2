using UnityEngine;
using System.Collections.Generic;

using System;
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

    public int tutorialStep = 0;                    // текущий step
    public int researchedCount = 0;                 // сколько уже исследовано в туториале
    public bool hasPlayedReturnMonologue = false;
    public bool hasPlayedFinalMonologue = false;
    public bool anomalyPlaced = false;
    public bool playerSlept = false;

    public List<ObjectSaveData> objects = new List<ObjectSaveData>();
    public List<MineralSaveData> minerals = new List<MineralSaveData>();
    public List<DepositSaveData> deposits = new List<DepositSaveData>();
}
[System.Serializable]
public class TutorialSaveData
{
    public int step = 0;
    public int researchedCount = 0;
    public bool hasPlayedIntroMonologue = false;  // Новый флаг для 0-го
    public bool hasPlayedReturnMonologue = false;
    public bool hasPlayedFinalMonologue = false;
    public bool anomalyPlaced = false;
    public bool playerSlept = false;
    public bool flareHintWasShown = false;  // Используем существующий из SaveFile
    // Добавь другие флаги, если нужно (looked, moved и т.д.), но для минимума хватит
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