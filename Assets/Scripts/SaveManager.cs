using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Collections;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [SerializeField] private PrefabRegistry prefabRegistry; // ← ОБЯЗАТЕЛЬНО назначить в инспекторе!

    private string saveFilePath;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            saveFilePath = Path.Combine(Application.persistentDataPath, "save.json");
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    public void SaveGame()
    {
        var saveDataList = new List<SaveData>();
        var saveables = FindObjectsOfType<MonoBehaviour>(true).OfType<ISaveable>();

        foreach (var saveable in saveables)
            saveDataList.Add(saveable.GetSaveData());

        string json = JsonUtility.ToJson(new SaveWrapper { saves = saveDataList }, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log($"[SaveManager] Игра сохранена! Объектов: {saveDataList.Count}");
    }

    public void LoadGame() => StartCoroutine(LoadGameAfterSpawn());

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => StartCoroutine(LoadGameAfterSpawn());

    private IEnumerator LoadGameAfterSpawn()
    {
        // Ждём полной инициализации сцены
        for (int i = 0; i < 10; i++) yield return null;
        yield return new WaitForFixedUpdate();

        if (!File.Exists(saveFilePath))
        {
            Debug.Log("[SaveManager] Сохранения нет");
            yield break;
        }

        string json = File.ReadAllText(saveFilePath);
        SaveWrapper wrapper = JsonUtility.FromJson<SaveWrapper>(json);
        var saveDataList = wrapper.saves ?? new List<SaveData>();

        var existingSaveables = FindObjectsOfType<MonoBehaviour>(true).OfType<ISaveable>().ToList();
        var remainingData = new List<SaveData>(saveDataList);

        // 1. Загружаем уже существующие в сцене
        foreach (var saveable in existingSaveables)
        {
            var data = remainingData.Find(d => d.uniqueID == saveable.GetUniqueID());
            if (data != null)
            {
                saveable.LoadFromSaveData(data);
                remainingData.Remove(data);
            }
        }

        // 2. Спавним отсутствующие
        int spawned = 0;
        foreach (var data in remainingData)
        {
            if (string.IsNullOrEmpty(data.prefabIdentifier)) continue;

            GameObject prefab = prefabRegistry?.GetPrefab(data.prefabIdentifier);
            if (prefab == null)
            {
                Debug.LogError($"[SaveManager] Prefab not found: {data.prefabIdentifier} (ID: {data.uniqueID})");
                continue;
            }

            GameObject newObj = Instantiate(prefab, data.position, data.rotation);
            Debug.Log($"[SaveManager] Заспавнен объект: {newObj.name}, uniqueID: {data.uniqueID}, prefabID: {data.prefabIdentifier}");
            Rigidbody rb = newObj.GetComponent<Rigidbody>();
            if (rb) rb.isKinematic = true; // ← временно, чтобы не упал

            ISaveable saveable = newObj.GetComponent<ISaveable>();
            if (saveable != null)
            {
                saveable.LoadFromSaveData(data);

                if (!string.IsNullOrEmpty(data.parentPath))
                {
                    Transform parent = GameObject.Find(data.parentPath)?.transform;
                    if (parent) newObj.transform.SetParent(parent);
                }

                StartCoroutine(ActivatePhysicsAfterFrame(newObj, rb));
                spawned++;
            }
        }

        yield return new WaitForFixedUpdate();
        yield return new WaitForEndOfFrame(); // ← для корутин в RestoreStateAfterLoad
        Physics.SyncTransforms();
        yield return new WaitForEndOfFrame();
        yield return null;

        // ← ГЛАВНОЕ: Ищем минерал, который был в сканере
        SaveData scannerMineralData = wrapper.saves.Find(d => d.wasInScannerZone);
        if (scannerMineralData != null)
        {
            // Ищем объект с этим uniqueID
            var saveable = FindObjectsOfType<SaveableObject>()
                .FirstOrDefault(s => s.GetUniqueID() == scannerMineralData.uniqueID);

            if (saveable != null)
            {
                GameObject mineral = saveable.gameObject;
                SnapZone scannerZone = MineralScannerManager.Instance?.targetSnapZone;

                if (scannerZone != null)
                {
                    var grabbable = mineral.GetComponent<GrabbableItem>();
                    if (grabbable != null)
                    {
                        Debug.Log($"<color=magenta>[SaveManager] Восстанавливаем минерал в сканере: {mineral.name}</color>");
                        scannerZone.LoadSnappedItem(grabbable, scannerMineralData.snapPointIndex);

                        // Принудительно включаем сканер
                        MineralScannerManager.Instance?.ForceScanCurrentMineral();
                    }
                }
            }
        }
    }
        private IEnumerator ActivatePhysicsAfterFrame(GameObject obj, Rigidbody rb)
    {
        yield return new WaitForFixedUpdate();

        if (rb != null && obj != null)
        {
            // ← ВАЖНО: НЕ ЛОМАЕМ isKinematic! Он уже восстановлен из SaveData
            if (!rb.isKinematic) // только динамические объекты будим
            {
                rb.WakeUp();
            }
        }
    }
}

[System.Serializable]
public class SaveWrapper
{
    public List<SaveData> saves;
}