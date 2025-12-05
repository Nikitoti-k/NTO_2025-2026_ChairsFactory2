using UnityEngine;
using System.Reflection;

public class GameStateSaver : MonoBehaviour
{
    static GameStateSaver _instance;
    public static GameStateSaver Instance => FindObjectOfType<GameStateSaver>() ?? _instance;

    [SerializeField] GameDayManager dayManager;
    [SerializeField] WeatherManager weatherManager;

    private void Awake()
    {
        if (_instance != null && _instance != this) Destroy(gameObject);
        else _instance = this;
        DontDestroyOnLoad(gameObject);

        dayManager = FindObjectOfType<GameDayManager>();
        weatherManager = FindObjectOfType<WeatherManager>();
    }

    public GameStateBlock GetGameStateBlock()
    {
        return new GameStateBlock
        {
            currentDay = dayManager ? dayManager.CurrentDay : 1,
            currentTimeInMinutes = weatherManager ? weatherManager.CurrentTimeInMinutes : 480f,
            depositsBrokenToday = dayManager ? dayManager.DepositsBrokenToday : 0,
            mineralsResearchedToday = dayManager ? dayManager.MineralsResearchedToday : 0
        };
    }

    public void LoadFromBlock(GameStateBlock block)
    {
        if (block == null) return;

        weatherManager.SetTimeDirectly(block.currentDay, block.currentTimeInMinutes);
        dayManager?.SetDay(block.currentDay);

        var type = dayManager.GetType();
        type.GetField("depositsBrokenToday", BindingFlags.NonPublic | BindingFlags.Instance)?
            .SetValue(dayManager, block.depositsBrokenToday);
        type.GetField("mineralsResearchedToday", BindingFlags.NonPublic | BindingFlags.Instance)?
            .SetValue(dayManager, block.mineralsResearchedToday);

        dayManager.OnDepositsChanged?.Invoke(block.depositsBrokenToday);
        dayManager.OnMineralsResearchedChanged?.Invoke(block.mineralsResearchedToday);

        if (block.depositsBrokenToday >= dayManager.DepositsToBreak)
            dayManager.OnAllDepositsBroken?.Invoke();
        if (block.mineralsResearchedToday >= dayManager.MineralsToResearch)
            dayManager.OnAllReportsSubmitted?.Invoke();
    }
}