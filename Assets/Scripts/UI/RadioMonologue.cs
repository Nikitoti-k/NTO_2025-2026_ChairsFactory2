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

    [Header("Monologue Sets (0=Intro, 1=Return, 2=Final, 3=Morning Day2, 4=Morning Day3)")]
    [SerializeField] public MonologueSet[] monologueSets = new MonologueSet[5];

    [Header("Radio Noise")]
    [SerializeField] private string radioNoiseKey = "radio_static";
    [SerializeField] [Range(0f, 1f)] private float radioNoiseVolume = 0.7f;

    public TutorialManager tutorialManager;

    private int currentSet = 0;
    private int currentPhrase = 0;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

  
    public static RadioMonologue Instance { get; private set; }

    private void Awake()
    {
       
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

   
    private GameObject RadioPanel
    {
        get
        {
            if (radioPanel != null) return radioPanel;
            var panel = transform.Find("RadioPanel")?.gameObject ??
                       FindObjectOfType<Canvas>()?.transform.Find("RadioPanel")?.gameObject;
            if (panel != null) radioPanel = panel;
            return radioPanel;
        }
    }

    private TMP_Text RadioText
    {
        get
        {
            if (radioText != null) return radioText;
            var text = transform.Find("RadioText")?.GetComponent<TMP_Text>() ??
                      RadioPanel?.transform.Find("RadioText")?.GetComponent<TMP_Text>();
            if (text != null) radioText = text;
            return radioText;
        }
    }

    private TMP_Text SpeakerText
    {
        get
        {
            if (speakerText != null) return speakerText;
            var text = transform.Find("SpeakerText")?.GetComponent<TMP_Text>() ??
                      RadioPanel?.transform.Find("SpeakerText")?.GetComponent<TMP_Text>();
            if (text != null) speakerText = text;
            return speakerText;
        }
    }

    private TMP_Text PromptText
    {
        get
        {
            if (promptText != null) return promptText;
            var text = transform.Find("PromptText")?.GetComponent<TMP_Text>() ??
                      RadioPanel?.transform.Find("PromptText")?.GetComponent<TMP_Text>();
            if (text != null) promptText = text;
            return promptText;
        }
    }

   
    public bool HasPlayedIntroMonologue
    {
        get => tutorialManager != null && tutorialManager.HasPlayedIntroMonologue;
        set { if (tutorialManager != null) tutorialManager.HasPlayedIntroMonologue = value; }
    }

    public bool HasPlayedReturnMonologue
    {
        get => tutorialManager != null && tutorialManager.HasPlayedReturnMonologue;
        set { if (tutorialManager != null) tutorialManager.HasPlayedReturnMonologue = value; }
    }

    public bool HasPlayedFinalMonologue
    {
        get => tutorialManager != null && tutorialManager.HasPlayedFinalMonologue;
        set { if (tutorialManager != null) tutorialManager.HasPlayedFinalMonologue = value; }
    }

    public bool HasPlayedMorningDay2
    {
        get => tutorialManager != null && tutorialManager.HasPlayedMorningDay2;
        set { if (tutorialManager != null) tutorialManager.HasPlayedMorningDay2 = value; }
    }

    public bool HasPlayedMorningDay3
    {
        get => tutorialManager != null && tutorialManager.HasPlayedMorningDay3;
        set { if (tutorialManager != null) tutorialManager.HasPlayedMorningDay3 = value; }
    }

    public bool IsPlaying => RadioPanel != null && RadioPanel.activeSelf;

    private void Start()
    {
        if (RadioPanel != null) RadioPanel.SetActive(false);
        Localize();
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
    }

    private void OnEnable() => LocalizationManager.Register(this);
    private void OnDisable() => LocalizationManager.Unregister(this);

    public void Localize()
    {
        if (PromptText != null)
            PromptText.text = LocalizationManager.Loc("RADIO_PROMPT");
    }

    private void OnLanguageChanged(LocalizationManager.Language lang) => Localize();

  
    public void PlayIntroMonologue() => StartMonologue(0);
    public void PlayReturnToBaseMonologue() => StartMonologue(1);
    public void PlayFinalTutorialMonologue() => StartMonologue(2);

    public void PlayMorningMonologue_Day2()
    {
        if (HasPlayedMorningDay2) return;
        if (monologueSets.Length > 3) StartMonologue(3);
    }

    public void PlayMorningMonologue_Day3()
    {
        if (HasPlayedMorningDay3) return;
        if (monologueSets.Length > 4) StartMonologue(4);
    }

    
    public void StartMonologue(int setIndex)
    {
        
        if (gameObject == null) return;

        if (monologueSets == null || setIndex < 0 || setIndex >= monologueSets.Length) return;

       
        switch (setIndex)
        {
            case 0 when HasPlayedIntroMonologue: return;
            case 1 when HasPlayedReturnMonologue: return;
            case 2 when HasPlayedFinalMonologue: return;
            case 3 when HasPlayedMorningDay2: return;
            case 4 when HasPlayedMorningDay3: return;
        }

        currentSet = setIndex;
        currentPhrase = 0;

        if (RadioPanel != null) RadioPanel.SetActive(true);
        else return; // Нет панели — выходим

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

        if (RadioText != null) RadioText.text = "";
        if (SpeakerText != null)
            SpeakerText.text = LocalizationManager.Loc(monologueSets[currentSet].speakerNameKey);
        if (PromptText != null) PromptText.gameObject.SetActive(false);

        isTyping = true;
        string fullText = LocalizationManager.Loc(monologueSets[currentSet].phraseKeys[currentPhrase]);
        typingCoroutine = StartCoroutine(TypeText(fullText));
    }

    private IEnumerator TypeText(string text)
    {
        if (RadioText == null) yield break;

        float delay = 1f / charsPerSecond;
        int index = 0;
        while (index < text.Length)
        {
            if (!isTyping || RadioText == null) yield break;
            RadioText.text = text.Substring(0, ++index);
            yield return new WaitForSeconds(delay);
        }

        if (RadioText != null) RadioText.text = text;
        isTyping = false;
        if (PromptText != null) PromptText.gameObject.SetActive(true);
    }

    private void Update()
    {
        
        if (RadioPanel == null || !RadioPanel.activeSelf) return;

        if (InputManager.Instance != null && InputManager.Instance.RadioNext)
        {
            if (isTyping && typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
                if (RadioText != null)
                    RadioText.text = LocalizationManager.Loc(monologueSets[currentSet].phraseKeys[currentPhrase]);
                isTyping = false;
                if (PromptText != null) PromptText.gameObject.SetActive(true);
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
        if (RadioPanel != null) RadioPanel.SetActive(false);
        BlockPlayerControls(false);
        StopRadioNoise();

        
        switch (currentSet)
        {
            case 0: HasPlayedIntroMonologue = true; break;
            case 1: HasPlayedReturnMonologue = true; break;
            case 2: HasPlayedFinalMonologue = true; break;
            case 3: HasPlayedMorningDay2 = true; break;
            case 4: HasPlayedMorningDay3 = true; break;
        }

       
        if (currentSet == 0 && tutorialManager != null)
            tutorialManager.ForceStartTutorial();
    }

    private void BlockPlayerControls(bool block)
    {
        var player = FindObjectOfType<PlayerMovement>();
        if (player != null) player.enabled = !block;

        var cam = CameraController.Instance;
        if (cam != null) cam.enabled = !block;

        Cursor.lockState = block ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = block;

        if (block && InputManager.Instance != null)
            InputManager.ClearAll();
    }

    private void PlayRadioNoise()
    {
        AudioManager.Instance?.PlayPersistentSFX(radioNoiseKey, radioNoiseVolume);
    }

    private void StopRadioNoise()
    {
        AudioManager.Instance?.StopPersistentSFX(0.7f);
    }

    // Тесты в инспекторе
    [ContextMenu("TEST → 0 Intro Monologue")] private void Test0() => StartMonologue(0);
    [ContextMenu("TEST → 1 Return Monologue")] private void Test1() => StartMonologue(1);
    [ContextMenu("TEST → 2 Final Tutorial")] private void Test2() => StartMonologue(2);
    [ContextMenu("TEST → 3 Morning Day 2")] private void Test3() => StartMonologue(3);
    [ContextMenu("TEST → 4 Morning Day 3")] private void Test4() => StartMonologue(4);
}