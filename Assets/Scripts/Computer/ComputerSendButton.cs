using UnityEngine;

public class ComputerSendButton : MonoBehaviour
{
    public ComputerUIController controller;

    public void Send()
    {
        controller.ExitUI();
    }
}
