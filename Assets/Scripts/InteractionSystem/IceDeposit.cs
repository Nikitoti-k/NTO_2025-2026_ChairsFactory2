using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SaveableObject))]
public class IceDeposit : SaveableObject, IHasDepositData
{
    [Header("Ice Deposit Settings")]
    [SerializeField] private int hitsRequired = 3;
    [SerializeField] private GameObject mineralPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Sounds")]
    [SerializeField] private string hitSoundKey = "ice_hit";
    [SerializeField] private string breakSoundKey = "ice_break";

    private int currentHits = 0;
    private bool _isBreaking = false;
    private bool _hasBeenLoaded = false;  // ← флаг, что загрузка прошла

    protected override void Awake()
    {
        base.Awake();

        Debug.Log($"[IceDeposit:{name}] Awake | currentHits = {currentHits} | active = {gameObject.activeSelf}");

        // Если уже сломано — отключаем сразу
        if (currentHits >= hitsRequired && _hasBeenLoaded == false)
        {
            Debug.Log($"[IceDeposit:{name}] ПРИ СТАРТЕ УЖЕ СЛОМАН — ОТКЛЮЧАЕМ!");
            gameObject.SetActive(false);
        }
    }
    private void LateUpdate()
    {
        Debug.Log($"[IceDeposit:{name}] Awake | currentHits = {currentHits} | active = {gameObject.activeSelf}");

        // Если уже сломано — отключаем сразу
        if (currentHits >= hitsRequired)
        {
            Debug.Log($"[IceDeposit:{name}] ПРИ СТАРТЕ УЖЕ СЛОМАН — ОТКЛЮЧАЕМ!");
            gameObject.SetActive(false);
        }
    }
    public void Hit()
    {
        if (_isBreaking || currentHits >= hitsRequired) return;

        currentHits++;
        Debug.Log($"[IceDeposit:{name}] Удар! currentHits = {currentHits}/{hitsRequired}");

        AudioManager.Instance?.PlaySFX(hitSoundKey, 1f, 1f, transform.position);

        if (currentHits >= hitsRequired)
            BreakDeposit();
    }

    private void BreakDeposit()
    {
        if (_isBreaking) return;
        _isBreaking = true;
        currentHits = hitsRequired;

        Debug.Log($"[IceDeposit:{name}] РАЗРУШЕН! Спавн минерала + отключение");

        if (mineralPrefab != null)
        {
            var pos = spawnPoint ? spawnPoint.position : transform.position + Vector3.up * 0.5f;
            var mineral = Instantiate(mineralPrefab, pos, Quaternion.identity);
            var saveable = mineral.GetComponent<SaveableObject>() ?? mineral.AddComponent<SaveableObject>();
            saveable.SetPrefabIdentifier(GetPrefabIdentifier(mineralPrefab));
        }

        GameDayManager.Instance?.RegisterDepositBroken();
        PlayBreakSoundAndDisable();
    }

    private void PlayBreakSoundAndDisable()
    {
        // звук...
        if (AudioManager.Instance?.audioDatabase != null &&
            AudioManager.Instance.audioDatabase.TryGetSound(breakSoundKey, out var soundEvent))
        {
            var source = AudioManager.Instance.sfxPool.GetAvailableSource();
            if (source != null)
            {
                source.transform.position = transform.position;
                source.clip = soundEvent.clip;
                source.volume = 1.2f * AudioManager.Instance.sfxVolume * AudioManager.Instance.masterVolume;
                source.pitch = Random.Range(0.95f, 1.05f);
                source.spatialBlend = 1f;
                source.Play();
                StartCoroutine(ReturnSource(source, soundEvent.clip.length + 0.1f));
            }
        }

        gameObject.SetActive(false);
        Debug.Log($"[IceDeposit:{name}] ОТКЛЮЧЁН — сломан навсегда");
    }

    private IEnumerator ReturnSource(AudioSource s, float d)
    {
        yield return new WaitForSeconds(d);
        if (s) { s.Stop(); s.clip = null; }
    }

    private string GetPrefabIdentifier(GameObject p)
    {
        if (SaveManager.Instance?.prefabRegistry != null)
            foreach (var e in SaveManager.Instance.prefabRegistry.prefabs)
                if (e.prefab == p) return e.identifier;
        return p.name.Replace("(Clone)", "").Trim();
    }

    // ─────────────────────────────────────────────────────────────
    // КЛЮЧЕВЫЕ МЕТОДЫ СОХРАНЕНИЯ
    // ─────────────────────────────────────────────────────────────

    public DepositSaveData GetDepositSaveData()
    {
        var data = new DepositSaveData { uniqueID = uniqueID, currentHits = currentHits };
        Debug.Log($"[IceDeposit:{name}] СОХРАНЯЕМ: currentHits = {currentHits} (сломано: {currentHits >= hitsRequired})");
        return data;
    }

    public void LoadDepositData(DepositSaveData data)
    {
        _hasBeenLoaded = true;
        currentHits = data.currentHits;

        Debug.Log($"[IceDeposit:{name}] ЗАГРУЗКА ДЕПОЗИТА! currentHits = {currentHits}/{hitsRequired}");

        if (currentHits >= hitsRequired)
        {
            Debug.Log($"[IceDeposit:{name}] БЫЛ СЛОМАН → ОТКЛЮЧАЕМ НАВСЕГДА!");
            gameObject.SetActive(false);
        }
        else
        {
            Debug.Log($"[IceDeposit:{name}] Целый → оставляем активным");
            gameObject.SetActive(true);
        }
    }

    // ЭТО САМОЕ ВАЖНОЕ — ПЕРЕОПРЕДЕЛЯЕМ, ЧТОБЫ НЕ ВКЛЮЧАЛ НАС ОБРАТНО!
    public override void LoadCommonData(ObjectSaveData data)
    {
        base.LoadCommonData(data);

        // Даже если SaveableObject включил нас — мы снова выключаем, если сломаны
        if (currentHits >= hitsRequired)
        {
            Debug.Log($"[IceDeposit:{name}] ПРИНУДИТЕЛЬНО ВЫКЛЮЧАЕМ после LoadCommonData (был сломан)");
            gameObject.SetActive(false);
        }
    }
}