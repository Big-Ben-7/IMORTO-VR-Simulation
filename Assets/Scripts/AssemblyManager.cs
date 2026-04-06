using Palmmedia.ReportGenerator.Core.Reporting.Builders;
using UnityEngine;

public class AssemblyManager : MonoBehaviour
{
    AudioSource audioSource;
    [SerializeField] AudioClip suction, unsuction, carMoving, carStart, honk;
    [SerializeField] Material headlights, redHeadlights;
    ManuManager manuManager;
    void Awake()
    {
        audioSource = GetComponentInChildren<AudioSource>(true);
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
        manuManager.CompleteAssembly();
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
        transform.parent.GetChild(3).GetComponent<AudioSource>().Stop();
    }

    public void PlayCar()
    {
        audioSource.PlayOneShot(carStart, 0.25f);
        audioSource.PlayOneShot(carMoving, 0.6f);
        MeshRenderer mr = transform.GetChild(0).GetChild(5).GetComponent<MeshRenderer>();
        Material[] mats = mr.materials;
        mats[11] = headlights;
        mats[12] = redHeadlights;
        mr.materials = mats;
    }

    public void Honk()
    {
        audioSource.PlayOneShot(honk, 0.4f);
    }
}
