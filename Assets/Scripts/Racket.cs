using System.Collections;
using UnityEngine;

public class Racket : MonoBehaviour
{
    Collider col;
    AudioSource audioSource;
    [SerializeField] LayerMask body;
    [SerializeField] AudioClip hit;
    string lastCollision = "";
    WaitForSeconds fifthSecond = new WaitForSeconds(0.2f);
    Coroutine collisionCoroutine;
    public static bool grabbed;
    WaitForSeconds second = new WaitForSeconds(1);

    void Awake()
    {
        col = transform.GetChild(1).GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!Player.initialized) return;
        GameObject go = collision.gameObject;
        if (go.layer == 3 || go.layer == 7) return;
        string name = go.name;
        if (!grabbed && lastCollision != name)
        {
            audioSource.PlayOneShot(hit, Mathf.Clamp01(Mathf.Log10(collision.relativeVelocity.magnitude * 2 + 1)));
            lastCollision = name;
            if (collisionCoroutine != null) StopCoroutine(collisionCoroutine);
            collisionCoroutine = StartCoroutine(ResetLastCollision());
        }
    }

    IEnumerator ResetLastCollision()
    {
        string t = lastCollision;
        yield return fifthSecond;
        if (lastCollision == t) lastCollision = "";
    }

    public void Grab()
    {
        grabbed = !grabbed;
        if (grabbed)
        {
            col.excludeLayers = body;
            Player.Grab();
        }
        else StartCoroutine(Release());
    }

    IEnumerator Release()
    {
        yield return second;
        if (!grabbed) col.excludeLayers = 0;
    }
}
