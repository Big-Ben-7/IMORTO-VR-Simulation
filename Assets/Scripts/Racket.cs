using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class Racket : XRGrabInteractable
{
    Collider col;
    AudioSource audioSource;
    [SerializeField] LayerMask body;
    [SerializeField] AudioClip hit;
    string lastCollision = "";
    WaitForSeconds fifthSecond = new WaitForSeconds(0.2f);
    Coroutine collisionCoroutine;
    public static bool grabbed;
    public enum Handedness { Left, Right, None }
    public static Handedness handeness = Handedness.None;
    WaitForSeconds second = new WaitForSeconds(1);

    protected override void Awake()
    {
        base.Awake();
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

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        grabbed = true;
        col.excludeLayers = body;
        Player.Grab();
        switch(args.interactorObject.transform.GetComponent<XRBaseInteractor>().handedness)
        {
            case InteractorHandedness.Left:
                handeness = Handedness.Left;
                break;
            case InteractorHandedness.Right:
                handeness = Handedness.Right;
                break;
            default:
                handeness = Handedness.None;
                break;
        }
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        grabbed = false;
        StartCoroutine(Release());
        handeness = Handedness.None;
    }

    IEnumerator Release()
    {
        yield return second;
        if (!grabbed) col.excludeLayers = 0;
    }
}
