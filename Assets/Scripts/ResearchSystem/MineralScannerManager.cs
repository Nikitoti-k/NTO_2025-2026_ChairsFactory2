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
           
            wasOccupied = occupied;
        }
    }

    public MineralData CurrentMineral =>
        targetSnapZone != null && targetSnapZone.IsOccupied
            ? targetSnapZone.GetComponentInChildren<MineralData>()
            : null;
}