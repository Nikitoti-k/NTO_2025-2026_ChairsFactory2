using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class TestMetricsSaveManager : MonoBehaviour
{
    private string savePath;
    private TestMetricsSaveData allMetricsSaveData;

    [System.Serializable]
    public class TestMetricsSaveData
    {
        public List<float> sessionTime = new List<float>();
        public List<int> tutorialStep = new List<int>();
    }

    private void Start()
    {
        allMetricsSaveData = new TestMetricsSaveData();
        savePath = Application.persistentDataPath + "/testMetricsSave.json";

        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            allMetricsSaveData = JsonUtility.FromJson<TestMetricsSaveData>(json);
        }
    }

    private void OnApplicationQuit()
    {
        TutorialSaveData tutorialSaveData = FindObjectOfType<TutorialManager>().GetTutorialSaveData();

        float newSessionTime = Time.time;
        int newTutorialStep = tutorialSaveData.step;

        allMetricsSaveData.sessionTime.Add(newSessionTime);
        allMetricsSaveData.tutorialStep.Add(newTutorialStep);

        string json = JsonUtility.ToJson(allMetricsSaveData);
        File.WriteAllText(savePath, json);

        string loadedJson = File.ReadAllText(savePath);
        TestMetricsSaveData loadedData = JsonUtility.FromJson<TestMetricsSaveData>(loadedJson);

        for (int i = 0; i < loadedData.sessionTime.Count; i++)
        {
            print(i + ". Время сессии: " + loadedData.sessionTime[i] + " Шаг обучения: " + loadedData.tutorialStep[i]);
        }

        ExportMetricsToCsv();
    }

    public void ExportMetricsToCsv()
    {
        if (!File.Exists(savePath))
        {
            Debug.LogWarning("Файл с метриками не найден!");
            return;
        }

        string loadedJson = File.ReadAllText(savePath);
        TestMetricsSaveData loadedData = JsonUtility.FromJson<TestMetricsSaveData>(loadedJson);

        StringBuilder csv = new StringBuilder();

        csv.AppendLine("Номер записи;Время сессии (сек);Шаг обучения");

        int count = Mathf.Min(loadedData.sessionTime.Count, loadedData.tutorialStep.Count);

        for (int i = 0; i < count; i++)
        {
            csv.AppendLine($"{i + 1};{loadedData.sessionTime[i]:F2};{loadedData.tutorialStep[i]}");
        }

        string csvPath = Path.Combine(Application.persistentDataPath, "test_metrics_export.csv");
        File.WriteAllText(csvPath, csv.ToString(), Encoding.UTF8);

        Debug.Log($"<color=green>CSV файл сохранён: {csvPath}</color>");

        Application.OpenURL(Application.persistentDataPath);
    }

    public void OpenMetricsFolder()
    {
        Application.OpenURL(Application.persistentDataPath);
    }
}