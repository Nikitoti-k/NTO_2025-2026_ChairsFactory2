using System;
using UnityEngine;

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

    // Для физики и коллайдеров
    public bool isTrigger;
    public bool useGravity;
    public int constraints;
    public string seatedInTransportID = "";
    public string controllingTransportID = "";
}