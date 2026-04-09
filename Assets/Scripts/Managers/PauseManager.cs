using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public interface IPauseManager
{
    void Pause();
    void Resume();
    void ToMainMenu();
    void Quit();
}

public class PauseManager : MonoBehaviour, IPauseManager
{
    public static PauseManager Instance { get; private set; }
    public static bool IsPaused => Time.timeScale == 0f;

    [Header("UI")]
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject manualSavePopup;

    [Header("Кнопки")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button settingsButton;

    [Header("Сцены")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    private bool isSavePopupOpen = false;

    private void Awake()
    {
        try
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
            if (manualSavePopup != null) manualSavePopup.SetActive(false);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PauseManager] Awake error: {e.Message}");
        }
    }

    private void Start()
    {
        try
        {
            if (resumeButton) resumeButton.onClick.AddListener(Resume);
            if (saveButton) saveButton.onClick.AddListener(OpenSavePopup);
            if (mainMenuButton) mainMenuButton.onClick.AddListener(ToMainMenu);
            if (quitButton) quitButton.onClick.AddListener(Quit);
            if (settingsButton) settingsButton.onClick.AddListener(OpenSettings);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PauseManager] Start error: {e.Message}");
        }
    }

    private void Update()
    {
        if (InputManager.Instance != null && InputManager.Instance.EscapePressed)
        {
            try
            {
                if (isSavePopupOpen)
                    CloseSavePopup();
                else if (IsPaused)
                    Resume();
                else
                    Pause();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PauseManager] Update error: {e.Message}");
            }
        }
    }

    public void Pause()
    {
        if (IsPaused) return;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        if (!IsPaused) return;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (manualSavePopup != null) manualSavePopup.SetActive(false);
        isSavePopupOpen = false;
        Time.timeScale = 1f;
    }

    private void OpenSavePopup()
    {
        if (manualSavePopup != null)
        {
            manualSavePopup.SetActive(true);
            isSavePopupOpen = true;
        }
    }

    public void CloseSavePopup()
    {
        if (manualSavePopup != null)
        {
            manualSavePopup.SetActive(false);
            isSavePopupOpen = false;
        }
    }

    private void OpenSettings()
    {
        if (AudioSettingsUI.Instance != null)
            AudioSettingsUI.Instance.Open();
    }

    public void ToMainMenu()
    {
        try
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuScene);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PauseManager] ToMainMenu error: {e.Message}");
        }
    }

    public void Quit()
    {
        try
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PauseManager] Quit error: {e.Message}");
        }
    }
}