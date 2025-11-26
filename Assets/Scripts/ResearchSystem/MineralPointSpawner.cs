using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MineralPointSpawner : MonoBehaviour
{
    [Header("РАЗБРОС ТОЧЕК")]
    [SerializeField] private float spawnRadius = 0.35f;
    [SerializeField] private float minDistanceFromCenter = 0.15f;
    [SerializeField] private float minDistanceBetweenPoints = 0.18f;

    [Header("Автогенерация")]
    [SerializeField] private bool spawnPointsOnStart = true;
    [SerializeField] private bool generateDataOnStart = true;

    private MineralData mineralData;

    private void Awake()
    {
        mineralData = GetComponent<MineralData>();
    }

    private void Start()
    {
        // Генерируем данные только если это новый минерал
        if (generateDataOnStart && Mathf.Approximately(mineralData.realAge, 0f))
        {
            mineralData.GenerateData();
        }

        // ТОЛЬКО если это первый запуск и точки не были восстановлены из сохранения
        if (spawnPointsOnStart &&
            mineralData.AgePoint == null &&
            mineralData.CrystalPoint == null &&
            mineralData.RadioactivityPoint == null)
        {
            SpawnDistributedPoints();
        }
    }

    // ЭТОТ МЕТОД ТЕПЕРЬ ВЫЗЫВАЕТСЯ ИЗ SaveableObject ПРИ ЗАГРУЗКЕ!
    public void RestorePointsFromSaveData(Vector3 agePos, Vector3 crystalPos, Vector3 radioPos)
    {
        CreatePoint(ref mineralData.AgePoint, "Возраст", agePos, Color.yellow);
        CreatePoint(ref mineralData.CrystalPoint, "Крист. решётка", crystalPos, Color.magenta);
        CreatePoint(ref mineralData.RadioactivityPoint, "Радиоактивность", radioPos, Color.red);

        Debug.Log($"[MineralPointSpawner] Точки восстановлены из сохранения для {gameObject.name}");
    }

    public void SpawnDistributedPoints()
    {
        Vector3[] positions = GenerateFlatCirclePoints(3, spawnRadius, minDistanceFromCenter, minDistanceBetweenPoints);

        CreatePoint(ref mineralData.AgePoint, "Возраст", positions[0], Color.yellow);
        CreatePoint(ref mineralData.CrystalPoint, "Крист. решётка", positions[1], Color.magenta);
        CreatePoint(ref mineralData.RadioactivityPoint, "Радиоактивность", positions[2], Color.red);
    }

    private void CreatePoint(ref ScanPoint pointField, string name, Vector3 localPos, Color color)
    {
        if (pointField != null)
            DestroyImmediate(pointField.gameObject);

        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;

        var point = go.AddComponent<ScanPoint>();
        point.pointName = name;
        point.gizmoColor = color;
        point.gizmoSize = 0.08f;

        pointField = point;
    }

    private Vector3[] GenerateFlatCirclePoints(int count, float radius, float minFromCenter, float minBetween)
    {
        // ← твой код без изменений
        List<Vector3> points = new List<Vector3>();

        for (int i = 0; i < 100 && points.Count < count; i++)
        {
            float angle = Random.Range(0f, 360f);
            float dist = Random.Range(minFromCenter, radius);
            Vector3 candidate = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad)) * dist;

            if (candidate.magnitude < minFromCenter) continue;

            bool tooClose = false;
            foreach (Vector3 p in points)
                if (Vector3.Distance(p, candidate) < minBetween) { tooClose = true; break; }

            if (!tooClose) points.Add(candidate);
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
    private void Respawn() => SpawnDistributedPoints();
#endif
}