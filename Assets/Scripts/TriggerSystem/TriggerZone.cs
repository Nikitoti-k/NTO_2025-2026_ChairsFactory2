using UnityEngine;

public class TriggerZone : MonoBehaviour
{
    [SerializeField] private TriggerEventSO triggerEvent;
    [SerializeField] private string targetTag = "Player"; // для примера

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            triggerEvent?.Raise();
        }
    }
}