using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class TutorialManager : MonoBehaviour, ISaveableV2
{
    [Header("UI")]
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private float holdAfterSuccess = 1.8f;

    [Header("Тексты подсказок")]
    [TextArea] public string lookText = "Осмотритесь вокруг — двигайте мышью";
    [TextArea] public string moveText = "Двигайтесь с помощью <color=#ffff00>W A S D</color>";
    [TextArea] public string doorText = "Чтобы открыть дверь — подойдите и <color=#ffff00>зажмите ЛКМ</color>";
    [TextArea] public string vehicleText = "Подойдите к снегоходу и нажмите <color=#ffff00>E</color>";
    [TextArea] public string flareText = "Бросьте факел — нажмите <color=#ffff00>F</color>";
    [TextArea] public string breakDepositText = "Ломайте ледяную залежь — <color=#ffff00>ЛКМ</color>";
    [TextArea] public string carryToVehicleText = "Отнесите добытый образец\nв <color=#ffff00>снегоход</color>";
    [TextArea] public string returnToBaseText = "Вы собрали достаточно образцов.\n<color=#00ff00>Вернитесь на базу и изучите их</color>";
    [TextArea] public string bringToTableText = "Возьмите образец и отнесите его\nна <color=#ffff00>исследовательский стол</color>";

    [Header("Объекты и зоны")]
    [SerializeField] private Transform baseReturnPoint;
    [SerializeField] private float baseReturnDistance = 30f;
    [SerializeField] private GameObject researchTableHighlight;
    [SerializeField] private RadioMonologue radioMonologue;
    [SerializeField] private SnapZone vehicleMineralSnapZone; // ← SnapZone у снегохода

    private int step = 0;
    private float timer = 0f;
    private bool waitingHold = false;

    private bool looked = false, moved = false, doorOpened = false, vehicleEntered = false;
    private bool depositBroken = false;
    private bool firstMineralPlaced = false; // ← теперь отслеживаем первый минерал
    private bool flareHintActive = false, flareThrown = false;
    private bool returnedHintShown = false, tableHintShown = false;

    private Transform player;
    private CanGrab canGrab;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        canGrab = player?.GetComponent<CanGrab>();
        if (radioMonologue == null) radioMonologue = FindObjectOfType<RadioMonologue>();
      
    }

    private void Start()
    {
        gameObject.SetActive(false);
        if (researchTableHighlight) researchTableHighlight.SetActive(false);
    }

    public void ForceStartTutorial()
    {
        gameObject.SetActive(true);
        enabled = true;
        step = 0;
        ResetAllFlags();
        ShowCurrent();
    }

    private void OnEnable()
    {
        if (GameDayManager.Instance != null)
            GameDayManager.Instance.OnDepositsChanged.AddListener(OnDepositBroken);

        if (vehicleMineralSnapZone != null)
            vehicleMineralSnapZone.onItemSnapped.AddListener(OnMineralPlacedInVehicle);
    }

    private void OnDisable()
    {
        if (GameDayManager.Instance != null)
            GameDayManager.Instance.OnDepositsChanged.RemoveListener(OnDepositBroken);

        if (vehicleMineralSnapZone != null)
            vehicleMineralSnapZone.onItemSnapped.RemoveListener(OnMineralPlacedInVehicle);
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
                if (!vehicleEntered && InputRouter.Instance?.CurrentController is TransportMovement)
                {
                    vehicleEntered = true;
                    Success();
                    if (hintPanel) hintPanel.SetActive(false); // ← панель гаснет после снегохода
                }
                break;

            case 4: // ждём внешнего вызова факела
                break;

            case 5: // ждём ломания первой залежи
                break;

            case 6: // ждём первого минерала в снегоходе
                break;

            case 7: // ждём третьего минерала
                break;

            case 8: // приближение к базе
                if (returnedHintShown && player != null && baseReturnPoint != null)
                {
                    if (Vector3.Distance(player.position, baseReturnPoint.position) <= baseReturnDistance)
                    {
                        radioMonologue?.PlayReturnToBaseMonologue();
                        step = 9;
                        ShowHint(bringToTableText);
                        if (researchTableHighlight) researchTableHighlight.SetActive(true);
                        tableHintShown = true;
                    }
                }
                break;
        }

        // Факел — только после внешнего вызова
        if (flareHintActive && !flareThrown && Keyboard.current.fKey.wasPressedThisFrame)
        {
            flareThrown = true;
            Success(); // → step = 5 → ломать залежь
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
            Success(); // → step = 6 → "Отнесите образец в снегоход"
        }
    }

    private void OnMineralPlacedInVehicle(GrabbableItem item)
    {
        if (item.ItemType != GrabbableType.Mineral) return;

        // Первый минерал — завершаем этап "Отнесите в снегоход"
        if (step == 6 && !firstMineralPlaced)
        {
            firstMineralPlaced = true;
            Success(); // ← ПОДСКАЗКА ИСЧЕЗАЕТ СРАЗУ!
            return;
        }

        // Третий минерал — показываем "Вернитесь на базу"
        int required = GameDayManager.Instance.MineralsToResearch;
        if (step == 7 && vehicleMineralSnapZone.AttachedItemsCount >= required && !returnedHintShown)
        {
            ShowHint(returnToBaseText);
            returnedHintShown = true;
            step = 8;
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

        if (step == 5)
            ShowHint(breakDepositText);
        else if (step == 6)
            ShowHint(carryToVehicleText);
        else if (step == 7)
        {
            // После первого минерала — убираем подсказку, дальше игрок сам
            if (hintPanel) hintPanel.SetActive(false);
        }
        else if (step < 4)
        {
            ShowCurrent();
        }
    }

    private void ShowCurrent()
    {
        if (!hintText) return;
        hintText.text = step switch
        {
            0 => lookText,
            1 => moveText,
            2 => doorText,
            3 => vehicleText,
            _ => ""
        };
        hintPanel.SetActive(true);
    }

    private void ShowHint(string text)
    {
        if (hintText) hintText.text = text;
        hintPanel.SetActive(true);
    }

    private void ResetAllFlags()
    {
        looked = moved = doorOpened = vehicleEntered = depositBroken = false;
        firstMineralPlaced = flareHintActive = flareThrown = returnedHintShown = tableHintShown = false;
        waitingHold = false;
        if (researchTableHighlight) researchTableHighlight.SetActive(false);
    }

    private bool IsWASDPressed()
    {
        var kb = Keyboard.current;
        return kb != null && (kb.wKey.isPressed || kb.aKey.isPressed || kb.sKey.isPressed || kb.dKey.isPressed);
    }

    private bool IsHoldingDoor()
    {
        return canGrab != null && canGrab.IsHoldingObject() && canGrab.GetGrabbedItem()?.CompareTag("Door") == true;
    }

    // Сохранения
    public string GetUniqueID() => "TutorialSystem";
    public ObjectSaveData GetCommonSaveData() => null;
    public void LoadCommonData(ObjectSaveData data) { }
}