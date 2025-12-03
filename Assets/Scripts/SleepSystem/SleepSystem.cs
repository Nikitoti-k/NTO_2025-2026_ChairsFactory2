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
    }

    public bool CanSleepNow()
    {
        if (GameDayManager.Instance == null) return false;
        bool nearBed = bedTransform == null || Vector3.Distance(player.transform.position, bedTransform.position) <= maxSleepDistance;
        return nearBed && GameDayManager.Instance.CanSleep;
    }

    public void StartSleep()
    {
        if (!CanSleepNow() || player == null || sleepImage == null) return;

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
        }

        yield return FadeTo(0f);

        if (playerRb) playerRb.isKinematic = false;
        player.enabled = true;
        player.EndSleep();
        TutorialManager.Instance?.OnPlayerSlept();
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