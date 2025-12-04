using UnityEngine;

public class IceDeposit : SaveableObject, IHasDepositData
{
    [Header("Ice Deposit")]
    [SerializeField] private int hitsRequired = 3;
    [SerializeField] private GameObject mineralPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Звуки 🔊")]
    [SerializeField] private string hitSoundKey = "ice_hit";     // удар киркой
    [SerializeField] private string breakSoundKey = "ice_break"; // разрушение

    private int currentHits = 0;

  
    

    public void Hit()
    {
        currentHits++;

        // Звук удара каждый раз
        AudioManager.Instance?.PlaySFX(hitSoundKey, 1f, 1f, transform.position);

        // Лёгкий эффект разрушения на последнем ударе
        if (currentHits >= hitsRequired)
        {
            AudioManager.Instance?.PlaySFX(breakSoundKey, 1.2f, 1f, transform.position);
        }

        if (currentHits >= hitsRequired)
            BreakDeposit();
    }

    private void BreakDeposit()
    {
        // Звук разрушения уже сыгран в Hit()

        if (mineralPrefab != null)
        {
            var pos = spawnPoint ? spawnPoint.position : transform.position + Vector3.up * 0.5f;
            var mineral = Instantiate(mineralPrefab, pos, Quaternion.identity);
            var saveable = mineral.GetComponent<SaveableObject>() ?? mineral.AddComponent<SaveableObject>();
            saveable.SetPrefabIdentifier(GetPrefabIdentifier(mineralPrefab));
        }

        // ✅ ФИКС: вызов GameDayManager ДО deactivation!
        GameDayManager.Instance.RegisterDepositBroken();

        gameObject.SetActive(false);
    }

  

    private string GetPrefabIdentifier(GameObject prefab)
    {
        if (SaveManager.Instance?.prefabRegistry != null)
        {
            foreach (var e in SaveManager.Instance.prefabRegistry.prefabs)
                if (e.prefab == prefab) return e.identifier;
        }
        return prefab.name.Replace("(Clone)", "").Trim();
    }

    public DepositSaveData GetDepositSaveData()
        => new DepositSaveData { uniqueID = uniqueID, currentHits = currentHits };

    public void LoadDepositData(DepositSaveData data)
        => currentHits = data.currentHits;
}