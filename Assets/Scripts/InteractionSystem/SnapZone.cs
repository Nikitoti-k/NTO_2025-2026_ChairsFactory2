using UnityEngine;
using System.Collections;

public class SnapZone : MonoBehaviour
{
    [Header("Настройки зоны")]
    [SerializeField] private GrabbableType requiredType = GrabbableType.Mineral;
    [SerializeField] private Transform snapPoint;
    [SerializeField] private float snapDistance = 1.5f;
    [SerializeField] private float snapSpeed = 25f;
    [SerializeField, Min(0.1f)] private float minSnapDuration = 0.3f;
    [SerializeField] private AnimationCurve snapCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    
    private GrabbableItem attachedItem;
    private int originalLayer;
    private Coroutine snapRoutine;

    public bool IsOccupied => attachedItem != null;

    private void Awake()
    {
        if (TryGetComponent<Collider>(out var col))
            col.isTrigger = true;

        if (snapPoint == null) snapPoint = transform;
    }

    public bool CanSnap(GrabbableItem item)
    {
        return attachedItem == null &&
               item != null &&
               item.ItemType == requiredType &&
               Vector3.Distance(item.transform.position, snapPoint.position) <= snapDistance;
    }

    public void Snap(GrabbableItem item)
    {
        if (snapRoutine != null) return;

        attachedItem = item;
        originalLayer = item.gameObject.layer;               
        item.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast"); 
        snapRoutine = StartCoroutine(Snapping(item));
    }

    private IEnumerator Snapping(GrabbableItem item)
    {
        CanGrab grabber = FindFirstObjectByType<CanGrab>();
        grabber?.StartSnappingToZone();

        Rigidbody rb = item.GetComponent<Rigidbody>();
        Collider col = item.GetComponent<Collider>();

        bool wasKinematic = rb ? rb.isKinematic : true;
        bool hadGravity = rb ? rb.useGravity : true;

        if (col) col.isTrigger = true;
        if (rb)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Сразу отпускаем из руки
        grabber?.ForceRelease();

        Vector3 startPos = item.transform.position;
        Quaternion startRot = item.transform.rotation;

        float distance = Vector3.Distance(startPos, snapPoint.position);
        float duration = Mathf.Max(minSnapDuration, distance / snapSpeed);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float curve = snapCurve.Evaluate(t);
            item.transform.position = Vector3.Lerp(startPos, snapPoint.position, curve);
            item.transform.rotation = Quaternion.Slerp(startRot, snapPoint.rotation, curve);
            yield return null;
        }

        // Финальная привязка
        item.transform.SetParent(snapPoint);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        // Возвращаем коллайдер и физику
        if (col) col.isTrigger = false;
        if (rb)
        {
            rb.isKinematic = wasKinematic;
            rb.useGravity = hadGravity;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // ←←← ВОЗВРАЩАЕМ ОРИГИНАЛЬНЫЙ СЛОЙ (чтобы можно было снова взять!)
        item.gameObject.layer = originalLayer;

        snapRoutine = null;
        grabber?.EndSnappingToZoneComplete();
    }

    // ←←← ЭТО САМОЕ ГЛАВНОЕ! Вызывается при взятии объекта из зоны
    public void OnItemGrabbedFromZone()
    {
        if (attachedItem != null)
        {
            // Если объект ещё не успел улететь — отрываем от зоны
            attachedItem.transform.SetParent(null);
        }

        attachedItem = null;        // ← зона снова свободна!
        if (snapRoutine != null)
        {
            StopCoroutine(snapRoutine);
            snapRoutine = null;
        }
    }
}