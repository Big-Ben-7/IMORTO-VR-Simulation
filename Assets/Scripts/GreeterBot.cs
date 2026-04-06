using System.Collections;
using TMPro;
using UnityEngine;

public class GreeterBot : MonoBehaviour
{
    WaitForSeconds fourSeconds = new WaitForSeconds(4);
    WaitForSeconds twoSeconds;
    [SerializeField] AudioClip greeting;
    [SerializeField] Animation anim;
    AudioSource audioSource, botSource;
    //bool started;
    Coroutine play;
    void Awake()
    {
        AudioSource[] sources = GetComponents<AudioSource>();
        audioSource = sources[0];
        botSource = sources[1];
        twoSeconds = Player.two;
    }

    void OnTriggerEnter(Collider other)
    {
        if (botSource.isPlaying) return;
        play = StartCoroutine(Play());
    }

    void OnTriggerExit()
    {
        if (!botSource.isPlaying) return;
        StopCoroutine(play);
        botSource.Stop();
        anim.Stop();
    }

    IEnumerator Play()
    {
        botSource.Play();
        anim.Play();
        yield return Player.one;
        while (true)
        {
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
