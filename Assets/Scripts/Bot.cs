using UnityEngine;

public class Bot : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Table") Ball.collidingTable = true;
    }
    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Table") Ball.collidingTable = false;
    }
}
