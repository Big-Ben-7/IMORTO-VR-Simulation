using UnityEngine;

public class UIButtonAudio : MonoBehaviour
{
    [SerializeField] AudioClip hover, click;
    public static AudioSource audioSource;
    public void Hover(float vol)
    {
        audioSource.PlayOneShot(hover, Mathf.Clamp01(vol));
    }
    public void Click(float vol)
    {
        audioSource.PlayOneShot(click, Mathf.Clamp01(vol));
    }
}
