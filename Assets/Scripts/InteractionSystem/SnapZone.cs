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
    private Coroutine snapRoutine;

    public bool IsOccupied => attachedItem != null;

    private void Awake()
    {
        // Установка триггера
        if (TryGetComponent<Collider>(out var col))
            col.isTrigger = true;
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
        if (snapRoutine != null) StopCoroutine(snapRoutine);

        attachedItem = item;
        snapRoutine = StartCoroutine(Snapping(item));
    }

    private IEnumerator Snapping(GrabbableItem item)
    {
        // Анимация snap
        CanGrab grabber = FindFirstObjectByType<CanGrab>();
        grabber?.StartSnappingToZone();

        Rigidbody rb = item.GetComponent<Rigidbody>();
        Collider col = item.GetComponent<Collider>();

        bool wasKinematic = rb ? rb.isKinematic : true;

        if (col) col.isTrigger = true;
        if (rb) rb.isKinematic = true;

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

        item.transform.SetParent(snapPoint);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        if (col) col.isTrigger = false;
        if (rb)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        item.gameObject.layer = LayerMask.NameToLayer("Default");

        snapRoutine = null;
        grabber?.ForceRelease();
    }

    public void OnItemGrabbedFromZone()
    {
        attachedItem = null;
    }
}