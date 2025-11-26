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

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        saveFilePath = Path.Combine(Application.persistentDataPath, "save.json");
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void SaveGame()
    {
        var saveDataList = new List<SaveData>();
        foreach (var saveable in FindObjectsOfType<MonoBehaviour>(true).OfType<ISaveable>())
            saveDataList.Add(saveable.GetSaveData());

        string json = JsonUtility.ToJson(new SaveWrapper { saves = saveDataList }, true);
        File.WriteAllText(saveFilePath, json);
    }

    public void LoadGame() => StartCoroutine(LoadGameAfterSpawn());
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => StartCoroutine(LoadGameAfterSpawn());

    private IEnumerator LoadGameAfterSpawn()
    {
        yield return new WaitForSeconds(0.1f);
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
            if (!prefab) continue;

            GameObject obj = Instantiate(prefab, data.position, data.rotation);
            if (obj.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;

            if (obj.TryGetComponent<ISaveable>(out var saveable))
            {
                saveable.LoadFromSaveData(data);
                if (!string.IsNullOrEmpty(data.parentPath))
                {
                    Transform parent = GameObject.Find(data.parentPath)?.transform;
                    if (parent) obj.transform.SetParent(parent);
                }
                if (rb) StartCoroutine(ActivatePhysicsNextFrame(obj, rb));
            }
        }

        yield return new WaitForFixedUpdate();
        yield return new WaitForEndOfFrame();
        Physics.SyncTransforms();

        SaveData scannerMineral = wrapper.saves.Find(d => d.wasInScannerZone);
        if (scannerMineral != null)
        {
            var saveable = FindObjectsOfType<SaveableObject>().FirstOrDefault(s => s.GetUniqueID() == scannerMineral.uniqueID);
            if (saveable != null && saveable.TryGetComponent<GrabbableItem>(out var grabbable))
            {
                MineralScannerManager.Instance?.targetSnapZone?.LoadSnappedItem(grabbable, scannerMineral.snapPointIndex);
                MineralScannerManager.Instance?.ForceScanCurrentMineral();
            }
        }
    }

    private IEnumerator ActivatePhysicsNextFrame(GameObject obj, Rigidbody rb)
    {
        yield return new WaitForFixedUpdate();
        if (rb && obj && !rb.isKinematic) rb.WakeUp();
    }
}

[System.Serializable]
public class SaveWrapper
{
    public List<SaveData> saves;
}