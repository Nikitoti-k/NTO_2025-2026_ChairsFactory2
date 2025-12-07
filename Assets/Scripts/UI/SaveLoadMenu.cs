using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // если используешь TextMeshPro
using System.Collections.Generic;

public class SaveLoadMenu : MonoBehaviour
{
    [SerializeField] private Transform slotsParent;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Button backButton;

    // ←←← НОВОЕ ПОЛЕ ДЛЯ СООБЩЕНИЙ ОБ ОШИБКАХ
    [Header("UI Feedback")]
    [SerializeField] private Text legacyErrorText;                    // обычный Unity Text
    [SerializeField] private TMP_Text tmpErrorText;                   // TextMeshPro
    [SerializeField] private float errorDisplayTime = 6f;             // сколько показывать

    private List<GameObject> spawnedSlots = new List<GameObject>();
    private Coroutine errorCoroutine;

    private void OnEnable()
    {
        RefreshSlots();
        ClearErrorMessage(); // на всякий случай
    }

    public void RefreshSlots()
    {
        foreach (var s in spawnedSlots)
            if (s != null) Destroy(s);
        spawnedSlots.Clear();

        var slots = SaveManager.Instance.GetAllSaveSlots();
        foreach (var slot in slots)
        {
            var obj = Instantiate(slotPrefab, slotsParent);
            var ui = obj.GetComponent<SaveSlotUI>();
            ui.Init(slot, this);
            spawnedSlots.Add(obj);
        }
    }

    private void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(BackToMainMenu);
    }

    public void BackToMainMenu()
    {
        gameObject.SetActive(false);
    }

    // ========================= НОВЫЕ МЕТОДЫ =========================

    /// <summary>
    /// Показывает сообщение об ошибке загрузки сохранения
    /// </summary>
    public void ShowErrorMessage(string message = null)
    {
        string text = message ?? LocalizationManager.Loc("SAVE_ERROR_CORRUPTED");

        if (tmpErrorText != null)
            tmpErrorText.text = text;
        else if (legacyErrorText != null)
            legacyErrorText.text = text;
        return; // нет UI-элемента

        // Перезапускаем таймер исчезновения
        if (errorCoroutine != null)
            StopCoroutine(errorCoroutine);

        errorCoroutine = StartCoroutine(HideErrorAfterDelay());
    }

    private IEnumerator HideErrorAfterDelay()
    {
        yield return new WaitForSeconds(errorDisplayTime);
        ClearErrorMessage();
    }

    public void ClearErrorMessage()
    {
        if (tmpErrorText != null)
            tmpErrorText.text = "";
        if (legacyErrorText != null)
            legacyErrorText.text = "";

        if (errorCoroutine != null)
        {
            StopCoroutine(errorCoroutine);
            errorCoroutine = null;
        }
    }
}