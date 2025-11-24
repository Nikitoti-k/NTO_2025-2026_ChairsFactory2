using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class GameDayManager : MonoBehaviour
{
    public static GameDayManager Instance { get; private set; }

    [Header("Настройки дня")]
    [SerializeField] private int depositsToBreakPerDay = 5;
    [SerializeField] private int mineralsToResearchPerDay = 3;

   
    private HashSet<string> fullyResearchedMinerals = new HashSet<string>();

   
    private int depositsBrokenToday = 0;
    private int mineralsBroughtToday = 0;
    private int mineralsResearchedToday = 0;

   
    private bool allDepositsBroken = false;
    private bool allReportsSubmitted = false;

   
    public UnityEvent OnAllDepositsBroken;     
    public UnityEvent OnAllReportsSubmitted;    
    public UnityEvent OnDayFullyCompleted;     

    public UnityEvent<int> OnDepositsChanged;
    public UnityEvent<int> OnMineralsResearchedChanged;

    
    public int CurrentDay { get; private set; } = 1;
    public int DepositsToBreak => depositsToBreakPerDay;
    public int MineralsToResearch => mineralsToResearchPerDay;
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

        ResetDailyCounters();
        DebugLog($"<color=cyan>GameDayManager инициализирован. День {CurrentDay}</color>");
    }

    private void ResetDailyCounters()
    {
        depositsBrokenToday = 0;
        mineralsBroughtToday = 0;
        mineralsResearchedToday = 0;
        allDepositsBroken = false;
        allReportsSubmitted = false;

        DebugLog("<color=yellow>Счётчики дня сброшены</color>");
    }

    public void RegisterDepositBroken()
    {
        depositsBrokenToday++;
        OnDepositsChanged?.Invoke(depositsBrokenToday);

        DebugLog($"<color=orange>Залежь сломана! {depositsBrokenToday}/{depositsToBreakPerDay}</color>");

        if (depositsBrokenToday >= depositsToBreakPerDay && !allDepositsBroken)
        {
            allDepositsBroken = true;
            DebugLog("<color=lime>ВСЕ ЗАЛЕЖИ СЛОМАНЫ! Можно начинать вечер!</color>");
            OnAllDepositsBroken?.Invoke();
        }

        CheckFullCompletion();
    }

    public void RegisterMineralBrought(GameObject obj)
    {
        mineralsBroughtToday++;
        DebugLog($"<color=purple>Минерал принесён на базу: {obj.name} (всего: {mineralsBroughtToday})</color>");
    }

    public void RegisterMineralResearched(MineralData mineral)
    {
        if (mineral == null) return;

        string uniqueID = mineral.UniqueInstanceID;

        if (fullyResearchedMinerals.Add(uniqueID))
        {
            mineralsResearchedToday++;
            OnMineralsResearchedChanged?.Invoke(mineralsResearchedToday);

            DebugLog($"<color=lime>НОВЫЙ ОТЧЁТ: {mineral.transform.root.name} (ID: {uniqueID.Substring(0, 8)}...)</color>");

            if (mineralsResearchedToday >= mineralsToResearchPerDay && !allReportsSubmitted)
            {
                allReportsSubmitted = true;
                OnAllReportsSubmitted?.Invoke();
            }
            CheckFullCompletion();
        }
        else
        {
            DebugLog($"<color=gray>Этот экземпляр уже был исследован (ID: {uniqueID.Substring(0, 8)}...)</color>");
        }
    }

    private void CheckFullCompletion()
    {
        if (allDepositsBroken && allReportsSubmitted)
        {
            DebugLog("<color=magenta>ДЕНЬ ПОЛНОСТЬЮ ЗАВЕРШЁН!</color>");
            OnDayFullyCompleted?.Invoke();
        }
    }

    public void StartNewDay(int day)
    {
        CurrentDay = day;
        ResetDailyCounters();
        DebugLog($"<color=cyan>НАЧАЛСЯ НОВЫЙ ДЕНЬ: {CurrentDay}!</color>");
    }

    private void DebugLog(string message)
    {
        Debug.Log($"<b>[GameDayManager]</b> {message}");
    }

#if UNITY_EDITOR
    [ContextMenu("Сломать все залежи (тест)")]
    private void TestBreakAllDeposits()
    {
        depositsBrokenToday = depositsToBreakPerDay;
        allDepositsBroken = true;
        OnAllDepositsBroken?.Invoke();
        CheckFullCompletion();
    }

    [ContextMenu("Отправить все отчёты (тест)")]
    private void TestSubmitAllReports()
    {
        mineralsResearchedToday = mineralsToResearchPerDay;
        allReportsSubmitted = true;
        OnAllReportsSubmitted?.Invoke();
        CheckFullCompletion();
    }
#endif
}