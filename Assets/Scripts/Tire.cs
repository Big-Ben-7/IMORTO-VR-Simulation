using UnityEngine;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class Tire : XRGrabInteractable
{
    [SerializeField] AudioClip hit, socket;
    string lastCollision;
    WaitForSeconds fifthSecond;
    bool grabbed;
    AudioSource audioSource;
    Coroutine collisionCoroutine;
    Collider col;
    [SerializeField] LayerMask body;
    protected override void Awake()
    {
        base.Awake();
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


    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        if (args.interactorObject is XRSocketInteractor)
        {
            audioSource.PlayOneShot(socket);
            return;
        }
        grabbed = true;
        Player.Grab();
        col.excludeLayers = body;
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        if (args.interactorObject is XRSocketInteractor)
        {
            return;
        }
        grabbed = false;
        col.excludeLayers = 0;
    }

    public void Freeze()
    {
        GetComponent<Rigidbody>().isKinematic = true;
        col.enabled = false;
    }
}
