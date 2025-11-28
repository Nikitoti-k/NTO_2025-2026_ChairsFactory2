using UnityEngine;
using System.Collections.Generic;

public class SaveLoadMenu : MonoBehaviour
{
    [SerializeField] private Transform slotsParent;
    [SerializeField] private GameObject slotPrefab;

    private List<GameObject> spawnedSlots = new List<GameObject>();

    private void OnEnable() => RefreshSlots();

    public void RefreshSlots()
    {
        foreach (var s in spawnedSlots) Destroy(s);
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

    public void BackToMainMenu() => gameObject.SetActive(false);
}