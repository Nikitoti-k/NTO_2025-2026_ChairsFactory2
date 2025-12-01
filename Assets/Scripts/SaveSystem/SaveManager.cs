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
    public PrefabRegistry prefabRegistry;

    private const int MAX_SLOTS = 3;
    private string BasePath => Application.persistentDataPath;

    private static int pendingLoadSlot = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void SaveGame(string customName = null)
    {
        string slotName = string.IsNullOrWhiteSpace(customName)
            ? GenerateDefaultName()
            : customName.Trim();

        int targetSlot = FindOldestSlot();

        var save = new SaveFile();
        CollectSaveData(ref save);

        var meta = new SaveSlotMeta
        {
            slotName = slotName,
            saveTime = DateTime.Now.ToString("dd MMMM yyyy, HH:mm")
        };

        string slotFolder = Path.Combine(BasePath, $"Save_{targetSlot}");
        Directory.CreateDirectory(slotFolder);

        string dataPath = Path.Combine(slotFolder, "data.json");
        string metaPath = Path.Combine(slotFolder, "meta.json");
        string previewPath = Path.Combine(slotFolder, "preview.png");

        StartCoroutine(CaptureCleanPreviewAndSave(targetSlot, save, meta, dataPath, metaPath, previewPath));
    }

    public void AutoSave() => SaveGame("Автосохранение");

    public string GenerateDefaultName()
    {
        int number = 1;
        var used = new HashSet<string>();

        for (int i = 0; i < MAX_SLOTS; i++)
        {
            string metaPath = Path.Combine(BasePath, $"Save_{i}", "meta.json");
            if (File.Exists(metaPath))
            {
                try
                {
                    var meta = JsonUtility.FromJson<SaveSlotMeta>(File.ReadAllText(metaPath));
                    if (!string.IsNullOrEmpty(meta.slotName))
                        used.Add(meta.slotName);
                }
                catch { }
            }
        }

        while (used.Contains("Сохранение " + number)) number++;
        return "Сохранение " + number;
    }

    private IEnumerator CaptureCleanPreviewAndSave(int slot, SaveFile save, SaveSlotMeta meta,
        string dataPath, string metaPath, string previewPath)
    {
        yield return new WaitForEndOfFrame();

        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        bool[] states = new bool[canvases.Length];

        for (int i = 0; i < canvases.Length; i++)
        {
            states[i] = canvases[i].gameObject.activeSelf;
            if (canvases[i].name.Contains("Load") || canvases[i].name.Contains("Save") ||
                canvases[i].renderMode != RenderMode.WorldSpace)
            {
                canvases[i].gameObject.SetActive(false);
            }
        }

        yield return new WaitForEndOfFrame();

        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        Texture2D preview = ScaleTexture(screenshot, 256, 144);
        Destroy(screenshot);

        for (int i = 0; i < canvases.Length; i++)
            canvases[i].gameObject.SetActive(states[i]);

        File.WriteAllBytes(previewPath, preview.EncodeToPNG());
        Destroy(preview);

        SaveFileAndMeta(save, meta, dataPath, metaPath);
        SaveFeedbackUI.ShowSave();
    }

    private Texture2D ScaleTexture(Texture2D source, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);
        Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }

    private int FindOldestSlot()
    {
        DateTime oldest = DateTime.MaxValue;
        int index = 0;

        for (int i = 0; i < MAX_SLOTS; i++)
        {
            string metaPath = Path.Combine(BasePath, $"Save_{i}", "meta.json");
            if (!File.Exists(metaPath)) return i;

            try
            {
                var meta = JsonUtility.FromJson<SaveSlotMeta>(File.ReadAllText(metaPath));
                if (DateTime.TryParse(meta.saveTime, out DateTime dt) && dt < oldest)
                {
                    oldest = dt;
                    index = i;
                }
            }
            catch { return i; }
        }
        return index;
    }

    private void SaveFileAndMeta(SaveFile save, SaveSlotMeta meta, string dataPath, string metaPath)
    {
        var clean = new SaveFile
        {
            version = save.version,
            gameState = save.gameState,
            globalReports = save.globalReports,
            cameraLookDirection = save.cameraLookDirection,
            objects = save.objects,
            minerals = save.minerals,
            deposits = save.deposits
        };

        save.checksum = ComputeHash(JsonUtility.ToJson(clean, true));

        File.WriteAllText(dataPath + ".tmp", JsonUtility.ToJson(save, true));
        File.WriteAllText(metaPath, JsonUtility.ToJson(meta, true));

        if (File.Exists(dataPath))
            File.Replace(dataPath + ".tmp", dataPath, dataPath + ".bak");
        else
            File.Move(dataPath + ".tmp", dataPath);
    }

    public void LoadFromSlot(int slotIndex)
    {
        string path = Path.Combine(BasePath, $"Save_{slotIndex}", "data.json");
        if (!File.Exists(path))
        {
            Debug.LogWarning($"Слот {slotIndex} не найден");
            return;
        }

        pendingLoadSlot = slotIndex;
        SceneManager.LoadScene("GameScene");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Contains("Menu")) return;

        if (pendingLoadSlot != -1)
        {
            string path = Path.Combine(BasePath, $"Save_{pendingLoadSlot}", "data.json");
            if (File.Exists(path))
                StartCoroutine(LoadCoroutine(path));
            pendingLoadSlot = -1;
            return;
        }

        for (int i = 0; i < MAX_SLOTS; i++)
        {
            string path = Path.Combine(BasePath, $"Save_{i}", "data.json");
            if (File.Exists(path))
            {
                StartCoroutine(LoadCoroutine(path));
                return;
            }
        }

        CameraController.Instance?.SetMode(CameraController.ControlMode.FPS);
    }

    private IEnumerator LoadCoroutine(string path)
    {
        yield return new WaitForEndOfFrame();

        string json = File.ReadAllText(path);
        SaveFile save = JsonUtility.FromJson<SaveFile>(json);

        if (save == null || save.version != SaveFile.CURRENT_VERSION)
        {
            Debug.LogError("Неверная версия или повреждённый файл");
            yield break;
        }

        var clean = new SaveFile
        {
            version = save.version,
            gameState = save.gameState,
            globalReports = save.globalReports,
            cameraLookDirection = save.cameraLookDirection,
            objects = save.objects,
            minerals = save.minerals,
            deposits = save.deposits
        };

        if (save.checksum != ComputeHash(JsonUtility.ToJson(clean, true)))
        {
            Debug.LogError("Checksum не совпал");
            yield break;
        }

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
                if (s is IHasMineralData m && mineralDict.TryGetValue(data.uniqueID, out var min))
                    m.LoadMineralData(min);
                if (s is IHasDepositData d && depositDict.TryGetValue(data.uniqueID, out var dep))
                    d.LoadDepositData(dep);
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
                if (s is IHasMineralData m && mineralDict.TryGetValue(data.uniqueID, out var min))
                    m.LoadMineralData(min);
                if (s is IHasDepositData d && depositDict.TryGetValue(data.uniqueID, out var dep))
                    d.LoadDepositData(dep);
            }
        }

        yield return new WaitForFixedUpdate();
        Physics.SyncTransforms();
        CameraController.Instance?.ForceCameraSync();
        CameraController.Instance?.SetMode(CameraController.ControlMode.FPS);

        SaveFeedbackUI.ShowLoad();
    }

    public List<SaveSlotInfo> GetAllSaveSlots()
    {
        var slots = new List<(int index, SaveSlotInfo info)>();

        for (int i = 0; i < MAX_SLOTS; i++)
        {
            string folder = Path.Combine(BasePath, $"Save_{i}");
            string dataPath = Path.Combine(folder, "data.json");
            string metaPath = Path.Combine(folder, "meta.json");
            string previewPath = Path.Combine(folder, "preview.png");

            SaveSlotInfo info;

            if (File.Exists(dataPath))
            {
                SaveSlotMeta meta = File.Exists(metaPath)
                    ? JsonUtility.FromJson<SaveSlotMeta>(File.ReadAllText(metaPath))
                    : new SaveSlotMeta { slotName = "???", saveTime = "???" };

                Texture2D preview = null;
                if (File.Exists(previewPath))
                {
                    byte[] bytes = File.ReadAllBytes(previewPath);
                    preview = new Texture2D(2, 2);
                    preview.LoadImage(bytes);
                }

                info = new SaveSlotInfo
                {
                    slotIndex = i,
                    slotName = meta.slotName,
                    saveTime = meta.saveTime,
                    previewTexture = preview,
                    hasData = true
                };
            }
            else
            {
                info = new SaveSlotInfo
                {
                    slotIndex = i,
                    slotName = "Пустой слот",
                    saveTime = "",
                    previewTexture = null,
                    hasData = false
                };
            }

            slots.Add((i, info));
        }

        var result = slots
            .OrderByDescending(x =>
            {
                if (!x.info.hasData) return DateTime.MinValue;
                return DateTime.TryParse(x.info.saveTime, out DateTime dt) ? dt : DateTime.MinValue;
            })
            .ThenBy(x => x.index)
            .Select(x => x.info)
            .ToList();

        return result;
    }

    public void DeleteSlot(int slotIndex)
    {
        string folder = Path.Combine(BasePath, $"Save_{slotIndex}");
        if (Directory.Exists(folder))
            Directory.Delete(folder, true);
    }

    public void CollectSaveData(ref SaveFile save)
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

    private string ComputeHash(string input)
    {
        using (var md5 = MD5.Create())
            return BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(input)))
                .Replace("-", "").ToLowerInvariant();
    }
}

[System.Serializable]
public class SaveSlotInfo
{
    public int slotIndex;
    public string slotName;
    public string saveTime;
    public Texture2D previewTexture;
    public bool hasData;
}

[System.Serializable]
public class SaveSlotMeta
{
    public string slotName = "Сохранение";
    public string saveTime = "";
}