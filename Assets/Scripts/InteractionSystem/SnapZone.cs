using UnityEngine;

public class SnapZone : MonoBehaviour
{
    [Header("Какой тип объекта принимает")]
    [SerializeField] private GrabbableType requiredType;

    [Header("Где объект фиксируется")]
    [SerializeField] private Transform snapPoint;

    [Header("Настройки")]
    [SerializeField] private float snapDistance = 1.2f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private GrabbableItem candidate;
    private GrabbableItem attachedItem;

    public bool IsOccupied => attachedItem != null;
    public GrabbableItem AttachedItem => attachedItem;

    private void Update()
    {
        if (candidate != null && attachedItem == null)
        {
            if (Input.GetKeyDown(interactKey))
                AttachObject(candidate);
        }
    }

    protected virtual void AttachObject(GrabbableItem item)
    {
        attachedItem = item;

        item.transform.SetParent(snapPoint);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (attachedItem != null) return;

        if (other.TryGetComponent(out GrabbableItem item) &&
            item.ItemType == requiredType)
        {
            candidate = item;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (candidate == null) return;

        if (other.gameObject == candidate.gameObject)
            candidate = null;
    }
}
