using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] AudioClip slide;
    Animator animator;
    AudioSource[] audioSources = new AudioSource[2];
    bool canTrigger = true;
    WaitForSeconds wait = new WaitForSeconds(0.5f);
    public static GameObject[] rooms;
    [SerializeField] int negRoom, posRoom;

    void Awake()
    {
        Transform parent = transform.parent;
        animator = parent.GetComponent<Animator>();
        audioSources[0] = parent.GetChild(0).GetComponent<AudioSource>();
        audioSources[1] = parent.GetChild(1).GetComponent<AudioSource>();
    }
    void OnTriggerEnter(Collider other)
    {
        if (!canTrigger) return;
        GameObject enteredObject = other.gameObject;
        if (enteredObject.CompareTag("Player"))
        {
            /*if (posRoom != -1)
            {
                if (transform.InverseTransformPoint(enteredObject.transform.position).z > 0)
                {
                    if (!rooms[negRoom].activeSelf) rooms[negRoom].SetActive(true);
                }
                else if (!rooms[posRoom].activeSelf) rooms[posRoom].SetActive(true);
            }*/
            if (Player.tp)
            {
                canTrigger = false;
                StartCoroutine(ResetTrigger());
            }
            animator.SetTrigger("Open");
            for (int i = 0; i < 2; i++) audioSources[i].PlayOneShot(slide);
        }
        
    }

    void OnTriggerExit(Collider other)
    {
        if (!canTrigger) return;
        GameObject enteredObject = other.gameObject;
        if (enteredObject.CompareTag("Player"))
        {
            animator.SetTrigger("Close");
            for (int i = 0; i < 2; i++) audioSources[i].PlayOneShot(slide);
            //StartCoroutine(DisableRoom(enteredObject.transform.position));
        }
    }

    IEnumerator DisableRoom(Vector3 pos)
    {
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName("Idle")) yield return null;
        if (posRoom != -1)
        {
            if (transform.InverseTransformPoint(pos).z > 0)
            {
                if (rooms[negRoom].activeSelf) rooms[negRoom].SetActive(false);
            }
            else if (rooms[posRoom].activeSelf) rooms[posRoom].SetActive(false);
        }
    }

    IEnumerator ResetTrigger()
    {
        yield return wait;
        canTrigger = true;
    }
}
