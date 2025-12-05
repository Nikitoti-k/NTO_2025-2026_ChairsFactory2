using UnityEngine.Events;
using UnityEngine;

public class TriggerEventListener : MonoBehaviour
{
    [SerializeField] private TriggerEventSO eventSO;
    public UnityEvent response;

    private void OnEnable() => eventSO.Register(this);
    private void OnDisable() => eventSO.Unregister(this);

    public void OnEventRaised() => response?.Invoke();
}