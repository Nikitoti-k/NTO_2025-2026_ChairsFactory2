using UnityEngine;

public class ProximityWave : MonoBehaviour
{
    [Header("Геометрия")]
    [SerializeField] private int segments = 140;
    [SerializeField] private float waveWidth = 620f;

    [Header("Амплитуда")]
    [SerializeField] private float minAmplitude = 8f;
    [SerializeField] private float maxAmplitude = 130f;

    [Header("Частота и скорость")]
    [SerializeField] private float baseFrequency = 4f;
    [SerializeField] private float speed = 5f;

    [Header("Визуал")]
    [SerializeField] private Gradient colorGradient;
    [SerializeField] private float lineWidth = 4f;

    private LineRenderer lr;
    private float phase;
    private float currentProximity;

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = segments;
        lr.useWorldSpace = false;
        lr.loop = false;
        lr.widthMultiplier = lineWidth;
      
        lr.receiveShadows = false;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private void Start()
    {
        if (MineralScanner_Renderer.Instance != null)
            MineralScanner_Renderer.Instance.SubscribeToProximity(SetProximity);
    }

    private void OnDestroy()
    {
        if (MineralScanner_Renderer.Instance != null)
            MineralScanner_Renderer.Instance.UnsubscribeFromProximity(SetProximity);
    }

    public void SetProximity(float proximity)
    {
        currentProximity = Mathf.Clamp01(proximity);
    }

    private void Update()
    {
        phase += Time.deltaTime * speed;
        DrawWave();
    }

    private void DrawWave()
    {
        float amplitude = Mathf.Lerp(minAmplitude, maxAmplitude, currentProximity);
        float half = waveWidth * 0.5f;

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);
            float x = Mathf.Lerp(-half, half, t);
            float y = amplitude * Mathf.Sin(t * baseFrequency * Mathf.PI * 2f + phase);
            lr.SetPosition(i, new Vector3(x, y, 0f));
        }

        Color c = colorGradient.Evaluate(currentProximity);
        lr.startColor = lr.endColor = c;
    }
}