using UnityEngine;

public class MoveObject : MonoBehaviour
{
    [Header("Настройки движения")]
    public float moveDistance = 50f; 
    private RectTransform rectTransform;
    
    [Header("Ограничения")]
    public float minY = 0f;
    public float maxY = 500f;
    
    [Header("Счетчик шагов")]
    private int stepCount = 0;
    public int maxSteps = 10;
    public int minSteps = 0;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("RectTransform не найден!");
        }
    }

    public void MoveUp()
    {
        if (rectTransform == null) return;

        if (stepCount < maxSteps)
        {
            float newY = rectTransform.anchoredPosition.y + moveDistance;
            
            if (newY <= maxY)
            {
                rectTransform.anchoredPosition = new Vector2(
                    rectTransform.anchoredPosition.x,
                    newY
                );
                stepCount++;
            }
        }
    }

    public void MoveDown()
    {
        if (rectTransform == null) return;

        if (stepCount > minSteps)
        {
            float newY = rectTransform.anchoredPosition.y - moveDistance;
            
            if (newY >= minY)
            {
                rectTransform.anchoredPosition = new Vector2(
                    rectTransform.anchoredPosition.x,
                    newY
                );
                stepCount--;
            }
        }
    }

    public void ResetPosition()
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = Vector2.zero;
            stepCount = 0;
        }
    }
}