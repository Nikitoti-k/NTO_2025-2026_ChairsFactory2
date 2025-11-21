using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Camera))]
public class MineralScannerManager : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private SnapZone targetSnapZone;
    [SerializeField] private Camera mineralCamera;              // Доп. камера с Render Texture
    [SerializeField] private Renderer screenRenderer;           // Quad с материалом экрана (для fade/glow)
    [SerializeField] private Material screenMaterial;           // Копия материала (чтобы не ломать общий)

    [Header("Настройки эффектов")]
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private Color emissionColor = new Color(0.1f, 0.8f, 1f, 1f);
    [SerializeField] private bool useEmissionGlow = true;

    // События для будущих PointsContainer и т.д.
    public UnityEvent<GrabbableItem> OnMineralScanned;
    public UnityEvent OnMineralRemoved;

    private bool wasOccupied = false;
    private const string EMISSION_COLOR_PROP = "_EmissionColor";

    private void Awake()
    {
        if (mineralCamera == null)
            mineralCamera = GetComponent<Camera>();

        // Делаем инстанс материала, чтобы не менять общий
        if (screenRenderer != null && screenMaterial == null)
        {
            screenMaterial = screenRenderer.material;
            if (useEmissionGlow && screenMaterial.HasProperty(EMISSION_COLOR_PROP))
                screenMaterial.EnableKeyword("_EMISSION");
        }

        TurnOffScreenImmediate();
    }

    private void Start()
    {
        // Автопоиск для прототипа (если не назначил в инспекторе)
        if (targetSnapZone == null)
            targetSnapZone = FindObjectOfType<SnapZone>();

        if (targetSnapZone != null)
            wasOccupied = targetSnapZone.IsOccupied;
    }

    private void Update()
    {
        if (targetSnapZone == null) return;

        bool currentlyOccupied = targetSnapZone.IsOccupied;

        if (currentlyOccupied != wasOccupied)
        {
            if (currentlyOccupied)
                TurnOnScreen();
            else
                TurnOffScreen();

            wasOccupied = currentlyOccupied;
        }
    }

    private void TurnOnScreen()
    {
        mineralCamera.enabled = true;

        if (screenMaterial != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeEmission(1f));
        }

        // Если у тебя в будущем будет MineralPointsContainer — вызывай сюда
        if (targetSnapZone.IsOccupied)
            OnMineralScanned?.Invoke(targetSnapZone.GetComponentInChildren<GrabbableItem>());
    }

    private void TurnOffScreen()
    {
        if (screenMaterial != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeEmission(0f));
        }

        mineralCamera.enabled = false;
        OnMineralRemoved?.Invoke();
    }

    private void TurnOffScreenImmediate()
    {
        mineralCamera.enabled = false;
        if (screenMaterial != null && screenMaterial.HasProperty(EMISSION_COLOR_PROP))
            screenMaterial.SetColor(EMISSION_COLOR_PROP, Color.black);
    }

    private System.Collections.IEnumerator FadeEmission(float target)
    {
        if (!screenMaterial.HasProperty(EMISSION_COLOR_PROP)) yield break;

        Color startColor = screenMaterial.GetColor(EMISSION_COLOR_PROP);
        Color endColor = emissionColor * Mathf.LinearToGammaSpace(target);
        endColor.a = 1f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            Color lerped = Color.Lerp(startColor, endColor, t);
            screenMaterial.SetColor(EMISSION_COLOR_PROP, lerped);
            yield return null;
        }

        screenMaterial.SetColor(EMISSION_COLOR_PROP, endColor);
    }

    // Внешний вызов (если захочешь из SnapZone явно)
    public void ForceScanUpdate()
    {
        wasOccupied = !targetSnapZone.IsOccupied;
        // Update сработает в следующем кадре
    }

#if UNITY_EDITOR
    private void Reset()
    {
        mineralCamera = GetComponent<Camera>();
        screenRenderer = GetComponentInParent<Renderer>();
    }
#endif
}