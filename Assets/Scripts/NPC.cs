using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPC : MonoBehaviour
{
    //[SerializeField] Transform initialPos;
    [SerializeField] float initialTime;
    [SerializeField] int destinationIndex, anim;
    [SerializeField] Transform destinationParent;
    [SerializeField] float minTime, maxTime, minAnimTime, maxAnimTime, rotSpeed;
    NavMeshAgent agent;
    NavMeshObstacle obstacle;
    Animator animator;
    Vector3[] destinationPos, destinationDir;
    int destinationCount;
    int idleHash = Animator.StringToHash("Idle");//, walkHash = Animator.StringToHash("Walk"),
    int pointHash = Animator.StringToHash("Point"), talkHash = Animator.StringToHash("Talk"), speedHash = Animator.StringToHash("Speed");
    Transform myT;
    static List<int> availableIndices = new List<int>() {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15};
    //WaitForSeconds wait = new WaitForSeconds(0.02f);
    float myVel;
    Vector2 lastPos;
    float lastVel;
    AudioSource audioSource;
    public static LayerMask groundMask;
    public static AudioClip tileWalk, concreteWalk, gravelWalk, metalWalk, grassWalk;
    void Awake()
    {
        myT = transform;
        audioSource = GetComponent<AudioSource>();
        agent = GetComponent<NavMeshAgent>();
        obstacle = GetComponent<NavMeshObstacle>();
        animator = GetComponent<Animator>();
        destinationCount = destinationParent.childCount;
        destinationPos = new Vector3[destinationCount];
        destinationDir = new Vector3[destinationCount];
        int j = 0;
        for (int i = 0; i < destinationCount; i++)
        {
            Transform t = destinationParent.GetChild(i);
            if (!t.gameObject.activeSelf) continue;
            destinationPos[j] = t.position;
            destinationDir[j] = t.forward;
            j++;
        }
        availableIndices.Remove(destinationIndex);
    }

    void Start()
    {
        //myT.SetPositionAndRotation(initialPos.position, initialPos.rotation);
        //StartCoroutine(CalculateVelocity());
        lastPos = new Vector2(myT.position.x, myT.position.z);
        StartCoroutine(Move());
    }

    void Update()
    {
        /*if (myVel > lastVel - 0.01f) animator.speed = myVel / 1.5f;
        else animator.speed = 1;
        animator.SetFloat(speedHash, myVel);*/
        lastVel = myVel;
        Vector3 pos = myT.position;
        Vector2 currentPos = new Vector2(pos.x, pos.z);
        myVel = ((currentPos - lastPos) / Time.deltaTime).magnitude;
        lastPos = currentPos;

        if (Physics.Raycast(pos + Vector3.up, Vector3.down, out RaycastHit hit, 10, groundMask))
        {
            //bool play = true;
            GameObject go = hit.transform.gameObject;
            if (go.CompareTag("Floor") || go.CompareTag("Entertainment") || go.CompareTag("Healthcare") || go.CompareTag("Exploration") || Vector2.SqrMagnitude(new Vector2(pos.x, pos.z)) <= 20.930625)
            {
                if (audioSource.clip != tileWalk)
                {
                    audioSource.volume = 0.3f;
                    audioSource.clip = tileWalk;
                }
            }
            else if (go.CompareTag("Metal"))
            {
                if (audioSource.clip != metalWalk)
                {
                    audioSource.volume = 0.2f;
                    audioSource.clip = metalWalk;
                }
            }
            else if (go.CompareTag("Grass"))
            {
                if (Vector2.SqrMagnitude(new Vector2(pos.x, pos.z)) <= 441)
                {
                    if (audioSource.clip != gravelWalk)
                    {
                        audioSource.volume = 0.2f;
                        audioSource.clip = gravelWalk;
                    }
                }
                else
                {
                    if (audioSource.clip != grassWalk)
                    {
                        audioSource.volume = 0.07f;
                        audioSource.clip = grassWalk;
                    }
                }
            }
            else
            {
                if (audioSource.clip != concreteWalk)
                {
                    audioSource.volume = 0.3f;
                    audioSource.clip = concreteWalk;
                }
            }
            if (myVel > 0.5f)
            {
                if (!audioSource.isPlaying)
                {
                    audioSource.Play();
                }
            }
            else if (audioSource.isPlaying) audioSource.Stop();
        }
    }

    IEnumerator Move()
    {
        yield return new WaitForSeconds(initialTime);
        while (true)
        {
            while (animator.GetCurrentAnimatorStateInfo(0).shortNameHash != idleHash) yield return null;
            //agent.enabled = true;
            //obstacle.enabled = false;
            agent.SetDestination(destinationPos[destinationIndex]);
            //animator.SetTrigger(walkHash);
            /*float lastVel = agent.velocity.magnitude;
            float diff = agent.remainingDistance - agent.stoppingDistance;
            while (agent.pathPending || diff >= 0 || agent.hasPath || myVel > 0.01f)
            {
                float vel = agent.velocity.magnitude;
                if (vel >= lastVel) animator.speed = vel / 1.5f;
                else animator.speed = 1;
                lastVel = vel;
                diff = agent.remainingDistance - agent.stoppingDistance;
                /*if (!stopTriggered && agent.hasPath && lastVel > 0.01f && diff < 0.5f)
                {
                    stopTriggered = true;
                    animator.SetTrigger(idleHash);
                }
                yield return null;
            }
            //if (!stopTriggered) animator.SetTrigger(idleHash);
            //animator.SetTrigger(idleHash);*/
            while (agent.pathPending || Vector2.Distance(new Vector2(myT.position.x, myT.position.z), new Vector2(destinationPos[destinationIndex].x, destinationPos[destinationIndex].z)) > agent.stoppingDistance || agent.hasPath)
            {
                if (myVel > 1e-5 && myVel >= lastVel) animator.speed = myVel / 1.5f;
                else animator.speed = 1;
                animator.SetFloat(speedHash, myVel);
                yield return null;
            }
            //agent.enabled = false;
            //obstacle.enabled = true;
            animator.speed = 1;
            animator.SetFloat(speedHash, 0);
            //yield return tenth;
            //while (animator.GetCurrentAnimatorStateInfo(0).shortNameHash != idleHash) yield return null;
            animator.speed = 1;
            Vector3 v = destinationDir[destinationIndex];
            Quaternion rot = myT.rotation, targetRot = Quaternion.LookRotation(new Vector3(v.x, 0, v.z));
            while (Quaternion.Angle(rot, targetRot) > 1)
            {
                myT.rotation = rot = Quaternion.Slerp(rot, targetRot, Time.deltaTime * rotSpeed);
                yield return null;
            }

            switch (anim)
            {
                case 0: break;
                case 1:
                    yield return new WaitForSeconds(Random.Range(minAnimTime, maxAnimTime));
                    animator.SetTrigger(pointHash);
                    break;
                case 2:
                    yield return new WaitForSeconds(Random.Range(minAnimTime, maxAnimTime));
                    animator.SetTrigger(talkHash);
                    break;
            }
            yield return new WaitForSeconds(Random.Range(minTime, maxTime));
            switch(Random.Range(0, 1f))
            {
                case < 0.35f:
                    anim = 1;
                    break;
                case < 0.8f:
                    anim = 2;
                    break;
                default:
                    anim = 0;
                    break;
            }
            availableIndices.Add(destinationIndex);
            destinationIndex = availableIndices[Random.Range(0, availableIndices.Count)];
            availableIndices.Remove(destinationIndex);
        }
    }
}
