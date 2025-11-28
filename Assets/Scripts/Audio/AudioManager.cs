using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioClipDatabase audioDatabase;
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

    public void PlaySFX(string key, float volumeMultiplier = 1f, float pitch = 1f, Vector3? position = null)
    {
        if (audioDatabase == null) return;
        if (!audioDatabase.TryGetSound(key, out var se)) return;

        AudioSource source = sfxPool.GetAvailableSource();

        source.clip = se.clip;
        source.pitch = pitch;

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

        _nextMusic = (_currentMusic == musicSourceA) ? musicSourceB : musicSourceA;
        _nextMusic.clip = clip;
        _nextMusic.volume = 0f;
        _nextMusic.Play();

        StartCoroutine(CrossfadeMusic(_currentMusic, _nextMusic, fadeTime));

        _currentMusic = _nextMusic;
    }

    private System.Collections.IEnumerator CrossfadeMusic(AudioSource from, AudioSource to, float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;

            if (from != null)
                from.volume = Mathf.Lerp(musicVolume * masterVolume, 0f, k);

            to.volume = Mathf.Lerp(0f, musicVolume * masterVolume, k);

            yield return null;
        }

        if (from != null)
            from.Stop();
    }


    public void PlayAmbience(AudioClip clip)
    {
        if (clip == null) return;

        ambienceSource.clip = clip;
        ambienceSource.volume = ambienceVolume * masterVolume;
        ambienceSource.Play();
    }
}
