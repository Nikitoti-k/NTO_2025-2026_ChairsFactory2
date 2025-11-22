using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Camera))]
public class ForceUIAlwaysClickable : MonoBehaviour, ISerializationCallbackReceiver
{
    [SerializeField] private Canvas targetCanvas; // твой World Space Canvas с кнопкой
    private PhysicsRaycaster physicsRaycaster;

    private void Awake()
    {
        physicsRaycaster = GetComponent<PhysicsRaycaster>();
        if (physicsRaycaster == null)
            physicsRaycaster = gameObject.AddComponent<PhysicsRaycaster>();

        // КЛЮЧЕВАЯ СТРОКА — ВЫРУБАЕМ ФИЗИЧЕСКИЙ РЕЙКАСТ ДЛЯ UI СОВСЕМ
        physicsRaycaster.eventMask = 0; // НИЧЕГО НЕ ЛОВИМ ФИЗИКОЙ
    }

    private void Update()
    {
        // Принудительно говорим EventSystem: "UI доступен всегда"
        if (targetCanvas != null && EventSystem.current != null)
        {
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
            };

            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, results);

            // Если хотя бы один результат — UI (наш Canvas) — разрешаем клик
            bool uiUnderPointer = false;
            foreach (var r in results)
            {
                if (r.gameObject.GetComponentInParent<Canvas>() == targetCanvas)
                {
                    uiUnderPointer = true;
                    break;
                }
            }

            // Принудительно разрешаем UI, даже если физика мешает
            if (uiUnderPointer)
            {
                targetCanvas.overrideSorting = true;
                targetCanvas.sortingOrder = 9999;
            }
        }
    }

    public void OnBeforeSerialize() { }
    public void OnAfterDeserialize()
    {
        // Автоматически найдёт твой World Space Canvas при старте
        if (targetCanvas == null)
        {
            var canvases = FindObjectsOfType<Canvas>();
            foreach (var c in canvases)
            {
                if (c.renderMode == RenderMode.WorldSpace && c.gameObject.name.Contains("Scanner"))
                {
                    targetCanvas = c;
                    break;
                }
            }
        }
    }
}