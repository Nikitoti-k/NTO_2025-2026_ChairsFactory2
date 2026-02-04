using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(WeatherManager))]
public class SleepSystem : MonoBehaviour
{
    public static SleepSystem Instance { get; private set; }

    [Header("Fade & UI")]
    [SerializeField] Image sleepImage;
    [SerializeField] float fadeDuration = 1.5f;
    [SerializeField] float sleepScreenDuration = 2f;

    [Header("Spawn & Bed")]
    [SerializeField] Transform spawnPointAfterSleep;
    [SerializeField] Transform bedTransform;
    [SerializeField] float maxSleepDistance = 3f;

    [Header("Первый сон — звук вылупления яйца")]
    [SerializeField] private AudioClip eggHatchSound;           
    [SerializeField] private float eggHatchVolume = 1f;         
    [SerializeField] private float eggHatchPitch = 1f;          

    private static bool hasPlayedEggHatch = false;            

    PlayerMovement player;
    Rigidbody playerRb;
    Color fadeColor;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
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

        bool weatherAllows = true; 
        bool nearBed = bedTransform == null || Vector3.Distance(player.transform.position, bedTransform.position) <= maxSleepDistance;
        bool tasksDone = GameDayManager.Instance != null && GameDayManager.Instance.CanSleep;

        Debug.Log($"[SleepSystem.CanSleepNow] " +
                  $"WeatherAllows: {weatherAllows} | " +
                  $"NearBed: {nearBed} (dist: {Vector3.Distance(player.transform.position, bedTransform.position):F2}/{maxSleepDistance}) | " +
                  $"TasksDone: {tasksDone} (Reports: {GameDayManager.Instance?.MineralsResearchedToday}/{GameDayManager.Instance?.MineralsToResearch})");

        return   nearBed && tasksDone;
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
        if (!hasPlayedEggHatch &&
           GameDayManager.Instance != null &&
           GameDayManager.Instance.CurrentDay == 2)
        {
            hasPlayedEggHatch = true;

            if (eggHatchSound != null && AudioManager.Instance != null)
            {
               
                AudioManager.Instance.PlaySFX(eggHatchSound, eggHatchVolume, eggHatchPitch);

               

                Debug.Log("<color=cyan>【EGG HATCH】 Проиграно вылупление яйца! День 2</color>");
            }
            else
            {
                Debug.LogWarning("<color=red>【EGG HATCH】 Не удалось проиграть: клип или AudioManager отсутствует!</color>");
            }
            yield return new WaitForSeconds(sleepScreenDuration);

        
        if (spawnPointAfterSleep)
        {
            player.transform.position = spawnPointAfterSleep.position;
            player.transform.rotation = spawnPointAfterSleep.rotation;
            Debug.Log("[SleepSystem] Телепорт после сна: " + spawnPointAfterSleep.position);
        }

       
        }

        
        yield return FadeTo(0f);

        
        if (playerRb) playerRb.isKinematic = false;
        player.enabled = true;
        player.EndSleep();

        TutorialManager.Instance?.OnPlayerSlept();

       
        TriggerPostSleepMonologue();

        Debug.Log("[SleepSystem] Проснулись! Новый день!");
    }

  
    public static bool HasPlayedPostFirstSleepMonologue { get; private set; } = false;

    private void TriggerPostSleepMonologue()
    {
        if (HasPlayedPostFirstSleepMonologue) return;
        if (GameDayManager.Instance == null || GameDayManager.Instance.CurrentDay != 2) return;

        var radio = FindObjectOfType<RadioMonologue>();
        if (radio != null && radio.monologueSets.Length > 3)
        {
            radio.StartMonologue(3);
            HasPlayedPostFirstSleepMonologue = true;
            Debug.Log("<color=magenta>【РАДИО】 Первый монолог после сна — День 2</color>");
        }
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