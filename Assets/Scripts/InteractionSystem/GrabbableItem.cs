using UnityEngine;

public enum GrabbableType { Mineral, Tool, Door, Resource, Junk, Cassette}

public abstract class GrabbableItem : MonoBehaviour
{
    [SerializeField] protected GrabbableType itemType;
    public GrabbableType ItemType => itemType;

    public static System.Action<GrabbableItem, Collision> OnCollision;

    private void OnCollisionEnter(Collision col)
    {
        OnCollision?.Invoke(this, col);
    }
}