using UnityEngine;

[CreateAssetMenu(fileName = "AudioClipDatabase")]
public class AudioClipDatabase : ScriptableObject
{
    [System.Serializable]
    public struct SoundEvent
    {
        public string key;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
    }

    public SoundEvent[] soundEvents;

    private System.Collections.Generic.Dictionary<string, SoundEvent> _dict;

    public void Initialize()
    {
        if (_dict != null) return;
        _dict = new System.Collections.Generic.Dictionary<string, SoundEvent>();
        foreach (var se in soundEvents)
        {
            _dict[se.key] = se;
        }
    }

    public bool TryGetSound(string key, out SoundEvent soundEvent)
    {
        Initialize();
        return _dict.TryGetValue(key, out soundEvent);
    }
}
