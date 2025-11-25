using UnityEngine;

public class SaveableObject : MonoBehaviour, ISaveable
{
    [SerializeField] private string uniqueID = "";
    [SerializeField] private string prefabIdentifier = ""; // ← ОБЯЗАТЕЛЬНО задать в префабе!

    private Rigidbody rb;
    private Collider col;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponentInChildren<Collider>();

        if (string.IsNullOrEmpty(uniqueID))
            uniqueID = System.Guid.NewGuid().ToString();

        // ← НОВАЯ ПРОВЕРКА
        if (string.IsNullOrEmpty(prefabIdentifier))
        {
            Debug.LogWarning($"[SaveableObject] PrefabIdentifier пустой на {gameObject.name}! Установите в инспекторе или через SetPrefabIdentifier()", this);
            prefabIdentifier = gameObject.name; // на крайний случай
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueID) && !Application.isPlaying)
            uniqueID = System.Guid.NewGuid().ToString();
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
            parentPath = transform.parent ? transform.parent.GetPath() : "",
            customInt1 = 0,
            isTrigger = col ? col.isTrigger : false,
            useGravity = rb ? rb.useGravity : true,
            constraints = rb ? (int)rb.constraints : 0
        };
    }

    public void LoadFromSaveData(SaveData data)
    {
        transform.position = data.position;
        transform.rotation = data.rotation;
        gameObject.SetActive(data.isActive);

        if (rb)
        {
            rb.linearVelocity = data.velocity;
            rb.angularVelocity = data.angularVelocity;
            rb.useGravity = data.useGravity;
            rb.constraints = (RigidbodyConstraints)data.constraints;
            rb.Sleep();
        }

        if (col)
            col.isTrigger = data.isTrigger;

        Physics.SyncTransforms();
    }

    public void SetPrefabIdentifier(string id) => prefabIdentifier = id;
}


public static class TransformExtensions
{
    public static string GetPath(this Transform t)
    {
        if (t.parent == null) return t.name;
        return t.parent.GetPath() + "/" + t.name;
    }
}