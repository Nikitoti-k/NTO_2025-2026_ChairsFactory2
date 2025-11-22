using UnityEngine;
using UnityEngine.EventSystems;

public class UI_IgnorePhysicsRaycast : MonoBehaviour, ICanvasRaycastFilter
{
    // Ётот метод вызываетс€ EventSystem перед тем, как отправить луч на UI
    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        // ѕровер€ем: если игрок держит объект Ч разрешаем клик по UI
        // если не держит Ч тоже разрешаем (всегда разрешаем!)
        // Ётот фильтр просто ќ“ Ћё„ј≈“ проверку Physics Raycaster дл€ этого Canvas
        return true;
    }
}