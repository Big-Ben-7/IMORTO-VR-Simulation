using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections.Generic;

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
    Transform launchMarker;
    WaitForSeconds fifthSecond = new WaitForSeconds(0.2f);
    bool cooldown;

    protected override void Awake()
    {
        base.Awake();
        launchMarker = transform.GetChild(0).GetChild(0);
        balls = ballParent.GetComponentsInChildren<Ball>(true);
        // balls[0];
        ballCount = balls.Length;

        Ball.rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (Ball.rightDevice.isValid && Ball.rightDevice.TryGetHapticCapabilities(out HapticCapabilities capabilities) && capabilities.supportsImpulse) Ball.rightHaptics = true;
        else Ball.rightHaptics = false;
        Ball.leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (Ball.leftDevice.isValid && Ball.leftDevice.TryGetHapticCapabilities(out capabilities) && capabilities.supportsImpulse) Ball.leftHaptics = true;
        else Ball.leftHaptics = false;
        Ball.fifthSecond = fifthSecond;
        Ball.halfSecond = new WaitForSeconds(0.5f);
        Ball.netPos = netMarker.position.z;
        Ball.netHeight = netHeight;
        Ball.tableHeight = tableHeight;
        Ball.returnX[0] = returnMarker1.position.x;
        Ball.returnX[1] = returnMarker2.position.x;
        Ball.returnZ[0] = returnMarker1.position.z;
        Ball.returnZ[1] = returnMarker2.position.z;
        Ball.launchZ[0] = farMarker1.position.z;
        Ball.launchZ[1] = farMarker2.position.z;
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
        ball.Activate(launchMarker.position, launchMarker.forward * Random.Range(minLaunchSpeed, maxLaunchSpeed) * Mathf.Sqrt((launchMarker.position.z - baseZ) / baseDistance)/*, launchMarker*/);
    }

    IEnumerator Cooldown()
    {
        yield return fifthSecond;
        cooldown = false;
    }

    public void SetDifficulty(int d)
    {
        diffButtons[Ball.difficulty].color = normalColor;
        Ball.difficulty = d;
        diffButtons[d].color = selectedColor;
        Player.Click();
    }
}
