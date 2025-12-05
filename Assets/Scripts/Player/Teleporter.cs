using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Teleporter : MonoBehaviour
{
    [Header("Teleport Settings")]
    public Transform targetPoint;  // Точка телепорта
    public float fadeDuration = 1f;  // Время fade (сек)

    [Header("UI")]
    public Image fadeImage;  // Чёрный Image для затемнения

    private bool canTeleport = true;  // Блокировка

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && canTeleport)
        {
            StartCoroutine(TeleportSequence(other.transform));
        }
    }

    private IEnumerator TeleportSequence(Transform player)
    {
        canTeleport = false;

        // Fade to black
        yield return StartCoroutine(FadeToBlack(true));

        // Телепорт
        if (targetPoint != null && player != null)
        {
            player.position = targetPoint.position;
            // player.rotation = targetPoint.rotation;  // Раскомментируй для поворота
        }

        // Fade back
        yield return StartCoroutine(FadeToBlack(false));

        canTeleport = true;  // Разблок (или false для one-shot)
    }

    private IEnumerator FadeToBlack(bool fadeIn)
    {
        if (fadeImage == null) yield break;

        float targetAlpha = fadeIn ? 1f : 0f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(fadeImage.color.a, targetAlpha, elapsed / fadeDuration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadeImage.color = new Color(0, 0, 0, targetAlpha);
    }
}