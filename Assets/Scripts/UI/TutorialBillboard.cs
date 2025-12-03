using UnityEngine;

public class TutorialBillboard : MonoBehaviour
{
    [SerializeField] private bool rotateX = true; // поворачивать по X?
    [SerializeField] private bool rotateY = true; // поворачивать по Y?
    [SerializeField] private bool rotateZ = false; // по Z НЕТ!

    private Transform cameraTransform;

    private void Start()
    {
        cameraTransform = Camera.main?.transform;
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 directionToCamera = (cameraTransform.position - transform.position).normalized;

        if (rotateX && rotateY && !rotateZ)
        {
            // ИДЕАЛЬНЫЙ БИЛБОРД: поворачиваем только по Y, но держим вертикально
            Quaternion targetRotation = Quaternion.LookRotation(directionToCamera, Vector3.up);
            transform.rotation = targetRotation;
        }
        else
        {
            // Полный поворот
            transform.LookAt(cameraTransform);
        }
    }
}