using UnityEngine;
using System.Collections;

public class TrailRendererLocal : MonoBehaviour
{
    public Transform[] targets;
    public int targetCount;
    //[SerializeField] float distIncrement = 0.1f;
    [SerializeField] float yOffset;
    //[SerializeField] bool limitLength;
    //[SerializeField] int maxPos = 10;
    LineRenderer[] lines;
    //Vector3 lastPos;
    WaitForSeconds wait = new WaitForSeconds(1 / 24.0f);
    Coroutine update;
    Transform myT;

    void Awake()
    {
        myT = transform;
        lines = GetComponentsInChildren<LineRenderer>();
        targetCount = targets.Length;
        for (int i = 0; i < targetCount; i++)
        {
            lines[i].positionCount = 0;
            AddPoint(i, targets[i].localPosition);
        }
    }

    IEnumerator MyUpdate()
    {
        while (true)
        {
            for (int i = 0; i < targetCount; i++)
            {
                Vector3 pos = targets[i].localPosition;
                AddPoint(i, pos + myT.InverseTransformDirection(new Vector3(0, yOffset, 0)));
                //if (Vector3.Distance(pos, lastPos) > distIncrement) AddPoint(i, targets[i].localPosition);
            }
            yield return wait;
        }
    }

    void AddPoint(int i, Vector3 newPos)
    {
        LineRenderer line = lines[i];
        line.positionCount ++;
        int n = line.positionCount;
        line.SetPosition(n - 1, newPos);
        //if (limitLength && n > maxPos) Truncate(i);
        //lastPos = newPos;
    }

    /*void Truncate(int i)
    {
        LineRenderer line = lines[i];
        int n = line.positionCount - 1;
        Vector3[] positions = new Vector3[n];
        for (int j = 0; j < n; j++) positions[j] = line.GetPosition(j + 1);
        line.positionCount--;
        line.SetPositions(positions);
    }*/

    public void ToggleTrail(bool activated)
    {
        for (int i = 0; i < targetCount; i++) lines[i].enabled = activated;
        if (activated && update == null) update = StartCoroutine(MyUpdate());
        else if (!activated && update != null)
        {
            StopCoroutine(MyUpdate());
            update = null;
        }
    }
}
