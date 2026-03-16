using UnityEngine;

public class CleanSpawner : MonoBehaviour
{
    public GameObject unit;
    public Vector3 spawnPosition = new Vector3(0, 5, 0);
    public float lifetime = 5f; // Максимальное время жизни клона

    private GameObject currentClone; // Храним ссылку на созданный клон

    private void OnTriggerEnter(Collider other)
    {
        // Игнорируем сами клоны, чтобы не было бесконечного цикла
        if (other.gameObject.name.EndsWith("(Pure)")) return;

        // Запоминаем объект, который вошел
        unit = other.gameObject;

        // 1. Создаем копию
        currentClone = Instantiate(unit, spawnPosition, unit.transform.rotation);
        currentClone.name = unit.name + " (Pure)";
        currentClone.transform.SetParent(null);

        // 2. Очищаем от логики (оставляем только визуал)
        CleanLogic(currentClone);

        // 3. Уничтожаем через заданное время (если объект не выйдет раньше)
        Destroy(currentClone, lifetime);

        Debug.Log($"Создана копия {currentClone.name}. Исчезнет через {lifetime} сек или при выходе из зоны.");
    }

    // Срабатывает в момент "отрыва" / выхода из зоны
    private void OnTriggerExit(Collider other)
    {
        // Если зону покинул именно тот объект, который мы копировали
        if (unit != null && other.gameObject == unit)
        {
            if (currentClone != null)
            {
                // Время на ноль: удаляем клон немедленно
                Destroy(currentClone);
                Debug.Log("Связь разорвана: клон удален.");
            }
        }
        
        if (other.CompareTag("Player")) 
        {
            Debug.Log("игрок -");
        }
    }

    private void CleanLogic(GameObject target)
    {
        Component[] components = target.GetComponentsInChildren<Component>();

        foreach (var comp in components)
        {
            if (comp is Rigidbody rb)
            {
                rb.isKinematic = true; 
                rb.linearVelocity = Vector3.zero;
            }
        }

        for (int i = components.Length - 1; i >= 0; i--)
        {
            Component comp = components[i];

            if (!(comp is Transform || 
                  comp is MeshFilter || 
                  comp is MeshRenderer || 
                  comp is SkinnedMeshRenderer))
            {
                Destroy(comp);
            }
        }
    }
}