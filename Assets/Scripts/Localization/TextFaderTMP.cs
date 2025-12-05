using System.Collections;
using UnityEngine;
using TMPro;

public class TextFaderTMP : MonoBehaviour
{
    [Header("Text Settings")]
    public TMP_Text targetText;           // Перетащи сюда свой TextMeshProUGUI или TextMeshPro
    public float fadeDuration = 1.5f;     // Время появления текста
    public bool playOnStart = false;      // Для теста: включить сразу при старте сцены

    

    [Header("One-shot")]
    public bool oneShot = true;           // Проявится только один раз

    private bool hasPlayed = false;

    private void Start()
    {
        // Изначально текст полностью прозрачный
        if (targetText != null)
        {
            Color c = targetText.color;
            targetText.color = new Color(c.r, c.g, c.b, 0f);
        }

        if (playOnStart) FadeInText();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && (!oneShot || !hasPlayed))
        {
            FadeInText();
            if (oneShot) hasPlayed = true;
        }
    }

    public void FadeInText() // Можно вызывать и из анимации / других скриптов
    {
        if (targetText != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeTextCoroutine(0f, 1f));
        }
    }

    public void FadeOutText()
    {
        if (targetText != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeTextCoroutine(1f, 0f));
        }
    }

    private IEnumerator FadeTextCoroutine(float startAlpha, float endAlpha)
    {
        Color startColor = targetText.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, endAlpha);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            targetText.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        targetText.color = endColor;
    }
}