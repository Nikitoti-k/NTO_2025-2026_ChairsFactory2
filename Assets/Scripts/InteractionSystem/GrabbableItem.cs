using UnityEngine;

public enum GrabbableType
{
    Mineral,
    Tool,
    Resource,
    Junk
}

public abstract class GrabbableItem : MonoBehaviour
{
    [SerializeField] private GrabbableType itemType;

    public GrabbableType ItemType => itemType;

    public static event System.Action<GrabbableItem, Collision> OnGrabbedCollision;

    private void OnCollisionEnter(Collision collision)
    {
        
        OnGrabbedCollision?.Invoke(this, collision);

        
    }
}
    
