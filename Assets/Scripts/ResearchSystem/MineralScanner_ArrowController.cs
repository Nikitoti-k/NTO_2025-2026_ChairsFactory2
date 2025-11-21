using UnityEngine;

public class MineralScanner_ArrowController : MonoBehaviour
{
    [SerializeField] private RectTransform scannerArea;
    [SerializeField] private RectTransform scannerDot;

    [SerializeField] private float moveSpeed = 450f;      // пикселей/сек
    [SerializeField] private float smoothTime = 0.07f;

    private Vector2 totalInput = Vector2.zero;
    private Vector2 velocity;

    private void Awake()
    {
        // Автоматом найдёт все 4 кнопки и передаст им ссылку на себя
        foreach (var btn in GetComponentsInChildren<MineralScanner_DirectionButton>())
            btn.Controller = this;
    }

    public void AddInput(Vector2 dir)
    {
        totalInput += dir;
    }

    private void Update()
    {
        if (scannerArea == null || scannerDot == null) return;

        Vector2 move = Vector2.zero;
        if (totalInput.sqrMagnitude > 0.01f)
            move = totalInput.normalized * moveSpeed * Time.deltaTime;

        Vector2 targetPos = scannerDot.anchoredPosition + move;

        // Кламп внутри радара
        targetPos.x = Mathf.Clamp(targetPos.x, -scannerArea.rect.width * 0.5f, scannerArea.rect.width * 0.5f);
        targetPos.y = Mathf.Clamp(targetPos.y, -scannerArea.rect.height * 0.5f, scannerArea.rect.height * 0.5f);

        scannerDot.anchoredPosition = Vector2.SmoothDamp(scannerDot.anchoredPosition, targetPos, ref velocity, smoothTime);

        totalInput = Vector2.zero; // сбрасываем каждый кадр
    }
}