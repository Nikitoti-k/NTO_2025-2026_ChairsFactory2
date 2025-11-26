using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(WeatherManager))]
public class SleepSystem : MonoBehaviour
{
    public static SleepSystem Instance { get; private set; }
    [SerializeField] private Image sleepImage;
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private float sleepScreenDuration = 2f;
    [SerializeField] private Transform spawnPointAfterSleep;
    [SerializeField] private Transform bedTransform;
    [SerializeField] private float maxSleepDistance = 3f;

    private PlayerMovement player;
    private Rigidbody playerRb;
    private Color fadeColor;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (sleepImage != null)
        {
            fadeColor = sleepImage.color;
            fadeColor.a = 0f;
            sleepImage.color = fadeColor;
            sleepImage.raycastTarget = false;
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
        if (playerRb) { playerRb.isKinematic = true; playerRb.linearVelocity = Vector3.zero; }

        int nextDay = WeatherManager.Instance.CurrentDay + 1;
        WeatherManager.Instance.SetTimeDirectly(nextDay, 480f);
        WeatherManager.Instance.StartMorning();
        GameDayManager.Instance?.StartNewDay(nextDay);

        StartCoroutine(SleepSequence());
    }

    private IEnumerator SleepSequence()
    {
        yield return FadeIn();
        yield return new WaitForSeconds(sleepScreenDuration);

        if (spawnPointAfterSleep != null)
        {
            player.transform.position = spawnPointAfterSleep.position;
            player.transform.rotation = spawnPointAfterSleep.rotation;
        }

        yield return FadeOut();

        if (playerRb) playerRb.isKinematic = false;
        player.enabled = true;
        player.EndSleep();
    }

    private IEnumerator FadeIn() => FadeTo(1f);
    private IEnumerator FadeOut() => FadeTo(0f);

    private IEnumerator FadeTo(float a)
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