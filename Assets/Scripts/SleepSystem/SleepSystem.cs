using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SleepSystem : MonoBehaviour
{
    [SerializeField] private WeatherManager weatherManager;
    [SerializeField] private Image sleepImage;
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private float sleepScreenDuration = 2f;
    [SerializeField] private Transform spawnPointAfterSleep;   // ����� ������ ����� ���

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
        return true;
        return weatherManager != null &&
               weatherManager.CurrentPeriod == WeatherManager.TimeOfDay.Evening;
    }

    public void StartSleep()
    {
        if (weatherManager == null || sleepImage == null || player == null) return;

        
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

        // ���� + ����� � ������ �����
        weatherManager.SetTimeDirectly(weatherManager.CurrentDay, 480f);
        weatherManager.StartMorning();

        if (spawnPointAfterSleep != null)
        {
            player.transform.position = spawnPointAfterSleep.position;
            player.transform.rotation = spawnPointAfterSleep.rotation;
        }

        yield return Fade(0f);

        // ������������ ������
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
}