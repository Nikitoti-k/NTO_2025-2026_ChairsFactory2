using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Teleporter : MonoBehaviour
{
    [Header("Teleport Settings")]
    public Transform targetPoint; 
    public float fadeDuration = 1f;  

    [Header("UI")]
    public Image fadeImage;  

    private bool canTeleport = true; 

    private void OnTriggerEnter(Collider other)
    {
        other.transform.position = targetPoint.position;
        Debug.Log($"Trigger entered by: {other.name}, tag: {other.tag}");
        if (other.CompareTag("Player") && canTeleport)
        {
            StartCoroutine(TeleportSequence(other.transform));
           
        }
    }

    private IEnumerator TeleportSequence(Transform player)
    {
        canTeleport = false;

        
        yield return StartCoroutine(FadeToBlack(true));

        // Телепорт
        if (targetPoint != null && player != null)
        {
            Debug.Log("телепорт!!!");
            Debug.Log(player);
            Debug.Log(targetPoint);
            player.position = targetPoint.position;
           
        }

     
        yield return StartCoroutine(FadeToBlack(false));

        canTeleport = true;  
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