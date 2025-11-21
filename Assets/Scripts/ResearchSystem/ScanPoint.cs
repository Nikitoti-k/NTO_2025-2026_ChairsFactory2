
using UnityEngine;
using System;

[ExecuteInEditMode]
public class ScanPoint : MonoBehaviour
{
    [HideInInspector] public string pointName = "Точка данных";
    public Color gizmoColor = Color.cyan;
    [Range(0.02f, 0.2f)] public float gizmoSize = 0.06f;

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, gizmoSize);
        Gizmos.DrawWireSphere(transform.position, gizmoSize * 1.5f);

#if UNITY_EDITOR
        UnityEditor.Handles.color = gizmoColor;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.12f, pointName);
#endif
    }
}