using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AudioClipDatabase", menuName = "Audio/AudioClipDatabase")]
public class AudioClipDatabase : ScriptableObject
{
    [System.Serializable]
    public struct SoundEvent
    {
        public string key;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
        [Range(0.5f, 2f)] public float pitchVariation; // для рандома
    }

    public SoundEvent[] soundEvents;
    private Dictionary<string, SoundEvent> _dict;

    private void OnEnable()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (_dict != null) return;
        _dict = new Dictionary<string, SoundEvent>();
        foreach (var se in soundEvents)
        {
            if (!string.IsNullOrEmpty(se.key))
                _dict[se.key] = se;
        }
    }

    public bool TryGetSound(string key, out SoundEvent soundEvent)
    {
        Initialize();
        return _dict.TryGetValue(key, out soundEvent);
    }
}