using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ManualSavePopup : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private void OnEnable()
    {
        inputField.text = SaveManager.Instance.GenerateDefaultSaveName();
        inputField.Select();
        inputField.ActivateInputField();
    }

    private void Start()
    {
        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(() =>
        {
            string name = string.IsNullOrWhiteSpace(inputField.text) ? null : inputField.text.Trim();
            SaveManager.Instance.SaveGame(name);
           // SaveFeedbackUI.ShowSave();
            PauseManager.Instance.CloseSavePopup();
        });

        cancelButton.onClick.RemoveAllListeners();
        cancelButton.onClick.AddListener(() =>
        {
            PauseManager.Instance.CloseSavePopup();
        });
    }
}