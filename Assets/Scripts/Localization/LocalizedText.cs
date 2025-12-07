using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour, ILocalizable
{
    [SerializeField] private string localizationKey = "YOUR_KEY_HERE";

    // Если нужно подставлять параметры: {0}, {1} и т.д.
    [SerializeField] private string[] formatArgs = new string[0];

    private TextMeshProUGUI tmpText;
    private Text legacyText;

    private void Start()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
        legacyText = GetComponent<Text>();

        Localize();
    }

    private void OnEnable()
    {
        LocalizationManager.Register(this);
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
        Localize(); // На всякий случай
    }

    private void OnDisable()
    {
        LocalizationManager.Unregister(this);
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

    public void Localize()
    {
        if (string.IsNullOrEmpty(localizationKey))
        {
            Debug.LogWarning($"[LocalizedText] Пустой ключ на объекте: {gameObject.name}", this);
            return;
        }

        string translated = LocalizationManager.Loc(localizationKey);

        if (formatArgs != null && formatArgs.Length > 0)
        {
            try
            {
                translated = string.Format(translated, formatArgs);
            }
            catch (System.FormatException ex)
            {
                translated += " <color=red>[FORMAT ERROR]</color>";
                Debug.LogError($"[LocalizedText] Ошибка форматирования ключа \"{localizationKey}\": {ex.Message}", this);
            }
        }

        if (tmpText != null)
            tmpText.text = translated;
        else if (legacyText != null)
            legacyText.text = translated;
    }

    private void OnLanguageChanged(LocalizationManager.Language lang) => Localize();

#if UNITY_EDITOR
    [ContextMenu("Обновить текст (в редакторе)")]
    private void EditorForceLocalize()
    {
        Localize();
    }

    // Проверка на существование ключа — теперь без SetObjectEnableState!
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(localizationKey)) return;

        bool keyExists = LocalizationData.RU.ContainsKey(localizationKey) ||
                         LocalizationData.EN.ContainsKey(localizationKey);

        if (!keyExists)
        {
            // Просто логируем в консоль — ничего не ломаем
            Debug.LogWarning($"[LocalizedText] Ключ \"{localizationKey}\" не найден в LocalizationData! (на объекте: {gameObject.name})", this);
        }
    }

    // Дополнительная кнопка в инспекторе — копирует текущий текст как ключ (удобно при миграции)
    [ContextMenu("Скопировать текущий текст → как ключ")]
    private void CopyCurrentTextToKey()
    {
        if (tmpText != null && !string.IsNullOrEmpty(tmpText.text))
        {
            localizationKey = tmpText.text.Trim();
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[LocalizedText] Ключ установлен: \"{localizationKey}\"");
        }
    }
#endif
}