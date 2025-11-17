using UnityEngine;
public class FlareObject : MonoBehaviour
{
    [Header("Как бросаем факел")]
    public float throwForce = 15f;
    public float upwardForce = 8f;
    public AnimationCurve scatterPattern = AnimationCurve.Linear(0, 0, 1, 1); //кривая для небольшого разброса 
    private Collider col;
    private Rigidbody rb;
  
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        col.isTrigger = true;
    }
    public void SetHeld(bool held)
    {
        
        if (rb != null)
            rb.isKinematic = held;
    }
    public void Initialize(Vector3 throwDirection, float scatterAmount = 1f)
    {
        
        if (rb == null)
        {
            return;
        }
       
        Vector3 scatterOffset = Random.insideUnitSphere * scatterAmount;
        scatterOffset = Vector3.Scale(scatterOffset, new Vector3(1.2f, 0.8f, 1.2f));
        scatterOffset = scatterPattern.Evaluate(Random.value) * scatterOffset;
        col.isTrigger = false;
        rb.linearVelocity = throwDirection * throwForce +
        Vector3.up * upwardForce +
        scatterOffset;
        rb.angularVelocity = Random.insideUnitSphere * 10f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }
    public void ReturnFlare()
    {
        col.isTrigger = true;
    }
}