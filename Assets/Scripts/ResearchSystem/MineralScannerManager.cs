using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class MineralScannerManager : MonoBehaviour
{
    public static MineralScannerManager Instance { get; private set; }

    [Header("Ссылки")]
    [SerializeField] public SnapZone targetSnapZone;
    [SerializeField] private Camera mineralCamera;
    [SerializeField] private Renderer screenRenderer;

    public UnityEvent<GameObject> OnMineralScanned;
    public UnityEvent OnMineralRemoved;

    private Material screenMaterial;
    private bool wasOccupied = false;

    
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
        if (targetSnapZone != null)
            wasOccupied = targetSnapZone.IsOccupied;
    }

    private void Update()
    {
        if (targetSnapZone == null) return;

        bool occupied = targetSnapZone.IsOccupied;
        GameObject snapped = targetSnapZone.CurrentSnappedObject;

        Debug.Log($"[Scanner Debug] IsOccupied: {occupied}, SnappedObject: {(snapped != null ? snapped.name : "null")}, WasOccupied: {wasOccupied}");

        if (occupied && !wasOccupied)
        {
            Debug.Log($"[Scanner] ВКЛЮЧАЕМ! Минерал: {snapped.name}, MineralData: {snapped.GetComponentInChildren<MineralData>() != null}");
            TurnOnScreen();
        }
        else if (!occupied && wasOccupied)
        {
            Debug.Log($"[Scanner] ВЫКЛЮЧАЕМ!");
            TurnOffScreen();
        }
        wasOccupied = occupied;
    }

    private void TurnOnScreen()
    {
        mineralCamera.enabled = true;

        GameObject mineralObject = targetSnapZone.CurrentSnappedObject;
        if (mineralObject != null)
        {
            var mineralData = mineralObject.GetComponentInChildren<MineralData>();
            if (mineralData != null)
            {
                string uniqueID = mineralData.UniqueInstanceID;

                
                if (broughtTodayMineralIDs.Add(uniqueID))
                {
                    GameDayManager.Instance.RegisterMineralBrought(mineralObject);
                    Debug.Log($"<color=purple>[Scanner] Новый минерал принесён: {mineralObject.name}</color>");
                }
                else
                {
                    Debug.Log($"<color=gray>[Scanner] Этот минерал уже был принесён сегодня — не считаем повторно</color>");
                }
            }
        }

        OnMineralScanned?.Invoke(mineralObject);
    }
    // Добавь в конец класса MineralScannerManager:
    public void ForceScanCurrentMineral()
    {
        if (targetSnapZone == null || !targetSnapZone.IsOccupied) return;

        GameObject mineralObject = targetSnapZone.CurrentSnappedObject;
        if (mineralObject == null) return;

        // Принудительно включаем экран
        mineralCamera.enabled = true;
        wasOccupied = true;

        // Имитируем событие "минерал вставлен"
        OnMineralScanned?.Invoke(mineralObject);

        // Уведомляем MineralScanner_Renderer вручную
        MineralScanner_Renderer.Instance?.OnMineralPlaced(mineralObject);

        Debug.Log("<color=green>[Scanner] Принудительно включён сканер — минерал был в слоте при загрузке!</color>");
    }
    private void TurnOffScreen()
    {
        mineralCamera.enabled = false;
        OnMineralRemoved?.Invoke();
    }

   
    private void OnEnable()
    {
        if (GameDayManager.Instance)
            GameDayManager.Instance.OnDayFullyCompleted.AddListener(ClearBroughtToday);
    }

    private void OnDisable()
    {
        if (GameDayManager.Instance)
            GameDayManager.Instance.OnDayFullyCompleted.RemoveListener(ClearBroughtToday);
    }

    private void ClearBroughtToday()
    {
        broughtTodayMineralIDs.Clear();
        Debug.Log("<color=yellow>[MineralScannerManager] Список принесённых минералов очищен — новый день!</color>");
    }

    public MineralData CurrentMineral
    {
        get
        {
            if (targetSnapZone == null || !targetSnapZone.IsOccupied) return null;
            return targetSnapZone.CurrentSnappedObject?.GetComponentInChildren<MineralData>();
        }
    }
}