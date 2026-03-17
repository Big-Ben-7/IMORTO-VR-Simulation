using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.InputSystem;

public class BallLauncher : XRGrabInteractable
{
    [Header("Table/Ball")]
    [SerializeField] Transform netMarker;
    [SerializeField] float netHeight, tableHeight, baseDistance;
    [SerializeField] Transform returnMarker1, returnMarker2, farMarker1, farMarker2;
    [SerializeField] Transform leftCornerMarker, rightCornerMarker;
    [SerializeField] LayerMask body, defaultLayer;

    [Header("Bot")]
    [SerializeField] Transform baseMarker;
    [SerializeField] Transform middle, top, holder;
    [SerializeField] float midL, topL, holdL;
    float baseZ;
    [SerializeField] Transform racket;
    [SerializeField] float minSpeed, maxSpeed, easyMinSpeed, easyMaxSpeed, hardMinSpeed, hardMaxSpeed;

    [Header("Audio/FX")]
    [SerializeField] Image[] diffButtons;
    [SerializeField] AudioClip[] racketHits;
    [SerializeField] AudioClip table, floor, wall, net, bot, other;
    Color selectedColor, normalColor;

    [Header("Launching")]
    [SerializeField] Transform ballParent;
    [SerializeField] float minLaunchSpeed, maxLaunchSpeed;
    Ball[] balls;
    int ballCount, currentBall = 0;
    Transform arm,launchMarker;
    Rigidbody rb;
    WaitForSeconds halfSecond;
    bool cooldown, grabbed;
    MyHandedness hand = MyHandedness.None;
    [SerializeField] InputActionProperty leftRotateProp, rightRotateProp;
    InputAction leftRotate, rightRotate;
    [SerializeField] float rotSpeed, soundVel, turnVol, rollVol;
    [SerializeField] AudioClip launch, roll, turn;
    AudioSource audioSource;
    bool moving, rotating;
    Coroutine stopping;
    float t;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        arm = transform.GetChild(0);
        launchMarker = arm.GetChild(0);
        balls = ballParent.GetComponentsInChildren<Ball>(true);
        // balls[0];
        ballCount = balls.Length;
        leftRotate = leftRotateProp.action;
        rightRotate = rightRotateProp.action;
        Ball.fifthSecond = Player.fifth;
        Ball.halfSecond = halfSecond = Player.half;
        Ball.netPos = netMarker.position.z;
        Ball.netHeight = netHeight;
        Ball.tableHeight = tableHeight;
        Ball.returnX[0] = returnMarker1.position.x;
        Ball.returnX[1] = returnMarker2.position.x;
        Ball.returnZ[0] = returnMarker1.position.z;
        Ball.returnZ[1] = returnMarker2.position.z;
        //Ball.launchZ[0] = farMarker1.position.z;
        //Ball.launchZ[1] = farMarker2.position.z;
        Ball.leftCorner = new Vector2(leftCornerMarker.position.x, leftCornerMarker.position.z);
        Ball.rightCorner = new Vector2(rightCornerMarker.position.x, rightCornerMarker.position.z);
        Ball.middle = middle;
        Ball.top = top;
        Ball.holder = holder;
        Ball.midL = midL;
        Ball.topL = topL;
        Ball.holdL = holdL;
        Vector3 basePos = Ball.basePos = baseMarker.position;
        baseZ = basePos.z;
        Ball.racket = racket;
        Ball.minSpeed = minSpeed;
        Ball.maxSpeed = maxSpeed;
        Ball.easyMinSpeed = easyMinSpeed;
        Ball.easyMaxSpeed = easyMaxSpeed; 
        Ball.hardMinSpeed = hardMinSpeed;
        Ball.hardMaxSpeed = hardMaxSpeed;
        Ball.racketHits = racketHits;
        Ball.table = table;
        Ball.floor = floor;
        Ball.wall = wall;
        Ball.net = net;
        Ball.bot = bot;
        Ball.other = other;
        Ball.launch = launch;
        Ball.racketLength = racketHits.Length;
        Ball.body = body;
        Ball.defaultLayer = defaultLayer;
        Ball.nextActive = new List<Ball>();
        selectedColor = diffButtons[0].color;
        normalColor = diffButtons[1].color;
    }

    void Update()
    {
        if (Ball.instance == null) Ball.ResetBot();
        //Debug.Log (grabbed + ", " + handeness + ", " + leftRotate.ReadValue<Vector2>() + ", " + rightRotate.ReadValue<Vector2>());
        if (grabbed)
        {
            rotating = true;
            switch (hand)
            {
                case MyHandedness.Left:
                    Vector2 rot = leftRotate.ReadValue<Vector2>();
                    if (rot.x > 0.1f || rot.x < -0.1f) arm.Rotate(0, 0, rot.x * rotSpeed * Time.deltaTime);
                    else rotating = false;
                    break;
                case MyHandedness.Right:
                    rot = rightRotate.ReadValue<Vector2>();
                    if (rot.x > 0.1f || rot.x < -0.1f) arm.Rotate(0, 0, rot.x * rotSpeed * Time.deltaTime);
                    else rotating = false;
                    break;
            }
            if (rotating)
            {
                moving = false;
                if (audioSource.clip != turn)
                {
                    audioSource.volume = turnVol;
                    audioSource.clip = turn;
                }
                if (!audioSource.isPlaying) audioSource.Play();
            }
            else if (rb.linearVelocity.sqrMagnitude > soundVel) 
            {
                moving = true;
                rotating = false;
                if (audioSource.clip != roll)
                {
                    audioSource.volume = rollVol;
                    audioSource.clip = roll;
                }
                if (!audioSource.isPlaying) audioSource.Play();
            }
            else
            {
                moving = rotating = false;
                if (stopping == null) stopping = StartCoroutine(StopAudio());
                else t = 0;
            }
        } else if (audioSource.isPlaying) audioSource.Stop();
    }

    IEnumerator StopAudio()
    {
        t = 0;
        while (t < 0.1f)
        {
            if (rotating || moving) yield break;
            yield return null;
            t += Time.deltaTime;
        }
        if (audioSource.isPlaying) audioSource.Stop();
    }

    protected override void OnActivated(ActivateEventArgs args)
    {
        base.OnActivated(args);
        if (cooldown) return;
        cooldown = true;
        StartCoroutine(Cooldown());

        currentBall++;
        if (currentBall == ballCount) currentBall = 0;
        Ball ball = null;
        for (int i = currentBall; i < currentBall + ballCount; i++)
        {
            Ball b = balls[i % ballCount];
            if (Ball.instance != b && !Ball.nextActive.Contains(b))
            {
                ball = b;
                break;
            }
        }
        if (ball == null)
        {
            for (int i = currentBall; i < currentBall + ballCount; i++)
            {
                Ball b = balls[i % ballCount];
                if (Ball.instance != b)
                {
                    ball = b;
                    break;
                }
            }
        }
        if (!ball.gameObject.activeSelf) ball.gameObject.SetActive(true);
        ball.Activate(launchMarker.position, launchMarker.forward * Random.Range(minLaunchSpeed, maxLaunchSpeed) * Mathf.Sqrt(Mathf.Abs(launchMarker.position.z - baseZ) / baseDistance)/*, launchMarker*/);
    }

    IEnumerator Cooldown()
    {
        yield return halfSecond;
        cooldown = false;
    }

    public void SetDifficulty(int d)
    {
        diffButtons[Ball.difficulty].color = normalColor;
        Ball.difficulty = d;
        diffButtons[d].color = selectedColor;
        Player.Click();
    }
    
    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        grabbed = true;
        //Player.Grab();
        switch(args.interactorObject.transform.GetComponent<XRBaseInteractor>().handedness)
        {
            case InteractorHandedness.Left:
                hand = MyHandedness.Left;
                break;
            case InteractorHandedness.Right:
                hand = MyHandedness.Right;
                break;
            default:
                hand = MyHandedness.None;
                break;
        }
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        grabbed = false;
        hand = MyHandedness.None;
    }
}
