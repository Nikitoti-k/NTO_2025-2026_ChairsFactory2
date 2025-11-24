using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Collections;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [SerializeField] private PrefabRegistry prefabRegistry;

    private string saveFilePath;
    private List<SaveData> saveDataList = new List<SaveData>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            saveFilePath = Application.persistentDataPath + "/save.json";
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void SaveGame()
    {
        saveDataList.Clear();
        var saveables = FindObjectsOfType<MonoBehaviour>(true).OfType<ISaveable>().ToList();
        Debug.Log("Saving objects:");  // Дебаг: Список сохраняемых
        foreach (var saveable in saveables)
        {
            var data = saveable.GetSaveData();
            Debug.Log($"Saving ID: {data.uniqueID}, Prefab: {data.prefabIdentifier}, Type: {saveable.GetType().Name}");
            saveDataList.Add(data);
        }

        string json = JsonUtility.ToJson(new SaveWrapper { saves = saveDataList }, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("Game saved. Objects: " + saveDataList.Count);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(LoadGameAfterSpawn());
    }

    private IEnumerator LoadGameAfterSpawn()
    {
        for (int i = 0; i < 5; i++) yield return null;
        yield return new WaitForFixedUpdate();

        if (!File.Exists(saveFilePath))
        {
            Debug.Log("No save file");
            yield break;
        }

        string json = File.ReadAllText(saveFilePath);
        SaveWrapper wrapper = JsonUtility.FromJson<SaveWrapper>(json);
        saveDataList = wrapper.saves ?? new List<SaveData>();

        // Дебаг: Список загружаемых данных
        Debug.Log("Loaded save data:");
        foreach (var data in saveDataList)
        {
            Debug.Log($"Loaded Data ID: {data.uniqueID}, Prefab: {data.prefabIdentifier}");
        }

        var saveables = FindObjectsOfType<MonoBehaviour>(true).OfType<ISaveable>().ToList();
        // Дебаг: Список existing объектов
        Debug.Log("Existing saveables on scene:");
        foreach (var saveable in saveables)
        {
            Debug.Log($"Existing ID: {saveable.GetUniqueID()}, Type: {saveable.GetType().Name}");
        }

        List<SaveData> remainingData = new List<SaveData>(saveDataList);
        int loadedExisting = 0;

        foreach (var saveable in saveables)
        {
            SaveData data = remainingData.FirstOrDefault(d => d.uniqueID == saveable.GetUniqueID());
            if (data != null)
            {
                Debug.Log($"Matching and loading existing ID: {data.uniqueID}");
                saveable.LoadFromSaveData(data);
                remainingData.Remove(data);
                loadedExisting++;
            }
        }

        // Дебаг: Оставшиеся для спавна
        Debug.Log("Remaining data for spawn:");
        foreach (var data in remainingData)
        {
            Debug.Log($"To spawn ID: {data.uniqueID}, Prefab: {data.prefabIdentifier}");
        }

        int spawned = 0;
        foreach (var data in remainingData)
        {
            if (string.IsNullOrEmpty(data.prefabIdentifier)) continue;

            GameObject prefab = prefabRegistry?.GetPrefab(data.prefabIdentifier);
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>(data.prefabIdentifier);
            }
            if (prefab == null)
            {
                Debug.LogWarning("Prefab not found: " + data.prefabIdentifier);
                continue;
            }

            Vector3 raisedPosition = data.position + new Vector3(0, 0.1f, 0);
            GameObject newObj = Instantiate(prefab, raisedPosition, data.rotation);

            Rigidbody newRb = newObj.GetComponent<Rigidbody>();
            if (newRb) newRb.isKinematic = true;

            ISaveable saveable = newObj.GetComponent<ISaveable>();
            if (saveable != null)
            {
                saveable.LoadFromSaveData(data);

                if (!string.IsNullOrEmpty(data.parentPath))
                {
                    Transform parent = GameObject.Find(data.parentPath)?.transform;
                    if (parent) newObj.transform.SetParent(parent);
                }

                if (newRb) newRb.isKinematic = false;

                spawned++;
            }
        }

        Physics.SyncTransforms();
        Debug.Log($"Loaded: Existing {loadedExisting}, Spawned {spawned}, Total saveables now {FindObjectsOfType<MonoBehaviour>(true).OfType<ISaveable>().Count()}");
    }
}

[System.Serializable]
public class SaveWrapper
{
    public List<SaveData> saves;
}