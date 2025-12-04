using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System;

[Serializable]
public class MonologueSet
{
    public string speakerName;
    [TextArea(3, 5)] public string[] phrases;
}

public class RadioMonologue : MonoBehaviour
{
    [SerializeField] private GameObject radioPanel;
    [SerializeField] private TMP_Text radioText;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private float charsPerSecond = 45f;
    [SerializeField] private MonologueSet[] monologueSets;

    private int currentSet = 0;
    private int currentPhrase = 0;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    public TutorialManager tutorialManager;
    public bool IsPlaying => radioPanel != null && radioPanel.activeSelf;

  //  private bool hasPlayedFinalMonologue = false;
  //  public bool HasPlayedFinalMonologue => hasPlayedFinalMonologue;

    public bool HasPlayedIntroMonologue { get; set; } = false;  // Новый
    public bool HasPlayedReturnMonologue { get; set; } = false;
    public bool HasPlayedFinalMonologue { get; set; } = false;  // Переименуй hasPlayedFinalMonologue

    private void Awake()
    {
        tutorialManager = FindObjectOfType<TutorialManager>();
    }

    private void Start()
    {
        if (radioPanel) radioPanel.SetActive(false);
        if (promptText) promptText.text = "Press Enter to continue";
       /// if (monologueSets != null && monologueSets.Length > 0 && HasPlayedIntroMonologue==false)
          //  StartMonologue(0);
    }

    public void StartMonologue(int setIndex)
    {
        if (monologueSets == null || setIndex < 0 || setIndex >= monologueSets.Length) return;

        currentSet = setIndex;
        currentPhrase = 0;
        radioPanel.SetActive(true);
        BlockPlayerControls(true);
        UpdateSpeakerAndStartTyping();
    }

    private void UpdateSpeakerAndStartTyping()
    {
        if (currentPhrase >= monologueSets[currentSet].phrases.Length)
        {
            EndMonologue();
            return;
        }

        radioText.text = "";
        if (speakerText) speakerText.text = monologueSets[currentSet].speakerName;
        if (promptText) promptText.gameObject.SetActive(false);

        isTyping = true;
        typingCoroutine = StartCoroutine(TypeText(monologueSets[currentSet].phrases[currentPhrase]));
    }

    private IEnumerator TypeText(string text)
    {
        float delay = 1f / charsPerSecond;
        int charIndex = 0;

        while (charIndex < text.Length)
        {
            if (!isTyping) yield break;

            radioText.text = text.Substring(0, charIndex + 1);
            charIndex++;
            yield return new WaitForSeconds(delay);
        }

        radioText.text = text;
        isTyping = false;
        if (promptText) promptText.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!radioPanel.activeSelf || InputManager.Instance == null) return;

        if (InputManager.Instance.RadioNext)
        {
            if (isTyping && typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                radioText.text = monologueSets[currentSet].phrases[currentPhrase];
                isTyping = false;
                if (promptText) promptText.gameObject.SetActive(true);
            }
            else if (!isTyping)
            {
                currentPhrase++;
                UpdateSpeakerAndStartTyping();
            }
        }
    }

    public void PlayFinalTutorialMonologue()
    {
        if (monologueSets.Length > 2)
            StartMonologue(2);
    }

    public void PlayReturnToBaseMonologue()
    {
        StartMonologue(1);
    }

    private void EndMonologue()
    {
        radioPanel.SetActive(false);
        BlockPlayerControls(false);

        // === ФИКС: отмечаем проигрывание монологов ===
        if (currentSet == 0) HasPlayedIntroMonologue = true;
        if (currentSet == 1) HasPlayedReturnMonologue = true;
        if (currentSet == 2) HasPlayedFinalMonologue = true;

        if (currentSet == 0 && tutorialManager != null)
            tutorialManager.ForceStartTutorial();
    }
    private bool hasPlayedReturnMonologue = false;
   // public bool HasPlayedReturnMonologue => hasPlayedReturnMonologue;

    private void BlockPlayerControls(bool block)
    {
        var player = FindObjectOfType<PlayerMovement>();
        if (player != null) player.enabled = !block;

        var cam = FindObjectOfType<CameraController>();
        if (cam != null) cam.enabled = !block;

        Cursor.lockState = block ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = block;

        if (block && InputManager.Instance != null)
            InputManager.ClearAll();
    }

    private void Test0() => StartMonologue(0);
}
