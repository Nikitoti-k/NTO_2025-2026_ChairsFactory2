using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/Trigger Event")]
public class TriggerEventSO : ScriptableObject
{
    private List<TriggerEventListener> listeners = new();

    public void Raise()
    {
        for (int i = listeners.Count - 1; i >= 0; i--)
        {
            listeners[i]?.OnEventRaised();
        }
    }

    public void Register(TriggerEventListener listener)
    {
        if (!listeners.Contains(listener))
            listeners.Add(listener);
    }

    public void Unregister(TriggerEventListener listener)
    {
        listeners.Remove(listener);
    }
}