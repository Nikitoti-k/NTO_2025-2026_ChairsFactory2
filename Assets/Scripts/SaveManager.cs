using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Collections;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [SerializeField] private PrefabRegistry prefabRegistry; // ← ОБЯЗАТЕЛЬНО назначить в сцене!

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
        Debug.Log($"Game saved! Objects: {saveDataList.Count}");
    }

    public void LoadGame() => StartCoroutine(LoadGameAfterSpawn());

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => StartCoroutine(LoadGameAfterSpawn());

    private IEnumerator LoadGameAfterSpawn()
    {
        // Ждём, пока всё точно заспавнится и Awake/Start отработают
        for (int i = 0; i < 10; i++) yield return null;
        yield return new WaitForFixedUpdate();

        if (!File.Exists(saveFilePath))
        {
            Debug.Log("No save file found");
            yield break;
        }

        string json = File.ReadAllText(saveFilePath);
        SaveWrapper wrapper = JsonUtility.FromJson<SaveWrapper>(json);
        var saveDataList = wrapper.saves ?? new List<SaveData>();

        // 1. Загружаем уже существующие объекты в сцене
        var existingSaveables = FindObjectsOfType<MonoBehaviour>(true).OfType<ISaveable>().ToList();
        var remainingData = new List<SaveData>(saveDataList);

        foreach (var saveable in existingSaveables)
        {
            var data = remainingData.Find(d => d.uniqueID == saveable.GetUniqueID());
            if (data != null)
            {
                saveable.LoadFromSaveData(data);
                remainingData.Remove(data);
            }
        }

        // 2. Спавним те, которых нет в сцене
        int spawned = 0;
        foreach (var data in remainingData)
        {
            if (string.IsNullOrEmpty(data.prefabIdentifier))
                continue;

            GameObject prefab = prefabRegistry?.GetPrefab(data.prefabIdentifier);
            if (prefab == null)
            {
                Debug.LogError($"[SaveManager] Prefab not found in Registry: {data.prefabIdentifier} (ID: {data.uniqueID})");
                continue;
            }

            GameObject newObj = Instantiate(prefab, data.position, data.rotation);

            Rigidbody rb = newObj.GetComponent<Rigidbody>();
            if (rb) rb.isKinematic = true;

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
        Physics.SyncTransforms();
        Debug.Log($"Load complete! Spawned new objects: {spawned}");
    }

    private IEnumerator ActivatePhysicsAfterFrame(GameObject obj, Rigidbody rb)
    {
        yield return new WaitForFixedUpdate();
        if (rb != null && obj != null)
        {
            rb.isKinematic = false;
            rb.WakeUp();
        }
    }
}

[System.Serializable]
public class SaveWrapper
{
    public List<SaveData> saves;
}