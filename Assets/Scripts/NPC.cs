using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.PlayerLoop;

public class NPC : MonoBehaviour
{
    [SerializeField] Transform[] destinations;
    [SerializeField] float[] times;
    [SerializeField] float initialTime, rotSpeed;
    NavMeshAgent agent;
    Animator animator;
    Vector3[] destinationPos, destinationDir;
    WaitForSeconds[] waits;
    int destinationCount;
    int destinationIndex;
    int idleHash = Animator.StringToHash("Idle");
    int walkHash = Animator.StringToHash("Walk");
    int pointHash = Animator.StringToHash("Point");
    int talkHash = Animator.StringToHash("Talk");
    enum Anim {IDLE, TALK, POINT};
    [SerializeField] Anim[] anims;
    [SerializeField] float[] animTimes;
    WaitForSeconds[] animWaits;
    bool stopTriggered;
    Transform myT;

    void Awake()
    {
        myT = transform;
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        destinationCount = destinations.Length;
        destinationPos = new Vector3[destinationCount];
        destinationDir = new Vector3[destinationCount];
        waits = new WaitForSeconds[destinationCount];
        animWaits = new WaitForSeconds[destinationCount];
        for (int i = 0; i < destinationCount; i++)
        {
            Transform t = destinations[i];
            destinationPos[i] = t.position;
            destinationDir[i] = t.forward;
            float animTime = animTimes[i];
            waits[i] = new WaitForSeconds(times[i] - animTime);
            animWaits[i] = new WaitForSeconds(animTime);
        }
    }

    void Start()
    {
        StartCoroutine(Move());
    }

    IEnumerator Move()
    {
        yield return new WaitForSeconds(initialTime);
        while (true)
        {
            while (animator.GetCurrentAnimatorStateInfo(0).shortNameHash != idleHash) yield return null;
            agent.SetDestination(destinationPos[destinationIndex]);
            animator.SetTrigger(walkHash);
            float lastVel = agent.velocity.magnitude;
            stopTriggered = false;
            float diff = agent.remainingDistance - agent.stoppingDistance;
            while (agent.pathPending || diff >= 0 || agent.hasPath)
            {
                float vel = agent.velocity.magnitude;
                if (vel >= lastVel) animator.speed = vel / 1.5f;
                else animator.speed = 1;
                lastVel = vel;
                diff = agent.remainingDistance - agent.stoppingDistance;
                if (!stopTriggered && agent.hasPath && vel > 0.01f && diff < 0.5f)
                {
                    stopTriggered = true;
                    animator.SetTrigger(idleHash);
                }
                yield return null;
            }
            if (!stopTriggered) animator.SetTrigger(idleHash);
            while (animator.GetCurrentAnimatorStateInfo(0).shortNameHash != idleHash) yield return null;
            animator.speed = 1;
            Vector3 v = destinationDir[destinationIndex];
            Quaternion rot = myT.rotation, targetRot = Quaternion.LookRotation(new Vector3(v.x, 0, v.z));
            while (Quaternion.Angle(rot, targetRot) > 1)
            {
                myT.rotation = rot = Quaternion.Slerp(rot, targetRot, Time.deltaTime * rotSpeed);
                yield return null;
            }
            switch (anims[destinationIndex])
            {
                case Anim.IDLE: break;
                case Anim.POINT:
                    yield return animWaits[destinationIndex];
                    animator.SetTrigger(pointHash);
                    break;
                case Anim.TALK:
                    yield return animWaits[destinationIndex];
                    animator.SetTrigger(talkHash);
                    break;
            }
            yield return waits[destinationIndex];
            destinationIndex = (destinationIndex + 1) % destinationCount;
        }
    }
}
