using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class Racket : XRGrabInteractable
{
    Rigidbody rb;
    AudioSource audioSource;
    [SerializeField] LayerMask body;
    [SerializeField] AudioClip hit;
    string lastCollision = "";
    WaitForSeconds fifthSecond;
    Coroutine collisionCoroutine;
    public static bool grabbed;
    public static MyHandedness hand = MyHandedness.None;
    WaitForSeconds second;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        fifthSecond = Player.fifth;
        second = Player.one;
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
        rb.excludeLayers = body;
        Player.Grab();
        switch(args.interactorObject.transform.GetComponent<XRBaseInteractor>().handedness)
        {
            case InteractorHandedness.Left:
                hand = MyHandedness.Left;
                break;
            case InteractorHandedness.Right:
                hand = MyHandedness.Right;
                break;
            default:
                hand = MyHandedness.None;
                break;
        }
        for (int i = 0; i < BallLauncher.ballCount; i++) BallLauncher.balls[i].rb.excludeLayers = body;
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        grabbed = false;
        StartCoroutine(Release());
        hand = MyHandedness.None;
        for (int i = 0; i < BallLauncher.ballCount; i++) BallLauncher.balls[i].rb.excludeLayers = 0;
    }

    IEnumerator Release()
    {
        yield return second;
        if (!grabbed) rb.excludeLayers = 0;
    }
}
