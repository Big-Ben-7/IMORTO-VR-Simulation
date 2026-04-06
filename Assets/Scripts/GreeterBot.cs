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
    AudioSource audioSource, botSource;
    //bool started;
    bool paused = true;
    void Awake()
    {
        AudioSource[] sources = head.GetComponents<AudioSource>();
        audioSource = sources[0];
        botSource = sources[1];
        twoSeconds = Player.two;
        StartCoroutine(Play());
    }

    void OnTriggerEnter(Collider other)
    {
        if (!paused || !other.gameObject.CompareTag("Player")) return;
        paused = false;
        botSource.Play();
    }

    void OnTriggerExit(Collider other)
    {
        if (paused || !other.gameObject.CompareTag("Player")) return;
        paused = true;
        botSource.Stop();
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
