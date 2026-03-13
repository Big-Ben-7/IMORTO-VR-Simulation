using UnityEngine;

public class ReverbZone : MonoBehaviour
{
    AudioReverbZone reverbZone;
    void Awake()
    {
        reverbZone = GetComponent<AudioReverbZone>();
        reverbZone.enabled = false;
    }
    void OnTriggerEnter(Collider other)
    {
        reverbZone.enabled = true;
    }
    void OnTriggerExit(Collider other)
    {
        reverbZone.enabled = false;
    }
}
