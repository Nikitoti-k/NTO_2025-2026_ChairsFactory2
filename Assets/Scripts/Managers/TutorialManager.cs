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
    [TextArea] public string takeNextSampleHint = "Возьмите следующий образец";

    [SerializeField] private Transform baseReturnPoint;
    [SerializeField] private float baseReturnDistance = 30f;
    [SerializeField] private RadioMonologue radioMonologue;
    [SerializeField] public SnapZone vehicleMineralSnapZone;
    [SerializeField] private SnapZone baseResearchSnapZone;

    private int step = 0;
    private float timer = 0f;
    private bool waitingHold = false;

    private bool looked = false, moved = false, doorOpened = false, vehicleEntered = false;
    private bool depositBroken = false, firstMineralPlaced = false;
    private bool flareHintActive = false, flareThrown = false;
    private bool returnedHintShown = false, researchTableHintShown = false;

    private bool firstMineralOnTable = false;
    private bool scanMoveHintShown = false;
    private bool scanClickHintShown = false;
    private bool accuracyHintShown = false;
    private bool showedFindTwoMore = false;
    private bool showedMakeConclusion = false;
    private bool showedTakeNextSample = false;
    private bool hasPlayedReturnMonologue = false;
    private bool hasPlayedFinalMonologue = false; // ← Новый флаг для третьего монолога

    private int researchedCount = 0;
    private MineralData lastTutorialMineral; // ← Запоминаем последний минерал

    private Transform player;
    private MineralData currentScannedMineral; // ← запоминаем, какой минерал сейчас лежит на столе
    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }
    private void OnMineralPlacedOnResearchTable(GrabbableItem item)
    {
        if (item.ItemType != GrabbableType.Mineral) return;
        var mineralData = item.GetComponentInChildren<MineralData>();
        if (mineralData == null || mineralData.isResearched) return;

        // ← ГАСИМ АУТЛАЙН
        mineralData.EnableTutorialOutline(false);

        // ← ЗАПОМИНАЕМ, КАКОЙ МИНЕРАЛ СЕЙЧАС НА СТОЛЕ
        currentScannedMineral = mineralData;

        if (!firstMineralOnTable)
        {
            firstMineralOnTable = true;
            ShowHint(scanMoveHint);
            scanMoveHintShown = true;
        }
    }
    public void OnRecordButtonPressed()
    {
        if (MineralScanner_Renderer.Instance == null) return;
        var mineral = MineralScanner_Renderer.Instance.GetCurrentMineral();
        if (mineral == null || mineral != currentScannedMineral) return;

        int scanned = 0;
        if (!string.IsNullOrEmpty(mineral.savedAgeLine)) scanned++;
        if (!string.IsNullOrEmpty(mineral.savedRadioactivityLine)) scanned++;
        if (!string.IsNullOrEmpty(mineral.savedCrystalLine)) scanned++;

        // Подсказка про точность — один раз
        if (!accuracyHintShown)
        {
            ShowHint(scanAccuracyHint);
            accuracyHintShown = true;
            StartCoroutine(HideHintAfter(3f));
            return;
        }

        // После первой точки
        if (scanned == 1 && !showedFindTwoMore)
        {
            ShowHint(findTwoMorePointsHint);
            showedFindTwoMore = true;
            StartCoroutine(HideHintAfter(3f));
        }
        // После ТРЁХ точек
        else if (scanned == 3 && !showedMakeConclusion)
        {
            ShowHint(makeConclusionHint);
            showedMakeConclusion = true;
            StartCoroutine(HideHintAfter(4f));

            // ← ТОЧНО ОПРЕДЕЛЯЕМ: ЭТО ПОСЛЕДНИЙ ТУТОРИАЛЬНЫЙ МИНЕРАЛ?
            if (mineral.isLastInTutorialQueue && !hasPlayedFinalMonologue)
            {
                hasPlayedFinalMonologue = true;
                radioMonologue?.PlayFinalTutorialMonologue();
                Debug.Log("ФИНАЛЬНЫЙ МОНОЛОГ ЗАПУЩЕН — последний минерал полностью отсканирован!");
            }
        }
    }
    private void Start() => gameObject.SetActive(false);

    public void ForceStartTutorial()
    {
        gameObject.SetActive(true);
        enabled = true;
        step = 0;
        researchedCount = 0;
        hasPlayedReturnMonologue = false;
        hasPlayedFinalMonologue = false;
        ResetAllFlags();
        ShowHint(lookText);
    }

    private void OnEnable()
    {
        if (GameDayManager.Instance != null)
        {
            GameDayManager.Instance.OnDepositsChanged.AddListener(OnDepositBroken);
            GameDayManager.Instance.OnMineralResearched.AddListener(OnMineralResearched);
        }
        if (vehicleMineralSnapZone != null)
            vehicleMineralSnapZone.onItemSnapped.AddListener(OnMineralPlacedInVehicle);
        if (baseResearchSnapZone != null)
            baseResearchSnapZone.onItemSnapped.AddListener(OnMineralPlacedOnResearchTable);
        if (MineralScanner_Renderer.Instance != null)
            MineralScanner_Renderer.Instance.SubscribeToProximity(OnScannerProximityChanged);
    }

    private void OnDisable()
    {
        if (GameDayManager.Instance != null)
        {
            GameDayManager.Instance.OnDepositsChanged.RemoveListener(OnDepositBroken);
            GameDayManager.Instance.OnMineralResearched.RemoveListener(OnMineralResearched);
        }
        if (vehicleMineralSnapZone != null)
            vehicleMineralSnapZone.onItemSnapped.RemoveListener(OnMineralPlacedInVehicle);
        if (baseResearchSnapZone != null)
            baseResearchSnapZone.onItemSnapped.RemoveListener(OnMineralPlacedOnResearchTable);
        if (MineralScanner_Renderer.Instance != null)
            MineralScanner_Renderer.Instance.UnsubscribeFromProximity(OnScannerProximityChanged);
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

        if (step == 8 && returnedHintShown && player != null && baseReturnPoint != null && !hasPlayedReturnMonologue)
        {
            if (Vector3.Distance(player.position, baseReturnPoint.position) <= baseReturnDistance)
            {
                radioMonologue?.PlayReturnToBaseMonologue();
                hasPlayedReturnMonologue = true;
                step = 9;
            }
        }

        if (step == 9 && !researchTableHintShown && radioMonologue != null && !radioMonologue.IsPlaying)
        {
            ShowHint(bringToTableText);
            researchTableHintShown = true;
            HighlightFirstTwoMineralsInVehicle();
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
        int required = GameDayManager.Instance.MineralsToResearch;
        if (step == 7 && vehicleMineralSnapZone.AttachedItemsCount >= required && !returnedHintShown)
        {
            ShowHint(returnToBaseText);
            returnedHintShown = true;
            step = 8;
        }
    }

   

    private void OnScannerProximityChanged(float proximity)
    {
        if (!firstMineralOnTable || !scanMoveHintShown) return;
        if (proximity >= 0.6f && !scanClickHintShown)
        {
            ShowHint(scanClickHint);
            scanMoveHintShown = false;
            scanClickHintShown = true;
        }
    }

   

    public void OnReportSubmittedFirstMineral()
    {
        if (!firstMineralOnTable || !showedMakeConclusion || showedTakeNextSample) return;
        ShowHint(takeNextSampleHint);
        showedTakeNextSample = true;
        StartCoroutine(HideHintAfter(3f));
        if (hintPanel) hintPanel.SetActive(false);
    }

    private IEnumerator HideHintAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (hintPanel) hintPanel.SetActive(false);
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
                {
                    md.EnableTutorialOutline(true);
                }
            }
        }
    }

    public bool CanGrabAnyMineralFromVehicle() => hasPlayedReturnMonologue;
    public bool CanGrabLastTutorialMineral() => researchedCount >= 2;

    public bool CanGrabMineralFromVehicle(GrabbableItem item)
    {
        if (!hasPlayedReturnMonologue) return false;
        var mineralData = item.GetComponentInChildren<MineralData>();
        if (mineralData == null) return true;
        if (mineralData.isLastInTutorialQueue)
            return CanGrabLastTutorialMineral();
        return true;
    }

    private void HighlightFirstTwoMineralsInVehicle()
    {
        if (vehicleMineralSnapZone == null) return;
        var items = vehicleMineralSnapZone.attachedItems;
        int highlighted = 0;
        foreach (var item in items)
        {
            var mineralData = item.GetComponentInChildren<MineralData>();
            if (mineralData == null) continue;
            if (highlighted < 2)
            {
                mineralData.EnableTutorialOutline(true);
                mineralData.isTutorialHighlighted = true;
                highlighted++;
            }
            else
            {
                mineralData.SetAsLastInTutorialQueue(true);
                mineralData.EnableTutorialOutline(false);
            }
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

    private void ResetAllFlags()
    {
        looked = moved = doorOpened = vehicleEntered = depositBroken = false;
        firstMineralPlaced = flareHintActive = flareThrown = returnedHintShown = researchTableHintShown = false;
        firstMineralOnTable = scanMoveHintShown = scanClickHintShown = accuracyHintShown = false;
        showedFindTwoMore = showedMakeConclusion = showedTakeNextSample = false;
        researchedCount = 0;
        hasPlayedReturnMonologue = false;
        hasPlayedFinalMonologue = false;
        lastTutorialMineral = null;
        waitingHold = false;
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