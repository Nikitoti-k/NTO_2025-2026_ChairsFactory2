using UnityEngine;

public class IceDeposit : MonoBehaviour, ISaveable
{
    [Header("Настройки депозита")]
    [SerializeField] private int hitsRequired = 3;
    [SerializeField] private GameObject mineralPrefab;     // ← Должен быть с SaveableObject!
    [SerializeField] private Transform spawnPoint;

    private int currentHits = 0;
    [SerializeField] private string uniqueID = "";
    [SerializeField] private string prefabIdentifier = "IceDeposit";

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (string.IsNullOrEmpty(uniqueID))
            uniqueID = System.Guid.NewGuid().ToString();
    }

    public void Hit()
    {
        currentHits++;
        if (currentHits >= hitsRequired)
            BreakDeposit();
    }

    private void BreakDeposit()
    {
        if (mineralPrefab != null)
        {
            GameObject mineral = Instantiate(mineralPrefab,
                spawnPoint ? spawnPoint.position : transform.position,
                Quaternion.identity);

            var saveable = mineral.GetComponent<SaveableObject>();
            if (saveable == null) saveable = mineral.AddComponent<SaveableObject>();
            saveable.SetPrefabIdentifier("Mineral"); // ← ДОЛЖНО СОВПАДАТЬ с реестром!

            Debug.Log($"[IceDeposit] Spawned mineral ID: {saveable.GetUniqueID()}");
        }

        gameObject.SetActive(false);
    }

    // ─── ISaveable ───
    public string GetUniqueID() => uniqueID;

    public SaveData GetSaveData()
    {
        return new SaveData
        {
            uniqueID = uniqueID,
            prefabIdentifier = prefabIdentifier,
            position = transform.position,
            rotation = transform.rotation,
            isActive = gameObject.activeSelf,
            parentPath = transform.parent ? transform.parent.GetPath() : "",
            customInt1 = currentHits
        };
    }

    public void LoadFromSaveData(SaveData data)
    {
        transform.position = data.position;
        transform.rotation = data.rotation;
        gameObject.SetActive(data.isActive);
        currentHits = data.customInt1;
    }
}