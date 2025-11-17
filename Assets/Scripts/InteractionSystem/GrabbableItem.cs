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
}
