using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections.Generic;

public class SaveLoadMenu : MonoBehaviour
{
    [SerializeField] private Transform slotsParent;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Button backButton;

   
    [Header("UI Feedback")]
    [SerializeField] private Text legacyErrorText;                  
    [SerializeField] private TMP_Text tmpErrorText;                   
    [SerializeField] private float errorDisplayTime = 6f;             

    private List<GameObject> spawnedSlots = new List<GameObject>();
    private Coroutine errorCoroutine;

    private void OnEnable()
    {
        RefreshSlots();
        ClearErrorMessage(); 
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

  
    public void ShowErrorMessage(string message = null)
    {
        string text = message ?? LocalizationManager.Loc("SAVE_ERROR_CORRUPTED");

        if (tmpErrorText != null)
            tmpErrorText.text = text;
        else if (legacyErrorText != null)
            legacyErrorText.text = text;
        return; 

       
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