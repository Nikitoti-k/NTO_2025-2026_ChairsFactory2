using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AudioSettingsUI : MonoBehaviour, ILocalizable
{
    public static AudioSettingsUI Instance { get; private set; }

    [Header("=== Слайдеры ===")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider ambienceSlider;

    [Header("=== Тексты ===")]
    [SerializeField] private TextMeshProUGUI masterText;
    [SerializeField] private TextMeshProUGUI sfxText;
    [SerializeField] private TextMeshProUGUI ambienceText;

    [Header("=== Кнопка закрытия ===")]
    [SerializeField] private Button closeButton;

    [Header("=== Панель ===")]
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

        if (settingsPanel) settingsPanel.SetActive(false);
        // НЕ регистрируемся здесь — сделаем в Start()
    }

    private void Start()
    {
        // Регистрируемся только после загрузки сцены и сохранения
        LocalizationManager.Register(this);

        // Принудительно обновляем UI после загрузки
        RefreshSliders();
        Localize();
    }

    private void OnDestroy()
    {
        LocalizationManager.Unregister(this);
    }

    private void OnEnable()
    {
        if (masterSlider) masterSlider.onValueChanged.AddListener(v => SetVolume(ref AudioManager.Instance.masterVolume, v, masterText));
        if (sfxSlider) sfxSlider.onValueChanged.AddListener(v => SetVolume(ref AudioManager.Instance.sfxVolume, v, sfxText));
        if (ambienceSlider) ambienceSlider.onValueChanged.AddListener(v => SetVolume(ref AudioManager.Instance.ambienceVolume, v, ambienceText));
        if (closeButton) closeButton.onClick.AddListener(Close);

        // НЕ вызываем RefreshSliders() здесь — он уже вызван в Start()
    }

    private void OnDisable()
    {
        if (masterSlider) masterSlider.onValueChanged.RemoveAllListeners();
        if (sfxSlider) sfxSlider.onValueChanged.RemoveAllListeners();
        if (ambienceSlider) ambienceSlider.onValueChanged.RemoveAllListeners();
        if (closeButton) closeButton.onClick.RemoveListener(Close);
    }

    public void Open()
    {
        if (settingsPanel) settingsPanel.SetActive(true);
        RefreshSliders();
        Localize();
    }

    public void Close()
    {
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    private void SetVolume(ref float targetField, float value, TextMeshProUGUI text)
    {
        if (AudioManager.Instance == null) return;

        targetField = value;
        UpdateText(text, value);
        AudioManager.Instance.ApplyVolumesFromSave(); // сразу применяем
    }

    public void RefreshSliders()
    {
        if (AudioManager.Instance == null) return;

        var am = AudioManager.Instance;

        if (masterSlider) { masterSlider.value = am.masterVolume; UpdateText(masterText, am.masterVolume); }
        if (sfxSlider) { sfxSlider.value = am.sfxVolume; UpdateText(sfxText, am.sfxVolume); }
        if (ambienceSlider) { ambienceSlider.value = am.ambienceVolume; UpdateText(ambienceText, am.ambienceVolume); }
    }

    private void UpdateText(TextMeshProUGUI text, float value)
    {
        if (text == null) return;

        int percent = Mathf.RoundToInt(value * 100);

        if (text == masterText)
            text.text = LocalizationManager.Loc("AUDIO_MASTER") + $": {percent}%";
        else if (text == sfxText)
            text.text = LocalizationManager.Loc("AUDIO_SFX") + $": {percent}%";
        else if (text == ambienceText)
            text.text = LocalizationManager.Loc("AUDIO_AMBIENCE") + $": {percent}%";
    }

    public void Localize()
    {
        if (AudioManager.Instance == null) return;
        UpdateText(masterText, AudioManager.Instance.masterVolume);
        UpdateText(sfxText, AudioManager.Instance.sfxVolume);
        UpdateText(ambienceText, AudioManager.Instance.ambienceVolume);
    }
}