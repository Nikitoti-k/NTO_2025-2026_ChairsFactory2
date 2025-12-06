using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AudioSettingsUI : MonoBehaviour
{
    public static AudioSettingsUI Instance { get; private set; }

    [Header("=== Твои слайдеры (перетащи сюда) ===")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider ambienceSlider;

    [Header("=== Текст с процентами (по желанию) ===")]
    [SerializeField] private TextMeshProUGUI masterText;
    [SerializeField] private TextMeshProUGUI sfxText;
    [SerializeField] private TextMeshProUGUI musicText;
    [SerializeField] private TextMeshProUGUI ambienceText;

    [Header("=== Кнопка закрытия (обязательно перетащи!) ===")]
    [SerializeField] private Button closeButton;

    [Header("=== Сама панель настроек звука (вся группа) ===")]
    [SerializeField] private GameObject settingsPanel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Скрываем при старте
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    private void OnEnable()
    {
        // Подписываемся на слайдеры
        if (masterSlider) masterSlider.onValueChanged.AddListener(v => SetVolume(ref AudioManager.Instance.masterVolume, v, masterText));
        if (sfxSlider) sfxSlider.onValueChanged.AddListener(v => SetVolume(ref AudioManager.Instance.sfxVolume, v, sfxText));
        if (musicSlider) musicSlider.onValueChanged.AddListener(v => SetVolume(ref AudioManager.Instance.musicVolume, v, musicText));
        if (ambienceSlider) ambienceSlider.onValueChanged.AddListener(v => SetVolume(ref AudioManager.Instance.ambienceVolume, v, ambienceText));

        // Кнопка закрытия
        if (closeButton) closeButton.onClick.AddListener(Close);

        // Синхронизируем слайдеры с текущими значениями
        RefreshSliders();
    }

    private void OnDisable()
    {
        // Отписываемся — важно!
        if (masterSlider) masterSlider.onValueChanged.RemoveAllListeners();
        if (sfxSlider) sfxSlider.onValueChanged.RemoveAllListeners();
        if (musicSlider) musicSlider.onValueChanged.RemoveAllListeners();
        if (ambienceSlider) ambienceSlider.onValueChanged.RemoveAllListeners();
        if (closeButton) closeButton.onClick.RemoveListener(Close);
    }

    // === Публичные методы для открытия из других скриптов ===
    public void Open()
    {
        if (settingsPanel) settingsPanel.SetActive(true);
        RefreshSliders(); // обновляем значения каждый раз при открытии
    }

    public void Close()
    {
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    // === Внутренние методы ===
    private void SetVolume(ref float targetField, float value, TextMeshProUGUI text = null)
    {
        if (AudioManager.Instance == null) return;

        targetField = value;

        if (text) text.text = $"{Mathf.RoundToInt(value * 100)}%";

        // Мгновенно обновляем играющие источники
        RefreshAudioSources();
    }

    private void RefreshAudioSources()
    {
        if (AudioManager.Instance == null) return;
        var am = AudioManager.Instance;

        float master = am.masterVolume;
        if (am.musicSourceA) am.musicSourceA.volume = am.musicVolume * master;
        if (am.musicSourceB) am.musicSourceB.volume = am.musicVolume * master;
        if (am.ambienceSource) am.ambienceSource.volume = am.ambienceVolume * master;
    }

    private void RefreshSliders()
    {
        if (AudioManager.Instance == null) return;
        var am = AudioManager.Instance;

        if (masterSlider) { masterSlider.value = am.masterVolume; UpdateText(masterText, am.masterVolume); }
        if (sfxSlider) { sfxSlider.value = am.sfxVolume; UpdateText(sfxText, am.sfxVolume); }
        if (musicSlider) { musicSlider.value = am.musicVolume; UpdateText(musicText, am.musicVolume); }
        if (ambienceSlider) { ambienceSlider.value = am.ambienceVolume; UpdateText(ambienceText, am.ambienceVolume); }
    }

    private void UpdateText(TextMeshProUGUI text, float value)
    {
        if (text) text.text = $"{Mathf.RoundToInt(value * 100)}%";
    }
}