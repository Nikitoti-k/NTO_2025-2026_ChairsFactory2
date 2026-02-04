using UnityEngine;
using System.Collections;

public class ObjectBlink : MonoBehaviour
{
    [Header("Объект для мигания (если пусто — сам этот объект)")]
    public GameObject targetObject;

    [Header("Время в секундах: включён / выключен")]
    public float onTime = 0.5f;
    public float offTime = 0.5f;

    [Header("Запускать автоматически при старте?")]
    public bool startOnAwake = true;

    private void Awake()
    {
        if (targetObject == null)
            targetObject = gameObject;
    }

    private void Start()
    {
        if (startOnAwake)
            StartBlinking();
    }

   
    public void StartBlinking()
    {
        StopAllCoroutines(); 
        StartCoroutine(BlinkRoutine());
    }

    public void StopBlinking()
    {
        StopAllCoroutines();
    }

    private IEnumerator BlinkRoutine()
    {
        while (true)
        {
            targetObject.SetActive(true);
            yield return new WaitForSeconds(onTime);

            targetObject.SetActive(false);
            yield return new WaitForSeconds(offTime);
        }
    }
}