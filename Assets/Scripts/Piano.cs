using UnityEngine;
public class Piano : MonoBehaviour
{
    bool played;
    public static Drone drone;
    void OnTriggerEnter(Collider other)
    {
        if (!played)
        {
            Invoke("PlayPiano", 1);
            played = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        drone.Reset();
    }

    void PlayPiano()
    {
        Transform p = transform.parent;
        p.GetChild(0).GetComponent<AudioSource>().Play();
        p.GetComponent<Animator>().SetTrigger("Play");
    }
}
