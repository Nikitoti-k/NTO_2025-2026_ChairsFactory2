using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MineralsHintsUI : MonoBehaviour
{
    [SerializeField] private string hintText = "ЛКМ - подобрать минерал";
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private GameObject hintsPanel;
    //private GameObject hintsPanel;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            hintsPanel.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            hintsPanel.SetActive(false);
        }
    }

    private void OnMouseExit()
    {
        //hintsPanel.SetActive(false);
    }
    /*private GameObject CreateDefaultHintsPanel()
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
        text.text = hintText;
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
    }*/
}
