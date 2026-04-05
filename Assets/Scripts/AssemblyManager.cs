using UnityEngine;

public class AssemblyManager : MonoBehaviour
{
    AudioSource audioSource;
    [SerializeField] AudioClip suction, unsuction;
    ManuManager manuManager;
    void Awake()
    {
        audioSource = GetComponentInChildren<AudioSource>();
    }
    void Start()
    {
        manuManager = ManuManager.instance;
    }
    public void Suction(int i)
    {
        manuManager.Suction(i);
        audioSource.PlayOneShot(suction, 0.7f);
    }
    public void UnSuction(int i)
    {
        manuManager.UnSuction(i);
        audioSource.PlayOneShot(unsuction);
    }

    public void Complete()
    {
        manuManager.assemblyComplete = true;
    }

    public void Stamp()
    {
        manuManager.stampTrigger = true;
    }

    public void SwapTires()
    {
        manuManager.SwapTires();
    }

    public void StopAudio()
    {
        transform.parent.GetChild(4).GetComponent<AudioSource>().Stop();
    }
}
