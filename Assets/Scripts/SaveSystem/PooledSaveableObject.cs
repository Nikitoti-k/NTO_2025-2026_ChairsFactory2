// PooledSaveableObject.cs — ИДЕАЛЬНАЯ ВЕРСИЯ
using UnityEngine;

public class PooledSaveableObject : SaveableObject
{
    [Header("Pooled Object Settings")]
    [SerializeField] private bool generateIDOnlyIfEmpty = true;

    protected override void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponentInChildren<Collider>();

        // ГЕНЕРИРУЕМ ID ТОЛЬКО ЕСЛИ ЕГО ЕЩЁ НЕТ (т.е. новый объект из пула)
        if (generateIDOnlyIfEmpty && string.IsNullOrEmpty(uniqueID))
        {
            uniqueID = System.Guid.NewGuid().ToString();
            // Debug.Log($"[PooledSaveable] Новый объект — сгенерирован ID: {uniqueID}");
        }
    }

    private void OnEnable()
    {
        // Больше НЕ генерируем ID при активации!
        // Он либо уже есть (из сохранения), либо сгенерирован в Awake
    }

    private void OnDisable()
    {
        // Ничего не трогаем — ID должен жить вечно у этого объекта
    }

    // ВАЖНО: в базовом SaveableObject уже есть:
    // public virtual void LoadFromSaveData(SaveData data) => uniqueID = data.uniqueID;
    // → это перезапишет ID при загрузке, и Awake его НЕ сломает
}