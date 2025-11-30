using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaveLoadMenu : MonoBehaviour
{
    [SerializeField] private Transform slotsParent;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Button backButton;

    private List<GameObject> spawnedSlots = new List<GameObject>();

    private void OnEnable()
    {
        RefreshSlots();
    }

    public void RefreshSlots()
    {
        foreach (var s in spawnedSlots)
            if (s != null) Destroy(s);
        spawnedSlots.Clear();

        var slots = SaveManager.Instance.GetAllSaveSlots();
        foreach (var slot in slots)
        {
            var obj = Instantiate(slotPrefab, slotsParent);
            var ui = obj.GetComponent<SaveSlotUI>();
            ui.Init(slot, this);
            spawnedSlots.Add(obj);
        }
    }

    private void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(BackToMainMenu);
    }

    public void BackToMainMenu()
    {
        gameObject.SetActive(false);
    }
}