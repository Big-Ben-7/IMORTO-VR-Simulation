using UnityEngine;
using System.Collections;

public class Boundary : MonoBehaviour
{
    [SerializeField] AudioClip error;
    MeshRenderer rend;
    WaitForSeconds fifthSecond = new WaitForSeconds(0.2f);
    Coroutine flashing;
    void Awake()
    {
        rend = GetComponent<MeshRenderer>();
    }
    void OnCollisionEnter(Collision collision)
    {
        Player.audioSource.PlayOneShot(error, 0.2f);
        if (flashing != null) StopCoroutine(flashing);
        flashing = StartCoroutine(Flash());
    }

    IEnumerator Flash()
    {
        rend.enabled = true;
        yield return fifthSecond;
        rend.enabled = false;
        yield return fifthSecond;
        rend.enabled = true;
        yield return fifthSecond;
        rend.enabled = false;
    }
}
