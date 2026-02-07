using UnityEngine;

public class FocusPoint : MonoBehaviour
{
    public Transform cameraPoint;
    public Transform playerPoint;

    private void OnValidate()
    {
        if (cameraPoint == null)
        {
            cameraPoint = CreateChildTransform("CameraPoint");
            cameraPoint.localPosition = new Vector3(0, 0.1f, -0.5f);
            cameraPoint.LookAt(transform);
        }

        if (playerPoint == null)
        {
            playerPoint = CreateChildTransform("PlayerPoint");
            playerPoint.localPosition = new Vector3(0, 0, 0.5f);
            playerPoint.localRotation = Quaternion.Euler(0, 180, 0);
        }
    }

    private Transform CreateChildTransform(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        return go.transform;
    }
}