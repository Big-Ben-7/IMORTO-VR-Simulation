using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TeleportEffect : MonoBehaviour
{
    // Start is called before the first frame update
    void OnEnable()
    {
        GetComponent<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea>().activated.AddListener(delegate { Manager.instance.Teleport(); });
    }
    void OnDisable()
    {
        GetComponent<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea>().activated.RemoveListener(delegate { Manager.instance.Teleport(); });
    }
}
