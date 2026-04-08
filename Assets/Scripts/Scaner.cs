using UnityEngine;

public class CleanSpawner : MonoBehaviour
{
    public GameObject unit;
    public Vector3 spawnPosition = new Vector3(0, 5, 0);
    public float lifetime = 5f; 

    private GameObject currentClone;

    private void OnTriggerEnter(Collider other)
    {

        if (other.gameObject.name.EndsWith("(Pure)")) return;


        unit = other.gameObject;


        currentClone = Instantiate(unit, spawnPosition, unit.transform.rotation);
        currentClone.name = unit.name + " (Pure)";
        currentClone.transform.SetParent(null);


        CleanLogic(currentClone);


        Destroy(currentClone, lifetime);

        Debug.Log($"={currentClone.name}. Исчезнет через {lifetime}");
    }

    private void OnTriggerExit(Collider other)
    {

        if (unit != null && other.gameObject == unit)
        {
            if (currentClone != null)
            {

                Destroy(currentClone);
                Debug.Log("клон удален.");
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