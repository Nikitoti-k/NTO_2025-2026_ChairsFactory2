using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class QuarantineBox : SnapZone
{
    [Header("Карантинный ящик")]
    [SerializeField] private float destroyDelay = 1.5f;
    [SerializeField] private ParticleSystem quarantineEffect;
    [SerializeField] private AudioSource quarantineSound;

    private void OnEnable()
    {
        // Подписываемся на событие из базового класса
        onItemSnapped.AddListener(OnMineralQuarantined);

    }

    private void OnDisable()
    {
        // Отписываемся — важно!
        onItemSnapped.RemoveListener(OnMineralQuarantined);
    }

    // ПЕРЕОПРЕДЕЛЯЕМ ПРОВЕРКУ: можно ли вообще сюда положить предмет
    public new bool CanSnap(GrabbableItem item)
    {
        // Сначала проверяем базовую логику SnapZone
        if (!base.CanSnap(item))
            return false;

        // Дополнительно: только аномальные минералы!
        var mineralData = item.GetComponentInChildren<MineralData>();
        if (mineralData == null)
            return false;

        return mineralData.isAnomaly; // Ключевое условие!
    }

    private void OnMineralQuarantined(GrabbableItem item)
    {
        // Дополнительная проверка на случай, если кто-то обошёл CanSnap
        var mineralData = item.GetComponentInChildren<MineralData>();
        if (mineralData == null || !mineralData.isAnomaly)
            return;

        if (quarantineEffect != null)
            quarantineEffect.Play();

        if (quarantineSound != null)
            quarantineSound.Play();

        StartCoroutine(DestroyAfterDelay(item.gameObject));
    }

    private IEnumerator DestroyAfterDelay(GameObject obj)
    {
        yield return new WaitForSeconds(destroyDelay);

        // Минерал исчезает (можно потом вернуть в пул)
        obj.SetActive(false);

        // Если хочешь полностью удалить — раскомментируй:
        // Destroy(obj);
    }

    // Запрещаем вытаскивать минерал обратно (по желанию)
   /* public  void OnItemGrabbedFromZone(GrabbableItem grabbedItem)
    {
        // Нельзя брать из карантина!
        FindObjectOfType<CanGrab>()?.ForceRelease();

        // Не вызываем base — иначе он удалит из attachedItems и разрешит взять
        // base.OnItemGrabbedFromZone(grabbedItem); // ← ЗАКОММЕНТИРОВАНО!
    }*/

    // Визуальная подсказка в редакторе
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
        Gizmos.DrawCube(transform.position + Vector3.up * 0.5f, new Vector3(1.2f, 1f, 1.2f));
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, new Vector3(1.3f, 1.1f, 1.3f));
    }
}