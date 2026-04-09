using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Cassette : MonoBehaviour
{
    [SerializeField] private CassetteData data;
    public CassetteData Data => data;

    public int id;
    private Rigidbody rb;
    private Collider col;
    private bool isInserted = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
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
        /*rb.isKinematic = false;
        rb.AddForce((Vector3.up + transform.forward) * 3f, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);*/
    }

    public bool IsInserted() => isInserted;
}