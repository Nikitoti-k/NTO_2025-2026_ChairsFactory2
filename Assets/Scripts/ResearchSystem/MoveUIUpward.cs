using UnityEngine;

public class MoveUIUpward : MonoBehaviour
{
    [Header("Скорость движения вверх (в единицах в секунду)")]
    public float speed = 100f;

    [Header("Двигаться в локальном или мировом пространстве?")]
    public bool useLocalPosition = true;

    // Если нужно, чтобы объект удалялся за пределами экрана
    public bool destroyWhenOffscreen = true;
    private Canvas parentCanvas;
    private RectTransform canvasRectTransform;

    private void Start()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace)
        {
            canvasRectTransform = parentCanvas.GetComponent<RectTransform>();
        }
    }

    private void Update()
    {
        if (useLocalPosition)
        {
            // Двигаем в локальных координатах (самый частый случай для UI)
            transform.localPosition += Vector3.up * speed * Time.deltaTime;
        }
        else
        {
            // Двигаем в мировых координатах (если канвас World Space и ты хочешь именно так)
            transform.position += Vector3.up * speed * Time.deltaTime;
        }

        // Опционально: удаляем объект, когда он уехал слишком высоко
        if (destroyWhenOffscreen && canvasRectTransform != null)
        {
            Vector3 viewportPos = parentCanvas.worldCamera.WorldToViewportPoint(transform.position);
            if (viewportPos.y > 1.2f) // немного за пределами экрана
            {
                Destroy(gameObject);
            }
        }
    }
}