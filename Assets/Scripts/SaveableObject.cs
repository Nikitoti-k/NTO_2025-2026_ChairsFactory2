using UnityEngine;

using UnityEngine;
using System.Collections;
using System.Linq;

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
        Debug.Log($"[SaveableObject] Awake: {gameObject.name}, uniqueID: {uniqueID}, prefabIdentifier: {prefabIdentifier}");
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueID) && !Application.isPlaying)
            uniqueID = System.Guid.NewGuid().ToString();
    }
#endif

    public string GetUniqueID() => uniqueID;

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
            customInt1 = 0,
            isTrigger = col ? col.isTrigger : false,
            useGravity = rb ? rb.useGravity : true,
            constraints = rb ? (int)rb.constraints : 0,
            isKinematic = rb ? rb.isKinematic : false, // ← НОВОЕ
            seatedInTransportID = "",
            controllingTransportID = "",
            snappedZoneID = "",
            snapPointIndex = -1

        };
        var mineralData = GetComponent<MineralData>();
        if (mineralData != null)
        {
            var snapZone = GetComponentInParent<SnapZone>();
            if (snapZone != null && MineralScannerManager.Instance != null)
            {
                if (snapZone == MineralScannerManager.Instance.targetSnapZone)
                {
                    data.wasInScannerZone = true;
                    Debug.Log($"[Save] Минерал {gameObject.name} был в сканере — пометили!");
                }
            }
            var mData = mineralData.GetMineralSaveData();
            data.customFloat1 = mData.realAge;
            data.customFloat2 = mData.realRadioactivity;
            data.customVector1 = mData.agePointLocalPos;
            data.customVector2 = mData.crystalPointLocalPos;
            data.customVector3 = mData.radioactivityPointLocalPos;
            data.customBool1 = mData.isResearched;
        }
        // === СНАП В ЗОНУ ===
        var grabbable = GetComponent<GrabbableItem>();
        if (grabbable != null && transform.parent != null)
        {
            var snapZone = transform.parent.GetComponentInParent<SnapZone>();
            if (snapZone != null)
            {
                var zoneSaveable = snapZone.GetComponent<SaveableObject>();
                if (zoneSaveable != null)
                {
                    data.snappedZoneID = zoneSaveable.GetUniqueID();
                    if (snapZone.isMultiSlot)
                        data.snapPointIndex = snapZone.multiSnapPoints.IndexOf(transform.parent);
                    data.parentPath = ""; // чистим — используем snappedZoneID
                }
            }
        }

        // === ИГРОК В ТРАНСПОРТЕ ===
        if (IsPlayer)
        {
            if (InputRouter.Instance?.CurrentController is TransportMovement transportCtrl)
            {
                var ctrlSaveable = transportCtrl.GetComponent<SaveableObject>();
                if (ctrlSaveable != null)
                    data.controllingTransportID = ctrlSaveable.GetUniqueID();
            }

            if (transform.parent != null)
            {
                var parentSaveable = transform.parent.GetComponentInParent<SaveableObject>();
                if (parentSaveable != null)
                    data.seatedInTransportID = parentSaveable.GetUniqueID();
            }
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
            rb.isKinematic = data.isKinematic;     // ← ВОССТАНАВЛИВАЕМ КИНЕМАТИК СРАЗУ!
            rb.linearVelocity = data.velocity;
            rb.angularVelocity = data.angularVelocity;
            rb.useGravity = data.useGravity;
            rb.constraints = (RigidbodyConstraints)data.constraints;
            rb.Sleep();
        }
        var mineral = GetComponent<MineralData>();
        if (mineral != null)
        {
            var mData = new MineralData.MineralSaveData
            {
                realAge = data.customFloat1,
                realRadioactivity = data.customFloat2,
                agePointLocalPos = data.customVector1,
                crystalPointLocalPos = data.customVector2,
                radioactivityPointLocalPos = data.customVector3,
                isResearched = data.customBool1
            };

            mineral.LoadMineralSaveData(mData); // ← восстанавливаем параметры

            // ← ГЛАВНОЕ: ВОССТАНАВЛИВАЕМ ТОЧКИ ЧЕРЕЗ SPAWNER!
            var spawner = GetComponent<MineralPointSpawner>();
            if (spawner != null)
            {
                spawner.RestorePointsFromSaveData(
                    mData.agePointLocalPos,
                    mData.crystalPointLocalPos,
                    mData.radioactivityPointLocalPos
                );
            }
        }
        if (col)
            col.isTrigger = data.isTrigger;

        // === ВОССТАНАВЛИВАЕМ СНАП ИЛИ ТРАНСПОРТ ===
        if (!string.IsNullOrEmpty(data.snappedZoneID) || !string.IsNullOrEmpty(data.seatedInTransportID) || !string.IsNullOrEmpty(data.controllingTransportID))
        {
            StartCoroutine(RestoreStateAfterLoad(data));
        }
        else if (!string.IsNullOrEmpty(data.parentPath))
        {
            Transform parent = GameObject.Find(data.parentPath)?.transform;
            if (parent) transform.SetParent(parent);
        }

        Physics.SyncTransforms();
    }

    private IEnumerator RestoreStateAfterLoad(SaveData data)
    {
        yield return new WaitForEndOfFrame();

        var allSaveables = FindObjectsOfType<SaveableObject>(true);

        // 1. SnapZone
        if (!string.IsNullOrEmpty(data.snappedZoneID))
        {
            var zoneSaveable = allSaveables.FirstOrDefault(s => s.GetUniqueID() == data.snappedZoneID);
            var snapZone = zoneSaveable?.GetComponent<SnapZone>();
            var grabbable = GetComponent<GrabbableItem>();
            if (snapZone && grabbable)
            {
                snapZone.LoadSnappedItem(grabbable, data.snapPointIndex);
            }
        }

        // 2. Посадка в транспорт + управление
        if (IsPlayer)
        {
            if (!string.IsNullOrEmpty(data.seatedInTransportID))
            {
                var transport = allSaveables
                    .FirstOrDefault(s => s.GetUniqueID() == data.seatedInTransportID)?
                    .GetComponent<TransportMovement>();

                if (transport)
                {
                    var playerMovement = GetComponent<PlayerMovement>();
                    playerMovement.GetType()
                        .GetMethod("Mount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.Invoke(playerMovement, new object[] { transport });
                }
            }

            if (!string.IsNullOrEmpty(data.controllingTransportID))
            {
                var controller = allSaveables
                    .FirstOrDefault(s => s.GetUniqueID() == data.controllingTransportID)?
                    .GetComponent<IControllable>();

                if (controller != null)
                    InputRouter.Instance?.SetController(controller);
            }
            else
            {
                InputRouter.Instance?.SetController(GetComponent<IControllable>());
            }
        }
    }

    public void SetPrefabIdentifier(string id) => prefabIdentifier = id;
}
public static class TransformExtensions
{
    public static string GetPath(this Transform t)
    {
        if (t.parent == null) return t.name;
        return t.parent.GetPath() + "/" + t.name;
    }
}