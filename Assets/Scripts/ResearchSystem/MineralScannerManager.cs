using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Camera))]
public class MineralScannerManager : MonoBehaviour
{
    public static MineralScannerManager Instance { get; private set; }

    [Header("Ссылки")]
    [SerializeField] private SnapZone targetSnapZone;
    [SerializeField] private Camera mineralCamera;
    [SerializeField] private Renderer screenRenderer;

    [Header("Эффекты")]
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private Color emissionColor = new Color(0.1f, 0.8f, 1f);

    public UnityEvent<GrabbableItem> OnMineralScanned;
    public UnityEvent OnMineralRemoved;

    private Material screenMaterial;
    private bool wasOccupied;
    private const string EMISSION_PROP = "_EmissionColor";

    private void Awake()
    {
        Instance = this;
        mineralCamera ??= GetComponent<Camera>();

        if (screenRenderer != null)
        {
            screenMaterial = screenRenderer.material;
            if (screenMaterial.HasProperty(EMISSION_PROP))
                screenMaterial.EnableKeyword("_EMISSION");
        }

        mineralCamera.enabled = false;
    }

    private void OnDestroy() => Instance = null;

    private void Start()
    {
        if (targetSnapZone != null)
            wasOccupied = targetSnapZone.IsOccupied;
    }

    private void Update()
    {
        if (targetSnapZone == null) return;

        bool occupied = targetSnapZone.IsOccupied;
        if (occupied != wasOccupied)
        {
            if (occupied) TurnOnScreen();
            else TurnOffScreen();
            wasOccupied = occupied;
        }
    }

    private void TurnOnScreen()
    {
        mineralCamera.enabled = true;
        if (screenMaterial != null) StartCoroutine(FadeEmission(1f));
        if (targetSnapZone.IsOccupied)
            OnMineralScanned?.Invoke(targetSnapZone.GetComponentInChildren<GrabbableItem>());
    }

    private void TurnOffScreen()
    {
        if (screenMaterial != null) StartCoroutine(FadeEmission(0f));
        mineralCamera.enabled = false;
        OnMineralRemoved?.Invoke();
    }

    private System.Collections.IEnumerator FadeEmission(float target)
    {
        if (screenMaterial == null) yield break;

        Color start = screenMaterial.GetColor(EMISSION_PROP);
        Color end = emissionColor * Mathf.LinearToGammaSpace(target);
        end.a = 1f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            screenMaterial.SetColor(EMISSION_PROP, Color.Lerp(start, end, t));
            yield return null;
        }
        screenMaterial.SetColor(EMISSION_PROP, end);
    }

    public MineralData CurrentMineral =>
        targetSnapZone != null && targetSnapZone.IsOccupied
            ? targetSnapZone.GetComponentInChildren<MineralData>()
            : null;
}