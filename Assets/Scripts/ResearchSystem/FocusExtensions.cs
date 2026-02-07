using UnityEngine;

public static class FocusExtensions
{
    public static void FocusOn(this CameraController camera, GameObject target, float distance = 2f, Vector3 offset = default)
    {
        if (camera != null && target != null)
        {
         //   camera.FocusOnObject(target.transform, distance, offset);
        }
    }

    public static void FocusOn(this CameraController camera, Component target, float distance = 2f, Vector3 offset = default)
    {
        if (camera != null && target != null)
        {
           // camera.FocusOnObject(target.transform, distance, offset);
        }
    }
}