using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TextMeshProUGUI errorText;

    [SerializeField] private string gameSceneName = "GameScene";
    private string ManualSavePath => Path.Combine(Application.persistentDataPath, "manual.json");

    private void Start()
    {
        newGameButton.onClick.AddListener(NewGame);
        loadGameButton.onClick.AddListener(TryLoadGame);
        settingsButton.onClick.AddListener(() => Debug.Log("Настройки"));
        quitButton.onClick.AddListener(QuitGame);
        UpdateLoadButton();
        if (errorText) errorText.gameObject.SetActive(false);
    }

    private void UpdateLoadButton()
    {
        bool hasSave = File.Exists(ManualSavePath);
        loadGameButton.interactable = hasSave;
        var txt = loadGameButton.GetComponentInChildren<TextMeshProUGUI>();
        if (txt) txt.text = hasSave ? "Загрузить игру" : "Нет сохранения";
    }

    private void NewGame()
    {
        if (File.Exists(ManualSavePath)) File.Delete(ManualSavePath);
        if (File.Exists(ManualSavePath + ".bak")) File.Delete(ManualSavePath + ".bak");
        ClearError();
        SceneManager.LoadScene(gameSceneName);
    }

    private void TryLoadGame()
    {
        ClearError();
        if (!File.Exists(ManualSavePath)) { ShowError("Сохранение не найдено!"); return; }

        if (!SaveManager.Instance.ValidateSaveFile(ManualSavePath, out string reason))
        {
            ShowError($"Ошибка загрузки:\n<color=red>{reason}</color>");
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    private void ShowError(string msg)
    {
        if (errorText) { errorText.gameObject.SetActive(true); errorText.text = msg; errorText.color = new Color(1f, 0.3f, 0.3f); }
    }

    private void ClearError() => errorText?.gameObject.SetActive(false);

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnEnable() => UpdateLoadButton();
}