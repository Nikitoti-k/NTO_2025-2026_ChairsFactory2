using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class SaveSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI slotNameText;
    [SerializeField] private TextMeshProUGUI saveTimeText;
    [SerializeField] private TextMeshProUGUI playTimeText;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private GameObject emptyOverlay;
    [SerializeField] private GameObject dataOverlay;

    private SaveSlotInfo info;
    private SaveLoadMenu menu;

    public void Init(SaveSlotInfo slotInfo, SaveLoadMenu parentMenu)
    {
        info = slotInfo;
        menu = parentMenu;

        slotNameText.text = slotInfo.slotName;
        saveTimeText.text = slotInfo.hasData ? slotInfo.saveTime : "";
        playTimeText.text = slotInfo.hasData ? slotInfo.playTime : "—";

        emptyOverlay.SetActive(!slotInfo.hasData);
        dataOverlay.SetActive(slotInfo.hasData);

        loadButton.interactable = slotInfo.hasData;
        deleteButton.gameObject.SetActive(slotInfo.hasData);

        loadButton.onClick.RemoveAllListeners();
        loadButton.onClick.AddListener(() =>
        {
            SaveManager.Instance.LoadFromSlot(info.slotIndex);
            SceneManager.LoadScene("GameScene");
        });

        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(() =>
        {
            SaveManager.Instance.DeleteSlot(info.slotIndex);
            menu.RefreshSlots();
        });
    }
}