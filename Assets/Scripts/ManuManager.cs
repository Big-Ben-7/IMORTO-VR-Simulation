using System.Collections;
using UnityEngine;

public class ManuManager : MonoBehaviour
{
    [SerializeField] float moveSpeed, timeInterval;
    [SerializeField] Transform[] parts, partMarkers;
    [SerializeField] GameObject sheetPrefab;
    MeshRenderer[] partRends;
    GameObject[] sheets;
    [SerializeField] Transform stamper, startMarker, endMarker;
    Animation stampAnim;
    float stampZ, endZ;
    Vector3 startPos;
    enum State {Idle, Moving, Stamping, Stamped};
    State[] partStates;
    int partCount;
    bool paused;
    WaitForSeconds wait = new WaitForSeconds(0.5833f);

    void Awake()
    {
        partCount = parts.Length;
        partStates = new State[partCount];
        partMarkers = new Transform[partCount];
        partRends = new MeshRenderer[partCount];
        sheets = new GameObject[partCount];
        for (int i = 0; i < partCount; i++) 
        {
            Transform partT = parts[i];
            Transform marker = partMarkers[i] = partT.GetChild(0);
            MeshRenderer rend = partRends[i] = partT.GetComponent<MeshRenderer>();
            rend.enabled = false;
            sheets[i] = Instantiate(sheetPrefab, marker.position, Quaternion.identity, partT);
        }
        stampAnim = stamper.GetComponent<Animation>();
        startPos = startMarker.position;
        endZ = endMarker.position.z;
        stampZ = stamper.position.z;
        StartCoroutine(SendParts());
    }

    IEnumerator SendParts()
    {
        for (int i = 0; i < partCount; i++)
        {
            while (paused) yield return null;
            Transform partT = parts[i];
            partT.position = startPos - partMarkers[i].localPosition;
            partT.gameObject.SetActive(true);
            partStates[i] = State.Moving;
            float t = 0;
            while (t < timeInterval)
            {
                yield return null;
                t += Time.deltaTime;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < partCount; i++)
        {
            switch(partStates[i])
            {
                case State.Idle: break;
                case State.Moving:
                    if (paused) break;
                    if (Mathf.Abs(partMarkers[i].position.z - stampZ) > 0.01f) parts[i].position += new Vector3(0, 0, moveSpeed * Time.deltaTime);
                    else partStates[i] = State.Stamping;
                    break;
                case State.Stamping:
                    if (paused) break;
                    paused = true;
                    partStates[i] = State.Stamping;
                    StartCoroutine(Stamp(i));
                    break;
                case State.Stamped:
                    if (paused) break;
                    if (Mathf.Abs(partMarkers[i].position.z - endZ) > 0.01f) parts[i].position += new Vector3(0, 0, moveSpeed * Time.deltaTime);
                    else partStates[i] = State.Idle;
                    break;
            }
        }
    }

    IEnumerator Stamp(int i)
    {
        if (!stampAnim.IsPlaying("Stamp")) stampAnim.Play();
        yield return wait;
        Destroy(sheets[i]);
        partRends[i].enabled = true;
        yield return wait;
        paused = false;
        partStates[i] = State.Stamped;
    }
}
