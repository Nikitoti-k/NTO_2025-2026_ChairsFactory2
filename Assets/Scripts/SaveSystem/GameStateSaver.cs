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
        var data = new SaveData
        {
            uniqueID = "GAME_STATE_SINGLETON",
            prefabIdentifier = "GameState",
            gameState = new SaveData.GameStateBlock
            {
                currentDay = dayManager.CurrentDay,
                currentTimeInMinutes = weatherManager.CurrentTimeInMinutes,
                depositsBrokenToday = dayManager.DepositsBrokenToday,
                mineralsResearchedToday = dayManager.MineralsResearchedToday,
                canStartEvening = dayManager.CanStartEvening,
                canSleep = dayManager.CanSleep
            }
        };
        return data;
    }

    public void LoadFromSaveData(SaveData data)
    {
        if (data.gameState == null) return;

        weatherManager.SetTimeDirectly(data.gameState.currentDay, data.gameState.currentTimeInMinutes);
        dayManager.StartNewDay(data.gameState.currentDay);

        var type = dayManager.GetType();
        type.GetField("depositsBrokenToday", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(dayManager, data.gameState.depositsBrokenToday);
        type.GetField("mineralsResearchedToday", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(dayManager, data.gameState.mineralsResearchedToday);
        type.GetField("allDepositsBroken", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(dayManager, data.gameState.canStartEvening);
        type.GetField("allReportsSubmitted", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(dayManager, data.gameState.canSleep);

        dayManager.OnDepositsChanged?.Invoke(data.gameState.depositsBrokenToday);
        dayManager.OnMineralsResearchedChanged?.Invoke(data.gameState.mineralsResearchedToday);

        if (data.gameState.canStartEvening) dayManager.OnAllDepositsBroken?.Invoke();
        if (data.gameState.canSleep) dayManager.OnAllReportsSubmitted?.Invoke();
        if (data.gameState.canStartEvening && data.gameState.canSleep) dayManager.OnDayFullyCompleted?.Invoke();
    }
}