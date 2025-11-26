
using UnityEngine;
using System.Collections.Generic;

public class FlarePool : MonoBehaviour
{
    [SerializeField] private GameObject flarePrefab;
    [SerializeField] private int poolSize = 20;

    private Queue<FlareObject> pool = new Queue<FlareObject>();
    public static FlarePool Instance;

    void Awake()
    {
        Instance = this;
        InitializePool();
    }

    void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(flarePrefab, transform);
            obj.SetActive(false);

            var flare = obj.GetComponent<FlareObject>();
            var saveable = obj.GetComponent<SaveableObject>() ?? obj.AddComponent<SaveableObject>();
            saveable.SetPrefabIdentifier("Flare"); // ← ВАЖНО!

            flare.ReturnToPool();
            pool.Enqueue(flare);
        }
    }

    public FlareObject GetFlare(Vector3 position)
    {
        if (pool.Count == 0)
        {
            Debug.LogWarning("Flare pool empty!");
            return null;
        }

        var flare = pool.Dequeue();
        flare.transform.position = position;
        flare.gameObject.SetActive(true);
        return flare;
    }

    public void ReturnFlare(FlareObject flare)
    {
        flare.ReturnToPool();
        pool.Enqueue(flare);
    }
}