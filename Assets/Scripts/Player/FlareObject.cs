
using UnityEngine;

public class FlareObject : MonoBehaviour
{
    [Header("Бросок")]
    public float throwForce = 15f;
    public float upwardForce = 8f;
    public AnimationCurve scatterPattern = AnimationCurve.Linear(0, 0, 1, 1); 

    private Rigidbody rb;
    private Collider col;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponentInChildren<Collider>();
    }

   
    public void Initialize(Vector3 throwDirection, float scatterAmount = 1f)
    {
        if (rb == null) return;

        
        col.isTrigger = false;
        rb.isKinematic = false;

        
        Vector3 scatter = Vector3.zero;
        if (scatterAmount > 0.01f)
        {
            scatter = Random.insideUnitSphere * scatterAmount;
            scatter = Vector3.Scale(scatter, new Vector3(1.2f, 0.8f, 1.2f));
            float curveValue = scatterPattern.Evaluate(Random.value);
            scatter *= curveValue;
        }

        rb.linearVelocity = throwDirection * throwForce + Vector3.up * upwardForce + scatter;
        rb.angularVelocity = Random.insideUnitSphere * 10f;

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        rb.WakeUp(); 
    }

    public void SetHeld(bool held)
    {
        if (rb) rb.isKinematic = held;
        if (col) col.isTrigger = held; 
    }

    public void ReturnToPool()
    {
        SetHeld(true);
        gameObject.SetActive(false);
    }
}