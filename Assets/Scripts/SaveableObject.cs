using UnityEngine;
using System;

public class SaveableObject : MonoBehaviour, ISaveable
{
    [SerializeField] private string uniqueID;
    [SerializeField] private string prefabIdentifier;

    private Rigidbody rb;
    private Collider col;  // Универсально для любого коллайдера

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();  // Или GetComponentInChildren, если коллайдер в childs
        if (string.IsNullOrEmpty(uniqueID))
        {
            uniqueID = Guid.NewGuid().ToString();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueID) && !Application.isPlaying)
        {
            uniqueID = Guid.NewGuid().ToString();
        }
    }
#endif

    public string GetUniqueID() => uniqueID;

    public SaveData GetSaveData()
    {
        return new SaveData
        {
            uniqueID = uniqueID,
            prefabIdentifier = prefabIdentifier,
            position = transform.position,
            rotation = transform.rotation,
            velocity = rb ? rb.linearVelocity : Vector3.zero,
            angularVelocity = rb ? rb.angularVelocity : Vector3.zero,
            isActive = gameObject.activeSelf,
            parentPath = GetParentPath(),
            customInt1 = 0,  // Для кастомных
            isTrigger = col ? col.isTrigger : false,
            useGravity = rb ? rb.useGravity : false,
            constraints = rb ? (int)rb.constraints : 0
        };
    }

    public void LoadFromSaveData(SaveData data)
    {
        transform.position = data.position;
        transform.rotation = data.rotation;
        if (rb)
        {
            rb.linearVelocity = data.velocity;
            rb.angularVelocity = data.angularVelocity;
            rb.useGravity = data.useGravity;
            rb.constraints = (RigidbodyConstraints)data.constraints;
        }
        if (col)
        {
            col.isTrigger = data.isTrigger;
        }
        gameObject.SetActive(data.isActive);

        // Синхронизация физики сразу
        Physics.SyncTransforms();
    }

    private string GetParentPath()
    {
        Transform parent = transform.parent;
        return parent ? parent.GetPath() : "";
    }

    public void SetPrefabIdentifier(string id)
    {
        prefabIdentifier = id;
    }
}

// TransformExtensions без изменений

// Расширение для Transform (можно вынести в отдельный файл Utilities.cs)
public static class TransformExtensions
{
    public static string GetPath(this Transform current)
    {
        if (current.parent == null) return current.name;
        return current.parent.GetPath() + "/" + current.name;
    }
}