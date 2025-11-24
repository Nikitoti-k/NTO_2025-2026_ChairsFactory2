using UnityEngine;
using System.Collections.Generic;

public class FlarePool : MonoBehaviour
{
    [Header("Пул")]
    [SerializeField] private GameObject flarePrefab;
    [SerializeField] private int poolSize = 20;
    [SerializeField] private bool autoExpand = true;

    private Queue<FlareObject> pool = new Queue<FlareObject>();
    private static FlarePool instance;

    public static FlarePool Instance => instance;

    void Awake()
    {
        instance = this;
        InitializePool();
    }

    void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject flareObj = Instantiate(flarePrefab, transform);
            flareObj.SetActive(false);
            FlareObject flare = flareObj.GetComponent<FlareObject>();

            SaveableObject saveable = flareObj.GetComponent<SaveableObject>();
            if (saveable == null)
            {
                saveable = flareObj.AddComponent<SaveableObject>();
            }
            saveable.SetPrefabIdentifier("Flare");

            // Явно задаём свойства (если триггер не нужен)
            Collider col = flareObj.GetComponent<Collider>();
            if (col) col.isTrigger = false;  // Или true, в зависимости от логики

            pool.Enqueue(flare);
        }
    }

    public FlareObject GetFlare(Vector3 position)
    {
        if (pool.Count == 0)
        {
            if (autoExpand)
            {
                return CreateFlare(position);
            }
            return null;
        }

        FlareObject flare = pool.Dequeue();
        flare.transform.position = position;
        flare.gameObject.SetActive(true);
        return flare;
    }

    private FlareObject CreateFlare(Vector3 position)
    {
        GameObject flareObj = Instantiate(flarePrefab, position, Quaternion.identity, transform);
        FlareObject flare = flareObj.GetComponent<FlareObject>();

        SaveableObject saveable = flareObj.GetComponent<SaveableObject>();
        if (saveable == null)
        {
            saveable = flareObj.AddComponent<SaveableObject>();
        }
        saveable.SetPrefabIdentifier("Flare");

        Collider col = flareObj.GetComponent<Collider>();
        if (col) col.isTrigger = false;  // Исправление по умолчанию

        return flare;
    }

    public void ReturnFlare(FlareObject flare)
    {
        flare.SetHeld(false);  // Важно!
        flare.gameObject.SetActive(false);
        pool.Enqueue(flare);
    }
}