using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System;
using WrightAngle.Waypoint;

public interface IGameDayManager
{
    int CurrentDay { get; }
    DailyTaskSO CurrentTask { get; }
    int DepositsToBreak { get; }
    int MineralsToResearch { get; }
    int DepositsBrokenToday { get; }
    int MineralsResearchedToday { get; }
    bool CanStartEvening { get; }
    bool CanSleep { get; }
    void SetDay(int day);
    void RegisterDepositBroken();
    void RegisterMineralResearched(MineralData mineral);
    void SleepAndStartNewDay();
}

public class GameDayManager : MonoBehaviour, IGameDayManager
{
    public static GameDayManager Instance { get; private set; }
    [SerializeField] private DailyTasksDatabase tasksDatabase;

    [Serializable]
    public class DayConfiguration
    {
        public string dayName = "День 1";
        public GameObject[] activateObjects;
        public GameObject[] deactivateObjects;
        public WaypointTarget waypointTarget;
    }

    [Header("Конфигурация по дням")]
    public DayConfiguration[] dayConfigurations;

    private WaypointTarget currentActiveWaypoint;
    private readonly HashSet<string> fullyResearchedMinerals = new HashSet<string>();
    private int depositsBrokenToday;
    private int mineralsResearchedToday;
    private bool allDepositsBroken;
    private bool allReportsSubmitted;
    private DailyTaskSO currentTask;

    public UnityEvent OnAllDepositsBroken = new UnityEvent();
    public UnityEvent OnAllReportsSubmitted = new UnityEvent();
    public UnityEvent OnDayFullyCompleted = new UnityEvent();
    public UnityEvent<int> OnDepositsChanged = new UnityEvent<int>();
    public UnityEvent<int> OnMineralsResearchedChanged = new UnityEvent<int>();
    public UnityEvent<MineralData> OnMineralResearched = new UnityEvent<MineralData>();
    public UnityEvent OnAnyDepositBroken = new UnityEvent();

    public int CurrentDay { get; private set; } = 1;
    public DailyTaskSO CurrentTask => currentTask;
    public int DepositsToBreak => currentTask?.depositsToBreak ?? 5;
    public int MineralsToResearch => currentTask?.mineralsToResearch ?? 3;
    public int DepositsBrokenToday => depositsBrokenToday;
    public int MineralsResearchedToday => mineralsResearchedToday;
    public bool CanStartEvening => allDepositsBroken;
    public bool CanSleep => allReportsSubmitted;

    private void Awake()
    {
        try
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (tasksDatabase == null)
                Debug.LogError("[GameDayManager] tasksDatabase not assigned");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameDayManager] Awake error: {e.Message}");
        }
    }

    private void Start()
    {
        SetDay(CurrentDay);
    }

    public void SetDay(int day)
    {
        try
        {
            CurrentDay = day;
            currentTask = tasksDatabase?.GetTaskForDay(day);
            ResetDailyCounters();
            ApplyDayConfiguration();

            OnDepositsChanged.Invoke(depositsBrokenToday);
            OnMineralsResearchedChanged.Invoke(mineralsResearchedToday);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameDayManager] SetDay failed: {e.Message}");
        }
    }

    private void ApplyDayConfiguration()
    {
        if (currentActiveWaypoint != null)
            currentActiveWaypoint.DeactivateWaypoint();

        if (dayConfigurations == null || CurrentDay <= 0 || CurrentDay > dayConfigurations.Length)
        {
            currentActiveWaypoint = null;
            return;
        }

        var config = dayConfigurations[CurrentDay - 1];

        if (config.deactivateObjects != null)
            foreach (var obj in config.deactivateObjects)
                if (obj != null) obj.SetActive(false);

        if (config.activateObjects != null)
            foreach (var obj in config.activateObjects)
                if (obj != null) obj.SetActive(true);

        if (config.waypointTarget != null)
        {
            currentActiveWaypoint = config.waypointTarget;
            currentActiveWaypoint.gameObject.SetActive(true);
            currentActiveWaypoint.ActivateWaypoint();
        }
        else
        {
            currentActiveWaypoint = null;
        }
    }

    private void ResetDailyCounters()
    {
        depositsBrokenToday = 0;
        mineralsResearchedToday = 0;
        allDepositsBroken = false;
        allReportsSubmitted = false;
        fullyResearchedMinerals.Clear();
    }

    public void RegisterDepositBroken()
    {
        try
        {
            depositsBrokenToday++;
            OnDepositsChanged.Invoke(depositsBrokenToday);
            OnAnyDepositBroken.Invoke();

            if (depositsBrokenToday >= DepositsToBreak && !allDepositsBroken)
            {
                allDepositsBroken = true;
                OnAllDepositsBroken.Invoke();
            }
            CheckFullCompletion();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameDayManager] RegisterDepositBroken error: {e.Message}");
        }
    }

    public void RegisterMineralResearched(MineralData mineral)
    {
        try
        {
            if (mineral == null) return;

            string id = mineral.UniqueInstanceID;
            if (!fullyResearchedMinerals.Add(id)) return;

            mineralsResearchedToday++;
            OnMineralsResearchedChanged.Invoke(mineralsResearchedToday);
            OnMineralResearched?.Invoke(mineral);

            if (mineralsResearchedToday >= MineralsToResearch && !allReportsSubmitted)
            {
                allReportsSubmitted = true;
                OnAllReportsSubmitted.Invoke();
            }
            CheckFullCompletion();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameDayManager] RegisterMineralResearched error: {e.Message}");
        }
    }

    private void CheckFullCompletion()
    {
        if (allDepositsBroken && allReportsSubmitted)
            OnDayFullyCompleted.Invoke();
    }

    public void SleepAndStartNewDay()
    {
        try
        {
            if (!CanSleep) return;
            SetDay(CurrentDay + 1);

            var radio = FindObjectOfType<RadioMonologue>();
            if (radio != null)
            {
                if (CurrentDay == 2) radio.PlayMorningMonologue_Day2();
                else if (CurrentDay == 3) radio.PlayMorningMonologue_Day3();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameDayManager] SleepAndStartNewDay error: {e.Message}");
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Тест: Следующий день")]
    private void TestNextDay() => SleepAndStartNewDay();

    [ContextMenu("Тест: Сломать все залежи")]
    private void TestBreakAll()
    {
        depositsBrokenToday = DepositsToBreak;
        allDepositsBroken = true;
        OnAllDepositsBroken.Invoke();
        OnDepositsChanged.Invoke(depositsBrokenToday);
        CheckFullCompletion();
    }

    [ContextMenu("Тест: Сдать все отчёты")]
    private void TestSubmitAll()
    {
        mineralsResearchedToday = MineralsToResearch;
        allReportsSubmitted = true;
        OnAllReportsSubmitted.Invoke();
        OnMineralsResearchedChanged.Invoke(mineralsResearchedToday);
        CheckFullCompletion();
    }
#endif
}