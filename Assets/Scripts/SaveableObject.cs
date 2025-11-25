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

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponentInChildren<Collider>();

        if (string.IsNullOrEmpty(uniqueID))
            uniqueID = System.Guid.NewGuid().ToString();

        if (string.IsNullOrEmpty(prefabIdentifier))
            prefabIdentifier = gameObject.name;
    }

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
            seatedInTransportID = "",
            controllingTransportID = ""
        };

        if (IsPlayer)
        {
            if (InputRouter.Instance != null && InputRouter.Instance.CurrentController is TransportMovement transportCtrl)
            {
                var saveable = transportCtrl.GetComponent<SaveableObject>();
                if (saveable != null)
                    data.controllingTransportID = saveable.GetUniqueID();
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
            rb.linearVelocity = data.velocity;
            rb.angularVelocity = data.angularVelocity;
            rb.useGravity = data.useGravity;
            rb.constraints = (RigidbodyConstraints)data.constraints;
            rb.Sleep();
        }

        if (col)
            col.isTrigger = data.isTrigger;

        if (IsPlayer)
            StartCoroutine(RestorePlayerControlAfterLoad(data.seatedInTransportID, data.controllingTransportID));

        Physics.SyncTransforms();
    }

    private IEnumerator RestorePlayerControlAfterLoad(string seatedID, string controllingID)
    {
        yield return new WaitForEndOfFrame();

        var allSaveables = FindObjectsOfType<SaveableObject>();

        if (!string.IsNullOrEmpty(seatedID))
        {
            var transport = allSaveables
                .FirstOrDefault(s => s.GetUniqueID() == seatedID)?
                .GetComponent<TransportMovement>();

            if (transport != null)
            {
                var playerMovement = GetComponent<PlayerMovement>();
                playerMovement.GetType()
                    .GetMethod("Mount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.Invoke(playerMovement, new object[] { transport });
            }
        }

        if (!string.IsNullOrEmpty(controllingID))
        {
            var controller = allSaveables
                .FirstOrDefault(s => s.GetUniqueID() == controllingID)?
                .GetComponent<IControllable>();

            if (controller != null)
            {
                InputRouter.Instance?.SetController(controller);
                yield break;
            }
        }

        InputRouter.Instance?.SetController(GetComponent<IControllable>());
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