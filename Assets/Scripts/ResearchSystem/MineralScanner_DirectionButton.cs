using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MineralScanner_DirectionButton : MonoBehaviour
{
    [Header("Направление")]
    public Vector2 Direction = Vector2.zero;

    [Header("Материалы (опционально)")]
    public Material defaultMat;
    public Material hoverMat;
    public Material pressedMat;

    private Renderer rend;
    private Material originalMat;
    private bool isPressed = false;
    public MineralScanner_ArrowController Controller { get; set; }

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        originalMat = rend.sharedMaterial;
        defaultMat ??= originalMat;
    }

    private void Update()
    {
        if (isPressed && Controller != null)
            Controller.AddInput(Direction);
    }

    // Основной способ — через встроенный OnMouse (работает всегда, если коллайдер есть)
    private void OnMouseOver()
    {
        if (Input.GetMouseButton(0))
        {
            isPressed = true;
            if (pressedMat) rend.material = pressedMat;
        }
        else
        {
            isPressed = false;
            if (hoverMat) rend.material = hoverMat;
        }
    }

    private void OnMouseExit()
    {
        isPressed = false;
        rend.material = defaultMat;
    }

    // Резервный способ — ручной рэйкаст (на случай если OnMouse не сработал)
    private void OnMouseEnter()
    {
        if (hoverMat && !isPressed)
            rend.material = hoverMat;
    }
}