using UnityEngine;

public class IceDeposit : SaveableObject, IHasDepositData
{
    [Header("Ice Deposit")]
    [SerializeField] private int hitsRequired = 3;
    [SerializeField] private GameObject mineralPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Звуки")]
    [SerializeField] private string hitSoundKey = "ice_hit";
    [SerializeField] private string breakSoundKey = "ice_break";

    private int currentHits = 0;
    private bool _isBreaking = false; // ← защита от двойного срабатывания

    public void Hit()
    {
        if (_isBreaking) return; // уже ломается — не трогаем

        currentHits++;

        // Звук удара — можно через пул, он короткий
        AudioManager.Instance?.PlaySFX(hitSoundKey, 1f, 1f, transform.position);

        if (currentHits >= hitsRequired)
        {
            BreakDeposit();
        }
    }

    private void BreakDeposit()
    {
        if (_isBreaking) return;
        _isBreaking = true;

        // 1. Сначала спавним минерал
        if (mineralPrefab != null)
        {
            var pos = spawnPoint ? spawnPoint.position : transform.position + Vector3.up * 0.5f;
            var mineral = Instantiate(mineralPrefab, pos, Quaternion.identity);
            var saveable = mineral.GetComponent<SaveableObject>() ?? mineral.AddComponent<SaveableObject>();
            saveable.SetPrefabIdentifier(GetPrefabIdentifier(mineralPrefab));
        }

        GameDayManager.Instance.RegisterDepositBroken();

        // 2. Проигрываем звук разрушения БЕЗОПАСНЫМ способом
        PlayBreakSoundAndDestroy();
    }

    // ← ГЛАВНОЕ ИСПРАВЛЕНИЕ: звук разрушения играет даже после отключения объекта
    private void PlayBreakSoundAndDestroy()
    {
        if (AudioManager.Instance?.audioDatabase != null &&
            AudioManager.Instance.audioDatabase.TryGetSound(breakSoundKey, out var soundEvent))
        {
            // Берём источник из пула
            var source = AudioManager.Instance.sfxPool.GetAvailableSource();
            if (source != null)
            {
                source.transform.position = transform.position;
                source.clip = soundEvent.clip;
                source.volume = 1.2f * AudioManager.Instance.sfxVolume * AudioManager.Instance.masterVolume;
                source.pitch = 1f;
                source.spatialBlend = 1f;
                source.Play();

                // Важно: запускаем корутину, которая вернёт источник в пул ПОСЛЕ окончания клипа
                StartCoroutine(ReturnToPoolWhenDone(source, soundEvent.clip.length));
            }
        }

        // 3. Только после этого — отключаем объект
        gameObject.SetActive(false);
    }

    private System.Collections.IEnumerator ReturnToPoolWhenDone(AudioSource source, float clipLength)
    {
        yield return new WaitForSeconds(clipLength + 0.1f); // + небольшой запас
        if (source != null)
        {
            source.Stop();
            source.clip = null;
            // источник автоматически вернётся в пул (если у тебя пул правильный)
        }
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

    public DepositSaveData GetDepositSaveData() => new DepositSaveData { uniqueID = uniqueID, currentHits = currentHits };
    public void LoadDepositData(DepositSaveData data) => currentHits = data.currentHits;
}