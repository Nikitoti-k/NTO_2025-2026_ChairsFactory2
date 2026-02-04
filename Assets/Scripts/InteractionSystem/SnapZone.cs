using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class SnapZone : MonoBehaviour
{
    [SerializeField] private GrabbableType requiredType = GrabbableType.Mineral;
    [SerializeField] public bool isMultiSlot = false;
    [SerializeField] private bool singleSlotOnlyOne = true;

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

    [System.Serializable]
    public class GrabbableItemEvent : UnityEvent<GrabbableItem> { }

    [Header("События")]
    public GrabbableItemEvent onItemSnapped = new GrabbableItemEvent();
    public GrabbableItemEvent onItemRemoved = new GrabbableItemEvent();

    public int AttachedItemsCount => isMultiSlot ? attachedItems.Count : (attachedItem != null ? 1 : 0);
    public bool IsOccupied => isMultiSlot ? attachedItems.Count > 0 : attachedItem != null;

    public GameObject CurrentSnappedObject =>
        isMultiSlot
            ? (attachedItems.Count > 0 ? attachedItems[0].gameObject : null)
            : (attachedItem != null ? attachedItem.gameObject : null);

    private GrabbableItem attachedItem;
    private Coroutine snapRoutine;

    public readonly List<GrabbableItem> attachedItems = new List<GrabbableItem>();
    private readonly Dictionary<GrabbableItem, Coroutine> snapRoutines = new Dictionary<GrabbableItem, Coroutine>();

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        if (!isMultiSlot && singleSnapPoint == null)
            singleSnapPoint = transform;

        if (isMultiSlot && multiSnapPoints.Count == 0)
            multiSnapPoints.Add(transform);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (CanGrab.Instance != null && Time.time - CanGrab.Instance.LastReleaseTime < 0.2f)
            return;
    }

    private void OnTriggerStay(Collider other)
    {
        if (CanGrab.Instance != null && Time.time - CanGrab.Instance.LastReleaseTime < 0.2f)
            return;
    }

    public virtual bool CanSnap(GrabbableItem item)
    {
        if (item == null) return false;
        if (item.ItemType != requiredType) return false;

        if (!isMultiSlot && singleSlotOnlyOne && attachedItem != null)
            return false;

        if (isMultiSlot)
        {
            if (attachedItems.Contains(item)) return false;
            if (attachedItems.Count >= multiSnapPoints.Count) return false;
        }
        else
        {
            if (attachedItem == item) return false;
        }

        Transform target = isMultiSlot ? GetBestMultiPoint(item) : singleSnapPoint;
        if (target == null) return false;

        return Vector3.Distance(item.transform.position, target.position) <= snapDistance;
    }

    public virtual void Snap(GrabbableItem item)
    {
        if (item == null) return;

        if (!isMultiSlot)
        {
            if (singleSlotOnlyOne && attachedItem != null)
                return;

            if (snapRoutine != null)
                return;
        }
        else
        {
            if (snapRoutines.ContainsKey(item))
                return;
        }

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

    public virtual void OnItemGrabbedFromZone(GrabbableItem grabbedItem)
    {
        if (grabbedItem == null) return;

        if (grabbedItem.ItemType == GrabbableType.Mineral)
        {
            if (TutorialManager.Instance != null &&
                !TutorialManager.Instance.CanGrabAnyMineralFromVehicle())
            {
                FindObjectOfType<CanGrab>()?.ForceRelease();
                return;
            }

            var mineralData = grabbedItem.GetComponentInChildren<MineralData>();
            if (mineralData != null && mineralData.isLastInTutorialQueue)
            {
                if (TutorialManager.Instance != null &&
                    !TutorialManager.Instance.CanGrabLastTutorialMineral())
                {
                    FindObjectOfType<CanGrab>()?.ForceRelease();
                    return;
                }
            }
        }

        grabbedItem.transform.SetParent(null);

        var col = grabbedItem.GetComponent<Collider>();
        if (col != null) col.isTrigger = false;

        onItemRemoved?.Invoke(grabbedItem);

        if (!isMultiSlot)
        {
            if (attachedItem == grabbedItem)
            {
                attachedItem = null;

                if (snapRoutine != null)
                {
                    StopCoroutine(snapRoutine);
                    snapRoutine = null;
                }
            }
        }
        else
        {
            if (attachedItems.Remove(grabbedItem))
            {
                if (snapRoutines.TryGetValue(grabbedItem, out var cr))
                {
                    if (cr != null) StopCoroutine(cr);
                    snapRoutines.Remove(grabbedItem);
                }
            }
        }
    }

    private Transform GetBestMultiPoint(GrabbableItem item)
    {
        var available = multiSnapPoints
            .Where(p => p != null &&
                        !attachedItems.Any(i => i != null && i.transform.parent == p))
            .ToList();

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

        float totalDist =
            Vector3.Distance(startPos, liftPos) +
            Vector3.Distance(liftPos, finalPos);

        float duration = Mathf.Max(minSnapDuration, totalDist / snapSpeed);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float curve = snapCurve.Evaluate(t);

            Vector3 currentPos =
                t < 0.5f
                    ? Vector3.Lerp(startPos, liftPos, t * 2f)
                    : Vector3.Lerp(liftPos, finalPos, (t - 0.5f) * 2f);

            item.transform.position = currentPos;
            item.transform.rotation = Quaternion.Slerp(startRot, target.rotation, curve);
            yield return null;
        }

        item.transform.SetParent(target);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        onItemSnapped?.Invoke(item);

        if (isMulti && rb)
        {
            rb.isKinematic = makeKinematicInMultiSlot;
            rb.useGravity = false;
        }

        if (!isMulti)
            snapRoutine = null;
        else
            snapRoutines.Remove(item);

        grabber?.EndSnappingToZoneComplete();
    }

    public virtual void LoadSnappedItem(GrabbableItem item, int pointIndex = -1)
    {
        if (item == null) return;

        if (!isMultiSlot && singleSlotOnlyOne && attachedItem != null)
            return;

        Transform target;

        if (!isMultiSlot)
        {
            target = singleSnapPoint ?? transform;
        }
        else
        {
            if (pointIndex >= 0 &&
                pointIndex < multiSnapPoints.Count &&
                multiSnapPoints[pointIndex] != null &&
                !attachedItems.Any(i => i != null && i.transform.parent == multiSnapPoints[pointIndex]))
            {
                target = multiSnapPoints[pointIndex];
            }
            else
            {
                target = GetBestMultiPoint(item) ??
                         multiSnapPoints.FirstOrDefault(p => p != null);
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

        if (MineralScannerManager.Instance != null &&
            this == MineralScannerManager.Instance.targetSnapZone)
            MineralScannerManager.Instance.ForceScanCurrentMineral();
        else
            StartCoroutine(DelayedScannerCheck());
    }

    private IEnumerator DelayedScannerCheck()
    {
        yield return new WaitForEndOfFrame();
        yield return null;

        if (MineralScannerManager.Instance != null &&
            this == MineralScannerManager.Instance.targetSnapZone &&
            IsOccupied)
            MineralScannerManager.Instance.ForceScanCurrentMineral();
    }

    public virtual void ReleaseItem(GrabbableItem item)
    {
        if (isMultiSlot && attachedItems.Remove(item))
        {
            if (snapRoutines.TryGetValue(item, out var cr))
            {
                if (cr != null) StopCoroutine(cr);
                snapRoutines.Remove(item);
            }
        }
    }

    private void OnDestroy()
    {
        if (!isMultiSlot && attachedItem != null)
            attachedItem.transform.SetParent(null);

        foreach (var item in attachedItems)
            if (item != null)
                item.transform.SetParent(null);
    }

    private void Reset()
    {
        singleSnapPoint = transform;
        multiSnapPoints.Clear();
        multiSnapPoints.Add(transform);
    }
}
