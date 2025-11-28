using UnityEngine;

public class GameStateSaver : MonoBehaviour
{
    private static GameStateSaver _instance;
    public static GameStateSaver Instance => FindObjectOfType<GameStateSaver>() ?? _instance;

    [SerializeField] private GameDayManager dayManager;
    [SerializeField] private WeatherManager weatherManager;

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
            currentDay = dayManager.CurrentDay,
            currentTimeInMinutes = weatherManager.CurrentTimeInMinutes,
            depositsBrokenToday = dayManager.DepositsBrokenToday,
            mineralsResearchedToday = dayManager.MineralsResearchedToday,
            canStartEvening = dayManager.CanStartEvening,
            canSleep = dayManager.CanSleep
        };
    }

    public void LoadFromBlock(GameStateBlock block)
    {
        if (block == null) return;

        weatherManager.SetTimeDirectly(block.currentDay, block.currentTimeInMinutes);
        dayManager.StartNewDay(block.currentDay);

        // Восстанавливаем счётчики через рефлексию (если приватные)
        var type = dayManager.GetType();
        type.GetField("depositsBrokenToday", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(dayManager, block.depositsBrokenToday);
        type.GetField("mineralsResearchedToday", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(dayManager, block.mineralsResearchedToday);

        // Вызываем события
        dayManager.OnDepositsChanged?.Invoke(block.depositsBrokenToday);
        dayManager.OnMineralsResearchedChanged?.Invoke(block.mineralsResearchedToday);

        if (block.canStartEvening) dayManager.OnAllDepositsBroken?.Invoke();
        if (block.canSleep) dayManager.OnAllReportsSubmitted?.Invoke();
    }

    // Если у тебя есть глобальные отчёты
   // public string GetGlobalReports() => ReportManager.Instance?.SerializeAllReports() ?? "";
   // public void LoadGlobalReports(string data) => ReportManager.Instance?.DeserializeReports(data);
}