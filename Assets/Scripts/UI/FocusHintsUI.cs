using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FocusHintsUI : MonoBehaviour
{
    [SerializeField] private GameObject hintsPanel;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private string exitHint = "ПКМ - Выход из фокуса";

    private void Awake()
    {
        if (hintsPanel == null)
            hintsPanel = CreateDefaultPanel();
        else
            hintsPanel.SetActive(false);
    }

    private void OnEnable()
    {
        CameraController.OnModeChanged += OnModeChanged;
        if (CameraController.Instance != null)
            OnModeChanged(CameraController.Instance.currentMode);
    }

    private void OnDisable()
    {
        CameraController.OnModeChanged -= OnModeChanged;
    }

    private void OnModeChanged(CameraController.ControlMode mode)
    {
        if (hintsPanel != null)
            hintsPanel.SetActive(mode == CameraController.ControlMode.Focus);
    }

    private GameObject CreateDefaultPanel()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("HintsCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        GameObject panel = new GameObject("FocusHintsPanel");
        panel.transform.SetParent(canvas.transform, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0);
        rect.anchorMax = new Vector2(0.5f, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.anchoredPosition = new Vector2(0, 20);
        rect.sizeDelta = new Vector2(300, 50);

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0.7f);

        GameObject textGO = new GameObject("HintText");
        textGO.transform.SetParent(panel.transform, false);
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = exitHint;
        text.fontSize = 20;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        panel.SetActive(false);
        return panel;
    }
}