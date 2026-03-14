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
    GameObject mySocket;
    bool initialFollow;
    //int play;
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
        mySocket = Instantiate(socket, pos, initialRot);
    }

    /*protected override void OnEnable()
    {
        base.OnEnable();
        //play = 0;
    }*/

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
        if (args.interactorObject is XRSocketInteractor)
        {
            return;
        }
        audioSource.PlayOneShot(toggle);
        interactors ++;
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        if (args.interactorObject is XRSocketInteractor)
        {
            return;
        }
        interactors --;
    }

    protected override void OnHoverEntered(HoverEnterEventArgs args)
    {
        base.OnHoverEntered(args);
        /*if (play > 2)
        {
            if (interactors == 0) audioSource.PlayOneShot(hover);
        }
        else play ++;*/
        if (args.interactorObject is XRSocketInteractor)
        {
            lf.enabled = initialFollow;
            if (!initialFollow && myT) myT.rotation = initialRot;
            if (Player.initialized) audioSource.PlayOneShot(hover);
            return;
        }
        if (Player.initialized && interactors == 0) audioSource.PlayOneShot(hover);
        interactors ++;
    }

    protected override void OnHoverExited(HoverExitEventArgs args)
    {
        base.OnHoverExited(args);
        if (args.interactorObject is XRSocketInteractor)
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

    public void Select()
    {
        audioSource.PlayOneShot(toggle);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (mySocket) mySocket.SetActive(false);
    }
}
