using UnityEngine;

public class AdditionalCassette : MonoBehaviour
{
    //public AdditionalCassetteData Data => data;

    private Rigidbody rb;
    //private Collider col;
    private bool isInserted = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void InsertIntoPlayer(Transform slot)
    {
        isInserted = true;
        rb.isKinematic = true;
        transform.SetParent(slot);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void EjectFromPlayer()
    {
        isInserted = false;
        transform.SetParent(null);
    }

    public bool IsInserted() => isInserted;
}
