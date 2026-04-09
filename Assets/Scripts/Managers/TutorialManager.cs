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
    [SerializeField] private bool skipSnowmobile = false;

    [Header("=== ТЕКСТЫ НОВЫХ ПОДСКАЗОК (заполните вручную) ===")]
    [SerializeField] private string customBaseReturnedHintText = "Вернулись на базу! Теперь сделайте что-то...";
    [SerializeField] private string customPreAnomalyHintText = "Скоро нужно будет поместить аномалию в специальный ящик...";

    [Header("=== ОБЪЕКТЫ ДЛЯ АКТИВАЦИИ ПРИ ПОДСКАЗКАХ ===")]
    [SerializeField] private GameObject objectForFirstNewHint;   // активируется при показе подсказки шага 8
    [SerializeField] private GameObject objectForSecondNewHint;  // активируется при показе подсказки шага 15

    private readonly string[] hintKeys = new[]
    {
        "TUT_LOOK",                // 0
        "TUT_MOVE",                // 1
        "TUT_DOOR",                // 2
        "TUT_VEHICLE",             // 3
        "TUT_FLARE",               // 4
        "TUT_BREAK",               // 5
        "TUT_CARRY",               // 6
        "TUT_RETURN",              // 7
        "",                        // 8 - используется customBaseReturnedHintText
        "TUT_TABLE",               // 9
        "TUT_SCAN_MOVE",           // 10
        "TUT_SCAN_CLICK",          // 11
        "TUT_ACCURACY",            // 12
        "TUT_FIND_MORE",           // 13
        "TUT_CONCLUSION",          // 14
        "",                        // 15 - используется customPreAnomalyHintText
        "TUT_ANOMALY_PLACE",       // 16
        "TUT_GO_TO_BED"            // 17
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

    private int step = 0;
    private float timer = 0f;
    private bool waitingHold = false;
    private bool successScheduled = false;

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
    private bool conclusionHintShown = false;

    private bool baseReturnedHintCompleted = false;
    private bool preAnomalyHintCompleted = false;

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
        if (skipSnowmobile && step == 3)
        {
            step = 4;
            ShowHintByStep(4);
        }
    }

    private void OnEnable()
    {
        LocalizationManager.Register(this);
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
        StartCoroutine(SubscribeWhenReady());

        CassettePlayer.OnCassete2Play += OnCassette2Played;
        CassettePlayer.OnCassete3Play += OnCassette3Played;
        RadioMonologue.OnMonologueEnded += OnRadioMonologueEnded;

        if (!skipSnowmobile && vehicleMineralSnapZone != null)
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

        CassettePlayer.OnCassete2Play -= OnCassette2Played;
        CassettePlayer.OnCassete3Play -= OnCassette3Played;
        RadioMonologue.OnMonologueEnded -= OnRadioMonologueEnded;

        if (GameDayManager.Instance != null)
        {
            GameDayManager.Instance.OnAnyDepositBroken.RemoveListener(OnAnyDepositBroken_Tutorial);
            GameDayManager.Instance.OnMineralResearched.RemoveListener(OnMineralResearched);
            GameDayManager.Instance.OnAllReportsSubmitted.RemoveListener(OnAllReportsSubmitted);
        }

        if (!skipSnowmobile && vehicleMineralSnapZone != null)
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

    public void Localize()
    {
        if (hintPanel == null || hintText == null || !hintPanel.activeSelf) return;
        if (step < hintKeys.Length)
        {
            string baseText;
            if (step == 8 && !string.IsNullOrEmpty(customBaseReturnedHintText))
                baseText = customBaseReturnedHintText;
            else if (step == 15 && !string.IsNullOrEmpty(customPreAnomalyHintText))
                baseText = customPreAnomalyHintText;
            else
                baseText = LocalizationManager.Loc(hintKeys[step]);
            hintText.text = waitingHold ? baseText + " " + LocalizationManager.Loc("TUT_Done") : baseText;
        }
    }

    private void OnLanguageChanged(LocalizationManager.Language lang) => Localize();

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
        if (skipSnowmobile && idx == 3) return;
        if (idx >= hintKeys.Length) return;

        string text;
        if (idx == 8 && !string.IsNullOrEmpty(customBaseReturnedHintText))
            text = customBaseReturnedHintText;
        else if (idx == 15 && !string.IsNullOrEmpty(customPreAnomalyHintText))
            text = customPreAnomalyHintText;
        else
            text = LocalizationManager.Loc(hintKeys[idx]);

        ShowHint(text);

        // Активация объектов при показе соответствующих подсказок
        if (idx == 8 && objectForFirstNewHint != null)
        {
            objectForFirstNewHint.SetActive(true);
        }
        if (idx == 15 && objectForSecondNewHint != null)
        {
            objectForSecondNewHint.SetActive(true);
        }
    }

    private void ShowHint(string text)
    {
        if (hintPanel == null || hintText == null) return;
        hintText.text = text;
        hintPanel.SetActive(true);
    }

    private void Success()
    {
        if (successScheduled) return;
        successScheduled = true;
        waitingHold = true;
        timer = 0f;
        Localize();
    }

    private void NextStep()
    {
        waitingHold = false;
        successScheduled = false;
        step++;

        if (skipSnowmobile && step == 3)
        {
            step = 4;
            if (step == 4 && !flareHintActive)
            {
                hintPanel.SetActive(false);
            }
        }

        TryCompleteCurrentStep();

        if (!waitingHold && step < hintKeys.Length)
        {
            switch (step)
            {
                case 4:
                    break;
                case 5:
                    if (!depositBroken) ShowHintByStep(5);
                    break;
                case 6:
                    if (skipSnowmobile)
                    {
                        if (depositBroken && !returnedHintShown)
                        {
                            step = 7;
                            ShowHintByStep(7);
                            returnedHintShown = true;
                        }
                    }
                    else if (!firstMineralPlaced) ShowHintByStep(6);
                    break;
                case 7:
                    hintPanel.SetActive(false);
                    break;
                case 8:
                    if (!baseReturnedHintCompleted) ShowHintByStep(8);
                    break;
                case 9:
                    if (!researchTableHintShown)
                    {
                        ShowHintByStep(9);
                        researchTableHintShown = true;
                        HighlightFirstTwoMineralsInVehicle();
                    }
                    break;
                default:
                    if (step < 4) ShowHintByStep(step);
                    break;
            }
        }
    }

    private void TryCompleteCurrentStep()
    {
        if (waitingHold) return;

        switch (step)
        {
            case 5:
                if (depositBroken) Success();
                break;
            case 6:
                if (skipSnowmobile && depositBroken)
                {
                    Success();
                    step = 7;
                }
                else if (!skipSnowmobile && firstMineralPlaced) Success();
                break;
            case 7:
                if (skipSnowmobile)
                {
                    Success();
                    step = 8;
                }
                else if (vehicleMineralSnapZone != null &&
                    vehicleMineralSnapZone.AttachedItemsCount >= GameDayManager.Instance.MineralsToResearch)
                {
                    ShowHintByStep(7);
                    returnedHintShown = true;
                    step = 8;
                    TryCompleteCurrentStep();
                }
                break;
            case 8:
                // Условие выполняется через OnCassette2Played
                break;
            case 9:
                if (baseResearchSnapZone != null && baseResearchSnapZone.AttachedItemsCount > 0)
                {
                    if (!firstMineralOnTable)
                    {
                        firstMineralOnTable = true;
                        ShowHintByStep(10);
                        scanMoveHintShown = true;
                    }
                }
                break;
            case 15:
                // Условие выполняется через OnCassette3Played
                break;
        }
    }

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
                if (!skipSnowmobile && !vehicleEntered && InputRouter.Instance?.CurrentController is TransportMovement)
                { vehicleEntered = true; Success(); hintPanel.SetActive(false); }
                else if (skipSnowmobile)
                {
                    Success();
                    if (hintPanel != null) hintPanel.SetActive(false);
                }
                break;
        }

        if (flareHintActive && !flareThrown && Keyboard.current.fKey.wasPressedThisFrame)
        {
            flareThrown = true;
            Success();
        }

        if ((step == 7 || step == 8) && !baseReturnedHintCompleted && player != null && baseReturnPoint != null)
        {
            if (Vector3.Distance(player.position, baseReturnPoint.position) <= baseReturnDistance)
            {
                radioMonologue?.PlayReturnToBaseMonologue();
                if (step == 7)
                {
                    step = 8;
                    TryCompleteCurrentStep();
                }
                if (!baseReturnedHintCompleted)
                {
                    ShowHintByStep(8);
                }
            }
        }

        if (!bedHintShown && !playerSlept && anomalyPlaced && GameDayManager.Instance != null && GameDayManager.Instance.CanSleep)
        {
            ShowHintByStep(17);
            bedHintShown = true;
        }
    }

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
        if (depositBroken) return;
        depositBroken = true;
        if (step == 5) Success();
        if (skipSnowmobile && step == 6)
            Success();
    }

    private void OnMineralPlacedInVehicle(GrabbableItem item)
    {
        if (skipSnowmobile) return;
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
            TryCompleteCurrentStep();
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
            ShowHintByStep(10);
            scanMoveHintShown = true;
        }
    }

    private void OnScannerProximityChanged(float proximity)
    {
        if (!firstMineralOnTable || !scanMoveHintShown || scanClickHintShown) return;
        if (proximity >= 0.6f)
        {
            ShowHintByStep(11);
            scanMoveHintShown = false;
            scanClickHintShown = true;
        }
    }

    private void OnAllThreeValuesScanned()
    {
        if (currentScannedMineral == null || currentScannedMineral.isResearched) return;
        bool allScanned = !string.IsNullOrEmpty(currentScannedMineral.savedAgeLine) &&
                          !string.IsNullOrEmpty(currentScannedMineral.savedRadioactivityLine) &&
                          !string.IsNullOrEmpty(currentScannedMineral.savedCrystalLine);
        if (allScanned)
        {
            if (!conclusionHintShown)
            {
                ShowHintByStep(14);
                conclusionHintShown = true;
            }
            if (currentScannedMineral.isLastInTutorialQueue && !finalMonologuePlayed && radioMonologue != null)
            {
                radioMonologue.PlayFinalTutorialMonologue();
                finalMonologuePlayed = true;
            }
        }
        else
        {
            if (hintPanel.activeSelf && hintText.text.Contains(LocalizationManager.Loc("TUT_CONCLUSION")))
                hintPanel.SetActive(false);
        }
    }

    public void OnReportEverSubmitted()
    {
        conclusionHintShown = true;
        if (hintPanel != null && hintPanel.activeSelf)
        {
            string currentHint = hintText.text;
            string conclusionText = LocalizationManager.Loc("TUT_CONCLUSION");
            if (currentHint.Contains(conclusionText))
                hintPanel.SetActive(false);
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
            ShowHintByStep(12);
            accuracyHintShown = true;
            StartCoroutine(HideHintAfter(3f));
            return;
        }
        if (scanned == 1 && !showedFindTwoMore)
        {
            ShowHintByStep(13);
            showedFindTwoMore = true;
            StartCoroutine(HideHintAfter(3f));
        }
    }

    private void OnMineralResearched(MineralData mineral)
    {
        researchedCount++;
        if (researchedCount >= 2 && vehicleMineralSnapZone != null && !skipSnowmobile)
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
            ShowHintByStep(17);
            bedHintShown = true;
        }
    }

    public void OnAnomalyReportSubmitted()
    {
        // Больше не используется
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

    public void ActivateFlareHint()
    {
        if (flareHintActive || step != 4) return;
        flareHintActive = true;
        ShowHintByStep(4);
    }

    private void HighlightFirstTwoMineralsInVehicle()
    {
        if (skipSnowmobile || vehicleMineralSnapZone == null) return;
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
        successScheduled = false;
        baseReturnedHintCompleted = false;
        preAnomalyHintCompleted = false;
        if (hintPanel) hintPanel.SetActive(false);

        // Деактивируем объекты подсказок при сбросе
        if (objectForFirstNewHint != null) objectForFirstNewHint.SetActive(false);
        if (objectForSecondNewHint != null) objectForSecondNewHint.SetActive(false);
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

    private void OnCassette2Played()
    {
        if (step == 8 && !baseReturnedHintCompleted)
        {
            baseReturnedHintCompleted = true;
            Success();
        }
    }

    private void OnCassette3Played()
    {
        if (step == 15 && !waitingHold)
        {
            Success();
        }
    }

    private void OnRadioMonologueEnded(int monologueIndex)
    {
        if (monologueIndex == 2 && !preAnomalyHintCompleted)
        {
            preAnomalyHintCompleted = true;
            ShowHintByStep(15);
        }
    }

    [ContextMenu("SKIP → Положите минерал на стол")]
    public void SkipToPutMineralOnTable()
    {
        ResetAllFlags();
        step = 9;
        researchedCount = 2;
        radioMonologue?.PlayReturnToBaseMonologue();
        ShowHintByStep(9);
        researchTableHintShown = true;
        if (!skipSnowmobile)
            HighlightFirstTwoMineralsInVehicle();
    }

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
            hintShown_GoToBed = bedHintShown,
            baseReturnedHintCompleted = baseReturnedHintCompleted,
            preAnomalyHintCompleted = preAnomalyHintCompleted
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
        baseReturnedHintCompleted = data.baseReturnedHintCompleted;
        preAnomalyHintCompleted = data.preAnomalyHintCompleted;

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
        TryCompleteCurrentStep();
        if (!waitingHold && step < hintKeys.Length && !hintPanel.activeSelf)
            ShowHintByStep(step);
    }

    public bool CanGrabAnyMineralFromVehicle() => radioMonologue != null && !skipSnowmobile;
    public bool CanGrabLastTutorialMineral() => researchedCount >= 2;
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

[System.Serializable]
public class TutorialSaveData
{
    public int step;
    public int researchedCount;
    public bool hasPlayedIntroMonologue;
    public bool hasPlayedReturnMonologue;
    public bool hasPlayedFinalMonologue;
    public bool hasPlayedMorningDay2;
    public bool hasPlayedMorningDay3;
    public bool flareHintActive;
    public bool flareThrown;
    public bool anomalyPlaced;
    public bool playerSlept;
    public bool hintShown_Look;
    public bool hintShown_Move;
    public bool hintShown_Door;
    public bool hintShown_Vehicle;
    public bool hintShown_Flare;
    public bool hintShown_Break;
    public bool hintShown_Carry;
    public bool hintShown_Return;
    public bool hintShown_Table;
    public bool hintShown_ScanMove;
    public bool hintShown_ScanClick;
    public bool hintShown_Accuracy;
    public bool hintShown_FindMore;
    public bool hintShown_Conclusion;
    public bool hintShown_AnomalyPlace;
    public bool hintShown_GoToBed;
    public bool baseReturnedHintCompleted;
    public bool preAnomalyHintCompleted;
}