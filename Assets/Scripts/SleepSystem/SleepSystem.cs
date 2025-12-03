using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(WeatherManager))]
public class SleepSystem : MonoBehaviour
{
    public static SleepSystem Instance { get; private set; }
    [SerializeField] Image sleepImage;
    [SerializeField] float fadeDuration = 1.5f;
    [SerializeField] float sleepScreenDuration = 2f;
    [SerializeField] Transform spawnPointAfterSleep;
    [SerializeField] Transform bedTransform;
    [SerializeField] float maxSleepDistance = 3f;

    PlayerMovement player;
    Rigidbody playerRb;
    Color fadeColor;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (sleepImage)
        {
            fadeColor = sleepImage.color;
            fadeColor.a = 0f;
            sleepImage.color = fadeColor;
        }

        player = FindFirstObjectByType<PlayerMovement>();
        if (player) playerRb = player.GetComponent<Rigidbody>();
        else Debug.LogError("[SleepSystem] PlayerMovement не найден!");
    }

    public bool CanSleepNow()
    {
        if (WeatherManager.Instance == null)
        {
            Debug.LogError("[SleepSystem] WeatherManager.Instance == null!");
            return false;
        }

        bool weatherAllows = true;//WeatherManager.Instance.CanSleepNow();
        bool nearBed = bedTransform == null || Vector3.Distance(player.transform.position, bedTransform.position) <= maxSleepDistance;
        bool tasksDone = GameDayManager.Instance != null && GameDayManager.Instance.CanSleep;

        Debug.Log($"[SleepSystem.CanSleepNow] " +
                  $"WeatherAllows: {weatherAllows} | " +
                  $"NearBed: {nearBed} (dist: {Vector3.Distance(player.transform.position, bedTransform.position):F2}/{maxSleepDistance}) | " +
                  $"TasksDone: {tasksDone} (Reports: {GameDayManager.Instance?.MineralsResearchedToday}/{GameDayManager.Instance?.MineralsToResearch})");

        return weatherAllows && nearBed && tasksDone;
    }

    public void StartSleep()
    {
        if (!CanSleepNow())
        {
            Debug.LogWarning("[SleepSystem] StartSleep() заблокирован — CanSleepNow() == false");
            return;
        }

        if (player == null || sleepImage == null)
        {
            Debug.LogError("[SleepSystem] Player или sleepImage == null!");
            return;
        }

        Debug.Log("[SleepSystem] Начинаем сон... Доброй ночи!");

        player.enabled = false;
        if (playerRb) playerRb.isKinematic = true;

        WeatherManager.Instance.SleepAndNextDay();
        StartCoroutine(SleepSequence());
    }

    IEnumerator SleepSequence()
    {
        yield return FadeTo(1f);
        yield return new WaitForSeconds(sleepScreenDuration);

        if (spawnPointAfterSleep)
        {
            player.transform.position = spawnPointAfterSleep.position;
            player.transform.rotation = spawnPointAfterSleep.rotation;
            Debug.Log("[SleepSystem] Телепорт после сна: " + spawnPointAfterSleep.position);
        }

        yield return FadeTo(0f);

        if (playerRb) playerRb.isKinematic = false;
        player.enabled = true;
        player.EndSleep();
        TutorialManager.Instance?.OnPlayerSlept();

        Debug.Log("[SleepSystem] Проснулись! Новый день!");
    }

    IEnumerator FadeTo(float a)
    {
        sleepImage.raycastTarget = a > 0f;
        Color start = sleepImage.color;
        fadeColor.a = a;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            sleepImage.color = Color.Lerp(start, fadeColor, t / fadeDuration);
            yield return null;
        }
        sleepImage.color = fadeColor;
    }
}