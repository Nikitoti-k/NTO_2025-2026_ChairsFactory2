using UnityEngine;

public class SFXPool : MonoBehaviour
{
    [SerializeField] private int poolSize = 20; // увеличил для шагов

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
        // Ищем свободный источник
        for (int i = 0; i < poolSize; i++)
        {
            int index = (_currentIndex + i) % poolSize;
            if (!_pool[index].isPlaying)
            {
                _currentIndex = (index + 1) % poolSize;
                return _pool[index];
            }
        }

        // Нет свободных — принудительно останавливаем самый старый
        AudioSource src = _pool[_currentIndex];
        src.Stop();
        src.clip = null;
        _currentIndex = (_currentIndex + 1) % poolSize;
        return src;
    }
}