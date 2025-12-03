using UnityEngine;
using UnityEngine.Events;

public class WeatherManager : MonoBehaviour
{
    public enum TimeOfDay { Night, Morning, Day, Evening }
    public static WeatherManager Instance { get; private set; }

    [SerializeField] Light mainDirectionalLight;
    [SerializeField] Light moonLight;
    [SerializeField] Gradient directionalLightGradient;
    [SerializeField] Gradient ambientLightGradient;
    [SerializeField, Range(60f, 3600f)] float realSecondsPerGameDay = 300f;

    [Space]
    [Tooltip("Если включено — время идёт всегда, даже без разрешений фаз")]
    public bool forceTimeFlow = false;

    [Header("=== ЗОНЫ ДЛЯ СМЕНЫ ВРЕМЕНИ ===")]
    [SerializeField] private Transform baseCenterPoint;        // Центр базы (кровать/дом)
    [SerializeField] private float distanceToStartDay = 150f;  // Отъехал → день (12:00)
    [SerializeField] private float distanceToTriggerEvening = 80f; // Вернулся → вечер (18:00)

    public UnityEvent<int> OnDayChanged = new UnityEvent<int>();
    public UnityEvent<TimeOfDay> OnPeriodChanged = new UnityEvent<TimeOfDay>();
    public UnityEvent<float, float> OnTimeChanged = new UnityEvent<float, float>();

    public int CurrentDay { get; private set; } = 1;
    public float CurrentTimeInMinutes { get; private set; } = 480f; // 8:00 утра
    public TimeOfDay CurrentPeriod => GetCurrentPeriod();

    private float timeProgress;
    private Vector3 sunDefaultAngles;
    private Vector3 moonDefaultAngles;
    private TimeOfDay currentPeriodCache;

    // === ФАЗЫ ВРЕМЕНИ ===
    private bool dayTriggered = false;     // Отъехал далеко → день разрешён
    private bool depositsBroken = false;    // Сломаны все залежи → вечер разрешён
    private bool eveningTriggered = false;  // Вернулся → ночь разрешена

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (mainDirectionalLight == null) mainDirectionalLight = FindObjectOfType<Light>();
        sunDefaultAngles = mainDirectionalLight.transform.localEulerAngles;
        if (moonLight) moonDefaultAngles = moonLight.transform.localEulerAngles;

        JumpTo(480f); // 8:00 утра
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        // === 1. ВРЕМЯ ИДЁТ ТОЛЬКО ЕСЛИ ФАЗА РАЗРЕШЕНА ===
        if (forceTimeFlow || CanTimeProgress())
        {
            timeProgress += Time.deltaTime / realSecondsPerGameDay;
            CurrentTimeInMinutes = timeProgress * 1440f;
        }

        // === 2. НОВЫЙ ДЕНЬ ===
        if (CurrentTimeInMinutes >= 1440f)
        {
            timeProgress = 0f;
            CurrentTimeInMinutes = 480f; // 8:00 утра
            CurrentDay++;
            OnDayChanged.Invoke(CurrentDay);
            ResetPhaseFlags();
            GameDayManager.Instance?.SetDay(CurrentDay);
        }

        // === 3. ПРОВЕРЯЕМ УСЛОВИЯ ФАЗ ===
        HandlePhaseTriggers();

        // === 4. ОБНОВЛЯЕМ ОСВЕЩЕНИЕ ===
        timeProgress = CurrentTimeInMinutes / 1440f;
        UpdateLightingAndRotation();

        TimeOfDay newPeriod = GetCurrentPeriod();
        if (newPeriod != currentPeriodCache)
        {
            currentPeriodCache = newPeriod;
            OnPeriodChanged.Invoke(newPeriod);
        }

        OnTimeChanged.Invoke(CurrentDay, CurrentTimeInMinutes);
    }

    // === МОЖНО ЛИ ПРОГРЕССИРОВАТЬ ВРЕМЯ? ===
    private bool CanTimeProgress()
    {
        // 8:00-12:00 — всегда идёт (утро)
        if (CurrentTimeInMinutes < 720f) return true;

        // 12:00-18:00 — ждём все залежи
        if (CurrentTimeInMinutes < 1080f) return depositsBroken;

        // 18:00-24:00 — ждём возвращения
        return eveningTriggered;
    }

    // === ПРОВЕРКА ТРИГГЕРОВ ФАЗ ===
    private void HandlePhaseTriggers()
    {
        var player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null || baseCenterPoint == null) return;

        float distFromBase = Vector3.Distance(player.position, baseCenterPoint.position);

        // 1. ОТЪЕЗЖАЕТ ДАЛЕКО → ДЕНЬ РАЗРЕШЁН (12:00)
        if (!dayTriggered && distFromBase >= distanceToStartDay && CurrentTimeInMinutes >= 720f)
        {
            dayTriggered = true;
            Debug.Log("[Weather] День начался — игрок отъехал далеко от базы!");
        }

        // 2. СЛОМАНЫ ВСЕ ЗАЛЕЖИ → ВЕЧЕР РАЗРЕШЁН (18:00)
        if (dayTriggered && !depositsBroken && GameDayManager.Instance != null &&
            GameDayManager.Instance.DepositsBrokenToday >= GameDayManager.Instance.DepositsToBreak &&
            CurrentTimeInMinutes >= 1080f)
        {
            depositsBroken = true;
            Debug.Log("[Weather] Вечер разрешён — все залежи сломаны!");
        }

        // 3. ВЕРНУЛСЯ НА БАЗУ → НОЧЬ РАЗРЕШЕНА (24:00)
        if (depositsBroken && !eveningTriggered && distFromBase <= distanceToTriggerEvening &&
            CurrentTimeInMinutes >= 1080f)
        {
            eveningTriggered = true;
            Debug.Log("[Weather] Ночь разрешена — игрок вернулся на базу!");
        }
    }

    // === СПАТЬ МОЖНО ТОЛЬКО ПОСЛЕ ВСЕХ ЗАДАНИЙ ===
    public bool CanSleepNow()
    {
        return depositsBroken && eveningTriggered && GameDayManager.Instance != null &&
               GameDayManager.Instance.CanSleep;
    }

    public void SleepAndNextDay()
    {
        CurrentDay++;
        JumpTo(480f); // 8:00 утра следующего дня
        ResetPhaseFlags();
        OnDayChanged.Invoke(CurrentDay);
        GameDayManager.Instance?.SetDay(CurrentDay);
    }

    public void SetTimeDirectly(int day, float minutes)
    {
        CurrentDay = day;
        JumpTo(minutes);
    }

    private void JumpTo(float minutes)
    {
        CurrentTimeInMinutes = Mathf.Clamp(minutes, 0f, 1439.99f);
        timeProgress = CurrentTimeInMinutes / 1440f;
        UpdateLightingAndRotation();
    }

    private void UpdateLightingAndRotation()
    {
        if (mainDirectionalLight)
        {
            mainDirectionalLight.color = directionalLightGradient.Evaluate(timeProgress);
            mainDirectionalLight.transform.localEulerAngles = new Vector3(360f * timeProgress - 90f, sunDefaultAngles.y, sunDefaultAngles.z);
            float sun = Mathf.Clamp01(Mathf.Sin(timeProgress * Mathf.PI));
            sun = Mathf.Max(sun, 0.03f);
            mainDirectionalLight.intensity = sun * 2f;
        }

        if (moonLight)
        {
            moonLight.transform.localEulerAngles = new Vector3(360f * timeProgress + 90f, moonDefaultAngles.y, moonDefaultAngles.z);
            moonLight.intensity = (1f - Mathf.Abs(Mathf.Sin(timeProgress * Mathf.PI))) * 0.8f;
        }

        RenderSettings.ambientLight = ambientLightGradient.Evaluate(timeProgress);
    }

    private TimeOfDay GetCurrentPeriod()
    {
        float m = CurrentTimeInMinutes;
        if (m < 480f) return TimeOfDay.Night;
        if (m < 720f) return TimeOfDay.Morning;
        if (m < 1080f) return TimeOfDay.Day;
        return TimeOfDay.Evening;
    }

    private void ResetPhaseFlags()
    {
        dayTriggered = false;
        depositsBroken = false;
        eveningTriggered = false;
    }

    public string GetFormattedTime()
    {
        int h = Mathf.FloorToInt(CurrentTimeInMinutes / 60f);
        int m = Mathf.FloorToInt(CurrentTimeInMinutes % 60f);
        return $"{h:00}:{m:00}";
    }
}