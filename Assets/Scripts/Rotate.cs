using UnityEngine;

public class Rotate : MonoBehaviour
{
    public Vector3 rot;
    Transform myT;
    public bool rotate = true;
    void Awake()
    {
        myT = transform;
    }
    void Update()
    {
        if (rotate) myT.Rotate(rot * Time.deltaTime);
    }
}
