using UnityEngine;

public class IceDeposit : MonoBehaviour, ISaveable
{
    [Header("��������� ������")]
    [SerializeField] private int hitsRequired = 3;
    [SerializeField] private GameObject mineralPrefab;
    [SerializeField] private Transform spawnPoint;

    private int currentHits = 0;

    // ��� ����������: ���������� ID � ������������� ������� (���� ��� ������������ ������)
    [SerializeField] private string uniqueID;
    [SerializeField] private string prefabIdentifier = "IceDeposit";  // ����� � Inspector ��� prefab

    private Rigidbody rb;  // ���� ���� ������, ����� null

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (string.IsNullOrEmpty(uniqueID))
        {
            uniqueID = System.Guid.NewGuid().ToString();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueID) && !Application.isPlaying)
        {
            uniqueID = System.Guid.NewGuid().ToString();
        }
    }
#endif

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
            GameObject mineral = Instantiate(
                mineralPrefab,
                spawnPoint ? spawnPoint.position : transform.position,
                Quaternion.identity
            );
            SaveableObject saveable = mineral.GetComponent<SaveableObject>();
            if (saveable == null)
            {
                saveable = mineral.AddComponent<SaveableObject>();
            }
            saveable.SetPrefabIdentifier("Mineral");
            Debug.Log($"Spawned Mineral with ID: {saveable.GetUniqueID()}");  // Дебаг: Проверка ID после спавна
            GameDayManager.Instance.RegisterDepositBroken();
        }

        gameObject.SetActive(false);
    }

    // ���������� ISaveable
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
            // ��������� ���� ��� currentHits (������� SaveData ������� public int customInt1;)
            customInt1 = currentHits  // ������ ��� ���� � SaveData.cs: public int customInt1;
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
        if (GetComponent<Collider>())  // Добавь, если коллайдер не в Awake
        {
            GetComponent<Collider>().isTrigger = data.isTrigger;
        }
        gameObject.SetActive(data.isActive);
        currentHits = data.customInt1;

        Physics.SyncTransforms();
    }
    private string GetParentPath()
    {
        Transform parent = transform.parent;
        return parent ? parent.GetPath() : "";
    }
}