using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class WeatherManager : MonoBehaviour
{
    public enum TimeOfDay { Night, Morning, Day, Evening }

    private const float REAL_SECONDS_PER_GAME_DAY = 10f;
    public static WeatherManager Instance { get; private set; }

    [SerializeField] private Light mainDirectionalLight;
    [SerializeField] private float nightIntensity = 0.1f;
    [SerializeField] private float morningIntensity = 0.7f;
    [SerializeField] private float dayIntensity = 1.0f;
    [SerializeField] private float eveningIntensity = 0.5f;
    [SerializeField] private float transitionDuration = 3f;

    public UnityEvent<int> OnDayChanged = new();
    public UnityEvent<TimeOfDay> OnPeriodChanged = new();
    public UnityEvent<float, float> OnTimeChanged = new();

    public int CurrentDay { get; private set; } = 1;
    public float CurrentTimeInMinutes { get; private set; } = 480f;
    public TimeOfDay CurrentPeriod => GetCurrentPeriod();

    private bool morningStarted, dayStarted, eveningStarted, nightStarted;
    private Coroutine lightTransition;
    private TimeOfDay queuedPeriod = (TimeOfDay)(-1);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SetTimeDirectly(CurrentDay, CurrentTimeInMinutes);
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
            ResetPeriodFlags();
            OnDayChanged.Invoke(CurrentDay);
        }

        OnTimeChanged.Invoke(CurrentDay, CurrentTimeInMinutes);
        CheckPeriodTransition(previous);
    }

    public void SetTimeDirectly(int day, float minutes)
    {
        CurrentDay = day;
        CurrentTimeInMinutes = Mathf.Clamp(minutes, 0f, 1439.99f);
        ResetPeriodFlags();
        CheckPeriodTransition(-1f);
        OnDayChanged.Invoke(day);
        OnTimeChanged.Invoke(day, CurrentTimeInMinutes);
    }

    public void StartMorning() => QueueOrTriggerPeriod(TimeOfDay.Morning, 480f, morningIntensity);
    public void StartDay() => QueueOrTriggerPeriod(TimeOfDay.Day, 720f, dayIntensity);
    public void StartEvening() => QueueOrTriggerPeriod(TimeOfDay.Evening, 1080f, eveningIntensity);
    public void StartNight() => QueueOrTriggerPeriod(TimeOfDay.Night, 0f, nightIntensity);

    private void QueueOrTriggerPeriod(TimeOfDay period, float startMinute, float intensity)
    {
        if (CurrentTimeInMinutes >= startMinute && !PeriodStarted(period))
            TriggerPeriod(period, intensity);
        else
            queuedPeriod = period;
    }

    private void CheckPeriodTransition(float previousMinutes)
    {
        TimeOfDay current = CurrentPeriod;

        if (queuedPeriod != (TimeOfDay)(-1) && queuedPeriod == current && !PeriodStarted(current))
        {
            TriggerPeriod(queuedPeriod, GetIntensity(current));
            queuedPeriod = (TimeOfDay)(-1);
        }

        if (!PeriodStarted(current) && HasCrossedIntoPeriod(previousMinutes, current))
            TriggerPeriod(current, GetIntensity(current));
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

    private void TriggerPeriod(TimeOfDay period, float intensity)
    {
        ApplyLighting(intensity);
        OnPeriodChanged.Invoke(period);
        SetPeriodStarted(period, true);
    }

    private TimeOfDay GetCurrentPeriod()
    {
        float m = CurrentTimeInMinutes;
        return m < 480f ? TimeOfDay.Night :
               m < 720f ? TimeOfDay.Morning :
               m < 1080f ? TimeOfDay.Day : TimeOfDay.Evening;
    }

    private float GetIntensity(TimeOfDay p) => p switch
    {
        TimeOfDay.Night => nightIntensity,
        TimeOfDay.Morning => morningIntensity,
        TimeOfDay.Day => dayIntensity,
        TimeOfDay.Evening => eveningIntensity,
        _ => dayIntensity
    };

    private bool PeriodStarted(TimeOfDay p) => p switch
    {
        TimeOfDay.Morning => morningStarted,
        TimeOfDay.Day => dayStarted,
        TimeOfDay.Evening => eveningStarted,
        TimeOfDay.Night => nightStarted,
        _ => true
    };

    private void SetPeriodStarted(TimeOfDay p, bool value)
    {
        switch (p)
        {
            case TimeOfDay.Morning: morningStarted = value; break;
            case TimeOfDay.Day: dayStarted = value; break;
            case TimeOfDay.Evening: eveningStarted = value; break;
            case TimeOfDay.Night: nightStarted = value; break;
        }
    }

    private void ApplyLighting(float target)
    {
        if (!mainDirectionalLight) return;
        if (lightTransition != null) StopCoroutine(lightTransition);
        lightTransition = StartCoroutine(TransitionLight(target));
    }

    private IEnumerator TransitionLight(float target)
    {
        float start = mainDirectionalLight.intensity;
        float t = 0f;
        while (t < transitionDuration)
        {
            t += Time.deltaTime;
            mainDirectionalLight.intensity = Mathf.Lerp(start, target, t / transitionDuration);
            yield return null;
        }
        mainDirectionalLight.intensity = target;
    }

    private void ResetPeriodFlags()
    {
        morningStarted = dayStarted = eveningStarted = nightStarted = false;
        queuedPeriod = (TimeOfDay)(-1);
    }

    public string GetFormattedTime()
    {
        int h = Mathf.FloorToInt(CurrentTimeInMinutes / 60f);
        int m = Mathf.FloorToInt(CurrentTimeInMinutes % 60f);
        return $"{h:00}:{m:00}";
    }
}