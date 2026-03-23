using UnityEngine;
public class Piano : MonoBehaviour
{
    bool played;
    public static Drone drone;
    AudioSource audioSource;
    Animation anim;
    AnimationState key;

    void Awake()
    {
        Transform p = transform.parent;
        audioSource = p.GetChild(0).GetComponent<AudioSource>();
        anim = p.GetComponent<Animation>();
        key = anim["KeyAnimation"];
    }
    void OnTriggerEnter(Collider other)
    {
        if (!played)
        {
            Invoke("PlayPiano", 1);
            played = true;
        }
        else
        {
            audioSource.UnPause();
            key.speed = 1;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (played)
        {
            audioSource.Pause();
            key.speed = 0;
        }
        drone.Reset();
    }

    void PlayPiano()
    {
        audioSource.Play();
        anim.Play();
    }
}
