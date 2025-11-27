using UnityEngine;

public class IceDeposit : MonoBehaviour, ISaveable
{
    [Header("Настройки депозита")]
    [SerializeField] private int hitsRequired = 3;
    [SerializeField] private GameObject mineralPrefab;
    [SerializeField] private Transform spawnPoint;

    private int currentHits = 0;
    [SerializeField] private string uniqueID = "";
    [SerializeField] private string prefabIdentifier = "IceDeposit";

    private Rigidbody rb;

    void OnEnable()
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

            // КРИТИЧЕСКИ ВАЖНО: устанавливаем prefabIdentifier
            var saveable = mineral.GetComponent<MineralSaveableObject>();
            if (saveable == null)
                saveable = mineral.AddComponent<MineralSaveableObject>();

            string identifier = GetPrefabIdentifier(mineralPrefab);
            saveable.SetPrefabIdentifier(identifier);

            Debug.Log($"[IceDeposit] Заспавнен минерал: {mineralPrefab.name} → prefabIdentifier = \"{identifier}\" | uniqueID = {saveable.GetUniqueID()}");
        }

        gameObject.SetActive(false);
    }

    // Универсальный метод — определяет identifier по реестру или имени
    private string GetPrefabIdentifier(GameObject prefab)
    {
        if (SaveManager.Instance?.prefabRegistry != null)
        {
            foreach (var entry in SaveManager.Instance.prefabRegistry.prefabs)
            {
                if (entry.prefab == prefab)
                    return entry.identifier;
            }
        }

        // Если не нашёл в реестре — берём имя префаба (например, Min_2)
        string name = prefab.name;
        if (name.Contains("(Clone)")) name = name.Replace("(Clone)", "");
        Debug.LogWarning($"[IceDeposit] Identifier не найден в реестре для {prefab.name}, используем имя: {name}");
        return name.Trim();
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
            deposit = new SaveData.DepositBlock { currentHits = currentHits }
        };
    }

    public void LoadFromSaveData(SaveData data)
    {
        transform.position = data.position;
        transform.rotation = data.rotation;
        gameObject.SetActive(data.isActive);
        if (data.deposit != null) currentHits = data.deposit.currentHits;

        Debug.Log($"[IceDeposit] Загружен депозит | hits: {currentHits} | active: {data.isActive}");
    }
}