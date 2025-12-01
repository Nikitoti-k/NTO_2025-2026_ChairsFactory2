using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class TutorialManager : MonoBehaviour, ISaveableV2
{
    [Header("Панель и текст")]
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private float holdAfterSuccess = 1.8f;

    [Header("Тексты подсказок")]
    [TextArea] public string lookText = "Осмотритесь вокруг — <color=#ffff00>двигайте мышью</color>";
    [TextArea] public string moveText = "Двигайтесь с помощью <color=#ffff00>W A S D</color>";
    [TextArea] public string doorText = "Чтобы открыть дверь — подойдите и <color=#ffff00>зажмите ЛКМ</color>";
    [TextArea] public string vehicleText = "Подойдите к снегоходу и нажмите <color=#ffff00>E</color>";
    [TextArea] public string flareText = "Бросьте факел — нажмите <color=#ffff00>F</color>";

    private int step = 0;           // 0-4
    private float timer = 0f;
    private bool waitingHold = false;

    private bool looked = false;
    private bool moved = false;
    private bool doorOpened = false;
    private bool vehicleEntered = false;
    private bool flareThrown = false;

    // Флаг: была ли активирована подсказка про факел извне
    private bool flareHintActivated = false;

    private void Start()
    {
        UpdatePanel();
        ShowCurrent();
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

        // Проверяем только текущий шаг
        switch (step)
        {
            case 0:
                if (!looked && Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 1f)
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
                { vehicleEntered = true; Success(); }
                break;

            case 4: // Факел — только если активировано извне!
                if (flareHintActivated && !flareThrown && Keyboard.current.fKey.wasPressedThisFrame)
                { flareThrown = true; Success(); }
                break;
        }
    }

    private void Success()
    {
        waitingHold = true;
        timer = 0f;
        if (hintText) hintText.text += "  Готово";
    }

    private void NextStep()
    {
        step++;
        waitingHold = false;

        // После шага 3 (снегоход) — НЕ переходим автоматически к факелу!
        // Останавливаемся. Ждём внешнего вызова.
        if (step == 4 && !flareHintActivated)
        {
            step = 3; // остаёмся на шаге "снегоход", пока не вызовут факел
            if (hintPanel) hintPanel.SetActive(false);
            return;
        }

        UpdatePanel();
        ShowCurrent();
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
            4 => flareText,
            _ => ""
        };
    }

    private void UpdatePanel()
    {
        if (hintPanel)
            hintPanel.SetActive(step < 4 || (step == 4 && flareHintActivated));
    }

    // ВЫЗЫВАЕТСЯ ИЗВНЕ — ОДИН РАЗ, КОГДА НУЖНО ПОКАЗАТЬ ФАКЕЛ
    public void ActivateFlareHint()
    {
        if (flareHintActivated) return;

        flareHintActivated = true;

        // Если игрок уже прошёл все шаги до снегохода — сразу показываем факел
        if (step >= 3 && looked && moved && doorOpened && vehicleEntered)
        {
            step = 4;
            UpdatePanel();
            ShowCurrent();
        }
        // Иначе — просто запоминаем, что нужно показать позже
    }

    // === СОХРАНЕНИЯ ===
    public string GetUniqueID() => "TutorialSystem";

    public ObjectSaveData GetCommonSaveData()
    {
        var data = new ObjectSaveData
        {
            uniqueID = GetUniqueID(),
            prefabIdentifier = "",
            position = transform.position,
            rotation = transform.rotation,
            isActive = gameObject.activeSelf
        };

        var saveFile = SaveManager.Instance?.GetType()
            .GetField("currentSave", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(SaveManager.Instance) as SaveFile;

        if (saveFile != null)
        {
            saveFile.tutorialProgress = step;
            saveFile.flareHintWasShown = flareHintActivated;
        }

        return data;
    }

    public void LoadCommonData(ObjectSaveData data)
    {
        var saveFile = SaveManager.Instance?.GetType()
            .GetField("currentLoadedSave", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(SaveManager.Instance) as SaveFile;

        if (saveFile != null)
        {
            step = saveFile.tutorialProgress;
            flareHintActivated = saveFile.flareHintWasShown;

            looked = step > 0;
            moved = step > 1;
            doorOpened = step > 2;
            vehicleEntered = step > 3;
            flareThrown = step > 4;
        }

        UpdatePanel();
        ShowCurrent();
    }

    private bool IsWASDPressed()
    {
        var kb = Keyboard.current;
        return kb != null && (kb.wKey.isPressed || kb.aKey.isPressed || kb.sKey.isPressed || kb.dKey.isPressed);
    }

    private bool IsHoldingDoor()
    {
        var grabber = FindObjectOfType<CanGrab>();
        if (grabber == null || !grabber.IsHoldingObject()) return false;
        var item = grabber.GetGrabbedItem();
        return item != null && (item.CompareTag("Door") || item.name.ToLower().Contains("door"));
    }

    [ContextMenu("Сбросить туториал")]
    private void ResetTutorial()
    {
        step = 0;
        looked = moved = doorOpened = vehicleEntered = flareThrown = false;
        flareHintActivated = false;
        waitingHold = false;
        UpdatePanel();
        ShowCurrent();
    }
}