using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
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

    [Header("Сцены")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    private bool isSavePopupOpen = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        pauseMenuUI.SetActive(false);
        if (manualSavePopup) manualSavePopup.SetActive(false);
    }

    private void Start()
    {
        if (resumeButton) resumeButton.onClick.AddListener(Resume);
        if (saveButton) saveButton.onClick.AddListener(OpenSavePopup);
        if (mainMenuButton) mainMenuButton.onClick.AddListener(ToMainMenu);
        if (quitButton) quitButton.onClick.AddListener(Quit);
    }

    private void Update()
    {
        if (InputManager.Instance?.EscapePressed == true)
        {
            if (isSavePopupOpen)
                CloseSavePopup();
            else if (IsPaused)
                Resume();
            else
                Pause();
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
        if (manualSavePopup) manualSavePopup.SetActive(false);
        isSavePopupOpen = false;

        Time.timeScale = 1f;
        CameraController.Instance.SetMode(CameraController.ControlMode.FPS);
    }

    private void OpenSavePopup()
    {
        if (manualSavePopup)
        {
            manualSavePopup.SetActive(true);
            isSavePopupOpen = true;
        }
    }

    public void CloseSavePopup()
    {
        if (manualSavePopup)
        {
            manualSavePopup.SetActive(false);
            isSavePopupOpen = false;
        }
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