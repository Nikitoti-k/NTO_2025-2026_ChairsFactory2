using UnityEngine;
using System.Collections;
using System.Linq;

public class SaveableObject : MonoBehaviour, ISaveableV2
{
    [Header("Save System")]
    [SerializeField] protected string uniqueID = "";
    [SerializeField] protected string prefabIdentifier = "";

    protected Rigidbody rb;
    protected Collider col;

    public bool IsPlayer => gameObject.CompareTag("Player");

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponentInChildren<Collider>();

        if (string.IsNullOrEmpty(uniqueID))
            uniqueID = System.Guid.NewGuid().ToString();
    }

    public string GetUniqueID() => uniqueID;
    public void SetPrefabIdentifier(string id) => prefabIdentifier = id;

    public virtual ObjectSaveData GetCommonSaveData()
    {
        var data = new ObjectSaveData
        {
            uniqueID = uniqueID,
            prefabIdentifier = prefabIdentifier,
            position = transform.position,
            rotation = transform.rotation,
            velocity = rb ? rb.linearVelocity : Vector3.zero,
            angularVelocity = rb ? rb.angularVelocity : Vector3.zero,
            isActive = gameObject.activeSelf,
            parentPath = transform.parent ? transform.parent.GetPath() : "",
            isTrigger = col ? col.isTrigger : false,
            useGravity = rb ? rb.useGravity : true,
            constraints = rb ? (int)rb.constraints : 0,
            isKinematic = rb ? rb.isKinematic : false
        };

        // Snap-зоны
        var grabbable = GetComponent<GrabbableItem>();
        if (grabbable && transform.parent != null)
        {
            var zone = transform.parent.GetComponentInParent<SnapZone>();
            var zoneSaveable = zone?.GetComponent<SaveableObject>();
            if (zoneSaveable != null)
            {
                data.snappedZoneID = zoneSaveable.GetUniqueID();
                if (zone.isMultiSlot)
                    data.snapPointIndex = zone.multiSnapPoints.IndexOf(transform.parent);
                data.parentPath = "";
            }
        }

        // Транспорт
        if (IsPlayer)
        {
            if (InputRouter.Instance?.CurrentController is TransportMovement transport && transport.TryGetComponent<SaveableObject>(out var ctrl))
                data.controllingTransportID = ctrl.GetUniqueID();

            if (transform.parent?.GetComponentInParent<SaveableObject>() is SaveableObject seated)
                data.seatedInTransportID = seated.GetUniqueID();
        }

        // Минерал в сканере
        if (TryGetComponent<MineralSaveableObject>(out _) &&
            GetComponentInParent<SnapZone>() == MineralScannerManager.Instance?.targetSnapZone)
            data.wasInScannerZone = true;

        return data;
    }

    public virtual void LoadCommonData(ObjectSaveData data)
    {
        uniqueID = data.uniqueID;

        transform.position = data.position;
        transform.rotation = data.rotation;
        gameObject.SetActive(data.isActive);

        if (rb)
        {
            rb.linearVelocity = data.velocity;
            rb.angularVelocity = data.angularVelocity;
            rb.useGravity = data.useGravity;
            rb.constraints = (RigidbodyConstraints)data.constraints;
            rb.isKinematic = data.isKinematic;
            rb.Sleep();
        }

        if (col) col.isTrigger = data.isTrigger;

        if (!string.IsNullOrEmpty(data.snappedZoneID) ||
            !string.IsNullOrEmpty(data.seatedInTransportID) ||
            !string.IsNullOrEmpty(data.controllingTransportID) ||
            data.wasInScannerZone)
        {
            StartCoroutine(RestoreRelations(data));
        }
        else if (!string.IsNullOrEmpty(data.parentPath))
        {
            var parent = GameObject.Find(data.parentPath)?.transform;
            if (parent) transform.SetParent(parent);
        }
    }

    protected virtual IEnumerator RestoreRelations(ObjectSaveData data)
    {
        // Ждём, пока ВСЕ SaveableObject + InputRouter точно проснутся
        yield return new WaitUntil(() =>
            InputRouter.Instance != null &&
            FindObjectsOfType<SaveableObject>().Length >= SaveManager.Instance.GetCurrentObjectCountEstimate());

        var all = FindObjectsOfType<SaveableObject>(true);

        // 1. Сначала восстанавливаем УПРАВЛЕНИЕ (самое важное!)
        if (IsPlayer && !string.IsNullOrEmpty(data.controllingTransportID))
        {
            var transportObj = all.FirstOrDefault(x => x.GetUniqueID() == data.controllingTransportID);
            if (transportObj != null && transportObj.TryGetComponent<IControllable>(out var ctrl))
            {
                // Прямой вызов — без рефлексии и без ожидания
                InputRouter.Instance.SetController(ctrl);
                Debug.Log($"[Save] Управление транспортом восстановлено: {transportObj.name}");
            }
        }

        // 2. Потом — посадку в транспорт (визуальное положение)
        if (IsPlayer && !string.IsNullOrEmpty(data.seatedInTransportID))
        {
            var transportObj = all.FirstOrDefault(x => x.GetUniqueID() == data.seatedInTransportID);
            if (transportObj != null && transportObj.TryGetComponent<TransportMovement>(out var transport))
            {
                if (TryGetComponent<PlayerMovement>(out var pm))
                {
                    // Используем ПУБЛИЧНЫЙ метод вместо рефлексии
                    pm.ForceMountWithoutControllerChange(transport);
                }
            }
        }

        // Snap-зоны и сканер — как было
        if (!string.IsNullOrEmpty(data.snappedZoneID))
        {
            var zoneObj = all.FirstOrDefault(x => x.GetUniqueID() == data.snappedZoneID);
            if (zoneObj && zoneObj.TryGetComponent<SnapZone>(out var zone) &&
                TryGetComponent<GrabbableItem>(out var grabbable))
            {
                zone.LoadSnappedItem(grabbable, data.snapPointIndex);
            }
        }

        if (data.wasInScannerZone && TryGetComponent<GrabbableItem>(out var item))
        {
            MineralScannerManager.Instance?.targetSnapZone?.LoadSnappedItem(item, data.snapPointIndex);
            MineralScannerManager.Instance?.ForceScanCurrentMineral();
        }
    }
}

    public static class TransformExtensions
{
    public static string GetPath(this Transform t)
        => t.parent == null ? t.name : t.parent.GetPath() + "/" + t.name;
}