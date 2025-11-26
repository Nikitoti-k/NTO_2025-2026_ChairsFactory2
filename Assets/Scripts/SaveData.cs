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

    
    public int customInt1, customInt2, customInt3;
    public float customFloat1, customFloat2;
    public bool customBool1, customBool2;
    public Vector3 customVector1, customVector2, customVector3;
    public string customString1, customString2, customString3;
}
