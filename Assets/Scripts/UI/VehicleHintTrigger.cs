using UnityEngine;

public class VehicleHintTrigger : MonoBehaviour
{
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private string hintMessage = "Нажмите E, чтобы сесть на снегоход";
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) && hintPanel != null)
        {
            var text = hintPanel.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (text != null) text.text = hintMessage;
            hintPanel.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) && hintPanel != null)
        {
            hintPanel.SetActive(false);
        }
    }
}