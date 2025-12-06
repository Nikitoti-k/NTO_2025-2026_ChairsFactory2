using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System;

[Serializable]
public class MonologueSet
{
    public string speakerNameKey;
    [TextArea(3, 5)] public string[] phraseKeys;
}

public class RadioMonologue : MonoBehaviour, ILocalizable
{
    [SerializeField] private GameObject radioPanel;
    [SerializeField] private TMP_Text radioText;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private float charsPerSecond = 45f;

    [Header("Monologue Sets")]
    [SerializeField] public MonologueSet[] monologueSets = new MonologueSet[1];

    [Header("Radio Noise")]
    [SerializeField] private string radioNoiseKey = "radio_static";
    [SerializeField] [Range(0f, 1f)] private float radioNoiseVolume = 0.7f;

    private int currentSet = 0;
    private int currentPhrase = 0;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    public TutorialManager tutorialManager;

    // ─────────────────────── ФЛАГИ ───────────────────────
    public bool HasPlayedIntroMonologue { get; set; } = false;
    public bool HasPlayedReturnMonologue { get; set; } = false;
    public bool HasPlayedFinalMonologue { get; set; } = false;
    public bool HasPlayedMorningDay2 { get; private set; } = false;
    public bool HasPlayedMorningDay3 { get; private set; } = false;

    // ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←
    // ВАЖНЫЕ ПУБЛИЧНЫЕ СВОЙСТВА И МЕТОДЫ (были удалены — вернул!)
    // ─────────────────────────────────────────────────────
    public bool IsPlaying => radioPanel != null && radioPanel.activeSelf;

    public void PlayFinalTutorialMonologue()
    {
        if (monologueSets.Length > 2) StartMonologue(2);
    }

    public void PlayReturnToBaseMonologue()
    {
        StartMonologue(1);
    }

    public void PlayMorningMonologue_Day2()
    {
        if (HasPlayedMorningDay2) return;
        if (monologueSets.Length > 3)
        {
            StartMonologue(3);
            HasPlayedMorningDay2 = true;
        }
    }

    public void PlayMorningMonologue_Day3()
    {
        if (HasPlayedMorningDay3) return;
        if (monologueSets.Length > 4)
        {
            StartMonologue(4);
            HasPlayedMorningDay3 = true;
        }
    }
    // ─────────────────────────────────────────────────────

    private void Start()
    {
        if (radioPanel) radioPanel.SetActive(false);
        Localize();
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
    }

    private void OnDestroy() => LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    private void OnEnable() => LocalizationManager.Register(this);
    private void OnDisable() => LocalizationManager.Unregister(this);

    public void Localize()
    {
        if (promptText != null)
            promptText.text = LocalizationManager.Loc("RADIO_PROMPT");
    }
    private void OnLanguageChanged(LocalizationManager.Language lang) => Localize();

    public void StartMonologue(int setIndex)
    {
        if (monologueSets == null || setIndex < 0 || setIndex >= monologueSets.Length) return;

        currentSet = setIndex;
        currentPhrase = 0;
        radioPanel.SetActive(true);
        BlockPlayerControls(true);
        PlayRadioNoise();
        UpdateSpeakerAndStartTyping();
    }

    private void UpdateSpeakerAndStartTyping()
    {
        if (currentPhrase >= monologueSets[currentSet].phraseKeys.Length)
        {
            EndMonologue();
            return;
        }

        radioText.text = "";
        if (speakerText)
            speakerText.text = LocalizationManager.Loc(monologueSets[currentSet].speakerNameKey);
        if (promptText) promptText.gameObject.SetActive(false);

        isTyping = true;
        string fullText = LocalizationManager.Loc(monologueSets[currentSet].phraseKeys[currentPhrase]);
        typingCoroutine = StartCoroutine(TypeText(fullText));
    }

    private IEnumerator TypeText(string text)
    {
        float delay = 1f / charsPerSecond;
        int index = 0;
        while (index < text.Length)
        {
            if (!isTyping) yield break;
            radioText.text = text.Substring(0, ++index);
            yield return new WaitForSeconds(delay);
        }
        radioText.text = text;
        isTyping = false;
        if (promptText) promptText.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!radioPanel.activeSelf) return;

        if (InputManager.Instance != null && InputManager.Instance.RadioNext)
        {
            if (isTyping && typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                radioText.text = LocalizationManager.Loc(monologueSets[currentSet].phraseKeys[currentPhrase]);
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

    private void EndMonologue()
    {
        radioPanel.SetActive(false);
        BlockPlayerControls(false);
        StopRadioNoise();

        switch (currentSet)
        {
            case 0: HasPlayedIntroMonologue = true; break;
            case 1: HasPlayedReturnMonologue = true; break;
            case 2: HasPlayedFinalMonologue = true; break;
        }

        if (currentSet == 0 && tutorialManager != null)
            tutorialManager.ForceStartTutorial();
    }

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

    private void PlayRadioNoise()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPersistentSFX(radioNoiseKey);
    }

    private void StopRadioNoise()
    {
        AudioManager.Instance?.StopPersistentSFX(0.7f); // плавное затухание
    }

    // Тесты в редакторе
    [ContextMenu("TEST → Intro Monologue")] private void Test0() => StartMonologue(0);
    [ContextMenu("TEST → Return Monologue")] private void Test1() => StartMonologue(1);
}