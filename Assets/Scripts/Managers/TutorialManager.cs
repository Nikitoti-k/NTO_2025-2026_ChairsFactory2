using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class TutorialManager : MonoBehaviour, ISaveableV2
{

    public static TutorialManager Instance { get; private set; }

    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private float holdAfterSuccess = 1.8f;

    [Space(10)]
    [Header("=== ДЕВ-СКИПЫ ===")]
    [SerializeField] private bool skipToResearchTableOnStart = false;

    [TextArea] public string lookText = "Осмотритесь вокруг — двигайте мышью";
    [TextArea] public string moveText = "Двигайтесь с помощью <color=#ffff00>W A S D</color>";
    [TextArea] public string doorText = "Чтобы открыть дверь — подойдите и <color=#ffff00>зажмите ЛКМ</color>";
    [TextArea] public string vehicleText = "Подойдите к снегоходу и нажмите <color=#ffff00>E</color>";
    [TextArea] public string flareText = "Бросьте факел — нажмите <color=#ffff00>F</color>";
    [TextArea] public string breakDepositText = "Ломайте ледяную залежь — <color=#ffff00>ЛКМ</color>";
    [TextArea] public string carryToVehicleText = "Отнесите добытый образец\nв <color=#ffff00>снегоход</color>";
    [TextArea] public string returnToBaseText = "Вы собрали достаточно образцов.\n<color=#00ff00>Вернитесь на базу и изучите их</color>";
    [TextArea] public string bringToTableText = "Возьмите образец и отнесите его\nна <color=#ffff00>исследовательский стол</color>";

    [Header("Сканер")]
    [TextArea] public string scanMoveHint = "Крутите джойстик на <color=#ffff00>LMB</color>, найдите активные зоны на образце!";
    [TextArea] public string scanClickHint = "Нажмите на кнопку <color=#ffff00>получить данные</color>";
    [TextArea] public string scanAccuracyHint = "Чем точнее вы выбрали точку на минерале,\nтем точнее вы получите данные.";
    [TextArea] public string findTwoMorePointsHint = "Найдите еще <color=#ffff00>2 точки</color>, чтобы собрать все данные";
    [TextArea] public string makeConclusionHint = "Сделайте вывод, к какому классу относится образец — <color=#ffff00>отправьте отчёт</color>";

    [Header("Аномалия и карантин")]
    [TextArea] public string placeAnomalyHint = "Поместите странный образец\nв <color=#ff3333>ящик для аномалий</color>";

    [Header("Финал — сон")]
    [TextArea] public string goToBedHint = "Лягте в кровать — нажмите <color=#ffff00>E</color>";

    [SerializeField] private Transform baseReturnPoint;
    [SerializeField] private float baseReturnDistance = 30f;
    [SerializeField] public RadioMonologue radioMonologue;
    [SerializeField] public SnapZone vehicleMineralSnapZone;
    [SerializeField] private SnapZone baseResearchSnapZone;
    [SerializeField] private QuarantineBox quarantineBox;

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
  

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        gameObject.SetActive(false);
        if (skipToResearchTableOnStart)
            SkipToPutMineralOnTable();
    }

    [ContextMenu("SKIP → Положите минерал на стол")]
    public void SkipToPutMineralOnTable()
    {
        ResetAllFlags();
        step = 9;
        researchedCount = 2;
        radioMonologue?.PlayReturnToBaseMonologue();
        ShowHint(bringToTableText);
        researchTableHintShown = true;
        HighlightFirstTwoMineralsInVehicle();
    }

    public void ForceStartTutorial()
    {
        gameObject.SetActive(true);
        enabled = true;
        ResetAllFlags();
        step = 0;
        researchedCount = 0;
        finalMonologuePlayed = false;
        ShowHint(lookText);
    }

    private void OnEnable()
    {
        if (GameDayManager.Instance != null)
        {
            GameDayManager.Instance.OnDepositsChanged.AddListener(OnDepositBroken);
            GameDayManager.Instance.OnMineralResearched.AddListener(OnMineralResearched);
            GameDayManager.Instance.OnAllReportsSubmitted.AddListener(OnAllReportsSubmitted);
        }
        if (vehicleMineralSnapZone != null)
            vehicleMineralSnapZone.onItemSnapped.AddListener(OnMineralPlacedInVehicle);
        if (baseResearchSnapZone != null)
            baseResearchSnapZone.onItemSnapped.AddListener(OnMineralPlacedOnResearchTable);
        if (MineralScanner_Renderer.Instance != null)
        {
            MineralScanner_Renderer.Instance.SubscribeToAllThreeScanned(OnAllThreeValuesScanned);
            MineralScanner_Renderer.Instance.SubscribeToProximity(OnScannerProximityChanged); // ← ВОССТАНАВЛИВАЕМ!
        }
        if (quarantineBox != null)
            quarantineBox.onItemSnapped.AddListener(OnAnomalyPlacedInQuarantine);
    }

    private void OnDisable()
    {
        if (GameDayManager.Instance != null)
        {
            GameDayManager.Instance.OnDepositsChanged.RemoveListener(OnDepositBroken);
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
            MineralScanner_Renderer.Instance.UnsubscribeFromProximity(OnScannerProximityChanged); // ← ВОССТАНАВЛИВАЕМ!
        }
        if (quarantineBox != null)
            quarantineBox.onItemSnapped.RemoveListener(OnAnomalyPlacedInQuarantine);
    }

    private void Update()
    {
        if (waitingHold)
        {
            timer += Time.deltaTime;
            if (timer >= holdAfterSuccess) NextStep();
            return;
        }

        switch (step)
        {
            case 0: if (!looked && Mouse.current.delta.ReadValue().sqrMagnitude > 10f) { looked = true; Success(); } break;
            case 1: if (!moved && IsWASDPressed()) { moved = true; Success(); } break;
            case 2: if (!doorOpened && IsHoldingDoor()) { doorOpened = true; Success(); } break;
            case 3:
                if (!vehicleEntered && InputRouter.Instance?.CurrentController is TransportMovement)
                {
                    vehicleEntered = true;
                    Success();
                    if (hintPanel) hintPanel.SetActive(false);
                }
                break;
        }

        if (flareHintActive && !flareThrown && Keyboard.current.fKey.wasPressedThisFrame)
        {
            flareThrown = true;
            Success();
        }

        if (step == 8 && returnedHintShown && player != null && baseReturnPoint != null)
        {
            if (Vector3.Distance(player.position, baseReturnPoint.position) <= baseReturnDistance)
            {
                radioMonologue?.PlayReturnToBaseMonologue();
                step = 9;
            }
        }

        if (step == 9 && !researchTableHintShown && radioMonologue != null && !radioMonologue.IsPlaying)
        {
            ShowHint(bringToTableText);
            researchTableHintShown = true;
            HighlightFirstTwoMineralsInVehicle();
        }

        // ← ПОДСКАЗКА СНА — ТОЛЬКО ПОСЛЕ КАРАНТИНА!
        if (!bedHintShown && !playerSlept && anomalyPlaced && GameDayManager.Instance != null && GameDayManager.Instance.CanSleep)
        {
            ShowHint(goToBedHint);
            bedHintShown = true;
        }
    }

    public void ActivateFlareHint()
    {
        if (flareHintActive || step != 4) return;
        flareHintActive = true;
        ShowHint(flareText);
    }

    private void OnDepositBroken(int count)
    {
        if (step == 5 && count >= 1 && !depositBroken)
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
            ShowHint(returnToBaseText);
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
            ShowHint(scanMoveHint);
            scanMoveHintShown = true;
        }
    }

    // ← ВОССТАНАВЛИВАЕМ ПОДСКАЗКУ ДЛЯ КНОПКИ СКАНИРОВАНИЯ
    private void OnScannerProximityChanged(float proximity)
    {
        if (!firstMineralOnTable || !scanMoveHintShown || scanClickHintShown) return;
        if (proximity >= 0.6f)
        {
            ShowHint(scanClickHint);
            scanMoveHintShown = false;
            scanClickHintShown = true;
        }
    }

    private void OnAllThreeValuesScanned()
    {
        if (currentScannedMineral == null || currentScannedMineral.isResearched)
        {
            if (hintPanel.activeSelf && hintText.text.Contains("отправьте отчёт"))
                hintPanel.SetActive(false);
            return;
        }

        bool allScanned = !string.IsNullOrEmpty(currentScannedMineral.savedAgeLine) &&
                          !string.IsNullOrEmpty(currentScannedMineral.savedRadioactivityLine) &&
                          !string.IsNullOrEmpty(currentScannedMineral.savedCrystalLine);

        if (allScanned)
        {
            ShowHint(makeConclusionHint);
            if (currentScannedMineral.isLastInTutorialQueue && !finalMonologuePlayed && radioMonologue != null)
            {
                radioMonologue.PlayFinalTutorialMonologue();
                finalMonologuePlayed = true;
            }
        }
        else if (hintPanel.activeSelf && hintText.text.Contains("отправьте отчёт"))
        {
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
            ShowHint(scanAccuracyHint);
            accuracyHintShown = true;
            StartCoroutine(HideHintAfter(3f));
            return;
        }

        if (scanned == 1 && !showedFindTwoMore)
        {
            ShowHint(findTwoMorePointsHint);
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
            ShowHint(goToBedHint);
            bedHintShown = true;
        }
    }

    public void OnAnomalyReportSubmitted()
    {
        if (anomalyHintShown || anomalyPlaced || currentScannedMineral == null) return;

        // ← ПРОВЕРЯЕМ, ЧТО ЭТО АНОМАЛИЯ И МОНОЛОГ ПРОИГРАН
        if (currentScannedMineral.isAnomaly && radioMonologue != null && radioMonologue.HasPlayedFinalMonologue)
        {
            ShowHint(placeAnomalyHint);
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
            if (hintPanel) hintPanel.SetActive(false);
        }
    }

    public void OnPlayerSlept()
    {
        playerSlept = true;
        if (hintPanel.activeSelf && hintText.text.Contains("кровать"))
            hintPanel.SetActive(false);
        Success();
    }
 

    public bool CanGrabAnyMineralFromVehicle()
    {
        // Можно брать минералы ТОЛЬКО ПОСЛЕ ВТОРОГО МОНОЛОГА
        return radioMonologue != null
               && radioMonologue.HasPlayedReturnMonologue;
    }

    public bool CanGrabLastTutorialMineral() => researchedCount >= 2;

    public bool CanGrabMineralFromVehicle(GrabbableItem item)
    {
        if (!returnedHintShown) return false;
        var mineralData = item.GetComponentInChildren<MineralData>();
        if (mineralData != null && mineralData.isLastInTutorialQueue)
            return researchedCount >= 2;
        return true;
    }

    private void HighlightFirstTwoMineralsInVehicle()
    {
        if (vehicleMineralSnapZone == null) return;

        foreach (var item in vehicleMineralSnapZone.attachedItems)
        {
            var md = item.GetComponentInChildren<MineralData>();
            if (md == null) continue;

            // Подсвечиваем только те, что ТЫ УКАЗАЛ
            md.EnableTutorialOutline(md.isTutorialHighlighted);
        }
    }


    private void Success()
    {
        waitingHold = true;
        timer = 0f;
        if (hintText) hintText.text += " Готово";
    }

    private void NextStep()
    {
        step++;
        waitingHold = false;
        switch (step)
        {
            case 4: break;
            case 5: ShowHint(breakDepositText); break;
            case 6: ShowHint(carryToVehicleText); break;
            case 7: if (hintPanel) hintPanel.SetActive(false); break;
            default:
                if (step < 4)
                    ShowHint(step switch { 0 => lookText, 1 => moveText, 2 => doorText, 3 => vehicleText, _ => "" });
                break;
        }
    }

    private void ShowHint(string text)
    {
        if (!hintPanel || !hintText) return;
        hintText.text = text;
        hintPanel.SetActive(true);
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
        firstMineralOnTable = scanMoveHintShown = scanClickHintShown = accuracyHintShown = false;
        showedFindTwoMore = false;
        researchedCount = 0;
        currentScannedMineral = null;
        anomalyHintShown = anomalyPlaced = bedHintShown = playerSlept = false;
        finalMonologuePlayed = false;
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

    public string GetUniqueID() => "TutorialSystem";
    public ObjectSaveData GetCommonSaveData() => null;
    public void LoadCommonData(ObjectSaveData data) { }
}