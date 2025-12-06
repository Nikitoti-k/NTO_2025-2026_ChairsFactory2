using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] public AudioClipDatabase audioDatabase;
    [SerializeField] public SFXPool sfxPool;

    [Header("Volume Multipliers")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float ambienceVolume = 1f;

    [Header("Music Sources")]
    [SerializeField] public AudioSource musicSourceA;
    [SerializeField] public AudioSource musicSourceB;
    private AudioSource _currentMusic;
    private AudioSource _nextMusic;

    [Header("Ambience")]
    [SerializeField] public AudioSource ambienceSource;

    [Header("Ambience Keys")]
    [SerializeField] private string defaultAmbienceKey = "ambience_wind";

    [Header("Periodic Music")]
    [SerializeField] private AudioClip[] musicClips;
    [SerializeField] private float minMusicInterval = 60f;
    [SerializeField] private float maxMusicInterval = 120f;

    // ──────────────────────────────────────────────────────────────
    // ТЕСТОВЫЙ АВТОЗАПУСК — включай/выключай галочкой в инспекторе
    // ──────────────────────────────────────────────────────────────
    [Header("=== ТЕСТОВЫЙ АВТОЗАПУСК (для разработки) ===")]
    [SerializeField] private bool playTestAudioOnStart = true;
    [SerializeField] private AudioClip testMusicClip;      // перетащи сюда любой трек
    [SerializeField] private AudioClip testAmbienceClip;   // перетащи звук ветра/дождя и т.д.
    [SerializeField] private float testFadeInTime = 2f;
    // ──────────────────────────────────────────────────────────────
    // ДЛЯ ДОЛГОЖИВУЩИХ SFX (радио-шум, костёр, ветер и т.д.)
    // ──────────────────────────────────────────────────────────────
    private AudioSource persistentSfxSource; // один источник на всё "долгоиграющее"

    public void PlayPersistentSFX(string key, float volumeMultiplier = 1f, float pitch = 1f)
    {
        if (audioDatabase == null || sfxPool == null) return;
        if (!audioDatabase.TryGetSound(key, out var se)) return;

        // Если уже играет — просто обновляем
        if (persistentSfxSource == null)
        {
            persistentSfxSource = sfxPool.GetAvailableSource();
            if (persistentSfxSource == null) return;

            persistentSfxSource.transform.position = Vector3.zero;
            persistentSfxSource.spatialBlend = 0f; // 2D
            persistentSfxSource.loop = true;
            persistentSfxSource.playOnAwake = false;
        }

        persistentSfxSource.clip = se.clip;
        persistentSfxSource.pitch = pitch * Random.Range(0.95f, se.pitchVariation);
        persistentSfxSource.volume = se.volume * volumeMultiplier * sfxVolume * masterVolume;

        if (!persistentSfxSource.isPlaying)
            persistentSfxSource.Play();
    }

    public void StopPersistentSFX(float fadeTime = 0.5f)
    {
        if (persistentSfxSource != null && persistentSfxSource.isPlaying)
        {
            if (fadeTime > 0f)
                StartCoroutine(FadeOutAndRelease(persistentSfxSource, fadeTime));
            else
            {
                persistentSfxSource.Stop();
                persistentSfxSource = null;
            }
        }
    }
    private void Update()
    {
        // ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←
        // Это главное! Обновляем громкость долгоживущих SFX каждый кадр
        // ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←
        if (persistentSfxSource != null && persistentSfxSource.isPlaying)
        {
            // Пересчитываем громкость с учётом текущих настроек
            
                persistentSfxSource.volume =   sfxVolume * masterVolume;
            
        }
    }
    private IEnumerator FadeOutAndRelease(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, t / duration);
            yield return null;
        }
        source.Stop();
        source.volume = startVolume;
        persistentSfxSource = null;
    }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSourceA == null)
        {
            musicSourceA = CreateMusicSource("Music A");
            musicSourceB = CreateMusicSource("Music B");
        }

        if (ambienceSource == null)
            ambienceSource = CreateMusicSource("Ambience");

        _currentMusic = musicSourceA;
    }

    private AudioSource CreateMusicSource(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        AudioSource src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = true;
        return src;
    }

    private void Start()
    {
        // ТЕСТОВЫЙ ЗАПУСК — самое важное для тебя сейчас
        if (playTestAudioOnStart)
        {
            if (testMusicClip != null)
            {
                PlayMusic(testMusicClip, testFadeInTime);
                Debug.Log($"[AudioManager] ТЕСТ: запущена тестовая музыка → {testMusicClip.name}");
            }

            if (testAmbienceClip != null)
            {
                PlayAmbience(testAmbienceClip);
                Debug.Log($"[AudioManager] ТЕСТ: запущен тестовый ambience → {testAmbienceClip.name}");
            }
            else
            {
                // Если тестовый ambience не назначен — берём дефолтный из базы (ветер)
                PlayDefaultAmbience();
                Debug.Log("[AudioManager] ТЕСТ: запущен дефолтный ambience (по ключу)");
            }
        }

        // Периодическая музыка (оставляем, но она не будет мешать тесту)
        StartCoroutine(PlayPeriodicMusic());
    }

    // ====================== SFX ======================
    public void PlaySFX(string key, float volumeMultiplier = 1f, float pitch = 1f, Vector3? position = null)
    {
        if (audioDatabase == null || sfxPool == null) return;
        if (!audioDatabase.TryGetSound(key, out var se)) return;

        AudioSource source = sfxPool.GetAvailableSource();
        if (source == null) return;

        source.clip = se.clip;
        source.pitch = pitch * Random.Range(0.95f, se.pitchVariation);
        source.volume = se.volume * volumeMultiplier * sfxVolume * masterVolume;

        if (position.HasValue)
        {
            source.transform.position = position.Value;
            source.spatialBlend = 1f;
        }
        else
        {
            source.spatialBlend = 0f;
        }
        source.Play();
    }

    // Перегрузка: играть SFX напрямую по клипу (без базы)
    public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f, float pitch = 1f, Vector3? position = null)
    {
        if (clip == null || sfxPool == null) return;

        AudioSource source = sfxPool.GetAvailableSource();
        if (source == null) return;

        source.clip = clip;
        source.pitch = pitch;
        source.volume = volumeMultiplier * sfxVolume * masterVolume;

        if (position.HasValue)
        {
            source.transform.position = position.Value;
            source.spatialBlend = 1f;
            source.spread = 180f;
            source.dopplerLevel = 0f;
        }
        else
        {
            source.spatialBlend = 0f;
        }
        source.Play();
    }

    // ====================== MUSIC ======================
    public void PlayMusic(AudioClip clip, float fadeTime = 1f)
    {
        if (clip == null) return;

        if (_currentMusic.clip == null || !_currentMusic.isPlaying)
        {
            _currentMusic.clip = clip;
            _currentMusic.volume = musicVolume * masterVolume;
            _currentMusic.Play();
            return;
        }

        _nextMusic = (_currentMusic == musicSourceA) ? musicSourceB : musicSourceA;
        _nextMusic.clip = clip;
        _nextMusic.volume = 0f;
        _nextMusic.Play();

        StartCoroutine(CrossfadeMusic(_currentMusic, _nextMusic, fadeTime));
        _currentMusic = _nextMusic;
    }

    public void StopMusic(float fadeTime = 1f)
    {
        if (_currentMusic == null || !_currentMusic.isPlaying) return;
        StartCoroutine(FadeOutMusic(_currentMusic, fadeTime));
    }

    private IEnumerator CrossfadeMusic(AudioSource from, AudioSource to, float duration)
    {
        float startFromVolume = from?.volume ?? 0f;
        float targetToVolume = musicVolume * masterVolume;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;
            if (from != null) from.volume = Mathf.Lerp(startFromVolume, 0f, k);
            to.volume = Mathf.Lerp(0f, targetToVolume, k);
            yield return null;
        }

        if (from != null) from.Stop();
        to.volume = targetToVolume;
    }

    private IEnumerator FadeOutMusic(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, t / duration);
            yield return null;
        }
        source.Stop();
        source.volume = musicVolume * masterVolume;
    }

    // ====================== AMBIENCE ======================
    public void PlayDefaultAmbience()
    {
        if (audioDatabase != null && audioDatabase.TryGetSound(defaultAmbienceKey, out var ambienceEvent))
        {
            PlayAmbience(ambienceEvent.clip);
        }
    }

    public void PlayAmbience(AudioClip clip)
    {
        if (clip == null) return;

        ambienceSource.clip = clip;
        ambienceSource.volume = ambienceVolume * masterVolume;
        ambienceSource.loop = true;
        ambienceSource.Play();
    }

    // ====================== PERIODIC MUSIC ======================
    private IEnumerator PlayPeriodicMusic()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minMusicInterval, maxMusicInterval));

            if (musicClips.Length > 0 && (_currentMusic == null || !_currentMusic.isPlaying))
            {
                AudioClip randomClip = musicClips[Random.Range(0, musicClips.Length)];
                PlayMusic(randomClip, 2f);
                Debug.Log($"[AudioManager] Периодическая музыка: {randomClip.name}");
            }
        }
    }
}