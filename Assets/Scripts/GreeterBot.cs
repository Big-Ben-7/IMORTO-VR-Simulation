using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class GreeterBot : MonoBehaviour
{
    WaitForSeconds fourSeconds = new WaitForSeconds(4);
    WaitForSeconds twoSeconds;
    [SerializeField] AudioClip greeting;
    [SerializeField] Transform head;
    AudioSource audioSource, botSource, outsideSource;
    //bool started;
    bool paused;
    //float entryZ;
    bool triggered;
    void Awake()
    {
        Player.greeterBot = this;
        AudioSource[] sources = head.GetComponents<AudioSource>();
        audioSource = sources[0];
        botSource = sources[1];
        twoSeconds = Player.two;
        botSource.Play();
        StartCoroutine(Play());
        outsideSource = GetComponent<AudioSource>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Player")) return;
        //entryZ = other.transform.position.z;*/
        if (triggered) return;
        triggered = true;
        Player.instance.showMap();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.CompareTag("Player")) return;
    }

    public void Outside(bool outside)
    {
        if (!outside)
        {
            if (paused) return;
            paused = true;
            botSource.Stop();
            outsideSource.Pause();
        }
        else
        {
            if (!paused) return;
            paused = false;
            botSource.Play();
            outsideSource.UnPause();
        }
    }

    IEnumerator Play()
    {
        yield return new WaitForSeconds(1.5f);
        while (true)
        {
            while (paused) yield return null;
            botSource.Pause();
            audioSource.PlayOneShot(greeting);
            StartCoroutine(PlayBot());
            yield return fourSeconds;
        }
    }

    IEnumerator PlayBot()
    {
        yield return twoSeconds;
        if (!botSource.isPlaying) botSource.UnPause();
    }
}
