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

    // Новый метод вместо простого циклического выбора:
    public AudioSource GetAvailableSource()
    {
        // сначала ищем свободный источник
        for (int i = 0; i < poolSize; i++)
        {
            int index = (_currentIndex + i) % poolSize;
            if (!_pool[index].isPlaying)
            {
                _currentIndex = index;
                return _pool[index];
            }
        }

        // если нет свободных – берём самый старый
        AudioSource src = _pool[_currentIndex];
        _currentIndex = (_currentIndex + 1) % poolSize;
        return src;
    }
}
