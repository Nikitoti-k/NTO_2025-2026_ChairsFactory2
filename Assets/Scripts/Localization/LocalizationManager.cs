
using System;
using System.Collections.Generic;
using UnityEngine;
using System;
public interface ILocalizable
{
    void Localize();
}
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    public enum Language { RU, EN }
    [SerializeField] private Language editorLanguage = Language.RU;

    private Language currentLanguage = Language.RU;
    private Dictionary<string, string> currentDict = new();

    [Header("Перетащи сюда LocalizationTable.asset")]
    [SerializeField] private LocalizationTable csvLoader;

    private Dictionary<string, Dictionary<string, string>> allLanguages;

    public static event Action<Language> OnLanguageChanged;
    private readonly List<WeakReference<ILocalizable>> localizables = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadAllLanguagesFromCSV();

        
        ApplyLanguageFromSave(Language.RU);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.update += EditorUpdate;
#endif
    }

#if UNITY_EDITOR
    private void EditorUpdate()
    {
        if (editorLanguage != currentLanguage)
            ApplyLanguageFromSave(editorLanguage);
    }
#endif


private void LoadAllLanguagesFromCSV()
{
    if (csvLoader == null)
    {
        Debug.LogError("[Localization] CSV Loader не назначен в LocalizationManager!");
        return;
    }

        allLanguages = csvLoader.Load();

        if (allLanguages.Count == 0)
        Debug.LogError("[Localization] Не удалось загрузить локализацию из CSV!");
}

private void LoadLanguage(Language lang)
{
    currentDict.Clear();
    string langKey = lang == Language.EN ? "English" : "Russian";

    if (allLanguages != null && allLanguages.TryGetValue(langKey, out var dict))
    {
        foreach (var kvp in dict)
            currentDict[kvp.Key] = kvp.Value;
    }
}

    public static void ApplyLanguageFromSave(Language lang)
    {
        if (Instance == null) return;

        Instance.currentLanguage = lang;
        Instance.editorLanguage = lang;

        

        Instance.LoadLanguage(lang);
        OnLanguageChanged?.Invoke(lang);

        for (int i = Instance.localizables.Count - 1; i >= 0; i--)
        {
            if (Instance.localizables[i].TryGetTarget(out var target) && target != null)
                target.Localize();
            else
                Instance.localizables.RemoveAt(i);
        }
    }

    public static void SetLanguage(Language lang) => ApplyLanguageFromSave(lang);

public static string Loc(string key)
{
    if (Instance == null || !Instance.currentDict.TryGetValue(key, out var text) || string.IsNullOrEmpty(text))
        return $"[{key}]";
    return text;
}

public static string Loc(string key, params object[] args)
{
    try { return string.Format(Loc(key), args); }
    catch { return Loc(key) + " <color=red>[FORMAT ERROR]</color>"; }
}

public static void Register(ILocalizable obj)
{
    if (Instance != null && obj != null)
        Instance.localizables.Add(new WeakReference<ILocalizable>(obj));
}

public static void Unregister(ILocalizable obj)
{
    if (Instance == null) return;
    for (int i = Instance.localizables.Count - 1; i >= 0; i--)
    {
            if (Instance.localizables[i].TryGetTarget(out var target) && ReferenceEquals(target, obj))
                Instance.localizables.RemoveAt(i);
    }
}

public static Language CurrentLanguage => Instance != null ? Instance.currentLanguage : Language.RU;
}