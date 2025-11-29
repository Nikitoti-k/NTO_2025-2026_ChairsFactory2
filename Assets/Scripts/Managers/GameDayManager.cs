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
        Debug.Log("[GameDayManager] Инициализация GameDayManager...");
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameDayManager] Дубликат уничтожен!");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[GameDayManager] Instance установлен, DontDestroyOnLoad OK");

        if (tasksDatabase == null)
            Debug.LogError("[GameDayManager] tasksDatabase не назначен в инспекторе!");
        else
            Debug.Log($"[GameDayManager] База задач загружена: {tasksDatabase.tasks.Count} дней");

        SetDay(CurrentDay);
    }

    public void SetDay(int day)
    {
        Debug.Log($"[GameDayManager] Установка дня {day}...");
        CurrentDay = day;

        if (tasksDatabase == null)
        {
            Debug.LogError($"[GameDayManager] Не могу загрузить задачу для дня {day} - tasksDatabase == null!");
            currentTask = null;
        }
        else
        {
            currentTask = tasksDatabase.GetTaskForDay(day);
            if (currentTask == null)
                Debug.LogError($"[GameDayManager] Задача для дня {day} не найдена в базе!");
            else
                Debug.Log($"[GameDayManager] День {day}: {DepositsToBreak} залежей, {MineralsToResearch} минералов, пещера {currentTask.caveSceneName} на {currentTask.caveEntrancePosition}");
        }

        ResetDailyCounters();
        OnDepositsChanged.Invoke(depositsBrokenToday);
        OnMineralsResearchedChanged.Invoke(mineralsResearchedToday);
        Debug.Log($"[GameDayManager] День {day} установлен. Счётчики сброшены: залежи {depositsBrokenToday}/{DepositsToBreak}, минералы {mineralsResearchedToday}/{MineralsToResearch}");
    }

    private void ResetDailyCounters()
    {
        Debug.Log("[GameDayManager] Сброс дневных счётчиков...");
        depositsBrokenToday = 0;
        mineralsResearchedToday = 0;
        allDepositsBroken = false;
        allReportsSubmitted = false;
        fullyResearchedMinerals.Clear();
        Debug.Log("[GameDayManager] Счётчики сброшены успешно");
    }

    public void RegisterDepositBroken()
    {
        depositsBrokenToday++;
        Debug.Log($"[GameDayManager] Залежь сломана! Прогресс: {depositsBrokenToday}/{DepositsToBreak}");
        OnDepositsChanged.Invoke(depositsBrokenToday);

        if (depositsBrokenToday >= DepositsToBreak && !allDepositsBroken)
        {
            allDepositsBroken = true;
            Debug.Log("[GameDayManager] ✅ ВСЕ ЗАЛЕЖИ СЛОМАНЫ! Можно начинать вечер!");
            OnAllDepositsBroken.Invoke();
        }
        CheckFullCompletion();
    }

    public void RegisterMineralResearched(MineralData mineral)
    {
        if (mineral == null)
        {
            Debug.LogWarning("[GameDayManager] Попытка зарегистрировать null минерал!");
            return;
        }

        string id = mineral.UniqueInstanceID;
        if (!fullyResearchedMinerals.Add(id))
        {
            Debug.Log($"[GameDayManager] Минерал {id} уже исследован сегодня, пропускаем");
            return;
        }

        mineralsResearchedToday++;
        Debug.Log($"[GameDayManager] Минерал исследован! ID: {id}, Прогресс: {mineralsResearchedToday}/{MineralsToResearch}");
        OnMineralsResearchedChanged.Invoke(mineralsResearchedToday);

        if (mineralsResearchedToday >= MineralsToResearch && !allReportsSubmitted)
        {
            allReportsSubmitted = true;
            Debug.Log("[GameDayManager] ✅ ВСЕ ОТЧЁТЫ СДАНЫ! Можно спать!");
            OnAllReportsSubmitted.Invoke();
        }
        CheckFullCompletion();
    }

    private void CheckFullCompletion()
    {
        bool wasCompleted = allDepositsBroken && allReportsSubmitted;
        if (wasCompleted)
        {
            Debug.Log("[GameDayManager] 🎉 ДЕНЬ ПОЛНОСТЬЮ ЗАВЕРШЁН!");
            OnDayFullyCompleted.Invoke();
        }
        else
        {
            Debug.Log($"[GameDayManager] День не завершён: залежи {depositsBrokenToday}/{DepositsToBreak} ({allDepositsBroken}), минералы {mineralsResearchedToday}/{MineralsToResearch} ({allReportsSubmitted})");
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Тест: Сломать все залежи")]
    private void TestBreakAll()
    {
        Debug.Log("[GameDayManager] ТЕСТ: Ломаем все залежи");
        depositsBrokenToday = DepositsToBreak;
        allDepositsBroken = true;
        OnAllDepositsBroken.Invoke();
        CheckFullCompletion();
        OnDepositsChanged.Invoke(depositsBrokenToday);
    }

    [ContextMenu("Тест: Завершить все отчёты")]
    private void TestSubmitAll()
    {
        Debug.Log("[GameDayManager] ТЕСТ: Завершаем все отчёты");
        mineralsResearchedToday = MineralsToResearch;
        allReportsSubmitted = true;
        OnAllReportsSubmitted.Invoke();
        CheckFullCompletion();
        OnMineralsResearchedChanged.Invoke(mineralsResearchedToday);
    }
#endif
}