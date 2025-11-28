using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.IO;
using System;
using System.Collections.Generic;

public class SaveLoadMenu : MonoBehaviour
{
 /*   [SerializeField] GameObject slotPrefab;
    [SerializeField] Transform contentParent;
    [SerializeField] Button backButton;
    [SerializeField] string gameSceneName = "GameScene";

    void OnEnable()
    {
        RefreshSlots();
        if (backButton) backButton.onClick.AddListener(() => gameObject.SetActive(false));
    }

    void OnDisable()
    {
        if (backButton) backButton.onClick.RemoveAllListeners();
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);
    }

    public void RefreshSlots()
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        string[] files = Directory.GetFiles(Application.persistentDataPath, "*.json");
        Array.Sort(files, (a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));

        foreach (string file in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            if (fileName == "auto" || fileName.StartsWith("slot") || fileName == "manual")
                CreateSlot(fileName);
        }
    }

    void CreateSlot(string slotName)
    {
        string path = Path.Combine(Application.persistentDataPath, slotName + ".json");
        if (!File.Exists(path)) return;

        SaveFile save = JsonUtility.FromJson<SaveFile>(File.ReadAllText(path));

        GameObject slot = Instantiate(slotPrefab, contentParent);
        var ui = slot.GetComponent<SaveSlotUI>() ?? slot.AddComponent<SaveSlotUI>();

        ui.Setup(save, slotName, () =>
        {
            PlayerPrefs.SetString("LoadSlot", slotName);
            SceneManager.LoadScene(gameSceneName);
        },
        () =>
        {
            File.Delete(path);
            File.Delete(path + ".bak");
            RefreshSlots();
        });
    }*/
}

