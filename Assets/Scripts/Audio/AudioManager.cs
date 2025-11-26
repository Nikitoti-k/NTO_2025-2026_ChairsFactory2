using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioClipDatabase audioDatabase;
    [SerializeField] private SFXPool sfxPool;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

    }

    public void PlaySFX(string key)
    {
        if (audioDatabase == null)
        {
            return;
        }

        if (!audioDatabase.TryGetSound(key, out var soundEvent))
        {
            return;
        }

        AudioSource source = sfxPool.GetAvailableSource();
        source.clip = soundEvent.clip;
        source.volume = soundEvent.volume;
        source.Play();
    }
}
