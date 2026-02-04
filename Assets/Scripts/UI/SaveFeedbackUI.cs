using UnityEngine;
using TMPro;
using System.Collections;

public class SaveFeedbackUI : MonoBehaviour, ILocalizable
{
    public static SaveFeedbackUI Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private float duration = 2f;

    private Coroutine coroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Скрываем сразу
        if (text != null)
        {
            text.color = new Color(text.color.r, text.color.g, text.color.b, 0f);
            text.gameObject.SetActive(false);
        }

        LocalizationManager.Register(this);
        Localize(); // на всякий случай
    }

    private void OnDestroy()
    {
        LocalizationManager.Unregister(this);
        if (Instance == this) Instance = null;
    }

    public static void Show()
    {
        Instance?._Show();
    }

    private void _Show()
    {
        if (coroutine != null) StopCoroutine(coroutine);
        coroutine = StartCoroutine(ShowCoroutine());
    }

    private IEnumerator ShowCoroutine()
    {
        text.gameObject.SetActive(true);
        text.text = LocalizationManager.Loc("UI_PauseMenu_SaveFeedBack");

        // Появление
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.unscaledDeltaTime;
            text.color = new Color(text.color.r, text.color.g, text.color.b, Mathf.Lerp(0f, 1f, t / 0.2f));
            yield return null;
        }

        // Держим 2 секунды
        yield return new WaitForSecondsRealtime(duration);

        // Исчезновение
        t = 0f;
        while (t < 0.3f)
        {
            t += Time.unscaledDeltaTime;
            text.color = new Color(text.color.r, text.color.g, text.color.b, Mathf.Lerp(1f, 0f, t / 0.3f));
            yield return null;
        }

        text.color = new Color(text.color.r, text.color.g, text.color.b, 0f);
        text.gameObject.SetActive(false);

        coroutine = null;
    }

    public void Localize()
    {
        if (text != null && text.gameObject.activeSelf)
            text.text = LocalizationManager.Loc("UI_PauseMenu_SaveFeedBack");
    }
}