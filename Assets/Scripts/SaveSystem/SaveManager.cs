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

        // Используем наш новый Create()
        var meta = SaveSlotMeta.Create(slotName);

        string slotFolder = GetSlotFolder(targetSlot);
        Directory.CreateDirectory(slotFolder);

        string dataPath = Path.Combine(slotFolder, "data.json");
        string metaPath = Path.Combine(slotFolder, "meta.json");
        string previewPath = Path.Combine(slotFolder, "preview.png");

        StartCoroutine(SaveWithCleanPreviewCoroutine(targetSlot, save, meta, dataPath, metaPath, previewPath));
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
                    meta.ResolveDateTime(); // ← Восстанавливаем правильную дату!

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
                catch (System.Exception e)
                {
                    Debug.LogError("Ошибка чтения meta.json: " + e);
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

        // Сортировка по реальной дате (теперь надёжно!)
        return list
            .OrderByDescending(x =>
            {
                if (!x.hasData) return DateTime.MinValue;
                if (DateTime.TryParse(x.saveTime, out var dt)) return dt;
                return DateTime.MinValue;
            })
            .ThenBy(x => x.slotIndex)
            .ToList();
    }
    public void AutoSave() => SaveGame("Автосохранение");

    public void LoadFromSlot(int slotIndex)
    {
        string folder = GetSlotFolder(slotIndex);
        string dataPath = Path.Combine(folder, "data.json");

        if (!File.Exists(dataPath))
        {
            Debug.LogWarning($"Слот {slotIndex} пустой");
            ShowLoadError("SAVE_ERROR_CORRUPTED"); // или можно отдельный ключ, если хочешь
            return;
        }

        if (!IsSaveFileValid(dataPath, out string errorKey))
        {
            ShowLoadError(errorKey);
            return;
        }

        // Всё ок — грузим игру
        pendingLoadSlot = slotIndex;
        forceNewGame = false;
        SceneManager.LoadScene("GameScene");
    }
    private bool IsSaveFileValid(string dataPath, out string errorKey)
    {
        errorKey = "SAVE_ERROR_CORRUPTED"; // дефолтная ошибка

        try
        {
            string json = File.ReadAllText(dataPath);
            SaveFile save = JsonUtility.FromJson<SaveFile>(json);

            // Проверка версии
            if (save.version != SaveFile.CURRENT_VERSION)
            {
                if (save.version == "1.0" || string.IsNullOrEmpty(save.version))
                {
                    // миграция возможна — считаем валидным
                    return true;
                }
                else
                {
                    errorKey = "SAVE_ERROR_VERSION";
                    return false;
                }
            }

            // Проверка checksum
            var cleanCopy = save.GetCleanCopy();
            string cleanJson = JsonUtility.ToJson(cleanCopy, true);
            if (save.checksum != ComputeHash(cleanJson))
            {
                errorKey = "SAVE_ERROR_CHECKSUM";
                return false;
            }

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Ошибка чтения/валидации сохранения: " + e);
            errorKey = "SAVE_ERROR_READ";
            return false;
        }
    }

    private void ShowLoadError(string localizationKey)
    {
        var menu = FindObjectOfType<SaveLoadMenu>();
        menu?.ShowErrorMessage(LocalizationManager.Loc(localizationKey));
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

    /*  public List<SaveSlotInfo> GetAllSaveSlots()
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
      }*/

    #endregion

    #region === ВНУТРЕННЯЯ ЛОГИКА СОХРАНЕНИЯ ===
    private int GetNewestExistingSlot()
    {
        DateTime newest = DateTime.MinValue;
        int newestIndex = -1;

        for (int i = 0; i < MAX_SLOTS; i++)
        {
            string metaPath = Path.Combine(GetSlotFolder(i), "meta.json");
            if (!File.Exists(metaPath)) continue;

            try
            {
                var meta = JsonUtility.FromJson<SaveSlotMeta>(File.ReadAllText(metaPath));
                meta.ResolveDateTime();
                if (meta.saveTime > newest)
                {
                    newest = meta.saveTime;
                    newestIndex = i;
                }
            }
            catch { }
        }
        return newestIndex;
    }
    private int ChooseSlotForSave()
    {
        // 1. Ищем пустой слот
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            if (!File.Exists(Path.Combine(GetSlotFolder(i), "data.json")))
                return i;
        }

        // 2. Все заняты — ищем самый старый
        DateTime oldest = DateTime.MaxValue;
        int oldestIndex = 0;

        for (int i = 0; i < MAX_SLOTS; i++)
        {
            string metaPath = Path.Combine(GetSlotFolder(i), "meta.json");
            if (!File.Exists(metaPath)) continue;

            try
            {
                var meta = JsonUtility.FromJson<SaveSlotMeta>(File.ReadAllText(metaPath));
                meta.ResolveDateTime();
                if (meta.saveTime < oldest)
                {
                    oldest = meta.saveTime;
                    oldestIndex = i;
                }
            }
            catch
            {
                return i; // повреждённый meta — можно перезаписать
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
        yield return new WaitForEndOfFrame();

        
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

        yield return new WaitForEndOfFrame();

        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();


    Texture2D preview = CreateCleanPreview(screenshot);      


        Destroy(screenshot);

      
        foreach (var (canvas, wasActive) in originalStates)
            canvas.gameObject.SetActive(wasActive);

      
        File.WriteAllBytes(previewPath, preview.EncodeToPNG());
        Destroy(preview);

       
        string cleanJson = JsonUtility.ToJson(saveFile.GetCleanCopy(), true);
        saveFile.checksum = ComputeHash(cleanJson);
        string finalJson = JsonUtility.ToJson(saveFile, true);

        string tmpPath = dataPath + ".tmp";
        File.WriteAllText(tmpPath, finalJson);
        File.WriteAllText(metaPath, JsonUtility.ToJson(meta, true));

        if (File.Exists(dataPath))
            File.Replace(tmpPath, dataPath, dataPath + ".bak");
        else
            File.Move(tmpPath, dataPath);

        //SaveFeedbackUI.ShowSave();
        Debug.Log($"Игра сохранена в слот {slot} — {meta.slotName}");
    }
    private Texture2D CreateCleanPreview(Texture2D source)
    {
        int width = 256;
        int height = 144;

        RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, rt, new Vector2(1, -1), new Vector2(1, 0));

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D preview = new Texture2D(width, height, TextureFormat.RGB24, false);
        preview.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        preview.Apply();  

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return preview;  
    }

    // Вспомогательная функция — делает текстуру читаемой (если Read/Write Disabled)
    private Texture2D MakeTextureReadable(Texture2D original)
    {
        RenderTexture tmp = RenderTexture.GetTemporary(
            original.width, original.height, 0,
            RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

        Graphics.Blit(original, tmp);
        RenderTexture.active = tmp;

        Texture2D readable = new Texture2D(original.width, original.height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
        readable.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(tmp);

        return readable;
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
  /*  private int GetNewestExistingSlot()
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
            catch { /* повреждённый meta — пропускаем  }
        }

        return newestIndex;
    }*/

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
            Debug.LogError("Контрольная сумма не совпала! Файл повреждён или изменён вручную.");

            // ←←← ВЫВОДИМ СООБЩЕНИЕ В МЕНЮ
            var menu = FindObjectOfType<SaveLoadMenu>();
            if (menu != null)
                menu.ShowErrorMessage("Сохранение повреждено или было изменено вручную.\nЗагрузка невозможна.");
            else
                Debug.LogWarning("SaveLoadMenu не найден — не удалось показать ошибку игроку");

            yield break;
        }
        // === ЗАГРУЗКА ДАННЫХ ===
        ApplySaveData(save, isNewGame);

        //SaveFeedbackUI.ShowLoad();
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
            //flareHintWasShown = false
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

        // Туториал
        var tutorial = FindObjectOfType<TutorialManager>();
        if (tutorial != null && save.tutorialData != null)
        {
            tutorial.LoadTutorialSaveData(save.tutorialData);
            if (!isNewGame && save.tutorialData.hasPlayedIntroMonologue && tutorial.radioMonologue != null)
                tutorial.radioMonologue.HasPlayedIntroMonologue = true;
        }

        // === ВОССТАНОВЛЕНИЕ ГРОМКОСТИ ===
        if (AudioManager.Instance != null)
        {
            var am = AudioManager.Instance;
            am.masterVolume = Mathf.Clamp01(save.masterVolume);
            am.sfxVolume = Mathf.Clamp01(save.sfxVolume);
            am.ambienceVolume = Mathf.Clamp01(save.ambienceVolume);

            // Применяем к уже запущенным источникам
            float master = am.masterVolume;
            if (am.ambienceSource != null)
                am.ambienceSource.volume = am.ambienceVolume * master;

            // Если будут музыка — раскомментируй:
            // if (am.musicSourceA) am.musicSourceA.volume = am.musicVolume * master;
            // if (am.musicSourceB) am.musicSourceB.volume = am.musicVolume * master;
        }
        if (AudioManager.Instance != null)
        {
            var am = AudioManager.Instance;
            am.masterVolume = Mathf.Clamp01(save.masterVolume);
            am.sfxVolume = Mathf.Clamp01(save.sfxVolume);
            am.ambienceVolume = Mathf.Clamp01(save.ambienceVolume);
            am.ApplyVolumesFromSave();
        }

        // ЯЗЫК — САМОЕ ВАЖНОЕ
        if (!string.IsNullOrEmpty(save.language) &&
            Enum.TryParse<LocalizationManager.Language>(save.language, out var lang))
        {
            LocalizationManager.ApplyLanguageFromSave(lang);
        }
        else
        {
            LocalizationManager.ApplyLanguageFromSave(LocalizationManager.Language.RU);
        }

        // Обновляем настройки UI
        var settingsUI = FindObjectOfType<AudioSettingsUI>();
        if (settingsUI != null)
        {
            settingsUI.RefreshSliders();
            settingsUI.Localize();
        }

        
      
            var allSaveableObjects = FindObjectsOfType<SaveableObject>(true); // ← true = including inactive!!!
            Debug.Log($"[SaveManager] Найдено SaveableObject (включая отключённые): {allSaveableObjects.Length}");

            var mineralDict = save.minerals.ToDictionary(m => m.uniqueID, m => m);
            var depositDict = save.deposits.ToDictionary(d => d.uniqueID, d => d);

            var toInstantiate = new List<ObjectSaveData>(save.objects);

            // 1. Сначала обрабатываем ВСЕ объекты, которые УЖЕ есть на сцене (включая отключённые)
            foreach (var saveableObj in allSaveableObjects)
            {
                string uid = saveableObj.GetUniqueID();
                var commonData = toInstantiate.Find(d => d.uniqueID == uid);

                if (commonData == null)
                {
                    Debug.Log($"[SaveManager] Нет данных для объекта на сцене: {saveableObj.name} (ID: {uid}) — пропускаем");
                    continue;
                }

                Debug.Log($"[SaveManager] Загружаем данные для существующего объекта: {saveableObj.name} (ID: {uid})");

                // Загружаем общие данные (позиция, вращение и т.д.)
                saveableObj.LoadCommonData(commonData);

                // Загружаем Deposit-данные, если есть
                if (saveableObj.TryGetComponent<IHasDepositData>(out var deposit))
                {
                    if (depositDict.TryGetValue(uid, out var depData))
                    {
                        Debug.Log($"[SaveManager] → Загружаем DepositData для {saveableObj.name}: hits = {depData.currentHits}");
                        deposit.LoadDepositData(depData);
                    }
                    else
                    {
                        Debug.LogWarning($"[SaveManager] → Нет DepositData для {saveableObj.name}, хотя есть IHasDepositData");
                    }
                }

                // Загружаем Mineral-данные, если есть
                if (saveableObj.TryGetComponent<IHasMineralData>(out var mineral))
                {
                    if (mineralDict.TryGetValue(uid, out var minData))
                    {
                        Debug.Log($"[SaveManager] → Загружаем MineralData для {saveableObj.name}");
                        mineral.LoadMineralData(minData);
                    }
                }

                toInstantiate.Remove(commonData);
            }

            // 2. Создаём объекты, которых не было на сцене
            foreach (var data in toInstantiate)
            {
                var prefab = prefabRegistry?.GetPrefab(data.prefabIdentifier);
                if (prefab == null)
                {
                    Debug.LogWarning($"[SaveManager] Префаб не найден: {data.prefabIdentifier}");
                    continue;
                }

                Debug.Log($"[SaveManager] Создаём новый объект из сохранения: {data.prefabIdentifier}");
                var obj = Instantiate(prefab, data.position, data.rotation);

                if (obj.TryGetComponent<ISaveableV2>(out var saveable))
                {
                    saveable.LoadCommonData(data);

                    if (saveable is IHasDepositData dep && depositDict.TryGetValue(data.uniqueID, out var depData))
                    {
                        Debug.Log($"[SaveManager] → Новый объект — загружаем DepositData: hits = {depData.currentHits}");
                        dep.LoadDepositData(depData);
                    }

                    if (saveable is IHasMineralData min && mineralDict.TryGetValue(data.uniqueID, out var minData))
                        min.LoadMineralData(minData);
                }
            }
        foreach (var m in save.minerals)
        {
            if (!mineralDict.ContainsKey(m.uniqueID))
                mineralDict[m.uniqueID] = m;
            else
                Debug.LogWarning($"[SaveManager] Дубликат MineralSaveData ID: {m.uniqueID}");
        }

        foreach (var d in save.deposits)
        {
            if (!depositDict.ContainsKey(d.uniqueID))
                depositDict[d.uniqueID] = d;
            else
                Debug.LogWarning($"[SaveManager] Дубликат DepositSaveData ID: {d.uniqueID}");
        }
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

        // Туториал
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

        // === СОХРАНЕНИЕ ГРОМКОСТИ ===
        if (AudioManager.Instance != null)
        {
            save.masterVolume = AudioManager.Instance.masterVolume;
            save.sfxVolume = AudioManager.Instance.sfxVolume;
            save.ambienceVolume = AudioManager.Instance.ambienceVolume;
        }
        else
        {
            save.masterVolume = 1f;
            save.sfxVolume = 1f;
            save.ambienceVolume = 1f;
        }

        // === СОХРАНЕНИЕ ЯЗЫКА ===
        save.language = LocalizationManager.CurrentLanguage.ToString(); // "RU" или "EN"
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
[Serializable]
public class SaveSlotMeta
{
    public string slotName;

    // Сохраняем как Unix timestamp в секундах — это надёжно везде
    public long saveTimeUnix;

    // Это поле НЕ сериализуется, только для удобства в коде
    [NonSerialized]
    public DateTime saveTime;

    // Статический метод для создания при сохранении
    public static SaveSlotMeta Create(string name)
    {
        var meta = new SaveSlotMeta
        {
            slotName = name,
            saveTimeUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), // UTC → Unix
            saveTime = DateTime.Now // локальное время для отображения
        };
        return meta;
    }

    // Вызывать сразу после JsonUtility.FromJson
    public void ResolveDateTime()
    {
        // Преобразуем Unix → UTC → локальное время устройства
        saveTime = DateTimeOffset.FromUnixTimeSeconds(saveTimeUnix).LocalDateTime;
    }
}