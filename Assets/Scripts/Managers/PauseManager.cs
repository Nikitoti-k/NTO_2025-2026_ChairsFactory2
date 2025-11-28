using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }
    public static bool IsPaused => Time.timeScale == 0f;

    [Header("UI")]
    [SerializeField] private GameObject pauseMenuUI;

    [Header("Кнопки")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [Header("Сцены")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        pauseMenuUI.SetActive(false);
    }

    private void Start()
    {
        // ← ВОТ ЭТО ВСЁ ВЕРНУЛ! Подписываем кнопки один раз при старте
        if (resumeButton) resumeButton.onClick.AddListener(Resume);
        if (saveButton) saveButton.onClick.AddListener(SaveAndResume);
        if (settingsButton) settingsButton.onClick.AddListener(() => Debug.Log("Настройки — заглушка"));
        if (mainMenuButton) mainMenuButton.onClick.AddListener(ToMainMenu);
        if (quitButton) quitButton.onClick.AddListener(Quit);
    }

    private void OnDestroy()
    {
        // Отписываемся на всякий случай
        if (resumeButton) resumeButton.onClick.RemoveListener(Resume);
        if (saveButton) saveButton.onClick.RemoveListener(SaveAndResume);
        if (settingsButton) settingsButton.onClick.RemoveListener(() => Debug.Log("Настройки — заглушка"));
        if (mainMenuButton) mainMenuButton.onClick.RemoveListener(ToMainMenu);
        if (quitButton) quitButton.onClick.RemoveListener(Quit);
    }

    private void Update()
    {
        if (InputManager.Instance?.EscapePressed == true)
        {
            if (IsPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        CameraController.Instance.SetMode(CameraController.ControlMode.UI);
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        CameraController.Instance.SetMode(CameraController.ControlMode.FPS); // ← после закрытия!
    }

    public void SaveAndResume()
    {
        SaveManager.Instance.SaveToSlot(0, "Ручное сохранение");
        SaveFeedbackUI.ShowSave();
        Resume(); // ← игра продолжается сразу!
    }

    public void ToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}