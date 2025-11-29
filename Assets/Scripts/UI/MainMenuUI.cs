using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.IO;

public class MainMenuUI : MonoBehaviour
{
    [Header("Главное меню")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("Меню слотов")]
    [SerializeField] private GameObject saveLoadMenu;
    [SerializeField] private SaveLoadMenu saveLoadMenuScript;

    [Header("Ошибка")]
    [SerializeField] private TextMeshProUGUI errorText;

    [SerializeField] private string gameSceneName = "GameScene";

    private void Start()
    {
        newGameButton.onClick.AddListener(StartNewGame);
        loadGameButton.onClick.AddListener(OpenLoadMenu);
        settingsButton.onClick.AddListener(() => Debug.Log("Настройки"));
        quitButton.onClick.AddListener(QuitGame);

        if (errorText) errorText.gameObject.SetActive(false);
        saveLoadMenu.SetActive(false);
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