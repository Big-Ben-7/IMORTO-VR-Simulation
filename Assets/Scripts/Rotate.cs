using System.Collections;
using UnityEngine;

public class Rotate : MonoBehaviour
{
    public Vector3 rot;
    Vector3 initialRot, norm;
    float mag;
    Transform myT;
    public bool rotate = true;
    [SerializeField] bool localSpace = true, flatten;
    [SerializeField] float flattenSpeed;
    //[SerializeField] float limit1, limit2;
    Coroutine ease;
    void Awake()
    {
        myT = transform;
        initialRot = rot;
        norm = rot.normalized;
        mag = rot.sqrMagnitude;
    }
    void Update()
    {
        if (rotate)
        {
            myT.Rotate(rot * Time.deltaTime, localSpace? Space.Self : Space.World);
            /*if (limit1 != limit2)
            {
                if (myT.eulerAngles.y <= limit1 || myT.eulerAngles.y >= limit2) rot *= -1;
            }*/
            if (flatten)
            {
                Quaternion cur = myT.rotation;
                myT.rotation = Quaternion.RotateTowards(cur, Quaternion.Euler(0, cur.eulerAngles.y, 0), flattenSpeed * Time.deltaTime);
            }
        }
    }
    public void SetRotate(bool b, float easeSpeed)
    {
        if (easeSpeed > 0)
        {
            if (ease != null) StopCoroutine(ease);
            if (b) ease = StartCoroutine(EaseIn(easeSpeed));
            else ease = StartCoroutine(EaseOut(easeSpeed));
        }
        else rotate = b;
    }
    public void SetRotate(bool b)
    {
        rotate = b;
    }
    IEnumerator EaseIn(float easeSpeed)
    {
        if (rot == initialRot) rot = Vector3.zero;
        rotate = true;
        Vector3 inc = norm * easeSpeed;
        while (rot.sqrMagnitude < mag)
        {
            rot += inc * Time.deltaTime;
            yield return null;
        }
        rot = initialRot;
        ease = null;
    }

    IEnumerator EaseOut(float easeSpeed)
    {
        Vector3 inc = norm * easeSpeed;
        while (Vector3.Dot(rot, initialRot) > 0)
        {
            rot -= inc * Time.deltaTime;
            yield return null;
        }
        rot = Vector3.zero;
        ease = null;
        rotate = false;
    }
}
