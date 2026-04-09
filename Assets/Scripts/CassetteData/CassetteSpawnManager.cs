using System.Collections.Generic;
using UnityEngine;

public class CassetteSpawnManager : MonoBehaviour
{
    [Header("Кассеты в ящике (в порядке появления)")]
    [SerializeField] private List<GameObject> cassettesInScene;   

    private int nextIndex = 0;                                 

    public GameObject ActivateNextCassette()
    {
        if (cassettesInScene == null || nextIndex >= cassettesInScene.Count)
        {
            Debug.LogWarning("Нет доступных кассет для активации!");
            return null;
        }

        GameObject cass = cassettesInScene[nextIndex];
        if (cass == null)
        {
            Debug.LogError($"Объект кассеты с индексом {nextIndex} отсутствует в списке!");
            nextIndex++;
            return ActivateNextCassette();
        }

        cass.SetActive(true);
        nextIndex++;
        Debug.Log($"Активирована кассета: {cass.name} (индекс {nextIndex - 1})");
        return cass;
    }

    public bool HasMoreCassettes()
    {
        return nextIndex < cassettesInScene.Count;
    }

    public void ResetActivation()
    {
        nextIndex = 0;
        foreach (var cass in cassettesInScene)
        {
            if (cass != null)
                cass.SetActive(false);
        }
    }

    public void SetNextIndex(int index)
    {
        nextIndex = Mathf.Clamp(index, 0, cassettesInScene.Count);
    }

    public int GetNextIndex() => nextIndex;
    public int GetTotalCount() => cassettesInScene.Count;
}