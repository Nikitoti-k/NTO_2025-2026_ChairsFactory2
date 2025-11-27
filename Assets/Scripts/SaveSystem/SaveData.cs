using UnityEngine;
using System;

[System.Serializable]
public class SaveData
{
    public string uniqueID;
    public string prefabIdentifier;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;
    public Vector3 angularVelocity;
    public bool isActive = true;
    public string parentPath;
    public bool isTrigger;
    public bool useGravity = true;
    public int constraints;
    public bool isKinematic;
    public string seatedInTransportID = "";
    public string controllingTransportID = "";
    public string snappedZoneID = "";
    public int snapPointIndex = -1;
    public bool wasInScannerZone;

    // Вложенные блоки для логического разделения
    public GameStateBlock gameState;
    public MineralBlock mineral;
    public DepositBlock deposit;
    public ReportBlock report;

    [System.Serializable]
    public class GameStateBlock
    {
        public int currentDay;
        public float currentTimeInMinutes;
        public int depositsBrokenToday;
        public int mineralsResearchedToday;
        public bool canStartEvening;
        public bool canSleep;
    }

    [System.Serializable]
    public class MineralBlock
    {
        public float realAge;
        public float realRadioactivity;
        public Vector3 agePointLocalPos;
        public Vector3 crystalPointLocalPos;
        public Vector3 radioactivityPointLocalPos;
        public bool isResearched;
        public string savedAgeLine;
        public string savedRadioactivityLine;
        public string savedCrystalLine;
    }

    [System.Serializable]
    public class DepositBlock
    {
        public int currentHits;
    }

    [System.Serializable]
    public class ReportBlock
    {
        public string serializedReports;  // Строка с allReports, как в оригинале
    }

    // Валидатор для устойчивости
    public bool Validate()
    {
        if (string.IsNullOrEmpty(uniqueID)) return false;
        if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z)) return false;  // Позиция валидна

        if (gameState != null)
        {
            if (gameState.currentDay < 0) return false;
        }

        if (mineral != null)
        {
            if (mineral.realAge < 0 || mineral.realRadioactivity < 0) return false;
        }

        if (deposit != null)
        {
            if (deposit.currentHits < 0) return false;
        }

        if (report != null)
        {
            if (string.IsNullOrEmpty(report.serializedReports)) return true;  // Пусто ок
            // Можно добавить парсинг для проверки, но просто
        }

        return true;
    }
}