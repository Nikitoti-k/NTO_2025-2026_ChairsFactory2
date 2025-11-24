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

    public int customInt1;  // Уже есть для hits

    // Новое: Для коллайдера и rigidbody
    public bool isTrigger;  // Collider.isTrigger
    public bool useGravity;  // Rigidbody.useGravity
    public int constraints;  // (int)RigidbodyConstraints (для заморозки осей, e.g., FreezePositionY)
}