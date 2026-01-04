using UnityEngine;
public class Piano : MonoBehaviour
{
    bool played;
    void OnTriggerEnter(Collider other)
    {
        if (!played && other.gameObject.layer == 7)
        {
            Invoke("PlayPiano", 1);
            played = true;
        }
    }

    void PlayPiano()
    {
        Transform p = transform.parent;
        p.GetChild(0).GetComponent<AudioSource>().Play();
        p.GetComponent<Animator>().SetTrigger("Play");
    }
}
