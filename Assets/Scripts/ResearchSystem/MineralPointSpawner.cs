using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

public class MineralPointSpawner : MonoBehaviour
{
    [Header("РАЗБРОС ТОЧЕК — ПЛОСКИЙ КРУГ В XZ")]
    [SerializeField] private float spawnRadius = 0.35f;                    // Максимальный радиус
    [SerializeField] private float minDistanceFromCenter = 0.15f;          // Не ближе к центру
    [SerializeField] private float minDistanceBetweenPoints = 0.18f;       // Не ближе друг к другу!

    [Header("Автогенерация")]
    [SerializeField] private bool spawnPointsOnStart = true;
    [SerializeField] private bool generateDataOnStart = true;

    private void Start()
    {
        if (spawnPointsOnStart) SpawnDistributedPoints();
        if (generateDataOnStart) GetComponent<MineralData>().GenerateData();
    }

    private void SpawnDistributedPoints()
    {
        MineralData data = GetComponent<MineralData>();
        if (data == null) return;

        Vector3[] positions = GenerateFlatCirclePoints(
            count: 3,
            radius: spawnRadius,
            minFromCenter: minDistanceFromCenter,
            minBetween: minDistanceBetweenPoints
        );

        data.AgePoint = CreatePoint("Возраст", positions[0], Color.yellow);
        data.CrystalPoint = CreatePoint("Крист. решётка", positions[1], Color.magenta);
        data.RadioactivityPoint = CreatePoint("Радиоактивность", positions[2], Color.red);

        
    }

    private Vector3[] GenerateFlatCirclePoints(int count, float radius, float minFromCenter, float minBetween)
    {
        List<Vector3> points = new List<Vector3>();
        List<float> angles = new List<float>();

       
        for (int i = 0; i < 50 && points.Count < count; i++)
        {
            float angle = Random.Range(0f, 360f);
            float dist = Random.Range(minFromCenter, radius);
            Vector3 candidate = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad)) * dist;

            bool tooClose = false;

            if (candidate.magnitude < minFromCenter)
                tooClose = true;

            
            foreach (Vector3 p in points)
            {
                if (Vector3.Distance(p, candidate) < minBetween)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                points.Add(candidate);
                angles.Add(angle);
            }
        }

       
        if (points.Count < count)
        {
            points.Clear();
            float step = 360f / count;
            float offset = Random.Range(0f, step);
            for (int i = 0; i < count; i++)
            {
                float angle = (i * step + offset) * Mathf.Deg2Rad;
                float dist = Mathf.Lerp(minFromCenter, radius, Random.Range(0.7f, 1f));
                Vector3 pos = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * dist;
                points.Add(pos);
            }
        }

        return points.ToArray();
    }

    private ScanPoint CreatePoint(string name, Vector3 localPos, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;

        var point = go.AddComponent<ScanPoint>();
        point.pointName = name;
        point.gizmoColor = color;
        point.gizmoSize = 0.08f;
        return point;
    }

#if UNITY_EDITOR
    [ContextMenu("Переспавнить точки (XZ-плоскость)")]
    private void Respawn() => SpawnDistributedPoints();
#endif

    // ВИЗУАЛИЗАЦИЯ В SCENE VIEW
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.3f);
        Gizmos.DrawSphere(transform.position, spawnRadius);

        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.4f);
        Gizmos.DrawSphere(transform.position, minDistanceFromCenter);

        Handles.color = Color.cyan;
        Handles.DrawWireDisc(transform.position, Vector3.up, spawnRadius);
        Handles.DrawWireDisc(transform.position, Vector3.up, minDistanceFromCenter);
    }
}