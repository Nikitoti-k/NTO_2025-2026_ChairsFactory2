using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SleepSystem : MonoBehaviour
{
    [SerializeField] private WeatherManager weatherManager;
    [SerializeField] private Image sleepImage;
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private float sleepScreenDuration = 2f;
    [SerializeField] private Transform spawnPointAfterSleep; 

   
    [Header("Sleep Distance Check")]
    [SerializeField] private Transform bedTransform;           
    [SerializeField] private float maxSleepDistance = 3f;      

   
    [Tooltip("Если bedTransform не назначен — проверка расстояния будет отключена")]

    private PlayerMovement player;
    private Rigidbody playerRb;
    private Color targetColor;

    private void Awake()
    {
        if (sleepImage != null)
        {
            targetColor = sleepImage.color;
            targetColor.a = 0f;
            sleepImage.color = targetColor;
            sleepImage.raycastTarget = false;
        }

        player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
            playerRb = player.GetComponent<Rigidbody>();
    }

  
    public bool CanSleepNow()
    {
        bool nearBed = bedTransform == null ||
                       Vector3.Distance(player.transform.position, bedTransform.position) <= maxSleepDistance;

        bool isEvening = weatherManager.CurrentPeriod == WeatherManager.TimeOfDay.Evening;
        bool reportsDone = GameDayManager.Instance != null && GameDayManager.Instance.CanSleep;

        if (!nearBed) DebugLog("Слишком далеко от кровати!");
        if (!isEvening) DebugLog("Ещё не вечер!");
        if (!reportsDone) DebugLog("Не все отчёты отправлены!");

        return nearBed && reportsDone;//isEvening && reportsDone;
    }

    private void DebugLog(string msg)
    {
        Debug.Log($"<color=red>[SleepSystem]</color> {msg}");
    }

    public void StartSleep()
    {
        if (weatherManager == null || sleepImage == null || player == null) return;

        if (!CanSleepNow())
            return;

        player.enabled = false;
        if (playerRb != null)
        {
            playerRb.isKinematic = true;
            playerRb.linearVelocity = Vector3.zero;
        }

        
        weatherManager.AdvanceTimeByMinutes(1440f - weatherManager.CurrentTimeInMinutes + 1f);
        weatherManager.StartNight();
        StartCoroutine(SleepSequence());
    }

    private IEnumerator SleepSequence()
    {
        yield return Fade(1f);
        yield return new WaitForSeconds(sleepScreenDuration);

       
        weatherManager.SetTimeDirectly(weatherManager.CurrentDay, 480f);
        weatherManager.StartMorning();

        if (spawnPointAfterSleep != null)
        {
            player.transform.position = spawnPointAfterSleep.position;
            player.transform.rotation = spawnPointAfterSleep.rotation;
        }

        yield return Fade(0f);

       
        if (playerRb != null) playerRb.isKinematic = false;
        player.enabled = true;
        player.EndSleep();
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (targetAlpha > 0f) sleepImage.raycastTarget = true;

        Color start = sleepImage.color;
        targetColor.a = targetAlpha;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            sleepImage.color = Color.Lerp(start, targetColor, t / fadeDuration);
            yield return null;
        }

        sleepImage.color = targetColor;

        if (targetAlpha <= 0f) sleepImage.raycastTarget = false;
    }

   
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (bedTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(bedTransform.position, maxSleepDistance);
        }
    }
#endif
}