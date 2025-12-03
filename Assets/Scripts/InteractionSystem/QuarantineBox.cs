using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class QuarantineBox : SnapZone
{
    [Header("Карантинный ящик")]
    [SerializeField] private float destroyDelay = 1.5f;


    private void OnEnable()
    {
        onItemSnapped.AddListener(OnMineralQuarantined);
    }

    private void OnDisable()
    {
        onItemSnapped.RemoveListener(OnMineralQuarantined);
    }

    // Теперь можно переопределять!
    public override bool CanSnap(GrabbableItem item)
    {
        if (!base.CanSnap(item)) return false;

        var mineralData = item.GetComponentInChildren<MineralData>();
        return mineralData != null && mineralData.isAnomaly;
    }

    public override void Snap(GrabbableItem item)
    {
        if (!CanSnap(item)) return; // Блокируем полностью
        base.Snap(item);
    }

    public override void OnItemGrabbedFromZone(GrabbableItem grabbedItem)
    {
        // Запрещаем вытаскивать из карантина
        FindObjectOfType<CanGrab>()?.ForceRelease();
        // НЕ вызываем base — иначе можно будет вытащить!
    }

    private void OnMineralQuarantined(GrabbableItem item)
    {
        
        StartCoroutine(DestroyAfterDelay(item.gameObject));
    }

    private IEnumerator DestroyAfterDelay(GameObject obj)
    {
        yield return new WaitForSeconds(destroyDelay);
        obj.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
        Gizmos.DrawCube(transform.position + Vector3.up * 0.5f, new Vector3(1.2f, 1f, 1.2f));
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, new Vector3(1.3f, 1.1f, 1.3f));
    }
}