using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MineralData))]
public class MineralPointSpawner : MonoBehaviour
{
    [SerializeField] private float spawnRadius = 0.35f;
    [SerializeField] private float minDistanceFromCenter = 0.15f;
    [SerializeField] private float minDistanceBetweenPoints = 0.18f;
    [SerializeField] private bool spawnPointsOnStart = true;
    [SerializeField] private bool generateDataOnStart = true;

    private MineralData mineralData;

    private void Awake()
    {
        mineralData = GetComponent<MineralData>();
    }

    private void Start()
    {
        if (generateDataOnStart && Mathf.Approximately(mineralData.realAge, 0f))
            mineralData.GenerateData();

        if (spawnPointsOnStart &&
            mineralData.AgePoint == null &&
            mineralData.CrystalPoint == null &&
            mineralData.RadioactivityPoint == null)
        {
            SpawnDistributedPoints();
        }
    }

    public void RestorePointsFromSaveData(Vector3 agePos, Vector3 crystalPos, Vector3 radioPos)
    {
        CreatePoint(ref mineralData.AgePoint, "Возраст", agePos, Color.yellow);
        CreatePoint(ref mineralData.CrystalPoint, "Крист. решётка", crystalPos, Color.magenta);
        CreatePoint(ref mineralData.RadioactivityPoint, "Радиоактивность", radioPos, Color.red);
    }

    public void SpawnDistributedPoints()
    {
        Vector3[] positions = GenerateFlatCirclePoints(3, spawnRadius, minDistanceFromCenter, minDistanceBetweenPoints);
        CreatePoint(ref mineralData.AgePoint, "Возраст", positions[0], Color.yellow);
        CreatePoint(ref mineralData.CrystalPoint, "Крист. решётка", positions[1], Color.magenta);
        CreatePoint(ref mineralData.RadioactivityPoint, "Радиоактивность", positions[2], Color.red);
    }

    private void CreatePoint(ref ScanPoint target, string name, Vector3 localPos, Color color)
    {
        if (target != null)
            DestroyImmediate(target.gameObject);

        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;

        var point = go.AddComponent<ScanPoint>();
        point.pointName = name;
        point.gizmoColor = color;
        point.gizmoSize = 0.08f;
        target = point;
    }

    private Vector3[] GenerateFlatCirclePoints(int count, float radius, float minFromCenter, float minBetween)
    {
        var points = new List<Vector3>();

        for (int i = 0; i < 100 && points.Count < count; i++)
        {
            float angle = Random.Range(0f, 360f);
            float dist = Random.Range(minFromCenter, radius);
            Vector3 candidate = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad)) * dist;

            if (candidate.magnitude < minFromCenter) continue;
            if (points.Exists(p => Vector3.Distance(p, candidate) < minBetween)) continue;

            points.Add(candidate);
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
                points.Add(new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * dist);
            }
        }

        return points.ToArray();
    }

#if UNITY_EDITOR
    [ContextMenu("Переспавнить точки")]
    private void EditorRespawn() => SpawnDistributedPoints();
#endif
}