using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class WeatherManager : MonoBehaviour
{
    public enum TimeOfDay { Night, Morning, Day, Evening }

    private const float REAL_SECONDS_PER_GAME_DAY = 300f;
    public static WeatherManager Instance { get; private set; }

    [Header("Lights")]
    [SerializeField] private Light mainDirectionalLight;
    [SerializeField] private Light moonLight;

    [Header("Skybox Materials (URP)")]
    [SerializeField] private Material nightSkybox;
    [SerializeField] private Material morningSkybox;
    [SerializeField] private Material daySkybox;
    [SerializeField] private Material eveningSkybox;

    [Header("Lighting Settings")]
    [SerializeField] private Color nightAmbient = new Color(0.05f, 0.05f, 0.1f);
    [SerializeField] private Color morningAmbient = new Color(0.6f, 0.5f, 0.7f);
    [SerializeField] private Color dayAmbient = new Color(0.8f, 0.85f, 0.9f);
    [SerializeField] private Color eveningAmbient = new Color(0.7f, 0.5f, 0.4f);

    [SerializeField] private Color nightFog = new Color(0.01f, 0.01f, 0.03f);
    [SerializeField] private Color morningFog = new Color(0.8f, 0.7f, 0.9f);
    [SerializeField] private Color dayFog = new Color(0.7f, 0.8f, 0.9f);
    [SerializeField] private Color eveningFog = new Color(0.6f, 0.4f, 0.3f);

    [Header("Transition")]
    [SerializeField] private float transitionDuration = 3f;

    public UnityEvent<int> OnDayChanged = new UnityEvent<int>();
    public UnityEvent<TimeOfDay> OnPeriodChanged = new UnityEvent<TimeOfDay>();
    public UnityEvent<float, float> OnTimeChanged = new UnityEvent<float, float>();

    public int CurrentDay { get; private set; } = 1;
    public float CurrentTimeInMinutes { get; private set; } = 480f;
    public TimeOfDay CurrentPeriod => GetCurrentPeriod();

    private Coroutine skyboxCoroutine;
    private Material currentSkybox;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (mainDirectionalLight == null) mainDirectionalLight = FindObjectOfType<Light>();
        RenderSettings.skybox = daySkybox;
        currentSkybox = daySkybox;

        SetTimeDirectly(CurrentDay, CurrentTimeInMinutes);
        Debug.Log("[WeatherManager URP] Готов! Используется плавная смена материалов.");
    }

    public void AdvanceTime(float realSeconds)
    {
        float minutes = realSeconds * (1440f / REAL_SECONDS_PER_GAME_DAY);
        AdvanceTimeByMinutes(minutes);
    }

    public void AdvanceTimeByMinutes(float minutes)
    {
        float previous = CurrentTimeInMinutes;
        CurrentTimeInMinutes += minutes;

        while (CurrentTimeInMinutes >= 1440f)
        {
            CurrentTimeInMinutes -= 1440f;
            CurrentDay++;
            OnDayChanged.Invoke(CurrentDay);
        }

        OnTimeChanged.Invoke(CurrentDay, CurrentTimeInMinutes);
        CheckPeriodTransition(previous);
        UpdateSunAndMoon();
    }

    public void SetTimeDirectly(int day, float minutes)
    {
        CurrentDay = day;
        CurrentTimeInMinutes = Mathf.Clamp(minutes, 0f, 1439.99f);
        OnDayChanged.Invoke(day);
        OnTimeChanged.Invoke(day, CurrentTimeInMinutes);
        UpdateSunAndMoon();
        ApplyCurrentPeriodInstantly();
    }

    public void StartMorning() => TriggerPeriod(TimeOfDay.Morning);
    public void StartDay() => TriggerPeriod(TimeOfDay.Day);
    public void StartEvening() => TriggerPeriod(TimeOfDay.Evening);
    public void StartNight() => TriggerPeriod(TimeOfDay.Night);

    private void CheckPeriodTransition(float previousMinutes)
    {
        TimeOfDay current = CurrentPeriod;
        if (HasCrossedIntoPeriod(previousMinutes, current))
            TriggerPeriod(current);
    }

    private bool HasCrossedIntoPeriod(float previous, TimeOfDay period)
    {
        return period switch
        {
            TimeOfDay.Night => previous >= 1380f || (previous < 480f && CurrentTimeInMinutes >= 0f),
            TimeOfDay.Morning => previous < 480f && CurrentTimeInMinutes >= 480f,
            TimeOfDay.Day => previous < 720f && CurrentTimeInMinutes >= 720f,
            TimeOfDay.Evening => previous < 1080f && CurrentTimeInMinutes >= 1080f,
            _ => false
        };
    }

    private void TriggerPeriod(TimeOfDay period)
    {
        Debug.Log($"[WeatherManager URP] → Переход в {period}");
        OnPeriodChanged.Invoke(period);

        Material target = period switch
        {
            TimeOfDay.Night => nightSkybox,
            TimeOfDay.Morning => morningSkybox,
            TimeOfDay.Day => daySkybox,
            TimeOfDay.Evening => eveningSkybox,
            _ => daySkybox
        };

        if (skyboxCoroutine != null) StopCoroutine(skyboxCoroutine);
        skyboxCoroutine = StartCoroutine(SmoothSkyboxChange(target, GetAmbientColor(period), GetFogColor(period)));
    }

    private IEnumerator SmoothSkyboxChange(Material target, Color targetAmbient, Color targetFog)
    {
        Material startMat = RenderSettings.skybox;
        Color startAmbient = RenderSettings.ambientLight;
        Color startFog = RenderSettings.fogColor;

        float t = 0f;
        while (t < transitionDuration)
        {
            t += Time.deltaTime;
            float lerp = t / transitionDuration;

            // Плавная смена материала (мгновенно, но можно сделать fade через Volume)
            RenderSettings.skybox = target;
            RenderSettings.ambientLight = Color.Lerp(startAmbient, targetAmbient, lerp);
            RenderSettings.fogColor = Color.Lerp(startFog, targetFog, lerp);

            // Опционально: плавная смена Exposure через Volume (если есть Post-Processing)
            // UpdateExposure(lerp);

            yield return null;
        }

        RenderSettings.skybox = target;
        RenderSettings.ambientLight = targetAmbient;
        RenderSettings.fogColor = targetFog;
        currentSkybox = target;
        skyboxCoroutine = null;
    }

    private void ApplyCurrentPeriodInstantly()
    {
        TimeOfDay period = CurrentPeriod;
        Material mat = period switch
        {
            TimeOfDay.Night => nightSkybox,
            TimeOfDay.Morning => morningSkybox,
            TimeOfDay.Day => daySkybox,
            TimeOfDay.Evening => eveningSkybox,
            _ => daySkybox
        };

        RenderSettings.skybox = mat;
        RenderSettings.ambientLight = GetAmbientColor(period);
        RenderSettings.fogColor = GetFogColor(period);
        currentSkybox = mat;
    }

    private Color GetAmbientColor(TimeOfDay p) => p switch
    {
        TimeOfDay.Night => nightAmbient,
        TimeOfDay.Morning => morningAmbient,
        TimeOfDay.Day => dayAmbient,
        TimeOfDay.Evening => eveningAmbient,
        _ => dayAmbient
    };

    private Color GetFogColor(TimeOfDay p) => p switch
    {
        TimeOfDay.Night => nightFog,
        TimeOfDay.Morning => morningFog,
        TimeOfDay.Day => dayFog,
        TimeOfDay.Evening => eveningFog,
        _ => dayFog
    };

    private void UpdateSunAndMoon()
    {
        float progress = CurrentTimeInMinutes / 1440f;
        float angle = progress * 360f - 90f;

        if (mainDirectionalLight)
        {
            mainDirectionalLight.transform.rotation = Quaternion.Euler(angle, 30f, 0f);
            float intensity = Mathf.Clamp01(Mathf.Sin(progress * Mathf.PI));
            mainDirectionalLight.intensity = intensity * 2f;
            mainDirectionalLight.color = Color.Lerp(Color.cyan, Color.white, intensity);
        }

        if (moonLight)
        {
            moonLight.transform.rotation = Quaternion.Euler(angle + 180f, 30f, 0f);
            moonLight.intensity = (1f - Mathf.Abs(Mathf.Sin(progress * Mathf.PI))) * 0.8f;
        }
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