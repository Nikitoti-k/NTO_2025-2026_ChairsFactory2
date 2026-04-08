using UnityEngine;
using System.Collections;

public class CassettePlayer : MonoBehaviour
{
    [Header("Tochka Kubov")]
    public Transform kubMesto;
    
    [Header("Tochka Vibrosa")]
    public Transform vivod;
    
    [Header("Nabori")]
    public GameObject[] nabor1;
    public GameObject[] nabor2;
    public GameObject[] nabor3;
    public GameObject[] nabor4;
    
    [Header("Nastroiki")]
    public float vremya = 2f;
    public KeyCode knopka = KeyCode.E;
    public bool samoSbros = true;

    private GameObject kassetka;
    private GameObject kubik;
    private Coroutine pokaz;
    private bool zanyato;

    void Start()
    {
        if (vivod == null)
            vivod = transform;
    }

    void Update()
    {
        if (zanyato && Input.GetKeyDown(knopka))
            Sbros();
    }

    void OnTriggerEnter(Collider other)
    {
        if (zanyato) return;
        
        Cassette c = other.GetComponent<Cassette>();
        if (!c) return;

        kassetka = other.gameObject;
        zanyato = true;

        Rigidbody rr = kassetka.GetComponent<Rigidbody>();
        if (rr) rr.isKinematic = true;
        kassetka.GetComponent<Collider>().enabled = false;
        kassetka.transform.SetParent(transform);
        kassetka.transform.localPosition = Vector3.zero;
        kassetka.transform.localRotation = Quaternion.identity;

        Debug.Log($"Votknuta: {c.type}");

        GameObject[] bb = null;
        if (c.type == Cassette.CassetteType.Type1) bb = nabor1;
        else if (c.type == Cassette.CassetteType.Type2) bb = nabor2;
        else if (c.type == Cassette.CassetteType.Type3) bb = nabor3;
        else if (c.type == Cassette.CassetteType.Type4) bb = nabor4;

        if (bb == null || bb.Length == 0)
        {
            Debug.LogError($"Pusto dlya {c.type}");
            return;
        }

        if (pokaz != null) StopCoroutine(pokaz);
        pokaz = StartCoroutine(Kruzhit(bb));
    }

    void Sbros()
    {
        if (!zanyato || kassetka == null) return;

        Debug.Log("Vikinuta");

        if (pokaz != null)
        {
            StopCoroutine(pokaz);
            pokaz = null;
        }

        if (kubik != null)
        {
            Destroy(kubik);
            kubik = null;
        }

        kassetka.transform.SetParent(null);
        kassetka.transform.position = vivod.position;
        kassetka.transform.rotation = vivod.rotation;

        kassetka.GetComponent<Collider>().enabled = true;
        Rigidbody rr = kassetka.GetComponent<Rigidbody>();
        if (rr != null)
        {
            rr.isKinematic = false;
            rr.linearVelocity = Vector3.zero;
            rr.angularVelocity = Vector3.zero;
        }

        kassetka = null;
        zanyato = false;
    }

    IEnumerator Kruzhit(GameObject[] bb)
    {
        if (samoSbros)
        {
            for (int x = 0; x < bb.Length; x++)
            {
                if (kubik != null) Destroy(kubik);
                
                if (bb[x] != null)
                    kubik = Instantiate(bb[x], kubMesto.position, Quaternion.identity);
                
                yield return new WaitForSeconds(vremya);
            }
            
            if (kubik != null)
            {
                Destroy(kubik);
                kubik = null;
            }
            
            Sbros();
        }
        else
        {
            int x = 0;
            while (true)
            {
                if (kubik != null) Destroy(kubik);
                
                if (bb[x] != null)
                    kubik = Instantiate(bb[x], kubMesto.position, Quaternion.identity);
                
                yield return new WaitForSeconds(vremya);
                
                x++;
                if (x >= bb.Length) x = 0;
            }
        }
    }
}