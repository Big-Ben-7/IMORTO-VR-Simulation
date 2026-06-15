using UnityEngine;
using Microsoft.MixedReality.Toolkit.Experimental.UI;

public class KeyboardCollider : MonoBehaviour
{
    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player") && NonNativeKeyboard.Instance.gameObject.activeInHierarchy) NonNativeKeyboard.Instance.Close();
    }
}
