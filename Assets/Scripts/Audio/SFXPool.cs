using UnityEngine;

public class SFXPool : MonoBehaviour
{
    [SerializeField] private int poolSize = 10;
    private AudioSource[] _pool;
    private int _currentIndex = 0;

    private void Awake()
    {
        _pool = new AudioSource[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            GameObject go = new GameObject("SFX Source " + i);
            go.transform.SetParent(transform);
            _pool[i] = go.AddComponent<AudioSource>();
            _pool[i].playOnAwake = false;
            _pool[i].volume = 1f;
        }
    }

    public AudioSource GetAvailableSource()
    {
        AudioSource source = _pool[_currentIndex];
        _currentIndex = (_currentIndex + 1) % poolSize;
        return source;
    }
}