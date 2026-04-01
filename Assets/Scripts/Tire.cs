using UnityEngine;
using System.Collections;
using System;

public class Tire : MonoBehaviour
{
    [SerializeField] AudioClip hit;
    string lastCollision;
    WaitForSeconds fifthSecond;
    bool grabbed;
    AudioSource audioSource;
    Coroutine collisionCoroutine;
    Collider col;
    [SerializeField] LayerMask body;
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        col = GetComponent<Collider>();
        fifthSecond = Player.fifth;
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
            Player.Grab();
            col.excludeLayers = body;
        }
        else col.excludeLayers = 0;
    }
}
