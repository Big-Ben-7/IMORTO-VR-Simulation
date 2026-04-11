using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit.Samples.Hands;

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

    /*void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Player")) return;
        entryZ = other.transform.position.z;
    }*/

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
        yield return Player.one;
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
