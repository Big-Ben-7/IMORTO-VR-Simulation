using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class UIPanel : XRGrabInteractable
{
    protected Canvas canvas;
    public static AudioSource audioSource;
    [SerializeField] protected AudioClip toggle, hover;
    [SerializeField] GameObject socket;
    bool initialFollow;
    int play;
    LazyFollow lf;
    Transform myT;
    Quaternion initialRot;
    int interactors;

    protected override void Awake()
    {
        base.Awake();
        myT = transform;
        canvas = myT.GetChild(1).GetComponent<Canvas>();
        lf = GetComponent<LazyFollow>();
        initialFollow = lf.enabled;
        myT.GetPositionAndRotation(out Vector3 pos, out initialRot);
        Instantiate(socket, pos, initialRot);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        play = 0;
    }

    protected override void OnActivated(ActivateEventArgs args)
    {
        base.OnActivated(args);
        canvas.enabled = !canvas.enabled;
        audioSource.PlayOneShot(toggle);
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        /*if (play >= 2) 
        {
            audioSource.PlayOneShot(toggle);
        }
        else play ++;*/
        if (!(args.interactorObject is NearFarInteractor))
        {
            return;
        }
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        if (!(args.interactorObject is NearFarInteractor))
        {
            return;
        }
    }

    protected override void OnHoverEntered(HoverEnterEventArgs args)
    {
        base.OnHoverEntered(args);
        /*if (play > 2)
        {
            if (interactors == 0) audioSource.PlayOneShot(hover);
        }
        else play ++;*/
        if (!(args.interactorObject is NearFarInteractor))
        {
            lf.enabled = initialFollow;
            if (!initialFollow && myT) myT.rotation = initialRot;
            //if (Player.initialized) audioSource.PlayOneShot(hover);
            return;
        }
        interactors ++;
    }

    protected override void OnHoverExited(HoverExitEventArgs args)
    {
        base.OnHoverExited(args);
        if (!(args.interactorObject is NearFarInteractor))
        {
            lf.enabled = true;
            return;
        }
        interactors --;
    }

    public void Hover()
    {
        audioSource.PlayOneShot(hover);
    }
}
