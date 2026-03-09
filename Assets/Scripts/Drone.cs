using System.Collections;
using UnityEngine;

public class Drone : MonoBehaviour
{
    [SerializeField] Transform pathPoints;
    Vector3[] path;
    [SerializeField] Transform aim;
    Vector3 aimPos;
    [SerializeField] float speed;
    [SerializeField] float launchSpeed = 0.3f;
    [SerializeField] AudioClip launch;
    AudioSource audioSource;
    Transform myT;
    int pointCount, nextPoint = 0;
    enum State {Idle, Launch, Fly, Reset};
    State state = State.Idle;
    Vector3 launchPos, launchTarget;
    Animator animator;
    Canvas block;
    WaitForSeconds fifthSecond = new WaitForSeconds(0.2f);
    bool cooldown;

    void Awake()
    {
        myT = transform;
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        aimPos = aim.position;
        pointCount = pathPoints.childCount;
        path = new Vector3[pointCount];
        for (int i = 0; i < pointCount; i++) path[i] = pathPoints.GetChild(i).position;
        Piano.drone = this;
        block = myT.GetChild(11).gameObject.GetComponent<Canvas>();
    }

    void Start()
    {
        launchPos = myT.position;
        launchTarget = new Vector3(launchPos.x, 3, launchPos.z);
    }

    void Update()
    {
        if (state == 0) return;
        Vector3 pos = myT.position;
        switch (state)
        {
            case State.Idle: return;
            case State.Launch:
                myT.position = Vector3.MoveTowards(pos, launchTarget, launchSpeed * Time.deltaTime);
                if (Mathf.Abs(3 - pos.y) < 0.1f) state = State.Fly;
                break;
            case State.Fly:
                myT.position = Vector3.MoveTowards(pos, path[nextPoint], speed * Time.deltaTime);
                Vector3 d = path[nextPoint] - pos;
                if (Mathf.Abs(d.x) < 0.01f && Mathf.Abs(d.y) < 0.01f && Mathf.Abs(d.z) < 0.01f)
                {
                    if (nextPoint == pointCount - 1) nextPoint = 0;
                    else nextPoint++;
                    path[nextPoint].y = Random.Range(2, 4);
                }
                break;
            case State.Reset:
                myT.position = Vector3.MoveTowards(pos, launchPos, speed * Time.deltaTime);
                d = launchPos - pos;
                if (Mathf.Abs(d.x) < 0.01f && Mathf.Abs(d.y) < 0.01f && Mathf.Abs(d.z) < 0.01f)
                {
                    myT.position = launchPos;
                    audioSource.Stop();
                    animator.SetTrigger("Reset");
                    state = State.Idle;
                    block.enabled = true;
                }
                break;
        }
        myT.LookAt(aimPos);
    }

    public void Launch()
    {
        if (cooldown) return;
        cooldown = true;
        StartCoroutine(ResetCooldown());
        Player.Click();
        if (state != State.Idle)
        {
            Reset();
            return;
        }
        animator.SetTrigger("Launch");
        audioSource.PlayOneShot(launch, 0.15f);
        audioSource.Play();
        block.enabled = false;
        state = State.Launch;
    }

    public void Reset()
    {
        if (state == State.Idle || state == State.Reset) return;
        nextPoint = 0;
        path[nextPoint].y = Random.Range(2, 4);
        state = State.Reset;
    }

    IEnumerator ResetCooldown()
    {
        yield return fifthSecond;
        cooldown = false;
    }
}
