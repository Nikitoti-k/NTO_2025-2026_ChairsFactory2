using UnityEngine;
using System.Reflection;

[RequireComponent(typeof(GameDayManager), typeof(WeatherManager))]
public class GameStateSaver : MonoBehaviour, ISaveable
{
    private GameDayManager dayManager;
    private WeatherManager weatherManager;

    private void Awake()
    {
        dayManager = GetComponent<GameDayManager>();
        weatherManager = GetComponent<WeatherManager>();
    }

    public string GetUniqueID() => "GAME_STATE_SINGLETON";

    public SaveData GetSaveData()
    {
        return new SaveData
        {
            uniqueID = "GAME_STATE_SINGLETON",
            prefabIdentifier = "GameState",
            customInt1 = dayManager.CurrentDay,
            customFloat1 = weatherManager.CurrentTimeInMinutes,
            customInt2 = dayManager.DepositsBrokenToday,
            customInt3 = dayManager.MineralsResearchedToday,
            customBool1 = dayManager.CanStartEvening,
            customBool2 = dayManager.CanSleep
        };
    }

    public void LoadFromSaveData(SaveData data)
    {
        weatherManager.SetTimeDirectly(data.customInt1, data.customFloat1);
        dayManager.StartNewDay(data.customInt1);

        var type = dayManager.GetType();
        type.GetField("depositsBrokenToday", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(dayManager, data.customInt2);
        type.GetField("mineralsResearchedToday", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(dayManager, data.customInt3);
        type.GetField("allDepositsBroken", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(dayManager, data.customBool1);
        type.GetField("allReportsSubmitted", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(dayManager, data.customBool2);

        dayManager.OnDepositsChanged?.Invoke(data.customInt2);
        dayManager.OnMineralsResearchedChanged?.Invoke(data.customInt3);

        if (data.customBool1) dayManager.OnAllDepositsBroken?.Invoke();
        if (data.customBool2) dayManager.OnAllReportsSubmitted?.Invoke();
        if (data.customBool1 && data.customBool2) dayManager.OnDayFullyCompleted?.Invoke();
    }
}