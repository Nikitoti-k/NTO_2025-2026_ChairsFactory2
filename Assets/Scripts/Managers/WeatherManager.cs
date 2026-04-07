using UnityEngine;
using UnityEngine.Events;


public interface IWeatherManager
{
    int CurrentDay { get; }
    float CurrentTimeInMinutes { get; }
    TimeOfDay CurrentPeriod { get; }
    void SetTimeDirectly(int day, float minutes);
    void SleepAndNextDay();
    bool CanSleepNow();
    string GetFormattedTime();
}
public enum TimeOfDay { Night, Morning, Day, Evening }

public class WeatherManager : MonoBehaviour, IWeatherManager
{
    public static WeatherManager Instance { get; private set; }

    [SerializeField] Light mainDirectionalLight;
    [SerializeField] Light moonLight;
    [SerializeField] Gradient directionalLightGradient;
    [SerializeField] Gradient ambientLightGradient;
    [SerializeField, Range(60f, 3600f)] float realSecondsPerGameDay = 300f;
    [SerializeField] bool forceTimeFlow = false;
    [SerializeField] bool isTimeFlow = false;

    [Header("Зоны для смены времени (без isTimeFlow)")]
    [SerializeField] private Transform baseCenterPoint;
    [SerializeField] private float distanceToStartDay = 150f;
    [SerializeField] private float distanceToTriggerEvening = 80f;

    public UnityEvent<int> OnDayChanged = new UnityEvent<int>();
    public UnityEvent<TimeOfDay> OnPeriodChanged = new UnityEvent<TimeOfDay>();
    public UnityEvent<float, float> OnTimeChanged = new UnityEvent<float, float>();

    public int CurrentDay { get; private set; } = 1;
    public float CurrentTimeInMinutes { get; private set; } = 480f;
    public TimeOfDay CurrentPeriod => GetCurrentPeriod();

    private float timeProgress;
    private Vector3 sunDefaultAngles;
    private Vector3 moonDefaultAngles;
    private TimeOfDay currentPeriodCache;

    private bool dayTriggered = false;
    private bool depositsBroken = false;
    private bool eveningTriggered = false;
    private bool ambienceTriggered = false;

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

            if (mainDirectionalLight == null) mainDirectionalLight = FindObjectOfType<Light>();
            if (mainDirectionalLight != null)
                sunDefaultAngles = mainDirectionalLight.transform.localEulerAngles;
            if (moonLight != null)
                moonDefaultAngles = moonLight.transform.localEulerAngles;

            JumpTo(480f);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[WeatherManager] Awake error: {e.Message}");
        }
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        try
        {
            bool shouldAdvanceTime = isTimeFlow
                ? CurrentTimeInMinutes < 1440f
                : forceTimeFlow || CanTimeProgress();

            if (shouldAdvanceTime)
            {
                timeProgress += Time.deltaTime / realSecondsPerGameDay;
                CurrentTimeInMinutes = timeProgress * 1440f;
            }

            if (CurrentTimeInMinutes >= 1440f)
            {
                if (isTimeFlow)
                {
                    CurrentTimeInMinutes = 1439.99f;
                    timeProgress = CurrentTimeInMinutes / 1440f;
                }
                else
                {
                    StartNewDay();
                }
            }

            if (!isTimeFlow)
                HandlePhaseTriggers();

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
        catch (System.Exception e)
        {
            Debug.LogError($"[WeatherManager] Update error: {e.Message}");
        }
    }

    private void UpdateLightingAndRotation()
    {
        try
        {
            if (mainDirectionalLight != null)
            {
                mainDirectionalLight.color = directionalLightGradient.Evaluate(timeProgress);
                mainDirectionalLight.transform.localEulerAngles = new Vector3(360f * timeProgress - 90f, sunDefaultAngles.y, sunDefaultAngles.z);
                float sunValue = Mathf.Clamp01(Mathf.Sin(timeProgress * Mathf.PI));
                sunValue = Mathf.Max(sunValue, 0.03f);
                mainDirectionalLight.intensity = sunValue * 2f;
            }

            if (moonLight != null)
            {
                moonLight.transform.localEulerAngles = new Vector3(360f * timeProgress + 90f, moonDefaultAngles.y, moonDefaultAngles.z);
                float moonValue = 1f - Mathf.Abs(Mathf.Sin(timeProgress * Mathf.PI));
                moonLight.intensity = moonValue * 0.8f;
            }

            if (!CaveDarkness.IsInsideAnyCave)
                RenderSettings.ambientLight = ambientLightGradient.Evaluate(timeProgress);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[WeatherManager] Lighting update error: {e.Message}");
        }
    }

    private void StartNewDay()
    {
        timeProgress = 0f;
        CurrentTimeInMinutes = 480f;
        CurrentDay++;
        OnDayChanged.Invoke(CurrentDay);
        ResetPhaseFlags();
        if (GameDayManager.Instance != null)
            GameDayManager.Instance.SetDay(CurrentDay);
    }

    private bool CanTimeProgress()
    {
        if (CurrentTimeInMinutes < 720f) return true;
        if (CurrentTimeInMinutes < 1080f) return depositsBroken;
        return eveningTriggered;
    }

    private void HandlePhaseTriggers()
    {
        var player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null || baseCenterPoint == null) return;

        float distFromBase = Vector3.Distance(player.position, baseCenterPoint.position);

        if (!ambienceTriggered && distFromBase >= distanceToStartDay)
        {
            ambienceTriggered = true;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayDefaultAmbience();
            Debug.Log("[Weather] Ambience started");
        }

        if (!dayTriggered && distFromBase >= distanceToStartDay && CurrentTimeInMinutes >= 720f)
        {
            dayTriggered = true;
            Debug.Log("[Weather] Day started");
        }

        if (dayTriggered && !depositsBroken && GameDayManager.Instance != null &&
            GameDayManager.Instance.DepositsBrokenToday >= GameDayManager.Instance.DepositsToBreak &&
            CurrentTimeInMinutes >= 1080f)
        {
            depositsBroken = true;
            Debug.Log("[Weather] Evening allowed");
        }

        if (depositsBroken && !eveningTriggered && distFromBase <= distanceToTriggerEvening && CurrentTimeInMinutes >= 1080f)
        {
            eveningTriggered = true;
            Debug.Log("[Weather] Night allowed");
        }
    }

    public bool CanSleepNow()
    {
        if (isTimeFlow)
            return CurrentTimeInMinutes >= 1439f;
        return depositsBroken && eveningTriggered && GameDayManager.Instance != null && GameDayManager.Instance.CanSleep;
    }

    public void SleepAndNextDay()
    {
        try
        {
            CurrentDay++;
            JumpTo(480f);
            ResetPhaseFlags();
            OnDayChanged.Invoke(CurrentDay);
            if (GameDayManager.Instance != null)
                GameDayManager.Instance.SetDay(CurrentDay);
            Debug.Log($"[Weather] New day {CurrentDay}, 8:00 AM");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[WeatherManager] SleepAndNextDay error: {e.Message}");
        }
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
        ambienceTriggered = false;
    }

    public string GetFormattedTime()
    {
        if (isTimeFlow && CurrentTimeInMinutes >= 1439f)
            return "00:00 — Sleep!";
        int h = Mathf.FloorToInt(CurrentTimeInMinutes / 60f);
        int m = Mathf.FloorToInt(CurrentTimeInMinutes % 60f);
        return $"{h:00}:{m:00}";
    }
}