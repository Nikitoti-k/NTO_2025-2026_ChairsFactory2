using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using System;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    [SerializeField] public PrefabRegistry prefabRegistry;
    private string basePath;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        basePath = Application.persistentDataPath;
        SceneManager.sceneLoaded += OnSceneLoaded;
     //   LoadGame("manual");
        // Автосохранения: каждые 5 мин
        // InvokeRepeating("AutoSave", 300f, 300f);
    }

    private void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
        CancelInvoke("AutoSave");
    }

    public void SaveGame(string slot = "manual")
    {
        SaveInternal(slot);
        Debug.Log($"[SaveManager] Сохранено в слот: {slot}");
        // UX: Добавь сообщение/иконку/звук здесь
    }

    private void AutoSave()
    {
        SaveInternal("auto");
        Debug.Log("[SaveManager] Автосохранение выполнено");
        // UX: Тихая обратная связь
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Сначала пробуем ручное, если нет — автосохранение
        string pathManual = Path.Combine(Application.persistentDataPath, "manual.json");
        string pathAuto = Path.Combine(Application.persistentDataPath, "auto.json");

        if (File.Exists(pathManual))
            StartCoroutine(LoadGameAfterSpawn("manual"));
        else if (File.Exists(pathAuto))
            StartCoroutine(LoadGameAfterSpawn("auto"));
        else
            Debug.Log("Нет сохранений — новая игра");
    }
    private void SaveInternal(string slot)
    {
        var saveDataList = new List<SaveData>();
        foreach (var saveable in FindObjectsOfType<MonoBehaviour>(true).OfType<ISaveable>())
            saveDataList.Add(saveable.GetSaveData());

        var wrapper = new SaveWrapper { version = "1.0", saves = saveDataList };

        string jsonWithoutHash = JsonUtility.ToJson(wrapper, true);
        wrapper.checksum = ComputeHash(jsonWithoutHash);
        string finalJson = JsonUtility.ToJson(wrapper, true);

        string savePath = Path.Combine(basePath, $"{slot}.json");
        string tempPath = savePath + ".tmp";
        string backupPath = savePath + ".bak";

        try
        {
            File.WriteAllText(tempPath, finalJson);

            string testJson = File.ReadAllText(tempPath);
            SaveWrapper testWrapper = JsonUtility.FromJson<SaveWrapper>(testJson);
            if (testWrapper == null || !ValidateWrapper(testWrapper))
                throw new Exception("Данные не прошли валидацию!");

            if (File.Exists(savePath))
                File.Replace(tempPath, savePath, backupPath);
            else
                File.Move(tempPath, savePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Ошибка сохранения: {e.Message}");
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    public void LoadGame(string slot = "manual") => StartCoroutine(LoadGameAfterSpawn(slot));

 //   private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => StartCoroutine(LoadGameAfterSpawn("manual"));

    private IEnumerator LoadGameAfterSpawn(string slot)
    {
        yield return new WaitForSeconds(0.1f);
        yield return new WaitForFixedUpdate();

        string savePath = Path.Combine(basePath, $"{slot}.json");
        string backupPath = savePath + ".bak";
        string currentPath = savePath;

        if (!File.Exists(savePath))
        {
            if (File.Exists(backupPath))
            {
                Debug.LogWarning("[SaveManager] Загружаем бэкап");
                currentPath = backupPath;
            }
            else
            {
                yield break;
            }
        }

        string json = string.Empty;
        SaveWrapper wrapper = null;
        bool loadSuccess = false;

        try
        {
            json = File.ReadAllText(currentPath);
            wrapper = JsonUtility.FromJson<SaveWrapper>(json);

            if (wrapper == null) throw new Exception("Невалидный JSON");

            string jsonWithoutHash = JsonUtility.ToJson(new SaveWrapper { version = wrapper.version, saves = wrapper.saves }, true);
            if (wrapper.checksum != ComputeHash(jsonWithoutHash))
                throw new Exception("Checksum не совпадает");

            if (wrapper.version != "1.0")
                throw new Exception($"Несовместимая версия: {wrapper.version}");

            if (!ValidateWrapper(wrapper))
                throw new Exception("Данные невалидны");

            loadSuccess = true; // Если дошли сюда — можно безопасно загружать
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Ошибка: {e.Message}. Пробуем бэкап...");

            if (File.Exists(backupPath) && currentPath != backupPath)
            {
                StartCoroutine(LoadGameAfterSpawn(slot + ".bak"));
                yield break;
            }
            else
            {
                File.Delete(savePath);
                Debug.LogWarning("[SaveManager] Битый файл удалён, дефолт");
                yield break;
            }
        }

        // === Всё, что ниже — выполняется только при успешной загрузке JSON ===
        // Теперь yield return здесь разрешены

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
    if (string.IsNullOrEmpty(data.prefabIdentifier))
    {
        Debug.LogWarning($"[SaveManager] Объект без prefabIdentifier, пропускаем: {data.uniqueID}");
        continue;
    }

    GameObject prefab = prefabRegistry?.GetPrefab(data.prefabIdentifier);
    if (!prefab)
    {
        Debug.LogError($"[SaveManager] ПРЕФАБ НЕ НАЙДЕН В РЕЕСТРЕ: \"{data.prefabIdentifier}\" | uniqueID: {data.uniqueID}");
        continue;
    }

    Debug.Log($"[SaveManager] Спавним из реестра: {data.prefabIdentifier} | ID: {data.uniqueID} | Pos: {data.position}");

    GameObject obj = Instantiate(prefab, data.position, data.rotation);
    
    if (obj.TryGetComponent<Rigidbody>(out var rb))
        rb.isKinematic = true;

    if (obj.TryGetComponent<ISaveable>(out var saveable))
    {
        saveable.LoadFromSaveData(data);
        Debug.Log($"[SaveManager] Данные применены к {data.prefabIdentifier} | Mineral age: {(data.mineral != null ? data.mineral.realAge.ToString() : "нет")}");
    }
    else
    {
        Debug.LogError($"[SaveManager] У заспавненного объекта {data.prefabIdentifier} НЕТ ISaveable!");
    }

    if (!string.IsNullOrEmpty(data.parentPath))
    {
        Transform parent = GameObject.Find(data.parentPath)?.transform;
        if (parent) obj.transform.SetParent(parent);
    }

    if (rb) StartCoroutine(ActivatePhysicsNextFrame(obj, rb));
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

        Debug.Log($"[SaveManager] Загружено из {slot}");
    }

    private IEnumerator ActivatePhysicsNextFrame(GameObject obj, Rigidbody rb)
    {
        yield return new WaitForFixedUpdate();
        if (rb && obj && !rb.isKinematic) rb.WakeUp();
    }

    private string ComputeHash(string input)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }

    private bool ValidateWrapper(SaveWrapper wrapper)
    {
        if (wrapper.saves == null)
        {
            Debug.LogError("ValidateWrapper: wrapper.saves == null");
            return false;
        }

        var uniqueIDs = new HashSet<string>();
        foreach (var data in wrapper.saves)
        {
            // 1. Проверка дубликатов ID
            if (!uniqueIDs.Add(data.uniqueID))
            {
                Debug.LogError($"ДУБЛИКАТ UNIQUE ID: {data.uniqueID} (объект: {data.prefabIdentifier})");
                return false;
            }

            // 2. Проверка валидности каждого SaveData
            if (!data.Validate())
            {
                Debug.LogError($"НЕВАЛИДНЫЕ ДАННЫЕ у объекта {data.uniqueID} ({data.prefabIdentifier})");

                // Дополнительно выводим, что именно сломалось
                if (string.IsNullOrEmpty(data.uniqueID))
                    Debug.LogError(" → uniqueID пустой!");
                if (float.IsNaN(data.position.x) || float.IsInfinity(data.position.x))
                    Debug.LogError($" → Позиция NaN/Infinity: {data.position}");
                if (data.gameState != null && data.gameState.currentDay < 0)
                    Debug.LogError($" → Отрицательный день: {data.gameState.currentDay}");
                if (data.mineral != null && data.mineral.realAge < 0)
                    Debug.LogError($" → Отрицательный возраст минерала: {data.mineral.realAge}");
                // Добавь свои проверки по необходимости

                return false;
            }
        }
        return true;
    }
}

[System.Serializable]
public class SaveWrapper
{
    public string version;
    public string checksum;
    public List<SaveData> saves;
}