using UnityEngine;

public class ManuCol : MonoBehaviour
{
    [SerializeField] GameObject[] gos;
    Canvas[] canvases = new Canvas[2];
    //MeshRenderer[] mrs = new MeshRenderer[2];
    //Collider[] cols = new Collider[2];
    void Start()
    {
        for (int i = 0; i < 2; i++)
        {
            GameObject go = gos[i];
            canvases[i] = go.transform.GetChild(1).GetComponent<Canvas>();
            //mrs[i] = go.GetComponent<MeshRenderer>();
            //cols[i] = go.GetComponent<Collider>();
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            for (int i = 0; i < 2; i++)
            {
                canvases[i].enabled = false;
                //mrs[i].enabled = false;
            }
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            for (int i = 0; i < 2; i++)
            {
                canvases[i].enabled = true;
                //mrs[i].enabled = true;
            }
        }
    }
}
