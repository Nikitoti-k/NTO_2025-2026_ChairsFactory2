using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Collider))]
public class SnapZone : MonoBehaviour
{
    [SerializeField] private GrabbableType requiredType = GrabbableType.Mineral;
    [SerializeField] public bool isMultiSlot = false;
    [SerializeField] private Transform singleSnapPoint;
    [SerializeField] public List<Transform> multiSnapPoints = new List<Transform>();
    [SerializeField] private float snapDistance = 2f;
    [SerializeField] private float snapSpeed = 25f;
    [SerializeField, Min(0.1f)] private float minSnapDuration = 0.3f;
    [SerializeField] private AnimationCurve snapCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool prioritizeClosestPoint = true;
    [SerializeField] private bool makeKinematicInMultiSlot = true;
    [SerializeField] private bool makeTriggerInMultiSlot = true;
    [SerializeField] private float liftHeight = 0.6f;

    public bool IsOccupied => isMultiSlot ? attachedItems.Count > 0 : attachedItem != null;
    public GameObject CurrentSnappedObject => isMultiSlot
        ? (attachedItems.Count > 0 ? attachedItems[0].gameObject : null)
        : (attachedItem != null ? attachedItem.gameObject : null);

    private GrabbableItem attachedItem;
    private Coroutine snapRoutine;
    private bool wasKinematic, hadGravity, wasTrigger;
    private readonly List<GrabbableItem> attachedItems = new List<GrabbableItem>();
    private readonly Dictionary<GrabbableItem, Coroutine> snapRoutines = new Dictionary<GrabbableItem, Coroutine>();

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (!isMultiSlot && singleSnapPoint == null) singleSnapPoint = transform;
        if (isMultiSlot && multiSnapPoints.Count == 0) multiSnapPoints.Add(transform);
    }

    public bool CanSnap(GrabbableItem item)
    {
        if (item == null || item.ItemType != requiredType) return false;
        if (isMultiSlot ? attachedItems.Contains(item) : attachedItem == item) return false;
        if (isMultiSlot && attachedItems.Count >= multiSnapPoints.Count) return false;

        var target = isMultiSlot ? GetBestMultiPoint(item) : singleSnapPoint;
        return target != null && Vector3.Distance(item.transform.position, target.position) <= snapDistance;
    }

    public void Snap(GrabbableItem item)
    {
        if (item == null || (isMultiSlot ? snapRoutines.ContainsKey(item) : snapRoutine != null)) return;

        Transform target = isMultiSlot ? GetBestMultiPoint(item) : singleSnapPoint;
        if (target == null) return;

        if (!isMultiSlot)
        {
            attachedItem = item;
            snapRoutine = StartCoroutine(Snapping(item, target, false));
        }
        else
        {
            attachedItems.Add(item);
            snapRoutines[item] = StartCoroutine(Snapping(item, target, true));
        }
    }

    private Transform GetBestMultiPoint(GrabbableItem item)
    {
        var available = multiSnapPoints.Where(p => p != null &&
            !attachedItems.Any(i => i != null && i.transform.parent == p)).ToList();

        if (available.Count == 0) return null;

        return prioritizeClosestPoint
            ? available.OrderBy(p => Vector3.Distance(item.transform.position, p.position)).First()
            : available[0];
    }

    private IEnumerator Snapping(GrabbableItem item, Transform target, bool isMulti)
    {
        CanGrab grabber = FindFirstObjectByType<CanGrab>();
        grabber?.StartSnappingToZone();

        Rigidbody rb = item.GetComponent<Rigidbody>();
        Collider col = item.GetComponent<Collider>();

        if (!isMulti)
        {
            wasKinematic = rb ? rb.isKinematic : true;
            hadGravity = rb ? rb.useGravity : true;
            wasTrigger = col ? col.isTrigger : true;
        }

        if (rb)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        if (col && (isMulti ? makeTriggerInMultiSlot : true))
            col.isTrigger = true;

        grabber?.ForceRelease();

        Vector3 startPos = item.transform.position;
        Quaternion startRot = item.transform.rotation;
        Vector3 liftPos = startPos + Vector3.up * liftHeight;
        Vector3 finalPos = target.position;

        float liftDist = Vector3.Distance(startPos, liftPos);
        float dropDist = Vector3.Distance(liftPos, finalPos);
        float totalDist = liftDist + dropDist;
        float duration = Mathf.Max(minSnapDuration, totalDist / snapSpeed);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float curve = snapCurve.Evaluate(t);
            Vector3 currentPos = t < 0.5f
                ? Vector3.Lerp(startPos, liftPos, t * 2f)
                : Vector3.Lerp(liftPos, finalPos, (t - 0.5f) * 2f);

            item.transform.position = currentPos;
            item.transform.rotation = Quaternion.Slerp(startRot, target.rotation, curve);
            yield return null;
        }

        item.transform.SetParent(target);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        if (isMulti && rb)
        {
            rb.isKinematic = makeKinematicInMultiSlot;
            rb.useGravity = false;
        }
        else if (rb)
        {
            rb.isKinematic = wasKinematic;
            rb.useGravity = hadGravity;
        }

        if (!isMulti) snapRoutine = null;
        else snapRoutines.Remove(item);

        grabber?.EndSnappingToZoneComplete();
    }

    // ← ВАЖНЫЙ МЕТОД ДЛЯ ЗАГРУЗКИ СОХРАНЕНИЯ
    public void LoadSnappedItem(GrabbableItem item, int pointIndex = -1)
    {
        if (item == null) return;

        Transform target;
        if (!isMultiSlot)
        {
            target = singleSnapPoint ?? transform;
        }
        else
        {
            if (pointIndex >= 0 && pointIndex < multiSnapPoints.Count &&
                multiSnapPoints[pointIndex] != null &&
                !attachedItems.Any(i => i != null && i.transform.parent == multiSnapPoints[pointIndex]))
            {
                target = multiSnapPoints[pointIndex];
            }
            else
            {
                target = GetBestMultiPoint(item) ?? multiSnapPoints.FirstOrDefault(p => p != null);
            }
        }

        if (target == null) return;

        item.transform.SetParent(target);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        var rb = item.GetComponent<Rigidbody>();
        var col = item.GetComponent<Collider>();

        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            if (isMultiSlot)
            {
                rb.isKinematic = makeKinematicInMultiSlot;
                rb.useGravity = false;
            }
        }

        if (col && (isMultiSlot ? makeTriggerInMultiSlot : true))
            col.isTrigger = true;

        if (!isMultiSlot)
            attachedItem = item;
        else if (!attachedItems.Contains(item))
            attachedItems.Add(item);
    }

    public void OnItemGrabbedFromZone(GrabbableItem grabbedItem)
    {
        if (grabbedItem == null) return;

        if (!isMultiSlot)
        {
            if (attachedItem == grabbedItem)
            {
                RestorePhysicsAndRelease(attachedItem);
                attachedItem = null;
                if (snapRoutine != null) { StopCoroutine(snapRoutine); snapRoutine = null; }
            }
        }
        else
        {
            if (attachedItems.Contains(grabbedItem))
                ReleaseItem(grabbedItem);
        }
    }

    public void ReleaseItem(GrabbableItem item)
    {
        if (!isMultiSlot || !attachedItems.Remove(item)) return;
        RestorePhysicsAndRelease(item);
        if (snapRoutines.TryGetValue(item, out var cr) && cr != null) StopCoroutine(cr);
        snapRoutines.Remove(item);
    }

    private void RestorePhysicsAndRelease(GrabbableItem item)
    {
        item.transform.SetParent(null);
        var rb = item.GetComponent<Rigidbody>();
        var col = item.GetComponent<Collider>();
        if (rb) { rb.isKinematic = false; rb.useGravity = true; }
        if (col) col.isTrigger = false;
    }

    private void OnDestroy()
    {
        if (!isMultiSlot && attachedItem != null) attachedItem.transform.SetParent(null);
        foreach (var item in attachedItems) if (item != null) item.transform.SetParent(null);
    }

    private void Reset()
    {
        singleSnapPoint = transform;
        multiSnapPoints.Clear();
        multiSnapPoints.Add(transform);
    }
}