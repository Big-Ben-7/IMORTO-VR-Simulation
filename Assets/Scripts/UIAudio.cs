using System;
using UnityEngine;
using UnityEngine.UI;

public class UIAudio : MonoBehaviour
{
    [SerializeField] Sprite pause, play;
    Image image;
    AudioSource audioSource;
    public static Transform target;
    bool isPaused, isPlaying;
    Transform myT;
    [SerializeField] float volFactor, maxVol;

    void Awake()
    {
        image = GetComponent<Image>();
        audioSource = GetComponent<AudioSource>();
        myT = transform;
    }
    public void Click()
    {
        if (audioSource.isPlaying)
        {
            isPlaying = false;
            audioSource.Pause();
            isPaused = true;
            image.sprite = play;
        }
        else
        {
            isPlaying = true;
            if (isPaused) audioSource.UnPause();
            else audioSource.Play();
            image.sprite = pause;
        }
    }

    void Update()
    {
        if (audioSource.isPlaying)
        {
            Vector3 pos3 = myT.position;
            Vector2 pos = new Vector2(pos3.x, pos3.z);
            pos3 = target.position;
            Vector2 targetPos = new Vector2(pos3.x, pos3.z);
            audioSource.volume = Mathf.Clamp(maxVol - volFactor * (Vector2.SqrMagnitude(targetPos - pos) - 3), 0, maxVol);
            Debug.Log(Vector2.SqrMagnitude(targetPos - pos));
        }
        else if (isPlaying)
        {
            isPlaying = false;
            image.sprite = play;
        }
    }
}
