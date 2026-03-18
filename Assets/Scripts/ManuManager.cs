using System.Collections;
using UnityEngine;

public class ManuManager : MonoBehaviour
{
    [SerializeField] float moveSpeed;
    [SerializeField] Transform[] parts;
    [SerializeField] Transform stamper, endMarker;
    Animation stampAnim;
    float stampZ, endZ;
    enum State {Idle, Moving, ToStamp, Stamping, Stamped};
    State[] partStates;
    int partCount;

    void Awake()
    {
        partCount = parts.Length;
        partStates = new State[partCount];
        partStates[0] = State.Moving;
        stampAnim = stamper.GetComponent<Animation>();
        stampZ = stamper.position.z;
        endZ = endMarker.position.z;
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
                    Vector3 pos = parts[i].position;
                    if (Mathf.Abs(pos.z - stampZ) > 0.01f) parts[i].position = pos + new Vector3(0, 0, moveSpeed * Time.deltaTime);
                    else partStates[i] = State.ToStamp;
                    break;
                case State.ToStamp:
                    if (!stampAnim.isPlaying) stampAnim.Play();
                    partStates[i] = State.Stamping;
                    break;
                case State.Stamping:
                    if (!stampAnim.isPlaying) partStates[i] = State.Stamped;
                    break;
                case State.Stamped:
                    pos = parts[i].position;
                    if (Mathf.Abs(pos.z - endZ) > 0.01f) parts[i].position = pos + new Vector3(0, 0, moveSpeed * Time.deltaTime);
                    else partStates[i] = State.Idle;
                    break;
            }
        }
    }
}
