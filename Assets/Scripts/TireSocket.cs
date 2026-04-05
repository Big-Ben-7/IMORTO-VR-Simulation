using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using TMPro;

public class TireSocket : XRSocketInteractor
{
    [SerializeField] GameObject socketTire;
    [SerializeField] Color greenColor;
    [SerializeField] TMP_Text tireText;
    [SerializeField] AudioClip tiresReady;
    WaitForSeconds tenthSecond;
    ManuManager manuManager;
    protected override void Awake()
    {
        base.Awake();
        tenthSecond = Player.tenth;
    }
    protected override void Start()
    {
        base.Start();
        manuManager = ManuManager.instance;
    }
    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        if (args.interactableObject is Tire tire)
        {
            socketActive = false;
            tire.Freeze();
            StartCoroutine(SwapTire(tire.gameObject));
            int tires = manuManager.tiresReady;
            tires = manuManager.tiresReady = tires + 1;
            if (tires == 4)
            {
                tireText.text = "Assembly Ready";
                tireText.color = greenColor;
                manuManager.tireSource.PlayOneShot(tiresReady, 0.1f);
            }
            else
            {
                tireText.text = "Tires Secured: " + tires + "/4";
            }
        }
    }

    IEnumerator SwapTire(GameObject tire)
    {
        yield return tenthSecond;
        tire.SetActive(false);
        socketTire.SetActive(true);
    }
}
