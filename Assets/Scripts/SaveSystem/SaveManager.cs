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
    private const int MAX_SLOTS = 10;
    private string BasePath => Application.persistentDataPath;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    // === СОХРАНЕНИЕ В СЛОТ ===
    public void SaveToSlot(int slotIndex, string slotName = null)
    {
        if (slotIndex < 0 || slotIndex >= MAX_SLOTS) return;

        var save = new SaveFile();
        CollectSaveData(ref save);

        var meta = new SaveSlotMeta
        {
            slotName = string.IsNullOrEmpty(slotName) ? $"Сохранение {slotIndex + 1}" : slotName.Trim(),
            day = GetCurrentDay(),
            timeInMinutes = GetCurrentTimeInMinutes()
        };

        SaveFileAndMeta(slotIndex, save, meta);
        Debug.Log($"[Save] Сохранено в слот {slotIndex}: {meta.slotName}");
    }

    public void AutoSave() => SaveToSlot(0, "Автосохранение");

    private void SaveFileAndMeta(int slot, SaveFile save, SaveSlotMeta meta)
    {
        string dataPath = Path.Combine(BasePath, $"slot_{slot}.json");
        string metaPath = Path.Combine(BasePath, $"slot_{slot}_meta.json");

        // Хэш без checksum
        var temp = new SaveFile
        {
            version = save.version,
            gameState = save.gameState,
            globalReports = save.globalReports,
            cameraLookDirection = save.cameraLookDirection,
            objects = save.objects,
            minerals = save.minerals,
            deposits = save.deposits
        };
        save.checksum = ComputeHash(JsonUtility.ToJson(temp, true));

        try
        {
            File.WriteAllText(dataPath + ".tmp", JsonUtility.ToJson(save, true));
            File.WriteAllText(metaPath, JsonUtility.ToJson(meta, true));

            if (File.Exists(dataPath))
                File.Replace(dataPath + ".tmp", dataPath, dataPath + ".bak");
            else
                File.Move(dataPath + ".tmp", dataPath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Save] Ошибка записи слота {slot}: {e.Message}");
        }
    }

    // === ЗАГРУЗКА ИЗ СЛОТА ===
    public void LoadFromSlot(int slotIndex)
    {
        string path = Path.Combine(BasePath, $"slot_{slotIndex}.json");
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[Save] Слот {slotIndex} не найден");
            return;
        }

        StartCoroutine(LoadCoroutine(path));
    }

    private IEnumerator LoadCoroutine(string path)
    {
        yield return new WaitForEndOfFrame();

        string json = File.ReadAllText(path);
        SaveFile save = JsonUtility.FromJson<SaveFile>(json);
        if (save == null)
        {
            Debug.LogError("[Save] Не удалось распарсить сохранение");
            yield break;
        }

        var temp = new SaveFile
        {
            version = save.version,
            gameState = save.gameState,
            globalReports = save.globalReports,
            cameraLookDirection = save.cameraLookDirection,
            objects = save.objects,
            minerals = save.minerals,
            deposits = save.deposits
        };

        if (save.checksum != ComputeHash(JsonUtility.ToJson(temp, true)))
        {
            Debug.LogError("[Save] Файл повреждён (checksum не совпал)");
            yield break;
        }

        // === ВОССТАНОВЛЕНИЕ ДАННЫХ ===
        var mineralDict = save.minerals.ToDictionary(m => m.uniqueID, m => m);
        var depositDict = save.deposits.ToDictionary(d => d.uniqueID, d => d);

        FindObjectOfType<GameStateSaver>()?.LoadFromBlock(save.gameState);
        var reportViewer = FindObjectOfType<ResearchReportViewer>();
        if (reportViewer != null && !string.IsNullOrEmpty(save.globalReports))
            reportViewer.DeserializeReports(save.globalReports);

        if (CameraController.Instance != null && save.cameraLookDirection != Vector2.zero)
            CameraController.Instance.LoadCameraDirection(save.cameraLookDirection);

        var existing = FindObjectsOfType<MonoBehaviour>(true).OfType<ISaveableV2>().ToList();
        var remaining = new List<ObjectSaveData>(save.objects);

        foreach (var s in existing)
        {
            var data = remaining.Find(d => d.uniqueID == s.GetUniqueID());
            if (data != null)
            {
                s.LoadCommonData(data);
                if (s is IHasMineralData m && mineralDict.TryGetValue(data.uniqueID, out var min)) m.LoadMineralData(min);
                if (s is IHasDepositData d && depositDict.TryGetValue(data.uniqueID, out var dep)) d.LoadDepositData(dep);
                remaining.Remove(data);
            }
        }

        foreach (var data in remaining)
        {
            var prefab = prefabRegistry?.GetPrefab(data.prefabIdentifier);
            if (!prefab) continue;
            var obj = Instantiate(prefab, data.position, data.rotation);
            if (obj.TryGetComponent<ISaveableV2>(out var s))
            {
                s.LoadCommonData(data);
                if (s is IHasMineralData m && mineralDict.TryGetValue(data.uniqueID, out var min)) m.LoadMineralData(min);
                if (s is IHasDepositData d && depositDict.TryGetValue(data.uniqueID, out var dep)) d.LoadDepositData(dep);
            }
        }

        yield return new WaitForFixedUpdate();
        Physics.SyncTransforms();

        CameraController.Instance?.ForceCameraSync();
        CameraController.Instance?.SetMode(CameraController.ControlMode.FPS);
        SaveFeedbackUI.ShowLoad();
    }

    // === ПОЛУЧИТЬ ВСЕ СЛОТЫ ===
    public List<SaveSlotInfo> GetAllSaveSlots()
    {
        var list = new List<SaveSlotInfo>();

        for (int i = 0; i < MAX_SLOTS; i++)
        {
            string dataPath = Path.Combine(BasePath, $"slot_{i}.json");
            string metaPath = Path.Combine(BasePath, $"slot_{i}_meta.json");

            if (File.Exists(dataPath))
            {
                SaveSlotMeta meta = File.Exists(metaPath)
                    ? JsonUtility.FromJson<SaveSlotMeta>(File.ReadAllText(metaPath))
                    : new SaveSlotMeta { slotName = "Сохранение без имени" };

                list.Add(new SaveSlotInfo
                {
                    slotIndex = i,
                    slotName = meta.slotName,
                    saveTime = meta.saveTime,
                    playTime = $"День {meta.day}, {FormatTime(meta.timeInMinutes)}",
                    hasData = true
                });
            }
            else
            {
                list.Add(new SaveSlotInfo
                {
                    slotIndex = i,
                    slotName = "Пустой слот",
                    saveTime = "",
                    playTime = "",
                    hasData = false
                });
            }
        }

        return list;
    }

    public void DeleteSlot(int slotIndex)
    {
        string data = Path.Combine(BasePath, $"slot_{slotIndex}.json");
        string meta = Path.Combine(BasePath, $"slot_{slotIndex}_meta.json");
        string bak = data + ".bak";

        if (File.Exists(data)) File.Delete(data);
        if (File.Exists(meta)) File.Delete(meta);
        if (File.Exists(bak)) File.Delete(bak);
    }

    // === ВАЛИДАЦИЯ ФАЙЛА (для MainMenu) ===
    public bool ValidateSaveFile(string path, out string error)
    {
        error = "";
        if (!File.Exists(path)) { error = "Файл не найден"; return false; }
        try
        {
            SaveFile save = JsonUtility.FromJson<SaveFile>(File.ReadAllText(path));
            if (save == null) { error = "Файл пустой"; return false; }
            if (save.version != SaveFile.CURRENT_VERSION) { error = "Устаревшая версия"; return false; }

            var temp = JsonUtility.FromJson<SaveFile>(JsonUtility.ToJson(save));
            temp.checksum = null;
            if (save.checksum != ComputeHash(JsonUtility.ToJson(temp))) { error = "Файл повреждён"; return false; }
            return true;
        }
        catch (Exception e) { error = e.Message; return false; }
    }

    private void CollectSaveData(ref SaveFile save)
    {
        var gs = FindObjectOfType<GameStateSaver>();
        if (gs != null) save.gameState = gs.GetGameStateBlock();

        var rv = FindObjectOfType<ResearchReportViewer>();
        if (rv != null) save.globalReports = rv.SerializeReports();

        if (CameraController.Instance != null)
        {
            var cam = CameraController.Instance.transform;
            save.cameraLookDirection = new Vector2(cam.eulerAngles.y, cam.eulerAngles.x);
        }

        var saveables = FindObjectsOfType<MonoBehaviour>(true).OfType<ISaveableV2>();
        foreach (var s in saveables)
        {
            save.objects.Add(s.GetCommonSaveData());
            if (s is IHasMineralData m) save.minerals.Add(m.GetMineralSaveData());
            if (s is IHasDepositData d) save.deposits.Add(d.GetDepositSaveData());
        }
    }

    private int GetCurrentDay() => 1;
    private float GetCurrentTimeInMinutes() => 1;
    private string FormatTime(float minutes) => $"{(int)minutes / 60:D2}:{(int)minutes % 60:D2}";

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (s.name.Contains("Menu")) return;

        if (File.Exists(Path.Combine(BasePath, "slot_0.json")))
            LoadFromSlot(0);
        else
        {
            CameraController.Instance?.SetMode(CameraController.ControlMode.FPS);
            CameraController.Instance?.ForceCameraSync();
        }
    }

    private string ComputeHash(string input)
    {
        using (var md5 = MD5.Create())
            return BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(input))).Replace("-", "").ToLowerInvariant();
    }
}

[System.Serializable]
public class SaveSlotInfo
{
    public int slotIndex;
    public string slotName;
    public string saveTime;
    public string playTime;
    public bool hasData;
}

[System.Serializable]
public class SaveSlotMeta
{
    public string slotName = "Новое сохранение";
    public string saveTime;
    public int day = 1;
    public float timeInMinutes = 0f;

    public SaveSlotMeta()
    {
        saveTime = DateTime.Now.ToString("dd MMMM yyyy, HH:mm");
    }
}