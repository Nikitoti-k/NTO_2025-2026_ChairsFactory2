using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] public AudioClipDatabase audioDatabase;
    [SerializeField] private SFXPool sfxPool;

    [Header("Volume Multipliers")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float ambienceVolume = 1f;

    [Header("Music Sources")]
    [SerializeField] private AudioSource musicSourceA;
    [SerializeField] private AudioSource musicSourceB;
    private AudioSource _currentMusic;
    private AudioSource _nextMusic;

    [Header("Ambience")]
    [SerializeField] private AudioSource ambienceSource;

    [Header("Ambience Keys")]
    [SerializeField] private string defaultAmbienceKey = "ambience_wind"; // ключ ветра в базе

    [Header("Periodic Music")] // ← ИЗМЕНЕНИЕ: новая секция для периодической музыки
    [SerializeField] private AudioClip[] musicClips; // ← ИЗМЕНЕНИЕ: массив клипов для случайной музыки (заполни в инспекторе)
    [SerializeField] private float minMusicInterval = 60f; // ← ИЗМЕНЕНИЕ: мин. интервал между треками (сек)
    [SerializeField] private float maxMusicInterval = 120f; // ← ИЗМЕНЕНИЕ: макс. интервал между треками (сек)

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
        _currentMusic = musicSourceA;

        if (ambienceSource == null)
            ambienceSource = CreateMusicSource("Ambience");
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
        // ← ИЗМЕНЕНИЕ: убрал автозапуск ambience — теперь запускается из WeatherManager
        // PlayDefaultAmbience(); // ← ЗАКОММЕНТИРОВАНО

        StartCoroutine(PlayPeriodicMusic()); // ← ИЗМЕНЕНИЕ: запуск периодической музыки
    }

    public void PlaySFX(string key, float volumeMultiplier = 1f, float pitch = 1f, Vector3? position = null)
    {
        if (audioDatabase == null || sfxPool == null) return;
        if (!audioDatabase.TryGetSound(key, out var se)) return;

        AudioSource source = sfxPool.GetAvailableSource();
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

    public void PlayMusic(AudioClip clip, float fadeTime = 1f)
    {
        if (clip == null) return;

        // Первый запуск без кроссфейда
        if (_currentMusic.clip == null || !_currentMusic.isPlaying)
        {
            _currentMusic.clip = clip;
            _currentMusic.volume = musicVolume * masterVolume;
            _currentMusic.Play();
            return;
        }

        // Кроссфейд
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

            if (from != null)
                from.volume = Mathf.Lerp(startFromVolume, 0f, k);

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
    }

    public void PlayDefaultAmbience()
    {
        if (audioDatabase.TryGetSound(defaultAmbienceKey, out var ambienceEvent))
        {
            PlayAmbience(ambienceEvent.clip);
            ambienceSource.loop = true; // Включи loop для ветра!
        }
    }

    public void PlayAmbience(AudioClip clip)
    {
        if (clip == null) return;
        ambienceSource.clip = clip;
        ambienceSource.volume = ambienceVolume * masterVolume;
        ambienceSource.loop = true; // Авто-loop для ambience
        ambienceSource.Play();
    }

    // ← ИЗМЕНЕНИЕ: корутина для периодического включения музыки
    private IEnumerator PlayPeriodicMusic()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minMusicInterval, maxMusicInterval));

            if (musicClips.Length > 0 && !_currentMusic.isPlaying)
            {
                AudioClip randomClip = musicClips[Random.Range(0, musicClips.Length)];
                PlayMusic(randomClip);
                Debug.Log("[Audio] Периодическая музыка: играем " + randomClip.name);
            }
        }
    }
}