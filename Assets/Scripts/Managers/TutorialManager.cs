using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using UnityEngine.Events;

public class TutorialManager : MonoBehaviour, ISaveableV2, IHasTutorialData, ILocalizable
{
    public static TutorialManager Instance { get; private set; }

    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private float holdAfterSuccess = 1.8f;

    [Header("=== ДЕВ-СКИПЫ ===")]
    [SerializeField] private bool skipToResearchTableOnStart = false;

    private readonly string[] hintKeys = new[]
    {
        "TUT_LOOK",           // 0
        "TUT_MOVE",           // 1
        "TUT_DOOR",           // 2
        "TUT_VEHICLE",        // 3
        "TUT_FLARE",          // 4
        "TUT_BREAK",          // 5
        "TUT_CARRY",          // 6
        "TUT_RETURN",         // 7
        "TUT_TABLE",          // 8
        "TUT_SCAN_MOVE",      // 9
        "TUT_SCAN_CLICK",     // 10
        "TUT_ACCURACY",       // 11
        "TUT_FIND_MORE",      // 12
        "TUT_CONCLUSION",     // 13  ← показывается ТОЛЬКО ОДИН РАЗ!
        "TUT_ANOMALY_PLACE",  // 14
        "TUT_GO_TO_BED"       // 15
    };
    public bool HasPlayedIntroMonologue { get; set; } = false;
    public bool HasPlayedReturnMonologue { get; set; } = false;
    public bool HasPlayedFinalMonologue { get; set; } = false;
    public bool HasPlayedMorningDay2 { get; set; } = false;
    public bool HasPlayedMorningDay3 { get; set; } = false;
    [SerializeField] private Transform baseReturnPoint;
    [SerializeField] private float baseReturnDistance = 30f;
   
    [Space]
    [SerializeField] public RadioMonologue radioMonologue;
    [SerializeField] public SnapZone vehicleMineralSnapZone;
    [SerializeField] private SnapZone baseResearchSnapZone;
    [SerializeField] private QuarantineBox quarantineBox;

    // ============ Состояние туториала ============
    private int step = 0;
    private float timer = 0f;
    private bool waitingHold = false;

    private bool looked = false, moved = false, doorOpened = false, vehicleEntered = false;
    private bool depositBroken = false, firstMineralPlaced = false;
    private bool flareHintActive = false, flareThrown = false;
    private bool returnedHintShown = false, researchTableHintShown = false;
    private bool firstMineralOnTable = false;
    private bool scanMoveHintShown = false, scanClickHintShown = false, accuracyHintShown = false;
    private bool showedFindTwoMore = false;
    private int researchedCount = 0;
    private MineralData currentScannedMineral;
    private Transform player;

    private bool anomalyHintShown = false;
    private bool anomalyPlaced = false;
    private bool bedHintShown = false;
    private bool playerSlept = false;
    private bool finalMonologuePlayed = false;
    private bool conclusionHintShown = false; // ← ВАЖНО: "Сделайте вывод" — только один раз!
    [SerializeField] private bool conclusionHintPermanentlyDisabled = false; // ← новое поле
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void Start()
    {
        if (skipToResearchTableOnStart)
            SkipToPutMineralOnTable();
    }

    private void OnEnable()
    {
        LocalizationManager.Register(this);
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
        StartCoroutine(SubscribeWhenReady());

        if (vehicleMineralSnapZone != null)
            vehicleMineralSnapZone.onItemSnapped.AddListener(OnMineralPlacedInVehicle);
        if (baseResearchSnapZone != null)
            baseResearchSnapZone.onItemSnapped.AddListener(OnMineralPlacedOnResearchTable);
        if (MineralScanner_Renderer.Instance != null)
        {
            MineralScanner_Renderer.Instance.SubscribeToAllThreeScanned(OnAllThreeValuesScanned);
            MineralScanner_Renderer.Instance.SubscribeToProximity(OnScannerProximityChanged);
        }
        if (quarantineBox != null)
            quarantineBox.onItemSnapped.AddListener(OnAnomalyPlacedInQuarantine);
    }

    private void OnDisable()
    {
        LocalizationManager.Unregister(this);
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;

        if (GameDayManager.Instance != null)
        {
            GameDayManager.Instance.OnAnyDepositBroken.RemoveListener(OnAnyDepositBroken_Tutorial);
            GameDayManager.Instance.OnMineralResearched.RemoveListener(OnMineralResearched);
            GameDayManager.Instance.OnAllReportsSubmitted.RemoveListener(OnAllReportsSubmitted);
        }

        if (vehicleMineralSnapZone != null)
            vehicleMineralSnapZone.onItemSnapped.RemoveListener(OnMineralPlacedInVehicle);
        if (baseResearchSnapZone != null)
            baseResearchSnapZone.onItemSnapped.RemoveListener(OnMineralPlacedOnResearchTable);
        if (MineralScanner_Renderer.Instance != null)
        {
            MineralScanner_Renderer.Instance.UnsubscribeFromAllThreeScanned(OnAllThreeValuesScanned);
            MineralScanner_Renderer.Instance.UnsubscribeFromProximity(OnScannerProximityChanged);
        }
        if (quarantineBox != null)
            quarantineBox.onItemSnapped.RemoveListener(OnAnomalyPlacedInQuarantine);
    }

    private void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

    // ==================== ЛОКАЛИЗАЦИЯ ====================
    public void Localize()
    {
        if (hintPanel == null || hintText == null || !hintPanel.activeSelf) return;

        if (step < hintKeys.Length)
        {
            string baseText = LocalizationManager.Loc(hintKeys[step]);
            hintText.text = waitingHold ? baseText + " "+ LocalizationManager.Loc("TUT_Done") : baseText;
        }
    }

    private void OnLanguageChanged(LocalizationManager.Language lang) => Localize();

    // ==================== ОСНОВНЫЕ МЕТОДЫ ====================
    public void ForceStartTutorial()
    {
        gameObject.SetActive(true);
        enabled = true;
        ResetAllFlags();
        step = 0;
        researchedCount = 0;
        finalMonologuePlayed = false;
        conclusionHintShown = false;
        ShowHintByStep(0);
    }

    private void ShowHintByStep(int idx)
    {
        if (idx >= hintKeys.Length) return;
        ShowHint(LocalizationManager.Loc(hintKeys[idx]));
    }

    private void ShowHint(string text)
    {
        if (hintPanel == null || hintText == null) return;
        hintText.text = text;
        hintPanel.SetActive(true);
    }

    private void Success()
    {
        waitingHold = true;
        timer = 0f;
        Localize(); // автоматически добавит " Готово"
    }

    private void NextStep()
    {
        step++;
        waitingHold = false;

        switch (step)
        {
            case 4:
                // НИЧЕГО НЕ ДЕЛАЕМ! Факел показывается только по ActivateFlareHint()
                break;
            case 5: ShowHintByStep(5); break; // Ломай залежь
            case 6: ShowHintByStep(6); break; // Отнеси в снегоход
            case 7: hintPanel.SetActive(false); break;
            case 8: ShowHintByStep(7); returnedHintShown = true; break; // Вернитесь на базу
            case 9: ShowHintByStep(8); researchTableHintShown = true; HighlightFirstTwoMineralsInVehicle(); break;
            default:
                if (step < 4) ShowHintByStep(step);
                break;
        }
    }

    // ==================== UPDATE ====================
    private void Update()
    {
        if (waitingHold)
        {
            timer += Time.deltaTime;
            if (timer >= holdAfterSuccess)
                NextStep();
            return;
        }

        switch (step)
        {
            case 0:
                if (!looked && Mouse.current.delta.ReadValue().sqrMagnitude > 10f)
                { looked = true; Success(); }
                break;
            case 1:
                if (!moved && IsWASDPressed())
                { moved = true; Success(); }
                break;
            case 2:
                if (!doorOpened && IsHoldingDoor())
                { doorOpened = true; Success(); }
                break;
            case 3:
                if (!vehicleEntered && InputRouter.Instance?.CurrentController is TransportMovement)
                {
                    vehicleEntered = true;
                    Success();
                    hintPanel.SetActive(false);
                }
                break;
        }

        if (flareHintActive && !flareThrown && Keyboard.current.fKey.wasPressedThisFrame)
        {
            flareThrown = true;
            Success();
        }

        // Возврат на базу
        if (step == 8 && returnedHintShown && player != null && baseReturnPoint != null)
        {
            if (Vector3.Distance(player.position, baseReturnPoint.position) <= baseReturnDistance)
            {
                radioMonologue?.PlayReturnToBaseMonologue();
                step = 9;
            }
        }

        // Подсказка принести минерал на стол
        if (step == 9 && !researchTableHintShown && radioMonologue != null && !radioMonologue.IsPlaying)
        {
            ShowHintByStep(8);
            researchTableHintShown = true;
            HighlightFirstTwoMineralsInVehicle();
        }

        // Подсказка лечь спать — только после карантина
        if (!bedHintShown && !playerSlept && anomalyPlaced && GameDayManager.Instance != null && GameDayManager.Instance.CanSleep)
        {
            ShowHintByStep(15);
            bedHintShown = true;
        }
    }

    // ==================== СОБЫТИЯ ====================
    private IEnumerator SubscribeWhenReady()
    {
        yield return new WaitUntil(() => GameDayManager.Instance != null);
        var gdm = GameDayManager.Instance;
        gdm.OnAnyDepositBroken.AddListener(OnAnyDepositBroken_Tutorial);
        gdm.OnMineralResearched.AddListener(OnMineralResearched);
        gdm.OnAllReportsSubmitted.AddListener(OnAllReportsSubmitted);
    }

    private void OnAnyDepositBroken_Tutorial()
    {
        if (step == 5 && !depositBroken)
        {
            depositBroken = true;
            Success();
        }
    }

    private void OnMineralPlacedInVehicle(GrabbableItem item)
    {
        if (item.ItemType != GrabbableType.Mineral) return;
        if (step == 6 && !firstMineralPlaced)
        {
            firstMineralPlaced = true;
            Success();
            return;
        }
        if (step == 7 && vehicleMineralSnapZone.AttachedItemsCount >= GameDayManager.Instance.MineralsToResearch && !returnedHintShown)
        {
            ShowHintByStep(7);
            returnedHintShown = true;
            step = 8;
        }
    }

    private void OnMineralPlacedOnResearchTable(GrabbableItem item)
    {
        if (item.ItemType != GrabbableType.Mineral) return;
        var mineralData = item.GetComponentInChildren<MineralData>();
        if (mineralData == null || mineralData.isResearched) return;

        mineralData.EnableTutorialOutline(false);
        currentScannedMineral = mineralData;

        if (!firstMineralOnTable)
        {
            firstMineralOnTable = true;
            ShowHintByStep(9); // Крутите джойстик...
            scanMoveHintShown = true;
        }
    }

    private void OnScannerProximityChanged(float proximity)
    {
        if (!firstMineralOnTable || !scanMoveHintShown || scanClickHintShown) return;
        if (proximity >= 0.6f)
        {
            ShowHintByStep(10);
            scanMoveHintShown = false;
            scanClickHintShown = true;
        }
    }

    private void OnAllThreeValuesScanned()
    {
        Debug.Log("3 точки!");
        if (currentScannedMineral == null || currentScannedMineral.isResearched) return;

        bool allScanned = !string.IsNullOrEmpty(currentScannedMineral.savedAgeLine) &&
                          !string.IsNullOrEmpty(currentScannedMineral.savedRadioactivityLine) &&
                          !string.IsNullOrEmpty(currentScannedMineral.savedCrystalLine);

        if (allScanned)
        {
            // === ПОДСКАЗКА "СДЕЛАЙТЕ ВЫВОД" — ТОЛЬКО ОДИН РАЗ ЗА ВЕСЬ ТУТОРИАЛ ===
            if (!conclusionHintShown)
            {
                ShowHintByStep(13);
                conclusionHintShown = true; // ← теперь сохранится навсегда
            }

            // === ФИНАЛЬНЫЙ МОНОЛОГ — ТОЛЬКО ДЛЯ АНОМАЛЬНОГО (ПОСЛЕДНЕГО) МИНЕРАЛА ===
            if (currentScannedMineral.isLastInTutorialQueue &&      // это последний в очереди
                !finalMonologuePlayed &&
                radioMonologue != null)
            {
                radioMonologue.PlayFinalTutorialMonologue();
                finalMonologuePlayed = true;
            }
        }
        else
        {
            // Если игрок убрал сканер до завершения — убираем подсказку "отправьте отчёт"
            if (hintPanel.activeSelf && hintText.text.Contains(LocalizationManager.Loc("TUT_CONCLUSION")))
            {
                hintPanel.SetActive(false);
            }
        }
    }
    // Вызывается, когда игрок впервые в жизни отправил отчёт (любой)
    public void OnReportEverSubmitted()
    {
        

        conclusionHintShown = true;

        // Сразу убираем подсказку "Сделайте вывод", если она сейчас на экране
        if (hintPanel != null && hintPanel.activeSelf)
        {
            string currentHint = hintText.text;
            string conclusionText = LocalizationManager.Loc("TUT_CONCLUSION");
            if (currentHint.Contains(conclusionText))
            {
                hintPanel.SetActive(false);
            }
        }
    }
    public void OnRecordButtonPressed()
    {
        if (currentScannedMineral == null) return;

        int scanned = 0;
        if (!string.IsNullOrEmpty(currentScannedMineral.savedAgeLine)) scanned++;
        if (!string.IsNullOrEmpty(currentScannedMineral.savedRadioactivityLine)) scanned++;
        if (!string.IsNullOrEmpty(currentScannedMineral.savedCrystalLine)) scanned++;

        if (!accuracyHintShown)
        {
            ShowHintByStep(11);
            accuracyHintShown = true;
            StartCoroutine(HideHintAfter(3f));
            return;
        }

        if (scanned == 1 && !showedFindTwoMore)
        {
            ShowHintByStep(12);
            showedFindTwoMore = true;
            StartCoroutine(HideHintAfter(3f));
        }
    }

    private void OnMineralResearched(MineralData mineral)
    {
        researchedCount++;
        if (researchedCount >= 2 && vehicleMineralSnapZone != null)
        {
            foreach (var item in vehicleMineralSnapZone.attachedItems)
            {
                var md = item.GetComponentInChildren<MineralData>();
                if (md != null && md.isLastInTutorialQueue)
                    md.EnableTutorialOutline(true);
            }
        }
    }
    
    private void OnAllReportsSubmitted()
    {
        if (!bedHintShown && !playerSlept && anomalyPlaced)
        {
            ShowHintByStep(15);
            bedHintShown = true;
        }
    }

    public void OnAnomalyReportSubmitted()
    {
        if (anomalyHintShown || anomalyPlaced || currentScannedMineral == null) return;

        if (currentScannedMineral.isAnomaly && radioMonologue != null && radioMonologue.HasPlayedFinalMonologue)
        {
            ShowHintByStep(14);
            anomalyHintShown = true;
        }
    }

    private void OnAnomalyPlacedInQuarantine(GrabbableItem item)
    {
        var mineralData = item.GetComponentInChildren<MineralData>();
        if (mineralData != null && mineralData.isAnomaly && !anomalyPlaced)
        {
            anomalyPlaced = true;
            Success();
            hintPanel.SetActive(false);
        }
    }

    public void OnPlayerSlept()
    {
        playerSlept = true;
        if (hintPanel != null && hintPanel.activeSelf)
            hintPanel.SetActive(false);
        Success();
    }

    // Внешний вызов — например, из триггера в тёмной зоне
    public void ActivateFlareHint()
    {
        if (flareHintActive || step != 4) return;
        flareHintActive = true;
        ShowHintByStep(4);
    }

    private void HighlightFirstTwoMineralsInVehicle()
    {
        if (vehicleMineralSnapZone == null) return;
        foreach (var item in vehicleMineralSnapZone.attachedItems)
        {
            var md = item.GetComponentInChildren<MineralData>();
            if (md != null)
                md.EnableTutorialOutline(md.isTutorialHighlighted);
        }
    }

    private IEnumerator HideHintAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (hintPanel) hintPanel.SetActive(false);
    }

    private void ResetAllFlags()
    {
        looked = moved = doorOpened = vehicleEntered = depositBroken = false;
        firstMineralPlaced = flareHintActive = flareThrown = returnedHintShown = researchTableHintShown = false;
        firstMineralOnTable = scanMoveHintShown = scanClickHintShown = accuracyHintShown = showedFindTwoMore = false;
        researchedCount = 0;
        currentScannedMineral = null;
        anomalyHintShown = anomalyPlaced = bedHintShown = playerSlept = finalMonologuePlayed = false;
        conclusionHintShown = false;
        waitingHold = false;
        if (hintPanel) hintPanel.SetActive(false);
    }

    private bool IsWASDPressed()
    {
        var kb = Keyboard.current;
        return kb != null && (kb.wKey.isPressed || kb.aKey.isPressed || kb.sKey.isPressed || kb.dKey.isPressed);
    }

    private bool IsHoldingDoor()
    {
        var canGrab = FindObjectOfType<CanGrab>();
        return canGrab != null && canGrab.IsHoldingObject() && canGrab.GetGrabbedItem()?.CompareTag("Door") == true;
    }

    // ==================== СКИПЫ ====================
    [ContextMenu("SKIP → Положите минерал на стол")]
    public void SkipToPutMineralOnTable()
    {
        ResetAllFlags();
        step = 9;
        researchedCount = 2;
        radioMonologue?.PlayReturnToBaseMonologue();
        ShowHintByStep(8);
        researchTableHintShown = true;
        HighlightFirstTwoMineralsInVehicle();
    }

    // ==================== Сохранение ====================
    public string GetUniqueID() => "TutorialSystem";
    public TutorialSaveData GetTutorialSaveData()
    {
        return new TutorialSaveData
        {
            step = step,
            researchedCount = researchedCount,

            hasPlayedIntroMonologue = HasPlayedIntroMonologue,
            hasPlayedReturnMonologue = HasPlayedReturnMonologue,
            hasPlayedFinalMonologue = HasPlayedFinalMonologue,
            hasPlayedMorningDay2 = HasPlayedMorningDay2,
            hasPlayedMorningDay3 = HasPlayedMorningDay3,

            flareHintActive = flareHintActive,
            flareThrown = flareThrown,
            anomalyPlaced = anomalyPlaced,
            playerSlept = playerSlept,

            // === СОХРАНЯЕМ ВСЕ ПОДСКАЗКИ ===
            hintShown_Look = looked,
            hintShown_Move = moved,
            hintShown_Door = doorOpened,
            hintShown_Vehicle = vehicleEntered,
            hintShown_Flare = flareHintActive && flareThrown,
            hintShown_Break = depositBroken,
            hintShown_Carry = firstMineralPlaced,
            hintShown_Return = returnedHintShown,
            hintShown_Table = researchTableHintShown,
            hintShown_ScanMove = scanMoveHintShown,
            hintShown_ScanClick = scanClickHintShown,
            hintShown_Accuracy = accuracyHintShown,
            hintShown_FindMore = showedFindTwoMore,
            hintShown_Conclusion = conclusionHintShown,
            hintShown_AnomalyPlace = anomalyHintShown,
            hintShown_GoToBed = bedHintShown
        };
    }

    public void LoadTutorialSaveData(TutorialSaveData data)
    {
        if (data == null) return;

        ResetAllFlags();

        step = data.step;
        researchedCount = data.researchedCount;

        HasPlayedIntroMonologue = data.hasPlayedIntroMonologue;
        HasPlayedReturnMonologue = data.hasPlayedReturnMonologue;
        HasPlayedFinalMonologue = data.hasPlayedFinalMonologue;
        HasPlayedMorningDay2 = data.hasPlayedMorningDay2;
        HasPlayedMorningDay3 = data.hasPlayedMorningDay3;

        flareHintActive = data.flareHintActive;
        flareThrown = data.flareThrown;
        anomalyPlaced = data.anomalyPlaced;
        playerSlept = data.playerSlept;

        // === ВОССТАНАВЛИВАЕМ ФЛАГИ ПОДСКАЗОК ===
        looked = data.hintShown_Look;
        moved = data.hintShown_Move;
        doorOpened = data.hintShown_Door;
        vehicleEntered = data.hintShown_Vehicle;
        depositBroken = data.hintShown_Break;
        firstMineralPlaced = data.hintShown_Carry;
        returnedHintShown = data.hintShown_Return;
        researchTableHintShown = data.hintShown_Table;
        scanMoveHintShown = data.hintShown_ScanMove;
        scanClickHintShown = data.hintShown_ScanClick;
        accuracyHintShown = data.hintShown_Accuracy;
        showedFindTwoMore = data.hintShown_FindMore;
        conclusionHintShown = data.hintShown_Conclusion;
        anomalyHintShown = data.hintShown_AnomalyPlace;
        bedHintShown = data.hintShown_GoToBed;

        // Восстанавливаем монологи в радио
        if (radioMonologue != null)
        {
            radioMonologue.HasPlayedIntroMonologue = data.hasPlayedIntroMonologue;
            radioMonologue.HasPlayedReturnMonologue = data.hasPlayedReturnMonologue;
            radioMonologue.HasPlayedFinalMonologue = data.hasPlayedFinalMonologue;
        }

        if (playerSlept || conclusionHintShown)
        {
            gameObject.SetActive(false);
            hintPanel?.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        enabled = true;

        // Важно: НЕ показываем подсказку, если она уже была показана!
        if (step < hintKeys.Length)
        {
            bool shouldShow = step switch
            {
                0 => !looked,
                1 => !moved,
                2 => !doorOpened,
                3 => !vehicleEntered,
                4 => flareHintActive && !data.hintShown_Flare,
                5 => !depositBroken,
                6 => !firstMineralPlaced,
                7 => !returnedHintShown,
                8 => !researchTableHintShown,
                9 => !scanMoveHintShown,
                10 => !scanClickHintShown,
                11 => !accuracyHintShown,
                12 => !showedFindTwoMore,
                13 => !conclusionHintShown,
                14 => !anomalyHintShown,
                15 => !bedHintShown,
                _ => true
            };

            if (shouldShow)
                ShowHintByStep(step);
            else
                hintPanel?.SetActive(false);
        }
    }

    public bool CanGrabAnyMineralFromVehicle()
    {
        return radioMonologue != null && radioMonologue.HasPlayedReturnMonologue;
    }

    public bool CanGrabLastTutorialMineral()
    {
        return researchedCount >= 2;
    }

    public bool CanGrabMineralFromVehicle(GrabbableItem item)
    {
        if (!CanGrabAnyMineralFromVehicle()) return false;
        var mineralData = item.GetComponentInChildren<MineralData>();
        if (mineralData != null && mineralData.isLastInTutorialQueue)
            return CanGrabLastTutorialMineral();
        return true;
    }

    public ObjectSaveData GetCommonSaveData() => new ObjectSaveData { uniqueID = GetUniqueID(), isActive = gameObject.activeSelf };
    public void LoadCommonData(ObjectSaveData data) => gameObject.SetActive(data.isActive);
}