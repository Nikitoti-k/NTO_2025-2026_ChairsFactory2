using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class WeatherManager : MonoBehaviour
{
    public enum TimeOfDay
    {
        Night,
        Morning,
        Day,
        Evening
    }

    private const float REAL_SECONDS_PER_GAME_DAY = 10f;

    [SerializeField] private Light mainDirectionalLight;
    [SerializeField] private float nightIntensity = 0.1f;
    [SerializeField] private float morningIntensity = 0.7f;
    [SerializeField] private float dayIntensity = 1.0f;
    [SerializeField] private float eveningIntensity = 0.5f;
    [SerializeField] private float transitionDuration = 3f;

    public UnityEvent<int> OnDayChanged;
    public UnityEvent<TimeOfDay> OnPeriodChanged;
    public UnityEvent<float, float> OnTimeChanged;

    public int CurrentDay { get; private set; } = 1;
    public float CurrentTimeInMinutes { get; private set; } = 0f;
    public TimeOfDay CurrentPeriod => GetCurrentPeriodEnum();

    private bool morningStarted = false;
    private bool dayStarted = false;
    private bool eveningStarted = false;
    private bool nightStarted = false;

    private Coroutine lightTransition;
    private TimeOfDay queuedPeriod = (TimeOfDay)(-1);
 
    public void AdvanceTime(float realSecondsPassed)
    {
        float minutesPassed = realSecondsPassed * (1440f / REAL_SECONDS_PER_GAME_DAY);
        float previousMinutes = CurrentTimeInMinutes;
        CurrentTimeInMinutes += minutesPassed;

        while (CurrentTimeInMinutes >= 1440f)
        {
            CurrentTimeInMinutes -= 1440f;
            CurrentDay++;
            ResetPeriodFlags();
            OnDayChanged?.Invoke(CurrentDay);
        }

        OnTimeChanged?.Invoke(CurrentDay, CurrentTimeInMinutes);
        CheckAndTriggerPeriods(previousMinutes);
    }

    public void AdvanceTimeByMinutes(float gameMinutes)
    {
        float previousMinutes = CurrentTimeInMinutes;
        CurrentTimeInMinutes += gameMinutes;

        while (CurrentTimeInMinutes >= 1440f)
        {
            CurrentTimeInMinutes -= 1440f;
            CurrentDay++;
            ResetPeriodFlags();
            OnDayChanged?.Invoke(CurrentDay);
        }

        OnTimeChanged?.Invoke(CurrentDay, CurrentTimeInMinutes);
        CheckAndTriggerPeriods(previousMinutes);
    }

    public void SetTimeDirectly(int day, float minutesInDay)
    {
        CurrentDay = day;
        CurrentTimeInMinutes = Mathf.Clamp(minutesInDay, 0f, 1439.99f);
        ResetPeriodFlags();
        CheckAndTriggerPeriods(0f);
        OnDayChanged?.Invoke(CurrentDay);
        OnTimeChanged?.Invoke(CurrentDay, CurrentTimeInMinutes);
    }

    public void StartMorning() => TryTriggerPeriod(TimeOfDay.Morning, 480f, 720f, morningIntensity);
    public void StartDay() => TryTriggerPeriod(TimeOfDay.Day, 720f, 1080f, dayIntensity);
    public void StartEvening() => TryTriggerPeriod(TimeOfDay.Evening, 1080f, 1440f, eveningIntensity);
    public void StartNight() => TryTriggerPeriod(TimeOfDay.Night, 0f, 480f, nightIntensity);

    private void TryTriggerPeriod(TimeOfDay period, float startMin, float endMin, float intensity)
    {
        bool inRange = CurrentTimeInMinutes >= startMin && CurrentTimeInMinutes < endMin;
        if (inRange && !PeriodStarted(period))
        {
            TriggerPeriod(period, intensity);
        }
        else if (CurrentTimeInMinutes >= startMin)
        {
            queuedPeriod = period;
        }
    }

    private void CheckAndTriggerPeriods(float previousMinutes)
    {
        TimeOfDay current = CurrentPeriod;

        if (queuedPeriod != (TimeOfDay)(-1) && queuedPeriod == current && !PeriodStarted(current))
        {
            TriggerPeriod(queuedPeriod, GetIntensityForPeriod(queuedPeriod));
            queuedPeriod = (TimeOfDay)(-1);
        }

        if (!PeriodStarted(current) && CrossedIntoPeriod(previousMinutes, current))
        {
            TriggerPeriod(current, GetIntensityForPeriod(current));
        }
    }

    private bool CrossedIntoPeriod(float previous, TimeOfDay period)
    {
        return period switch
        {
            TimeOfDay.Night => previous >= 1380f || previous < 480f,
            TimeOfDay.Morning => previous < 480f && CurrentTimeInMinutes >= 480f,
            TimeOfDay.Day => previous < 720f && CurrentTimeInMinutes >= 720f,
            TimeOfDay.Evening => previous < 1080f && CurrentTimeInMinutes >= 1080f,
            _ => false
        };
    }

    private void TriggerPeriod(TimeOfDay period, float intensity)
    {
        ApplyLighting(intensity);
        OnPeriodChanged?.Invoke(period);

        switch (period)
        {
            case TimeOfDay.Morning: morningStarted = true; break;
            case TimeOfDay.Day: dayStarted = true; break;
            case TimeOfDay.Evening: eveningStarted = true; break;
            case TimeOfDay.Night: nightStarted = true; break;
        }
    }

    private bool PeriodStarted(TimeOfDay p) => p switch
    {
        TimeOfDay.Morning => morningStarted,
        TimeOfDay.Day => dayStarted,
        TimeOfDay.Evening => eveningStarted,
        TimeOfDay.Night => nightStarted,
        _ => true
    };

    private float GetIntensityForPeriod(TimeOfDay p) => p switch
    {
        TimeOfDay.Night => nightIntensity,
        TimeOfDay.Morning => morningIntensity,
        TimeOfDay.Day => dayIntensity,
        TimeOfDay.Evening => eveningIntensity,
        _ => dayIntensity
    };

    public TimeOfDay GetCurrentPeriodEnum()
    {
        float m = CurrentTimeInMinutes;
        if (m < 480f) return TimeOfDay.Night;
        if (m < 720f) return TimeOfDay.Morning;
        if (m < 1080f) return TimeOfDay.Day;
        return TimeOfDay.Evening;
    }

    private void ApplyLighting(float target)
    {
        if (!mainDirectionalLight) return;
        if (lightTransition != null) StopCoroutine(lightTransition);
        lightTransition = StartCoroutine(TransitionIntensity(target));
    }

    private IEnumerator TransitionIntensity(float target)
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