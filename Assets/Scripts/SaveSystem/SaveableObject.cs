using UnityEngine;
using System.Collections;
using System.Linq;
using System.Reflection;

public class SaveableObject : MonoBehaviour, ISaveable
{
    [SerializeField] public string uniqueID = "";
    [SerializeField] private string prefabIdentifier = "";
    public Rigidbody rb;
    public Collider col;
    public bool IsPlayer => gameObject.CompareTag("Player");

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponentInChildren<Collider>();
        if (string.IsNullOrEmpty(uniqueID))
            uniqueID = System.Guid.NewGuid().ToString();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // NO GENERATION HERE! To avoid dubs in prefabs
    }
#endif

    public string GetUniqueID() => uniqueID;
    public void SetPrefabIdentifier(string id) => prefabIdentifier = id;

    public virtual SaveData GetSaveData()
    {
        var data = new SaveData
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

        if (IsPlayer)
        {
            if (InputRouter.Instance?.CurrentController is TransportMovement transport)
                if (transport.GetComponent<SaveableObject>() is SaveableObject ctrl)
                    data.controllingTransportID = ctrl.GetUniqueID();

            if (transform.parent?.GetComponentInParent<SaveableObject>() is SaveableObject parent)
                data.seatedInTransportID = parent.GetUniqueID();
        }

        return data;
    }

    public virtual void LoadFromSaveData(SaveData data)
    {
        uniqueID = data.uniqueID;  // ← ФИКС: Восстанавливаем ID из сохранения

        transform.position = data.position;
        transform.rotation = data.rotation;
        gameObject.SetActive(data.isActive);

        if (rb)
        {
            rb.isKinematic = data.isKinematic;
            rb.linearVelocity = data.velocity;
            rb.angularVelocity = data.angularVelocity;
            rb.useGravity = data.useGravity;
            rb.constraints = (RigidbodyConstraints)data.constraints;
            rb.Sleep();
        }

        if (col) col.isTrigger = data.isTrigger;

        if (!string.IsNullOrEmpty(data.snappedZoneID) || !string.IsNullOrEmpty(data.seatedInTransportID) || !string.IsNullOrEmpty(data.controllingTransportID))
            StartCoroutine(RestoreRelations(data));
        else if (!string.IsNullOrEmpty(data.parentPath))
        {
            Transform parent = GameObject.Find(data.parentPath)?.transform;
            if (parent) transform.SetParent(parent);
        }

        Physics.SyncTransforms();
    }

    protected virtual IEnumerator RestoreRelations(SaveData data)
    {
        yield return new WaitForEndOfFrame();
        var all = FindObjectsOfType<SaveableObject>(true);

        if (!string.IsNullOrEmpty(data.snappedZoneID))
        {
            var zoneObj = all.FirstOrDefault(x => x.GetUniqueID() == data.snappedZoneID);
            if (zoneObj && zoneObj.TryGetComponent<SnapZone>(out var zone) && TryGetComponent<GrabbableItem>(out var grabbable))
                zone.LoadSnappedItem(grabbable, data.snapPointIndex);
        }

        if (IsPlayer)
        {
            if (!string.IsNullOrEmpty(data.seatedInTransportID))
            {
                var transport = all.FirstOrDefault(x => x.GetUniqueID() == data.seatedInTransportID)?.GetComponent<TransportMovement>();
                if (transport && TryGetComponent<PlayerMovement>(out var pm))
                {
                    var method = pm.GetType().GetMethod("Mount", BindingFlags.NonPublic | BindingFlags.Instance);
                    method?.Invoke(pm, new object[] { transport });
                }
            }

            if (!string.IsNullOrEmpty(data.controllingTransportID))
            {
                var ctrl = all.FirstOrDefault(x => x.GetUniqueID() == data.controllingTransportID)?.GetComponent<IControllable>();
                if (ctrl != null) InputRouter.Instance?.SetController(ctrl);
            }
            else if (TryGetComponent<IControllable>(out var playerCtrl))
                InputRouter.Instance?.SetController(playerCtrl);
        }
    }
}

public static class TransformExtensions
{
    public static string GetPath(this Transform t)
        => t.parent == null ? t.name : t.parent.GetPath() + "/" + t.name;
}