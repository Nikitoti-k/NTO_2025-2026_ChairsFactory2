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

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            saveFilePath = Path.Combine(Application.persistentDataPath, "save.json");
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else Destroy(gameObject);
    }

    void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    public void SaveGame()
    {
        var saveDataList = new List<SaveData>();
        var saveables = FindObjectsOfType<MonoBehaviour>(true).OfType<ISaveable>();
        foreach (var s in saveables) saveDataList.Add(s.GetSaveData());

        string json = JsonUtility.ToJson(new SaveWrapper { saves = saveDataList }, true);
        File.WriteAllText(saveFilePath, json);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => StartCoroutine(LoadGameAfterSpawn());

    private IEnumerator LoadGameAfterSpawn()
    {
        for (int i = 0; i < 10; i++) yield return null;
        yield return new WaitForFixedUpdate();

        if (!File.Exists(saveFilePath)) yield break;

        string json = File.ReadAllText(saveFilePath);
        SaveWrapper wrapper = JsonUtility.FromJson<SaveWrapper>(json);
        var saveDataList = wrapper.saves ?? new List<SaveData>();

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

        foreach (var data in remainingData)
        {
            if (string.IsNullOrEmpty(data.prefabIdentifier)) continue;

            GameObject prefab = prefabRegistry?.GetPrefab(data.prefabIdentifier);
            if (prefab == null)
            {
                Debug.LogError($"[SaveManager] Prefab not found: {data.prefabIdentifier} (ID: {data.uniqueID})");
                continue;
            }

            GameObject obj = Instantiate(prefab, data.position, data.rotation);
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb) rb.isKinematic = true;

            var saveable = obj.GetComponent<ISaveable>();
            if (saveable != null)
            {
                saveable.LoadFromSaveData(data);

                if (!string.IsNullOrEmpty(data.parentPath))
                {
                    Transform parent = GameObject.Find(data.parentPath)?.transform;
                    if (parent) obj.transform.SetParent(parent);
                }

                StartCoroutine(ActivatePhysics(obj, rb));
            }
        }

        yield return new WaitForFixedUpdate();
        Physics.SyncTransforms();
    }

    private IEnumerator ActivatePhysics(GameObject obj, Rigidbody rb)
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
