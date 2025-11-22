using UnityEngine;

public class ScanPoint : MonoBehaviour
{
    [HideInInspector] public string pointName = "Точка данных";
    public Color gizmoColor = Color.cyan;
    [Range(0.02f, 0.2f)] public float gizmoSize = 0.06f;
}