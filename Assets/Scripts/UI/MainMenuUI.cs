using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.IO;

public class MainMenuUI : MonoBehaviour, ILocalizable
{
    [Header("Главное меню")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("Переключение языка")]
    [SerializeField] private Button languageButton;           // ← НОВАЯ КНОПКА
    [SerializeField] private TextMeshProUGUI languageButtonText; // Текст на кнопке (RU / EN)

    [Header("Меню слотов")]
    [SerializeField] private GameObject saveLoadMenu;
    [SerializeField] private SaveLoadMenu saveLoadMenuScript;

    [Header("Ошибка")]
    [SerializeField] private TextMeshProUGUI errorText;

    [SerializeField] private string gameSceneName = "GameScene";

    private void Awake()
    {
        // Авто-нахождение кнопки языка, если не назначена
        if (languageButton == null)
        {
            var btn = transform.Find("LanguageButton")?.GetComponent<Button>();
            if (btn != null) languageButton = btn;
        }
        if (languageButtonText == null && languageButton != null)
            languageButtonText = languageButton.GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Start()
    {
        // === Основные кнопки ===
        newGameButton.onClick.AddListener(StartNewGame);
        loadGameButton.onClick.AddListener(OpenLoadMenu);
        settingsButton.onClick.AddListener(OpenSettings);
        quitButton.onClick.AddListener(QuitGame);

        // === Кнопка языка ===
        if (languageButton != null)
        {
            languageButton.onClick.RemoveAllListeners();
            languageButton.onClick.AddListener(ToggleLanguage);
            UpdateLanguageButtonText(); // Установим правильный текст сразу
        }

        if (errorText) errorText.gameObject.SetActive(false);
        saveLoadMenu.SetActive(false);

        // Регистрируемся для обновления при смене языка извне
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
    }

    private void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

    private void OnEnable()
    {
        LocalizationManager.Register(this);
    }

    private void OnDisable()
    {
        LocalizationManager.Unregister(this);
    }

    public void Localize()
    {
        UpdateLanguageButtonText();
        // Если используешь LocalizedText на кнопках — они сами обновятся
    }

    private void OnLanguageChanged(LocalizationManager.Language lang)
    {
        UpdateLanguageButtonText();
    }

    private void UpdateLanguageButtonText()
    {
        if (languageButtonText == null) return;

        string text = LocalizationManager.CurrentLanguage == LocalizationManager.Language.RU ? "EN" : "RU";
        languageButtonText.text = text;
    }

    private void ToggleLanguage()
    {
        var newLang = LocalizationManager.CurrentLanguage == LocalizationManager.Language.RU
            ? LocalizationManager.Language.EN
            : LocalizationManager.Language.RU;

        LocalizationManager.SetLanguage(newLang);

        // ────── БЕЗОПАСНЫЙ ВАРИАНТ анимации ──────
        var anim = languageButton?.GetComponent<Animator>();
        if (anim != null && anim.isActiveAndEnabled)
        {
            anim.Play("Click", -1, 0f); // или просто "Click"
        }
        // Если анимации нет — ничего не делаем, никаких ошибок!

        Debug.Log($"[MainMenu] Язык переключён на: {newLang}");
    }

    private void StartNewGame()
    {
        for (int i = 0; i < 3; i++)
            SaveManager.Instance.DeleteSlot(i);

        SceneManager.LoadScene(gameSceneName);
    }

    private void OpenLoadMenu()
    {
        saveLoadMenu.SetActive(true);
        saveLoadMenuScript.RefreshSlots();
    }

    private void OpenSettings()
    {
        AudioSettingsUI.Instance?.Open();
    }

    public void BackToMainMenu()
    {
        saveLoadMenu.SetActive(false);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ShowError(string msg)
    {
        if (errorText)
        {
            errorText.gameObject.SetActive(true);
            errorText.text = msg;
            errorText.color = new Color(1f, 0.3f, 0.3f);
        }
    }

    public void ClearError() => errorText?.gameObject.SetActive(false);
}