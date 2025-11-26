using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class MineralScannerManager : MonoBehaviour
{
    public static MineralScannerManager Instance { get; private set; }
    [SerializeField] public SnapZone targetSnapZone;
    [SerializeField] private Camera mineralCamera;
    [SerializeField] private Renderer screenRenderer;

    public UnityEvent<GameObject> OnMineralScanned = new();
    public UnityEvent OnMineralRemoved = new();

    private Material screenMaterial;
    private bool wasOccupied;
    private readonly HashSet<string> broughtTodayMineralIDs = new();

    private void Awake()
    {
        Instance = this;
        mineralCamera ??= GetComponent<Camera>();
        if (screenRenderer != null)
        {
            screenMaterial = screenRenderer.material;
            if (screenMaterial.HasProperty("_EmissionColor"))
                screenMaterial.EnableKeyword("_EMISSION");
        }
        mineralCamera.enabled = false;
    }

    private void Start()
    {
        if (targetSnapZone != null) wasOccupied = targetSnapZone.IsOccupied;
    }

    private void Update()
    {
        if (targetSnapZone == null) return;
        bool occupied = targetSnapZone.IsOccupied;
        if (occupied && !wasOccupied) TurnOnScreen();
        else if (!occupied && wasOccupied) TurnOffScreen();
        wasOccupied = occupied;
    }

    private void TurnOnScreen()
    {
        mineralCamera.enabled = true;
        GameObject obj = targetSnapZone.CurrentSnappedObject;
        if (obj != null)
        {
            var mineral = obj.GetComponentInChildren<MineralData>();
            if (mineral != null && broughtTodayMineralIDs.Add(mineral.UniqueInstanceID))
                GameDayManager.Instance.RegisterMineralBrought(obj);
        }
        OnMineralScanned?.Invoke(obj);
    }

    public void ForceScanCurrentMineral()
    {
        if (targetSnapZone == null || !targetSnapZone.IsOccupied) return;
        GameObject obj = targetSnapZone.CurrentSnappedObject;
        if (obj == null) return;

        mineralCamera.enabled = true;
        wasOccupied = true;
        OnMineralScanned?.Invoke(obj);
        MineralScanner_Renderer.Instance?.OnMineralPlaced(obj);
    }

    private void TurnOffScreen()
    {
        mineralCamera.enabled = false;
        OnMineralRemoved?.Invoke();
    }

    private void OnEnable() => GameDayManager.Instance?.OnDayFullyCompleted.AddListener(ClearBroughtToday);
    private void OnDisable() => GameDayManager.Instance?.OnDayFullyCompleted.RemoveListener(ClearBroughtToday);
    private void ClearBroughtToday() => broughtTodayMineralIDs.Clear();

    public MineralData CurrentMineral => targetSnapZone != null && targetSnapZone.IsOccupied
        ? targetSnapZone.CurrentSnappedObject?.GetComponentInChildren<MineralData>()
        : null;
}