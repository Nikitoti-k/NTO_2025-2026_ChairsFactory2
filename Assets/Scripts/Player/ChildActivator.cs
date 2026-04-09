using UnityEngine;

public class ChildActivator : MonoBehaviour
{
    [Header("Activation Settings")]
    public GameObject childObject;  // Дочерний объект (изначально SetActive(false))

    private bool canActivate = true;  // Блокировка повторной активации

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && canActivate && childObject != null)
        {
            FindFirstObjectByType<StartEndCassette>().StartEndVideo();
        }
    }
}