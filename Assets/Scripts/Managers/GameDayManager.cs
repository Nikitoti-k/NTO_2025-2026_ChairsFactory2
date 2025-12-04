using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using WrightAngle.Waypoint;
using System;

public class GameDayManager : MonoBehaviour
{
    public static GameDayManager Instance { get; private set; }

    [SerializeField] private DailyTasksDatabase tasksDatabase;

    // ────────────────────────────────
    // Конфигурация по дням
    // ────────────────────────────────
    [Serializable]
    public class DayConfiguration
    {
        public string dayName = "День 1";
        [Tooltip("Объекты, которые ВКЛЮЧАЮТСЯ в начале этого дня")]
        public GameObject[] activateObjects;
        [Tooltip("Объекты, которые ВЫКЛЮЧАЮТСЯ в начале этого дня")]
        public GameObject[] deactivateObjects;
        [Tooltip("Указатель (WaypointTarget), который будет активен в этот день")]
        public WaypointTarget waypointTarget;
    }

    [Header("Конфигурация по дням")]
    [Tooltip("Индекс 0 = День 1")]
    public DayConfiguration[] dayConfigurations;

    private WaypointTarget currentActiveWaypoint;

    // ────────────────────────────────
    // Счётчики и состояние дня
    // ────────────────────────────────
    private readonly HashSet<string> fullyResearchedMinerals = new HashSet<string>();
    private int depositsBrokenToday;
    private int mineralsResearchedToday;
    private bool allDepositsBroken;
    private bool allReportsSubmitted;
    private DailyTaskSO currentTask;

    // ────────────────────────────────
    // События
    // ────────────────────────────────
    public UnityEvent OnAllDepositsBroken = new UnityEvent();
    public UnityEvent OnAllReportsSubmitted = new UnityEvent();
    public UnityEvent OnDayFullyCompleted = new UnityEvent();
    public UnityEvent<int> OnDepositsChanged = new UnityEvent<int>();
    public UnityEvent<int> OnMineralsResearchedChanged = new UnityEvent<int>();
    public UnityEvent<MineralData> OnMineralResearched = new UnityEvent<MineralData>();
    public UnityEvent OnAnyDepositBroken = new UnityEvent(); // Важно! Для анимаций/звуков

    // ────────────────────────────────
    // Публичные свойства
    // ────────────────────────────────
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
            Debug.LogError("[GameDayManager] tasksDatabase не назначен в инспекторе!");

        if (dayConfigurations == null || dayConfigurations.Length == 0)
            Debug.LogWarning("[GameDayManager] Не задана конфигурация дней! Указатели и объекты не будут переключаться.");

        SetDay(CurrentDay);
    }

    public void SetDay(int day)
    {
        CurrentDay = day;
        currentTask = tasksDatabase?.GetTaskForDay(day);

        ResetDailyCounters();
        ApplyDayConfiguration();

        // Красивый лог начала дня
       

        OnDepositsChanged.Invoke(depositsBrokenToday);
        OnMineralsResearchedChanged.Invoke(mineralsResearchedToday);
    }

    private void ApplyDayConfiguration()
    {
        // Отключаем предыдущий указатель
        if (currentActiveWaypoint != null)
        {
            currentActiveWaypoint.DeactivateWaypoint();
        }

        if (dayConfigurations == null || CurrentDay <= 0 || CurrentDay > dayConfigurations.Length)
        {
            currentActiveWaypoint = null;
            return;
        }

        var config = dayConfigurations[CurrentDay - 1];

        // Деактивируем
        if (config.deactivateObjects != null)
            foreach (var obj in config.deactivateObjects)
                if (obj != null) obj.SetActive(false);

        // Активируем
        if (config.activateObjects != null)
            foreach (var obj in config.activateObjects)
                if (obj != null) obj.SetActive(true);

        // Новый указатель
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

        Debug.Log($"<color=#00FF88>[Day {CurrentDay}] Применена конфигурация: {config.dayName}</color>");
    }

    private void ResetDailyCounters()
    {
        depositsBrokenToday = 0;
        mineralsResearchedToday = 0;
        allDepositsBroken = false;
        allReportsSubmitted = false;
        fullyResearchedMinerals.Clear();
    }

    // ────────────────────────────────
    // Основные методы регистрации прогресса
    // ────────────────────────────────
    public void RegisterDepositBroken()
    {
        depositsBrokenToday++;
        OnDepositsChanged.Invoke(depositsBrokenToday);
        OnAnyDepositBroken.Invoke();

        Debug.Log($"<color=orange><b>ЗАЛЕЖЬ СЛОМАНА</b></color> " +
                  $"| {depositsBrokenToday}/{DepositsToBreak} (День {CurrentDay})");

        if (depositsBrokenToday >= DepositsToBreak && !allDepositsBroken)
        {
            allDepositsBroken = true;
            OnAllDepositsBroken.Invoke();
            Debug.Log($"<color=lime><size=13>ВСЕ ЗАЛЕЖИ СЛОМАНЫ ЗА ДЕНЬ {CurrentDay}!</size>\n" +
                      $"Можно идти в вечернюю зону</color>");
        }

        CheckFullCompletion();
    }

    public void RegisterMineralResearched(MineralData mineral)
    {
        if (mineral == null)
        {
            Debug.LogWarning("[GameDayManager] RegisterMineralResearched: mineral == null!");
            return;
        }

        string id = mineral.UniqueInstanceID;

        if (!fullyResearchedMinerals.Add(id))
        {
            Debug.Log($"[МИНЕРАЛ] Уже исследован:  (ID: {id})");
            return;
        }

        mineralsResearchedToday++;
        OnMineralsResearchedChanged.Invoke(mineralsResearchedToday);
        OnMineralResearched?.Invoke(mineral);

        Debug.Log($"<color=cyan><b>МИНЕРАЛ ИССЛЕДОВАН</b></color>: \"\" " +
                  $"| {mineralsResearchedToday}/{MineralsToResearch} (ID: {id}, День {CurrentDay})");

        if (mineralsResearchedToday >= MineralsToResearch && !allReportsSubmitted)
        {
            allReportsSubmitted = true;
            OnAllReportsSubmitted.Invoke();
            Debug.Log($"<color=magenta><size=13>ВСЕ ОТЧЁТЫ СДАНЫ ЗА ДЕНЬ {CurrentDay}!</size>\n" +
                      $"Теперь можно спать!</color>");
        }

        CheckFullCompletion();
    }

    private void CheckFullCompletion()
    {
        if (allDepositsBroken && allReportsSubmitted)
        {
            Debug.Log($"<color=yellow><size=16>ДЕНЬ {CurrentDay} ПОЛНОСТЬЮ ЗАВЕРШЁН!</size></color>");
            OnDayFullyCompleted.Invoke();
        }
    }

    // ────────────────────────────────
    // Переход на следующий день
    // ────────────────────────────────
    public void SleepAndStartNewDay()
    {
        if (!CanSleep)
        {
            Debug.LogWarning($"[GameDayManager] Нельзя спать! Не все отчёты сданы ({mineralsResearchedToday}/{MineralsToResearch})");
            return;
        }

        SetDay(CurrentDay + 1);
        Debug.Log($"<color=gold><b>=== ПРОСНУЛСЯ В НОВОМ ДНЕ {CurrentDay}! ==</b></color>");
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