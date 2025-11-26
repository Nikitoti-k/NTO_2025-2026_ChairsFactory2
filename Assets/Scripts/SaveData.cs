using UnityEngine;
using System;

[Serializable]
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
    public int customInt1;

    // Физика и коллайдеры
    public bool isTrigger;
    public bool useGravity;
    public int constraints;
    public bool isKinematic; // ← НОВОЕ: сохраняем kinematic состояние!

    // Посадка в транспорт
    public string seatedInTransportID = "";
    public string controllingTransportID = "";

    // SnapZone привязка
    public string snappedZoneID = "";
    public int snapPointIndex = -1;

    // ← НОВОЕ ПОЛЕ!
    public bool wasInScannerZone = false; // ← ВОТ ЭТО ГЛАВНОЕ!

    public float customFloat1, customFloat2;
    public Vector3 customVector1, customVector2, customVector3;
    public bool customBool1;

}
