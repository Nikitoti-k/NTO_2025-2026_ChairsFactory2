using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class GameDayManager : MonoBehaviour
{
    public static GameDayManager Instance { get; private set; }

    [SerializeField] private DailyTasksDatabase tasksDatabase;

    private readonly HashSet<string> fullyResearchedMinerals = new();
    private int depositsBrokenToday;
    private int mineralsResearchedToday;
    private bool allDepositsBroken;
    private bool allReportsSubmitted;
    private DailyTaskSO currentTask;

    public UnityEvent OnAllDepositsBroken = new();
    public UnityEvent OnAllReportsSubmitted = new();
    public UnityEvent OnDayFullyCompleted = new();
    public UnityEvent<int> OnDepositsChanged = new();
    public UnityEvent<int> OnMineralsResearchedChanged = new();

    // ← НОВОЕ СОБЫТИЕ ДЛЯ ТУТОРИАЛА
    public UnityEvent<MineralData> OnMineralResearched = new();

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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (tasksDatabase == null)
            Debug.LogError("[GameDayManager] tasksDatabase не назначен!");
        else
            SetDay(CurrentDay);
    }

    public void SetDay(int day)
    {
        CurrentDay = day;
        currentTask = tasksDatabase?.GetTaskForDay(day);
        ResetDailyCounters();
        OnDepositsChanged.Invoke(depositsBrokenToday);
        OnMineralsResearchedChanged.Invoke(mineralsResearchedToday);
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
        depositsBrokenToday++;
        OnDepositsChanged.Invoke(depositsBrokenToday);
        if (depositsBrokenToday >= DepositsToBreak && !allDepositsBroken)
        {
            allDepositsBroken = true;
            OnAllDepositsBroken.Invoke();
        }
        CheckFullCompletion();
    }

    public void RegisterMineralResearched(MineralData mineral)
    {
        if (mineral == null) return;

        string id = mineral.UniqueInstanceID;
        if (!fullyResearchedMinerals.Add(id)) return;

        mineralsResearchedToday++;
        OnMineralsResearchedChanged.Invoke(mineralsResearchedToday);

        // ← ВЫЗЫВАЕМ СОБЫТИЕ ДЛЯ ТУТОРИАЛА
        OnMineralResearched?.Invoke(mineral);

        if (mineralsResearchedToday >= MineralsToResearch && !allReportsSubmitted)
        {
            allReportsSubmitted = true;
            OnAllReportsSubmitted.Invoke();
        }
        CheckFullCompletion();
    }

    private void CheckFullCompletion()
    {
        if (allDepositsBroken && allReportsSubmitted)
            OnDayFullyCompleted.Invoke();
    }

#if UNITY_EDITOR
    [ContextMenu("Тест: Сломать все залежи")] private void TestBreakAll() { depositsBrokenToday = DepositsToBreak; allDepositsBroken = true; OnAllDepositsBroken.Invoke(); CheckFullCompletion(); OnDepositsChanged.Invoke(depositsBrokenToday); }
    [ContextMenu("Тест: Завершить все отчёты")] private void TestSubmitAll() { mineralsResearchedToday = MineralsToResearch; allReportsSubmitted = true; OnAllReportsSubmitted.Invoke(); CheckFullCompletion(); OnMineralsResearchedChanged.Invoke(mineralsResearchedToday); }
#endif
}