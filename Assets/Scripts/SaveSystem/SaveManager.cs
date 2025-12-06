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

    [Header("References")]
    public PrefabRegistry prefabRegistry;

    private const int MAX_SLOTS = 3;
    private const string SAVE_FOLDER = "Saves"; // чтобы не мусорить в корне persistentDataPath
    private string BasePath => Path.Combine(Application.persistentDataPath, SAVE_FOLDER);

    public int GetCurrentObjectCountEstimate() => 150;

    // Для надёжной загрузки конкретного слота
    private static int pendingLoadSlot = -1;
    private static bool forceNewGame = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Создаём папку, если её нет
        if (!Directory.Exists(BasePath))
            Directory.CreateDirectory(BasePath);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    #region === ПУБЛИЧНЫЕ МЕТОДЫ ===

    public static void ForceNewGameNextLaunch() => forceNewGame = true;

    public void SaveGame(string customName = null)
    {
        string slotName = string.IsNullOrWhiteSpace(customName)
            ? GenerateDefaultSaveName()
            : customName.Trim();

        int targetSlot = ChooseSlotForSave();

        var save = new SaveFile { version = SaveFile.CURRENT_VERSION };
        CollectAllSaveData(ref save);

        var meta = new SaveSlotMeta
        {
            slotName = slotName,
            saveTime = DateTime.Now
        };

        string slotFolder = GetSlotFolder(targetSlot);
        Directory.CreateDirectory(slotFolder);

        string dataPath = Path.Combine(slotFolder, "data.json");
        string metaPath = Path.Combine(slotFolder, "meta.json");
        string previewPath = Path.Combine(slotFolder, "preview.png");

        StartCoroutine(SaveWithCleanPreviewCoroutine(targetSlot, save, meta, dataPath, metaPath, previewPath));
    }

    public void AutoSave() => SaveGame("Автосохранение");

    public void LoadFromSlot(int slotIndex)
    {
        string folder = GetSlotFolder(slotIndex);
        if (!File.Exists(Path.Combine(folder, "data.json")))
        {
            Debug.LogWarning($"Слот {slotIndex} пустой");
            return;
        }

        pendingLoadSlot = slotIndex;
        forceNewGame = false; // на всякий случай
        SceneManager.LoadScene("GameScene");
    }

    public void DeleteSlot(int slotIndex)
    {
        string folder = GetSlotFolder(slotIndex);
        if (Directory.Exists(folder))
        {
            try { Directory.Delete(folder, true); }
            catch (System.Exception e) { Debug.LogError("Не удалось удалить слот: " + e); }
        }
    }

    public List<SaveSlotInfo> GetAllSaveSlots()
    {
        var list = new List<SaveSlotInfo>();

        for (int i = 0; i < MAX_SLOTS; i++)
        {
            string folder = GetSlotFolder(i);
            string dataPath = Path.Combine(folder, "data.json");
            string metaPath = Path.Combine(folder, "meta.json");
            string previewPath = Path.Combine(folder, "preview.png");

            SaveSlotInfo info = new SaveSlotInfo
            {
                slotIndex = i,
                hasData = File.Exists(dataPath)
            };

            if (info.hasData)
            {
                try
                {
                    string metaJson = File.ReadAllText(metaPath);
                    SaveSlotMeta meta = JsonUtility.FromJson<SaveSlotMeta>(metaJson);
                    info.slotName = meta.slotName;
                    info.saveTime = meta.saveTime.ToString("dd MMMM yyyy, HH:mm");

                    if (File.Exists(previewPath))
                    {
                        byte[] bytes = File.ReadAllBytes(previewPath);
                        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
                        tex.LoadImage(bytes);
                        info.previewTexture = tex;
                    }
                }
                catch
                {
                    info.slotName = "Повреждено";
                    info.saveTime = "??";
                }
            }
            else
            {
                info.slotName = "Пустой слот";
                info.saveTime = "";
            }

            list.Add(info);
        }

        // Сортировка: сначала новые, потом старые, пустые — в конец
        return list
            .OrderByDescending(x => x.hasData ? DateTime.TryParse(x.saveTime, out var dt) ? dt : DateTime.MinValue : DateTime.MinValue)
            .ThenBy(x => x.slotIndex)
            .ToList();
    }

    #endregion

    #region === ВНУТРЕННЯЯ ЛОГИКА СОХРАНЕНИЯ ===

    private int ChooseSlotForSave()
    {
        // 1. Ищем полностью пустой слот
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            if (!File.Exists(Path.Combine(GetSlotFolder(i), "data.json")))
                return i;
        }

        // 2. Все заняты — ищем самый старый по дате в meta.json
        DateTime oldest = DateTime.MaxValue;
        int oldestIndex = 0;

        for (int i = 0; i < MAX_SLOTS; i++)
        {
            string metaPath = Path.Combine(GetSlotFolder(i), "meta.json");
            if (!File.Exists(metaPath)) continue;

            try
            {
                var meta = JsonUtility.FromJson<SaveSlotMeta>(File.ReadAllText(metaPath));
                if (meta.saveTime < oldest)
                {
                    oldest = meta.saveTime;
                    oldestIndex = i;
                }
            }
            catch
            { /* повреждённый meta — можно смело перезаписать */
                return i;
            }
        }

        return oldestIndex;
    }

    public string GenerateDefaultSaveName()
    {
        var usedNames = new HashSet<string>();

        for (int i = 0; i < MAX_SLOTS; i++)
        {
            string metaPath = Path.Combine(GetSlotFolder(i), "meta.json");
            if (File.Exists(metaPath))
            {
                try
                {
                    var meta = JsonUtility.FromJson<SaveSlotMeta>(File.ReadAllText(metaPath));
                    if (!string.IsNullOrEmpty(meta.slotName))
                        usedNames.Add(meta.slotName);
                }
                catch { }
            }
        }

        int num = 1;
        while (usedNames.Contains("Сохранение " + num)) num++;
        return "Сохранение " + num;
    }

    private IEnumerator SaveWithCleanPreviewCoroutine(int slot, SaveFile saveFile, SaveSlotMeta meta,
        string dataPath, string metaPath, string previewPath)
    {
        // Ждём конца кадра, чтобы UI точно отрисовался
        yield return new WaitForEndOfFrame();

        // === СКРЫВАЕМ ВЕСЬ UI ===
        Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);
        var originalStates = new List<(Canvas canvas, bool active)>();

        foreach (var canvas in allCanvases)
        {
            if (canvas.isRootCanvas && canvas.gameObject.activeInHierarchy)
            {
                originalStates.Add((canvas, canvas.gameObject.activeSelf));
                canvas.gameObject.SetActive(false);
            }
        }

        // Ещё один кадр — чтобы скрытие применилось
        yield return new WaitForEndOfFrame();

        // Делаем скриншот
        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        Texture2D preview = CreateCleanPreview(screenshot);
        Destroy(screenshot);

        // Возвращаем UI
        foreach (var (canvas, wasActive) in originalStates)
            canvas.gameObject.SetActive(wasActive);

        // Сохраняем превью
        File.WriteAllBytes(previewPath, preview.EncodeToPNG());
        Destroy(preview);

        // === НАДЁЖНОЕ СОХРАНЕНИЕ ФАЙЛА ===
        string cleanJson = JsonUtility.ToJson(saveFile.GetCleanCopy(), true);
        saveFile.checksum = ComputeHash(cleanJson);

        string finalJson = JsonUtility.ToJson(saveFile, true);

        // Пишем через .tmp → заменяем атомарно
        string tmpPath = dataPath + ".tmp";
        File.WriteAllText(tmpPath, finalJson);
        File.WriteAllText(metaPath, JsonUtility.ToJson(meta, true));

        if (File.Exists(dataPath))
            File.Replace(tmpPath, dataPath, dataPath + ".bak");
        else
            File.Move(tmpPath, dataPath);

        SaveFeedbackUI.ShowSave();
        Debug.Log($"Игра сохранена в слот {slot} — {meta.slotName}");
    }

    private Texture2D CreateCleanPreview(Texture2D source)
    {
        int targetWidth = 256;
        int targetHeight = 144;

        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, rt, new Vector2(1, -1), new Vector2(0, 1)); // переворачиваем по Y (Unity делает upside-down)

        RenderTexture.active = rt;
        Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }

    #endregion

    // Продолжение в следующем сообщении → Часть 2/3 (загрузка и OnSceneLoaded)
    #region === ЗАГРУЗКА И АВТОЗАГРУЗКА ===

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.name.Equals("GameScene"))
            return;

        // 1. Если игрок явно выбрал слот — грузим его и выходим
        if (pendingLoadSlot != -1)
        {
            string path = Path.Combine(GetSlotFolder(pendingLoadSlot), "data.json");
            if (File.Exists(path))
                StartCoroutine(LoadGameCoroutine(path, isNewGame: false));
            else
                Debug.LogError($"Сохранение слота {pendingLoadSlot} исчезло во время загрузки сцены!");

            pendingLoadSlot = -1;
            return;
        }

        // 2. Если игрок явно попросил новую игру
        if (forceNewGame)
        {
            forceNewGame = false;
            StartCoroutine(NewGameSetup());
            return;
        }

        // 3. Автозагрузка САМОГО НОВОГО сохранения (а не Save_0!)
        int newestSlot = GetNewestExistingSlot();
        if (newestSlot != -1)
        {
            string path = Path.Combine(GetSlotFolder(newestSlot), "data.json");
            StartCoroutine(LoadGameCoroutine(path, isNewGame: false));
            Debug.Log($"Автозагрузка последнего сохранения — слот {newestSlot}");
        }
        else
        {
            // 4. Совсем нет сохранений → новая игра
            StartCoroutine(NewGameSetup());
        }
    }

    /// <summary>
    /// Возвращает индекс самого нового слота или -1
    /// </summary>
    private int GetNewestExistingSlot()
    {
        DateTime newestTime = DateTime.MinValue;
        int newestIndex = -1;

        for (int i = 0; i < MAX_SLOTS; i++)
        {
            string metaPath = Path.Combine(GetSlotFolder(i), "meta.json");
            if (!File.Exists(metaPath)) continue;

            try
            {
                var meta = JsonUtility.FromJson<SaveSlotMeta>(File.ReadAllText(metaPath));
                if (meta.saveTime > newestTime)
                {
                    newestTime = meta.saveTime;
                    newestIndex = i;
                }
            }
            catch { /* повреждённый meta — пропускаем */ }
        }

        return newestIndex;
    }

    private IEnumerator LoadGameCoroutine(string dataPath, bool isNewGame)
    {
        yield return new WaitForEndOfFrame(); // даём всем Awake/Start отработать

        string json = File.ReadAllText(dataPath);
        SaveFile save = null;

        try
        {
            save = JsonUtility.FromJson<SaveFile>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Не удалось распарсить JSON сохранения: " + e);
            yield break;
        }

        // === ПРОВЕРКА ВЕРСИИ И МИГРАЦИЯ ===
        if (save.version != SaveFile.CURRENT_VERSION)
        {
            if (save.version == "1.0" || string.IsNullOrEmpty(save.version))
            {
                Debug.Log("Миграция старого сохранения → 2.0");
                MigrateFromV1(ref save);
            }
            else
            {
                Debug.LogError($"Неподдерживаемая версия сохранения: {save.version}");
                yield break;
            }
        }

        // === ПРОВЕРКА CHECKSUM ===
        var cleanCopy = save.GetCleanCopy();
        string cleanJson = JsonUtility.ToJson(cleanCopy, true);
        if (save.checksum != ComputeHash(cleanJson))
        {
            Debug.LogError("Контрольная сумма не совпала! Файл повреждён.");
            // Можно добавить диалог "Сохранение повреждено, начать новую игру?"
            yield break;
        }

        // === ЗАГРУЗКА ДАННЫХ ===
        ApplySaveData(save, isNewGame);

        SaveFeedbackUI.ShowLoad();
        Debug.Log($"Сохранение успешно загружено ({dataPath})");
    }

    private void MigrateFromV1(ref SaveFile save)
    {
        // Создаём новый tutorialData из старых полей (они ещё есть в памяти при десериализации!)
        save.tutorialData = new TutorialSaveData
        {
            step = save.GetType().GetField("tutorialStep")?.GetValue(save) is int step ? step : 0,
            researchedCount = save.GetType().GetField("researchedCount")?.GetValue(save) is int rc ? rc : 0,
            hasPlayedIntroMonologue = save.GetType().GetField("tutorialStep")?.GetValue(save) is int ts && ts > 0,
            hasPlayedReturnMonologue = save.GetType().GetField("hasPlayedReturnMonologue")?.GetValue(save) is bool b1 && b1,
            hasPlayedFinalMonologue = save.GetType().GetField("hasPlayedFinalMonologue")?.GetValue(save) is bool b2 && b2,
            anomalyPlaced = save.GetType().GetField("anomalyPlaced")?.GetValue(save) is bool a && a,
            playerSlept = save.GetType().GetField("playerSlept")?.GetValue(save) is bool p && p,
            flareHintWasShown = false
        };

        // Очищаем старые поля через рефлексию (чтобы не падало при сохранении)
        var type = save.GetType();
        type.GetField("tutorialStep")?.SetValue(save, 0);
        type.GetField("researchedCount")?.SetValue(save, 0);
        type.GetField("hasPlayedReturnMonologue")?.SetValue(save, false);
        type.GetField("hasPlayedFinalMonologue")?.SetValue(save, false);
        type.GetField("anomalyPlaced")?.SetValue(save, false);
        type.GetField("playerSlept")?.SetValue(save, false);

        save.version = SaveFile.CURRENT_VERSION;
    }

    private void ApplySaveData(SaveFile save, bool isNewGame)
    {
        // GameState
        FindObjectOfType<GameStateSaver>()?.LoadFromBlock(save.gameState);

        // Отчёты исследований
        var reportViewer = FindObjectOfType<ResearchReportViewer>();
        if (reportViewer != null && !string.IsNullOrEmpty(save.globalReports))
            reportViewer.DeserializeReports(save.globalReports);

        // Камера
        if (CameraController.Instance != null && save.cameraLookDirection != Vector2.zero)
        {
            CameraController.Instance.LoadCameraDirectionAndSyncPlayer(save.cameraLookDirection);
        }

        // Туториал — теперь только через tutorialData!
        var tutorial = FindObjectOfType<TutorialManager>();
        if (tutorial != null && save.tutorialData != null)
        {
            tutorial.LoadTutorialSaveData(save.tutorialData);

            // Очень важно: если это загрузка сохранения — не проигрываем вступительный монолог!
            if (!isNewGame && save.tutorialData.hasPlayedIntroMonologue && tutorial.radioMonologue != null)
                tutorial.radioMonologue.HasPlayedIntroMonologue = true;
        }

        // === ОБЪЕКТЫ НА СЦЕНЕ ===
        var mineralDict = save.minerals.ToDictionary(m => m.uniqueID, m => m);
        var depositDict = save.deposits.ToDictionary(d => d.uniqueID, d => d);

        var existingSaveables = FindObjectsOfType<MonoBehaviour>(true)
            .OfType<ISaveableV2>()
            .ToList();

        var toInstantiate = new List<ObjectSaveData>(save.objects);

        // Сначала восстанавливаем уже существующие объекты
        foreach (var saveable in existingSaveables)
        {
            var data = toInstantiate.Find(d => d.uniqueID == saveable.GetUniqueID());
            if (data == null) continue;

            saveable.LoadCommonData(data);

            if (saveable is IHasMineralData min && mineralDict.TryGetValue(data.uniqueID, out var minData))
                min.LoadMineralData(minData);

            if (saveable is IHasDepositData dep && depositDict.TryGetValue(data.uniqueID, out var depData))
                dep.LoadDepositData(depData);

            toInstantiate.Remove(data);
        }

        // Инстантим новые объекты
        foreach (var data in toInstantiate)
        {
            var prefab = prefabRegistry?.GetPrefab(data.prefabIdentifier);
            if (prefab == null)
            {
                Debug.LogWarning($"Префаб не найден: {data.prefabIdentifier}");
                continue;
            }

            var obj = Instantiate(prefab, data.position, data.rotation);
            if (obj.TryGetComponent<ISaveableV2>(out var saveable))
            {
                saveable.LoadCommonData(data);

                if (saveable is IHasMineralData min && mineralDict.TryGetValue(data.uniqueID, out var minData))
                    min.LoadMineralData(minData);

                if (saveable is IHasDepositData dep && depositDict.TryGetValue(data.uniqueID, out var depData))
                    dep.LoadDepositData(depData);
            }
        }

        // === ФИНАЛЬНАЯ СИНХРОНИЗАЦИЯ ===
        StartCoroutine(FinalizeLoading());
    }

    private IEnumerator FinalizeLoading()
    {
        yield return new WaitForFixedUpdate();
        Physics.SyncTransforms();

        yield return new WaitForSeconds(0.15f); // даём всем LateUpdate/OnEnable пройти

        CameraController.Instance?.ForceCameraSync();
        CameraController.Instance?.SetMode(CameraController.ControlMode.FPS);
    }

    private IEnumerator NewGameSetup()
    {
        yield return new WaitForEndOfFrame();

        CameraController.Instance?.SetMode(CameraController.ControlMode.FPS);

        var tutorial = FindObjectOfType<TutorialManager>();
        tutorial?.ForceStartTutorial();

        if (tutorial != null && tutorial.radioMonologue != null)
            tutorial.radioMonologue.StartMonologue(0); // первый монолог только в новой игре!
    }

    #endregion

    #region === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===

    private string GetSlotFolder(int slot) => Path.Combine(BasePath, $"Save_{slot}");

    private void CollectAllSaveData(ref SaveFile save)
    {
        // GameState
        var gs = FindObjectOfType<GameStateSaver>();
        if (gs != null) save.gameState = gs.GetGameStateBlock();

        // Отчёты
        var rv = FindObjectOfType<ResearchReportViewer>();
        if (rv != null) save.globalReports = rv.SerializeReports();

        // Камера
        if (CameraController.Instance != null)
        {
            var cam = CameraController.Instance.transform;
            save.cameraLookDirection = new Vector2(cam.eulerAngles.y, cam.eulerAngles.x);
        }

        // Туториал — только через новый объект!
        var tutorial = FindObjectOfType<TutorialManager>();
        if (tutorial != null)
            save.tutorialData = tutorial.GetTutorialSaveData();

        // Все saveable объекты
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
        {
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }

    #endregion
}