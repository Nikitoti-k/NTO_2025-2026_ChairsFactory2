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

    private int nextAllowedPhase = 0; // 0=Morning, 1=Day, 2=Evening

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (mainDirectionalLight == null) mainDirectionalLight = FindObjectOfType<Light>();
        sunDefaultAngles = mainDirectionalLight.transform.localEulerAngles;
        if (moonLight) moonDefaultAngles = moonLight.transform.localEulerAngles;

        JumpTo(480f);
        nextAllowedPhase = 1;
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        timeProgress += Time.deltaTime / realSecondsPerGameDay;
        CurrentTimeInMinutes = timeProgress * 1440f;

        if (CurrentTimeInMinutes >= 1440f)
        {
            timeProgress = 0f;
            CurrentTimeInMinutes = 0f;
            CurrentDay++;
            OnDayChanged.Invoke(CurrentDay);
            nextAllowedPhase = 1;
        }

        if (!forceTimeFlow)
            ClampToCurrentPhase();

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

    private void ClampToCurrentPhase()
    {
        if (nextAllowedPhase == 1 && CurrentTimeInMinutes >= 720f) CurrentTimeInMinutes = 719.99f;
        if (nextAllowedPhase == 2 && CurrentTimeInMinutes >= 1080f) CurrentTimeInMinutes = 1079.99f;
    }

    public void AllowNextPhase()
    {
        if (nextAllowedPhase < 3) nextAllowedPhase++;
    }

    public void SleepAndNextDay()
    {
        CurrentDay++;
        JumpTo(480f);
        nextAllowedPhase = 1;
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
        if (!forceTimeFlow) ClampToCurrentPhase();
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
        if (m < 1080f) return TimeOfDay.Evening;
        return TimeOfDay.Day;
    }

    public string GetFormattedTime()
    {
        int h = Mathf.FloorToInt(CurrentTimeInMinutes / 60f);
        int m = Mathf.FloorToInt(CurrentTimeInMinutes % 60f);
        return $"{h:00}:{m:00}";
    }
}