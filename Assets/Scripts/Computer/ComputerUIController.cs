using UnityEngine;

public class ComputerUIController : MonoBehaviour
{
    [Header("UI")]
    public Canvas computerCanvas;
    public GameObject gameplayHUD;

    [Header("Optional")]
    //public MonoBehaviour lookInput;

    private bool active;

    private void Awake()
    {
        computerCanvas.gameObject.SetActive(false);
    }

    public void EnterUI()
    {
        if (active) return;
        active = true;

        computerCanvas.gameObject.SetActive(true);
        if (gameplayHUD) gameplayHUD.SetActive(false);

        //if (lookInput) lookInput.enabled = false;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;
    }

    public void ExitUI()
    {
        if (!active) return;
        active = false;

        computerCanvas.gameObject.SetActive(false);
        if (gameplayHUD) gameplayHUD.SetActive(true);

        //if (lookInput) lookInput.enabled = true;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
