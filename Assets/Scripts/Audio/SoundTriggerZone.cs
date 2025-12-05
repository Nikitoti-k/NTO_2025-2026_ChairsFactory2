using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class SoundTriggerZone : MonoBehaviour
{
    [Header("Звук в зоне")]
    [SerializeField] private AudioClip clip;                    // что играем
    [SerializeField] [Range(0f, 1f)] private float volume = 1f;
    [SerializeField] [Range(0.5f, 2f)] private float pitch = 1f;
    [SerializeField] [Range(0f, 0.5f)] private float randomPitch = 0.1f;

    [Header("Поведение")]
    [SerializeField] private bool playOnStart = false;          // играть сразу при входе (или только при движении внутри)
    [SerializeField] private bool loop = true;
    [SerializeField] private bool fadeInOut = true;             // плавное появление/исчезновение
    [SerializeField] private float fadeTime = 1.5f;

    [Header("Кто активирует")]
    [SerializeField] private LayerMask triggerMask = -1;        // по умолчанию всё

    private AudioSource source;
    private Coroutine fadeCoroutine;
    private int playersInside = 0;

    private void Awake()
    {
        // Автоматически делаем триггером
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        // Создаём AudioSource
        source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = loop;
        source.playOnAwake = false;
        source.spatialBlend = 1f;        // 3D звук
        source.volume = 0f;
        source.pitch = pitch;
        source.dopplerLevel = 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.maxDistance = 20f;
    }

    private void Start()
    {
        if (playOnStart && clip != null)
            TryPlay();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsInLayerMask(other.gameObject.layer, triggerMask)) return;

        playersInside++;
        if (playersInside == 1)
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
        if (clip == null || source.isPlaying && source.volume > 0.95f) return;

        StopAllCoroutines();

        source.pitch = pitch + Random.Range(-randomPitch, randomPitch);
        source.volume = 0f;

        if (!source.isPlaying)
            source.Play();

        if (fadeInOut)
            StartFade(0f, volume * AudioManager.Instance.masterVolume);
        else
            source.volume = volume * AudioManager.Instance.masterVolume;
    }

    private void TryStop()
    {
        if (clip == null || !source.isPlaying) return;

        StopAllCoroutines();

        if (fadeInOut)
            StartFade(source.volume, 0f, () => source.Stop());
        else
        {
            source.Stop();
            source.volume = 0f;
        }
    }

    private void StartFade(float from, float to, System.Action onComplete = null)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeVolume(from, to, onComplete));
    }

    private IEnumerator FadeVolume(float from, float to, System.Action onComplete)
    {
        source.volume = from;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(from, to, t / fadeTime) * AudioManager.Instance.masterVolume;
            yield return null;
        }
        source.volume = to * AudioManager.Instance.masterVolume;
        onComplete?.Invoke();
        fadeCoroutine = null;
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    // ?????????????????????????????????????
    // Удобные методы для вызова из анимаций/других скриптов
    // ?????????????????????????????????????
    public void ForcePlay() => TryPlay();
    public void ForceStop() => TryStop();
}