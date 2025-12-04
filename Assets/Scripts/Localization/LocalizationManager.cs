using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    public enum Language { RU, EN }

    [SerializeField] private Language editorLanguage = Language.RU;

    private Language currentLanguage = Language.RU;
    private Dictionary<string, string> currentDict = new();

    public static event Action<Language> OnLanguageChanged;

    // —писок слабых ссылок Ч не будет утечек!
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

        string saved = PlayerPrefs.GetString("GameLanguage", "");
        if (!string.IsNullOrEmpty(saved) && Enum.TryParse<Language>(saved, out var savedLang))
            currentLanguage = savedLang;
        else
            currentLanguage = editorLanguage;

        LoadLanguage(currentLanguage);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.update += EditorUpdate;
#endif
    }

#if UNITY_EDITOR
    private void EditorUpdate()
    {
        if (editorLanguage != currentLanguage)
            SetLanguage(editorLanguage);
    }
#endif

    public static void SetLanguage(Language lang)
    {
        if (Instance == null || Instance.currentLanguage == lang) return;

        Instance.currentLanguage = lang;
        Instance.editorLanguage = lang;
        PlayerPrefs.SetString("GameLanguage", lang.ToString());
        PlayerPrefs.Save();

        Instance.LoadLanguage(lang);
        OnLanguageChanged?.Invoke(lang);

        // „истим мЄртвые ссылки и локализуем живые
        for (int i = Instance.localizables.Count - 1; i >= 0; i--)
        {
            if (Instance.localizables[i].TryGetTarget(out var loc) && loc != null)
                loc.Localize();
            else
                Instance.localizables.RemoveAt(i);
        }
    }

    private void LoadLanguage(Language lang)
    {
        currentDict.Clear();
        var source = lang == Language.EN ? LocalizationData.EN : LocalizationData.RU;

        foreach (var pair in source)
            currentDict[pair.Key] = pair.Value;
    }

    public static string Loc(string key)
    {
        if (Instance == null || !Instance.currentDict.TryGetValue(key, out var text) || string.IsNullOrEmpty(text))
            return $"[{key}]";

        return text;
    }

    public static string Loc(string key, params object[] args)
    {
        try
        {
            return string.Format(Loc(key), args);
        }
        catch (FormatException)
        {
            return Loc(key) + " <color=red>[FORMAT ERROR]</color>";
        }
    }

    public static void Register(ILocalizable loc)
    {
        if (Instance != null && loc != null)
            Instance.localizables.Add(new WeakReference<ILocalizable>(loc));
    }

    public static void Unregister(ILocalizable loc)
    {
        if (Instance == null) return;

        for (int i = Instance.localizables.Count - 1; i >= 0; i--)
        {
            if (Instance.localizables[i].TryGetTarget(out var target) && ReferenceEquals(target, loc))
                Instance.localizables.RemoveAt(i);
        }
    }

    public static Language CurrentLanguage => Instance != null ? Instance.currentLanguage : Language.RU;
}
public interface ILocalizable
{
    void Localize();
}