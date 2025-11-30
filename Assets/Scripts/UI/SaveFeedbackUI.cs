using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SaveFeedbackUI : MonoBehaviour
{
    public static SaveFeedbackUI Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private float displayDuration = 2f;

   

   

    private AudioSource audioSource;
    private Coroutine currentRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Скрываем сразу
        if (icon) icon.enabled = false;
        if (messageText) messageText.enabled = false;

        audioSource = gameObject.AddComponent<AudioSource>();
    }

    public static void ShowSave(bool isAuto = false)
    {
        Instance?._Show("Сохранено");
    }

    public static void ShowLoad()
    {
        Instance?._Show("Загружено");
    }

    private void _Show(string text)
    {
        // Прерываем старую анимацию
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(ShowRoutine(text));
    }

    private IEnumerator ShowRoutine(string text)
    {
        // Включаем
        
        if (messageText)
        {
            messageText.text = text;
            messageText.enabled = true;
        }

      

        // Ждём
        float timer = 0f;
        while (timer < displayDuration)
        {
            timer += Time.unscaledDeltaTime; // работает даже при Time.timeScale = 0
            yield return null;
        }

        // Выключаем
        if (icon) icon.enabled = false;
        if (messageText) messageText.enabled = false;

        currentRoutine = null;
    }
}