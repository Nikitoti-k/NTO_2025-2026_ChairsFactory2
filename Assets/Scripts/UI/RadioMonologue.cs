using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class RadioMonologue : MonoBehaviour
{
    [Header("══ РАДИО-МОНОЛОГ ══")]
    [SerializeField] private GameObject radioPanel;           // весь UI (можно просто Image + Text)
    [SerializeField] private TMP_Text radioText;              // TextMeshPro компонент
    [SerializeField] private float charsPerSecond = 45f;      // скорость появления текста
    [SerializeField] private AudioSource radioVoice;          // опционально — голос по рации

    [Header("Фразы — задавай сколько угодно")]
    [TextArea(3, 5)] public string[] phrases;

    private int currentPhrase = 0;
    private bool isTyping = false;
    private bool skipRequested = false;
    private Coroutine typingCoroutine;

    private void Start()
    {
        if (radioPanel) radioPanel.SetActive(false);
    }

    // ═══════════════════════════════════════
    // ВЫЗЫВАЙ ЭТУ ФУНКЦИЮ, КОГДА НУЖНО ЗАПУСТИТЬ МОНОЛОГ
    // Например: при старте уровня, при входе в зону и т.д.
    // ═══════════════════════════════════════
    public void StartMonologue()
    {
        if (phrases == null || phrases.Length == 0) return;

        radioPanel.SetActive(true);
        currentPhrase = 0;
        BlockPlayerControls(true);
        StartNextPhrase();
    }

    private void StartNextPhrase()
    {
        if (currentPhrase >= phrases.Length)
        {
            EndMonologue();
            return;
        }

        radioText.text = "";
        skipRequested = false;
        isTyping = true;

        if (radioVoice != null && radioVoice.clip != null)
            radioVoice.Play();

        typingCoroutine = StartCoroutine(TypeText(phrases[currentPhrase]));
    }

    private IEnumerator TypeText(string text)
    {
        float delay = 1f / charsPerSecond;
        int charIndex = 0;

        while (charIndex < text.Length)
        {
            if (skipRequested)
            {
                radioText.text = text;
                skipRequested = false;
                isTyping = false;
                break;
            }

            radioText.text += text[charIndex];
            charIndex++;
            yield return new WaitForSeconds(delay);
        }

        isTyping = false;
    }

    private void Update()
    {
        if (!radioPanel.activeSelf) return;

        // Нажатие Enter
        if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            if (isTyping)
            {
                // Скип анимации появления
                skipRequested = true;
                if (typingCoroutine != null)
                    StopCoroutine(typingCoroutine);
            }
            else
            {
                // Переход к следующей фразе
                currentPhrase++;
                StartNextPhrase();
            }
        }
    }

    private void EndMonologue()
    {
        radioPanel.SetActive(false);
        BlockPlayerControls(false);

        if (radioVoice != null)
            radioVoice.Stop();
    }

    // Блокируем/разблокируем управление игроком
    private void BlockPlayerControls(bool block)
    {
        var player = FindObjectOfType<PlayerMovement>();
        if (player != null)
        {
            player.enabled = !block;
        }

        var cameraCtrl = FindObjectOfType<CameraController>();
        if (cameraCtrl != null)
        {
            cameraCtrl.enabled = !block;
        }

        // Блокируем ввод через InputManager (если он используется)
        if (InputManager.Instance != null)
        {
            if (block)
            {
                InputManager.ClearAll();
            }
        }

        // Альтернатива: блокируем курсор и ввод полностью
        Cursor.lockState = block ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = block;
    }

    // Для теста — запускаем монолог из контекстного меню
    [ContextMenu("▶ Запустить монолог (тест)")]
    public void TestStartMonologue()
    {
        StartMonologue();
    }

    // Сброс для теста
    [ContextMenu("Скрыть радио")]
    public void HideRadio()
    {
        radioPanel.SetActive(false);
        BlockPlayerControls(false);
    }
}