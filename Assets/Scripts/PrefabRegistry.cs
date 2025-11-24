using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PrefabRegistry", menuName = "SaveSystem/PrefabRegistry")]
public class PrefabRegistry : ScriptableObject
{
    public List<PrefabEntry> prefabs = new List<PrefabEntry>();

    [System.Serializable]
    public class PrefabEntry
    {
        public string identifier;
        public GameObject prefab;
    }

    public GameObject GetPrefab(string identifier)
    {
        var entry = prefabs.Find(e => e.identifier == identifier);
        return entry != null ? entry.prefab : null;
    }
}