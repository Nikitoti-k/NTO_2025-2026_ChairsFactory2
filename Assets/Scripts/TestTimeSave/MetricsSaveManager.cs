using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MetricsSaveManager : MonoBehaviour
{

    private string savePath;
    private MetricsSaveData allMetricsSaveData;

    [System.Serializable]
    public class MetricsSaveData
    {
        public List<float> sessionTime = new List<float>();
        public List<int> tutorialStep = new List<int>();
    }

    private void Start()
    {
        allMetricsSaveData = new MetricsSaveData();
        savePath = Application.persistentDataPath + "/metricsSave.json";
        if (File.Exists(savePath)) {
            string json = File.ReadAllText(savePath);
            allMetricsSaveData = JsonUtility.FromJson<MetricsSaveData>(json);
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
        MetricsSaveData loadedData = JsonUtility.FromJson<MetricsSaveData>(loadedJson);

        /*float sessionMedian = CalculateMedian(loadedData.sessionTime);
        float tutorialStepMedian = CalculateMedian(loadedData.tutorialStep);*/

        Debug.Log($"Загружено: {loadedData.sessionTime[0]}, {loadedData.tutorialStep[0]}");

    }

    
}
