using UnityEngine;
using System.Collections.Generic;

public class FlarePool : MonoBehaviour
{
    [SerializeField] private GameObject flarePrefab;
    [SerializeField] private int poolSize = 20;

    private Queue<FlareObject> pool = new Queue<FlareObject>();

    public static FlarePool Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
   
        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(flarePrefab, transform);
            obj.SetActive(false);

            var flare = obj.GetComponent<FlareObject>();
            if (flare == null)
            {
                Debug.LogError("FlarePrefab должен иметь компонент FlareObject!");
                continue;
            }


            flare.ReturnToPool();
            pool.Enqueue(flare);
        }
    }

    public FlareObject GetFlare(Vector3 position)
    {
        if (pool.Count == 0)
        {
            Debug.LogWarning("Flare pool пуст! Создаём новый на лету.");
            GameObject obj = Instantiate(flarePrefab, position, Quaternion.identity);
            var flare = obj.GetComponent<FlareObject>();
            var saveable = obj.GetComponent<PooledSaveableObject>() ?? obj.AddComponent<PooledSaveableObject>();
            saveable.SetPrefabIdentifier("Flare");
            obj.SetActive(true);
            return flare;
        }

        var flareFromPool = pool.Dequeue();
        flareFromPool.transform.position = position;
        flareFromPool.transform.rotation = Quaternion.identity;
        flareFromPool.gameObject.SetActive(true);

      
        var saveableComponent = flareFromPool.GetComponent<PooledSaveableObject>();
        if (saveableComponent == null)
        {
            saveableComponent = flareFromPool.gameObject.AddComponent<PooledSaveableObject>();
            saveableComponent.SetPrefabIdentifier("Flare");
        }

        return flareFromPool;
    }

    public void ReturnFlare(FlareObject flare)
    {
        if (flare == null) return;

        flare.ReturnToPool(); 
        flare.gameObject.SetActive(false);
        pool.Enqueue(flare);
    }
}