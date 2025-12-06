using UnityEngine;
using System.IO;
using System.Linq;
using System;

public class MainMenuSettingsLoader : MonoBehaviour
{
    [Header("Автоматически подгрузит настройки из последнего сохранения")]
    [SerializeField] private bool loadOnStart = true;

    private const int MAX_SLOTS = 3;
    private const string SAVE_FOLDER = "Saves";
    private string BasePath => Path.Combine(Application.persistentDataPath, SAVE_FOLDER);

    private void Start()
    {
        if (loadOnStart)
            LoadSettingsFromLatestSave();
    }

    public void LoadSettingsFromLatestSave()
    {
        string latestFile = FindLatestSaveFile();
        if (string.IsNullOrEmpty(latestFile))
        {
            ApplyDefaultSettings();
            return;
        }

        try
        {
            string json = File.ReadAllText(latestFile);
            SaveFile save = JsonUtility.FromJson<SaveFile>(json);

            // === ГРОМКОСТЬ ===
            if (AudioManager.Instance != null)
            {
                var am = AudioManager.Instance;
                am.masterVolume = Mathf.Clamp01(save.masterVolume);
                am.sfxVolume = Mathf.Clamp01(save.sfxVolume);
                am.ambienceVolume = Mathf.Clamp01(save.ambienceVolume);
               // am.musicVolume = Mathf.Clamp01(save.musicVolume); // если сохранял
                am.ApplyVolumesFromSave();
            }

            // === ЯЗЫК ===
            if (!string.IsNullOrEmpty(save.language) &&
                Enum.TryParse<LocalizationManager.Language>(save.language, out var lang))
            {
                LocalizationManager.ApplyLanguageFromSave(lang);
            }
            else
            {
                LocalizationManager.ApplyLanguageFromSave(LocalizationManager.Language.RU);
            }

            // Обновляем UI настроек, если оно уже открыто
            var settingsUI = FindObjectOfType<AudioSettingsUI>();
            if (settingsUI != null)
            {
                settingsUI.RefreshSliders();
                settingsUI.Localize();
            }

            Debug.Log($"[MainMenuSettingsLoader] Настройки загружены из: {Path.GetFileName(latestFile)}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[MainMenuSettingsLoader] Не удалось загрузить настройки: {e.Message}");
            ApplyDefaultSettings();
        }
    }

    private string FindLatestSaveFile()
    {
        string latestFile = null;
        DateTime latestTime = DateTime.MinValue;

        for (int i = 0; i < MAX_SLOTS; i++)
        {
            string path = Path.Combine(BasePath, $"Save_{i}", "data.json");
            if (!File.Exists(path)) continue;

            DateTime writeTime = File.GetLastWriteTime(path);
            if (writeTime > latestTime)
            {
                latestTime = writeTime;
                latestFile = path;
            }
        }

        return latestFile;
    }

    private void ApplyDefaultSettings()
    {
        // Дефолтные значения
        if (AudioManager.Instance != null)
        {
            var am = AudioManager.Instance;
            am.masterVolume = am.sfxVolume = am.ambienceVolume = am.musicVolume = 1f;
            am.ApplyVolumesFromSave();
        }

        LocalizationManager.ApplyLanguageFromSave(LocalizationManager.Language.RU);

        var settingsUI = FindObjectOfType<AudioSettingsUI>();
        if (settingsUI != null)
        {
            settingsUI.RefreshSliders();
            settingsUI.Localize();
        }
    }

    // Можно вызвать вручную (например, по кнопке "Применить настройки из игры")
    [ContextMenu("Загрузить настройки из последнего сохранения")]
    private void LoadFromContextMenu()
    {
        LoadSettingsFromLatestSave();
    }
}