using UnityEngine;

public class SurgicalSpot : MonoBehaviour
{
    bool hit;
    [SerializeField] Material greenMat;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip hitClip;

    void OnTriggerEnter(Collider other)
    {
        if (hit) return;
        hit = true;
        GetComponent<Animator>().SetTrigger("Hit");
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material = greenMat;
        meshRenderer.material.color = greenMat.color;
        audioSource.PlayOneShot(hitClip);
    }
}
