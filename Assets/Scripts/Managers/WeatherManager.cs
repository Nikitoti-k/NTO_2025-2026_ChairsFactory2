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

    [Header("=== НОВЫЙ РЕЖИМ: ВРЕМЯ ДО 00:00 + СОН ===")]
    [Tooltip("Время течёт непрерывно от 8:00 до 00:00, потом останавливается до сна")]
    public bool isTimeFlow = false;

    [Header("=== ЗОНЫ ДЛЯ СМЕНЫ ВРЕМЕНИ (используется только без isTimeFlow) ===")]
    [SerializeField] private Transform baseCenterPoint; // Центр базы
    [SerializeField] private float distanceToStartDay = 150f;
    [SerializeField] private float distanceToTriggerEvening = 80f;

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

    // Флаги фаз (используются только если isTimeFlow == false)
    private bool dayTriggered = false;
    private bool depositsBroken = false;
    private bool eveningTriggered = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
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

        bool shouldAdvanceTime = false;

        if (isTimeFlow)
        {
            // === РЕЖИМ: Время идёт до 00:00, потом ждём сон ===
            shouldAdvanceTime = CurrentTimeInMinutes < 1440f;
        }
        else
        {
            // === СТАРЫЙ РЕЖИМ: по фазам и триггерам ===
            shouldAdvanceTime = forceTimeFlow || CanTimeProgress();
        }

        // === ПРОДВИЖЕНИЕ ВРЕМЕНИ ===
        if (shouldAdvanceTime)
        {
            timeProgress += Time.deltaTime / realSecondsPerGameDay;
            CurrentTimeInMinutes = timeProgress * 1440f;
        }

        // === ОБРАБОТКА ПОЛУНОЧИ ===
        if (CurrentTimeInMinutes >= 1440f)
        {
            if (isTimeFlow)
            {
                // Замораживаем на 23:59:59 — день НЕ переходит автоматически
                CurrentTimeInMinutes = 1439.99f;
                timeProgress = CurrentTimeInMinutes / 1440f;
            }
            else
            {
                // Классический автоматический переход на новый день
                StartNewDay();
            }
        }

        // Обновляем триггеры фаз (только если не в режиме isTimeFlow)
        if (!isTimeFlow)
            HandlePhaseTriggers();

        // Обновляем освещение и поворот солнца/луны
        timeProgress = CurrentTimeInMinutes / 1440f;
        UpdateLightingAndRotation();

        // События смены периода и времени
        TimeOfDay newPeriod = GetCurrentPeriod();
        if (newPeriod != currentPeriodCache)
        {
            currentPeriodCache = newPeriod;
            OnPeriodChanged.Invoke(newPeriod);
        }

        OnTimeChanged.Invoke(CurrentDay, CurrentTimeInMinutes);
    }

    private void StartNewDay()
    {
        timeProgress = 0f;
        CurrentTimeInMinutes = 480f; // 8:00 утра
        CurrentDay++;
        OnDayChanged.Invoke(CurrentDay);
        ResetPhaseFlags();
        GameDayManager.Instance?.SetDay(CurrentDay);
    }

    private bool CanTimeProgress()
    {
        if (CurrentTimeInMinutes < 720f) return true;                   // 8:00–12:00 — утро всегда идёт
        if (CurrentTimeInMinutes < 1080f) return depositsBroken;       // 12:00–18:00 — ждём сломанные залежи
        return eveningTriggered;                                        // 18:00–00:00 — ждём возвращения на базу
    }

    private void HandlePhaseTriggers()
    {
        var player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null || baseCenterPoint == null) return;

        float distFromBase = Vector3.Distance(player.position, baseCenterPoint.position);

        // 1. Отъехал далеко → день (12:00) разрешён
        if (!dayTriggered && distFromBase >= distanceToStartDay && CurrentTimeInMinutes >= 720f)
        {
            dayTriggered = true;
            Debug.Log("[Weather] День начался — игрок отъехал далеко от базы!");
        }

        // 2. Сломаны все залежи → вечер (18:00) разрешён
        if (dayTriggered && !depositsBroken && GameDayManager.Instance != null &&
            GameDayManager.Instance.DepositsBrokenToday >= GameDayManager.Instance.DepositsToBreak &&
            CurrentTimeInMinutes >= 1080f)
        {
            depositsBroken = true;
            Debug.Log("[Weather] Вечер разрешён — все залежи сломаны!");
        }

        // 3. Вернулся на базу → ночь (00:00) разрешена
        if (depositsBroken && !eveningTriggered && distFromBase <= distanceToTriggerEvening && CurrentTimeInMinutes >= 1080f)
        {
            eveningTriggered = true;
            Debug.Log("[Weather] Ночь разрешена — игрок вернулся на базу!");
        }
    }

    // === СОН — теперь работает в обоих режимах ===
    public bool CanSleepNow()
    {
        if (isTimeFlow)
            return CurrentTimeInMinutes >= 1439f; // можно спать только в 00:00 и позже

        return depositsBroken && eveningTriggered && GameDayManager.Instance != null && GameDayManager.Instance.CanSleep;
    }

    public void SleepAndNextDay()
    {
        CurrentDay++;
        JumpTo(480f); // 8:00 утра следующего дня
        timeProgress = 480f / 1440f;

        ResetPhaseFlags();
        OnDayChanged.Invoke(CurrentDay);
        GameDayManager.Instance?.SetDay(CurrentDay);

        Debug.Log($"[Weather] Новый день! День {CurrentDay}, 8:00 утра.");
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
        if (isTimeFlow && CurrentTimeInMinutes >= 1439f)
            return "00:00 — Спать!"; // Подсказка игроку

        int h = Mathf.FloorToInt(CurrentTimeInMinutes / 60f);
        int m = Mathf.FloorToInt(CurrentTimeInMinutes % 60f);
        return $"{h:00}:{m:00}";
    }
}