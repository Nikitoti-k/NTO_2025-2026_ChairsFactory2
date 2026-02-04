using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(Collider))]
public class SoundTriggerZone : MonoBehaviour
{
    [Header("«вук в зоне")]
    [SerializeField] private AudioClip clip;

    [SerializeField] [Range(0.5f, 2f)] private float basePitch = 1f;
    [SerializeField] [Range(0f, 0.5f)] private float randomPitch = 0.1f;

    [Header("ѕоведение")]
    [SerializeField] private bool playOnEnter = true;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool fadeInOut = true;
    [SerializeField] private float fadeTime = 1.5f;

    [Header(" то активирует")]
    [SerializeField] private LayerMask triggerMask = -1;

    private int playersInside = 0;
    private Coroutine fadeCoroutine;
    private AudioSource activeSource; // источник из пула (дл€ loop + fade)

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsInLayerMask(other.gameObject.layer, triggerMask)) return;

        playersInside++;
        if (playersInside == 1 && playOnEnter)
            TryPlay();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsInLayerMask(other.gameObject.layer, triggerMask)) return;

        playersInside = Mathf.Max(0, playersInside - 1);
        if (playersInside == 0)
            TryStop();
    }

    private void TryPlay()
    {
        if (clip == null || AudioManager.Instance == null) return;

        // ќстанавливаем предыдущий фейд, если был
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        float pitch = basePitch + UnityEngine.Random.Range(-randomPitch, randomPitch);

        // ≈сли нужен loop или fade Ч берЄм источник из пула и управл€ем им вручную
        if (loop || fadeInOut)
        {
            activeSource = AudioManager.Instance.sfxPool.GetAvailableSource();
            if (activeSource != null)
            {
                activeSource.transform.position = transform.position;
                activeSource.clip = clip;
                activeSource.loop = loop;
                activeSource.pitch = pitch;
                activeSource.spatialBlend = 1f;
                activeSource.volume = 0f; // стартуем с нул€, если будет fade
                activeSource.Play();

                if (fadeInOut)
                    fadeCoroutine = StartCoroutine(Fade(0f, 1f)); // от 0 до полной громкости
                else
                    UpdateSourceVolume(1f); // сразу полна€ громкость
            }
        }
        else
        {
            // ќдноразовый SFX без лупа и без фейда Ч просто через стандартный PlaySFX
            AudioManager.Instance.PlaySFX(clip, 1f, pitch, transform.position);
        }
    }

    private void TryStop()
    {
        if (activeSource == null) return;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (fadeInOut)
            fadeCoroutine = StartCoroutine(Fade(1f, 0f, ReleaseSource));
        else
            ReleaseSource();
    }

    // ‘ейд в нормализованном диапазоне 0Ц1
    private IEnumerator Fade(float fromNorm, float toNorm, Action onComplete = null)
    {
        float timer = 0f;
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / fadeTime);
            float normalizedVolume = Mathf.Lerp(fromNorm, toNorm, t);
            UpdateSourceVolume(normalizedVolume);
            yield return null;
        }

        UpdateSourceVolume(toNorm);
        onComplete?.Invoke();
        fadeCoroutine = null;
    }

    // ”мна€ установка громкости: только глобальные множители AudioManager
    private void UpdateSourceVolume(float normalized01)
    {
        if (activeSource != null && AudioManager.Instance != null)
        {
            activeSource.volume = normalized01 * AudioManager.Instance.sfxVolume * AudioManager.Instance.masterVolume;
        }
    }

    private void ReleaseSource()
    {
        if (activeSource != null)
        {
            activeSource.Stop();
            activeSource.clip = null;
            activeSource = null;
        }
    }

    private bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

    // ƒл€ вызова из анимаций / других скриптов
    [ContextMenu("Force Play")] public void ForcePlay() => TryPlay();
    [ContextMenu("Force Stop")] public void ForceStop() => TryStop();
}