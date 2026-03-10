using UnityEngine;

public class Bot : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Table")) Ball.collidingTable = true;
    }
    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Table")) Ball.collidingTable = false;
    }
}
