using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class GameDayManager : MonoBehaviour
{
    public static GameDayManager Instance { get; private set; }

    [SerializeField] private int depositsToBreakPerDay = 5;
    [SerializeField] private int mineralsToResearchPerDay = 3;

    private readonly HashSet<string> fullyResearchedMinerals = new();

    private int depositsBrokenToday;
    private int mineralsBroughtToday;
    private int mineralsResearchedToday;
    private bool allDepositsBroken;
    private bool allReportsSubmitted;

    public UnityEvent OnAllDepositsBroken = new();
    public UnityEvent OnAllReportsSubmitted = new();
    public UnityEvent OnDayFullyCompleted = new();
    public UnityEvent<int> OnDepositsChanged = new();
    public UnityEvent<int> OnMineralsResearchedChanged = new();

    public int CurrentDay { get; private set; } = 1;
    public int DepositsToBreak => depositsToBreakPerDay;
    public int MineralsToResearch => mineralsToResearchPerDay;
    public int DepositsBrokenToday => depositsBrokenToday;
    public int MineralsResearchedToday => mineralsResearchedToday;
    public bool CanStartEvening => allDepositsBroken;
    public bool CanSleep => allReportsSubmitted;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        ResetDailyCounters();
    }

    private void ResetDailyCounters()
    {
        depositsBrokenToday = mineralsBroughtToday = mineralsResearchedToday = 0;
        allDepositsBroken = allReportsSubmitted = false;
        fullyResearchedMinerals.Clear();
    }

    public void RegisterDepositBroken()
    {
        depositsBrokenToday++;
        OnDepositsChanged.Invoke(depositsBrokenToday);

        if (depositsBrokenToday >= depositsToBreakPerDay && !allDepositsBroken)
        {
            allDepositsBroken = true;
            OnAllDepositsBroken.Invoke();
        }
        CheckFullCompletion();
    }

    public void RegisterMineralBrought(GameObject obj) => mineralsBroughtToday++;

    public void RegisterMineralResearched(MineralData mineral)
    {
        if (mineral == null || !fullyResearchedMinerals.Add(mineral.UniqueInstanceID)) return;

        mineralsResearchedToday++;
        OnMineralsResearchedChanged.Invoke(mineralsResearchedToday);

        if (mineralsResearchedToday >= mineralsToResearchPerDay && !allReportsSubmitted)
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

    public void StartNewDay(int day)
    {
        CurrentDay = day;
        ResetDailyCounters();
    }

#if UNITY_EDITOR
    [ContextMenu("Тест: Сломать все залежи")]
    private void TestBreakAll() { depositsBrokenToday = depositsToBreakPerDay; allDepositsBroken = true; OnAllDepositsBroken.Invoke(); CheckFullCompletion(); }

    [ContextMenu("Тест: Завершить все отчёты")]
    private void TestSubmitAll() { mineralsResearchedToday = mineralsToResearchPerDay; allReportsSubmitted = true; OnAllReportsSubmitted.Invoke(); CheckFullCompletion(); }
#endif
}