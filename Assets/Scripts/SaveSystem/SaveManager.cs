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
    private string BasePath => Application.persistentDataPath;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    public void SaveGame(string slot = "manual") => SaveInternal(slot);

    private void SaveInternal(string slot)
    {
        var save = new SaveFile();

        var gameStateObj = FindObjectOfType<GameStateSaver>();
        if (gameStateObj != null) save.gameState = gameStateObj.GetGameStateBlock();

        var reportViewer = FindObjectOfType<ResearchReportViewer>();
        if (reportViewer != null) save.globalReports = reportViewer.SerializeReports();

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

        var temp = new SaveFile { version = save.version, gameState = save.gameState, globalReports = save.globalReports, cameraLookDirection = save.cameraLookDirection, objects = save.objects, minerals = save.minerals, deposits = save.deposits };
        save.checksum = ComputeHash(JsonUtility.ToJson(temp, true));

        string path = Path.Combine(BasePath, $"{slot}.json");
        string tempPath = path + ".tmp";
        string backup = path + ".bak";

        try
        {
            File.WriteAllText(tempPath, JsonUtility.ToJson(save, true));
            if (File.Exists(path)) File.Replace(tempPath, path, backup);
            else File.Move(tempPath, path);
        }
        catch (System.Exception e) { Debug.LogError(e); }
    }

    public void LoadGame(string slot = "manual") => StartCoroutine(LoadCoroutine(slot));

    private IEnumerator LoadCoroutine(string slot)
    {
        yield return new WaitForEndOfFrame();

        string path = Path.Combine(BasePath, $"{slot}.json");
        if (!File.Exists(path)) yield break;

        SaveFile save = JsonUtility.FromJson<SaveFile>(File.ReadAllText(path));
        if (save == null) yield break;

        var temp = new SaveFile { version = save.version, gameState = save.gameState, globalReports = save.globalReports, cameraLookDirection = save.cameraLookDirection, objects = save.objects, minerals = save.minerals, deposits = save.deposits };
        if (save.checksum != ComputeHash(JsonUtility.ToJson(temp, true))) yield break;

        var mineralDict = save.minerals.ToDictionary(m => m.uniqueID, m => m);
        var depositDict = save.deposits.ToDictionary(d => d.uniqueID, d => d);

        FindObjectOfType<GameStateSaver>()?.LoadFromBlock(save.gameState);
        var reportViewer = FindObjectOfType<ResearchReportViewer>();
        if (reportViewer != null && !string.IsNullOrEmpty(save.globalReports)) reportViewer.DeserializeReports(save.globalReports);

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

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (File.Exists(Path.Combine(BasePath, "manual.json")))
            StartCoroutine(LoadCoroutine("manual"));
        else if (File.Exists(Path.Combine(BasePath, "auto.json")))
            StartCoroutine(LoadCoroutine("auto"));
        else
        {
            CameraController.Instance?.SetMode(CameraController.ControlMode.FPS);
            CameraController.Instance?.ForceCameraSync();
        }
    }

    public bool ValidateSaveFile(string path, out string errorReason)
    {
        errorReason = "";
        if (!File.Exists(path)) { errorReason = "Файл сохранения не найден."; return false; }
        try
        {
            string json = File.ReadAllText(path);
            SaveFile save = JsonUtility.FromJson<SaveFile>(json);
            if (save == null) { errorReason = "Файл пустой или повреждён."; return false; }
            if (save.version != SaveFile.CURRENT_VERSION) { errorReason = $"Версия сохранения устарела (текущая: {SaveFile.CURRENT_VERSION})"; return false; }
            var temp = new SaveFile { version = save.version, gameState = save.gameState, globalReports = save.globalReports, cameraLookDirection = save.cameraLookDirection, objects = save.objects, minerals = save.minerals, deposits = save.deposits };
            if (save.checksum != ComputeHash(JsonUtility.ToJson(temp, true))) { errorReason = "Файл повреждён (checksum не совпал)."; return false; }
            return true;
        }
        catch (System.Exception e) { errorReason = $"Ошибка чтения: {e.Message}"; return false; }
    }

    private string ComputeHash(string input)
    {
        using (var md5 = MD5.Create())
            return BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(input))).Replace("-", "").ToLowerInvariant();
    }
}