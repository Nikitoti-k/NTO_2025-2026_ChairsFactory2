using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

public class SaveableObject : MonoBehaviour, ISaveable
{
    [SerializeField] private string uniqueID = "";
    [SerializeField] private string prefabIdentifier = "";
    private Rigidbody rb;
    private Collider col;
    public bool IsPlayer => gameObject.CompareTag("Player");

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponentInChildren<Collider>();
        if (string.IsNullOrEmpty(uniqueID))
            uniqueID = System.Guid.NewGuid().ToString();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying && string.IsNullOrEmpty(uniqueID))
            uniqueID = System.Guid.NewGuid().ToString();
    }
#endif

    public string GetUniqueID() => uniqueID;
    public void SetPrefabIdentifier(string id) => prefabIdentifier = id;

    public SaveData GetSaveData()
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

        var mineral = GetComponent<MineralData>();
        if (mineral != null)
        {
            if (GetComponentInParent<SnapZone>() == MineralScannerManager.Instance?.targetSnapZone)
                data.wasInScannerZone = true;

            var m = mineral.GetMineralSaveData();
            data.customFloat1 = m.realAge;
            data.customFloat2 = m.realRadioactivity;
            data.customVector1 = m.agePointLocalPos;
            data.customVector2 = m.crystalPointLocalPos;
            data.customVector3 = m.radioactivityPointLocalPos;
            data.customBool1 = m.isResearched;
            data.customString1 = mineral.savedAgeLine;
            data.customString2 = mineral.savedRadioactivityLine;
            data.customString3 = mineral.savedCrystalLine;
        }

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

    public void LoadFromSaveData(SaveData data)
    {
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

        var mineral = GetComponent<MineralData>();
        if (mineral != null)
        {
            mineral.LoadMineralSaveData(new MineralData.MineralSaveData
            {
                realAge = data.customFloat1,
                realRadioactivity = data.customFloat2,
                agePointLocalPos = data.customVector1,
                crystalPointLocalPos = data.customVector2,
                radioactivityPointLocalPos = data.customVector3,
                isResearched = data.customBool1
            });

            mineral.savedAgeLine = data.customString1 ?? "";
            mineral.savedRadioactivityLine = data.customString2 ?? "";
            mineral.savedCrystalLine = data.customString3 ?? "";

            GetComponent<MineralPointSpawner>()?.RestorePointsFromSaveData(
                data.customVector1, data.customVector2, data.customVector3);
        }

        if (!string.IsNullOrEmpty(data.snappedZoneID) || !string.IsNullOrEmpty(data.seatedInTransportID) || !string.IsNullOrEmpty(data.controllingTransportID))
            StartCoroutine(RestoreRelations(data));
        else if (!string.IsNullOrEmpty(data.parentPath))
        {
            Transform parent = GameObject.Find(data.parentPath)?.transform;
            if (parent) transform.SetParent(parent);
        }

        Physics.SyncTransforms();
    }

    private IEnumerator RestoreRelations(SaveData data)
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
                    var method = pm.GetType().GetMethod("Mount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
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