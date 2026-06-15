using System;
using System.Collections;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
public enum MyHandedness {Left, Right, None};
public enum Location {Entertainment, Healthcare, Exploration, Manufacturing, Outside, Inside};
public class Player : MonoBehaviour
{
    [SerializeField] InputActionProperty leftGripInput, rightGripInput, mapInput, handInput, hideTipInput;
    [SerializeField] Transform[] leftInteractors, rightInteractors;
    [SerializeField] GameObject[] leftModels, rightModels;
    XRPokeInteractor leftPoke, rightPoke;
    XRInteractorLineVisual leftTP, rightTP;
    [SerializeField] XRTransformStabilizer leftStabilizer, rightStabilizer;
    [SerializeField] Transform[] leftPokes, rightPokes;
    [SerializeField] Vector3[] leftPos, rightPos;
    [SerializeField] Quaternion[] leftRot, rightRot;
    InputAction leftGrip, rightGrip, mapAction, handAction, hideTipAction;
    [SerializeField] Animator leftHand, rightHand;
    [SerializeField] AudioClip teleport, grab, button, tileWalk, /*woodWalk,*/ metalWalk, concreteWalk, grassWalk, gravelWalk, mapActivate, switchHands;
    [SerializeField] AudioClip[] tileLands, metalLands, concreteLands, grassLands, gravelLands, objectBumps;
    int tileCount, metalCount, concreteCount, grassCount, gravelCount;
    [SerializeField] CanvasGroup fadeCanvas;
    public static bool initialized;
    public static AudioSource audioSource;
    public static AudioClip grabClip, buttonClip;
    public static AudioClip[] bumpClips;
    public static int bumpCount;
    AudioSource walkSource, bgSource;
    WaitForSeconds twentiethSecond = new WaitForSeconds(0.05f);
    bool hands = false, tips = true;
    //WaitForSeconds halfSecond = new WaitForSeconds(0.5f);

    [Header("Physics")]
    //CapsuleCollider col;
    CapsuleCollider col;
    public static Rigidbody rb;
    Transform colT;
    [SerializeField] Transform head, leftController, rightController;
    [SerializeField] ConfigurableJoint headJoint, leftJoint, rightJoint;
    BoxCollider leftCol, rightCol;
    [SerializeField] float minHeight = 0.5f, maxHeight = 2;

    [Header("Movement")]
    [SerializeField] LayerMask groundMask;
    [SerializeField] float outSpeed, inSpeed, stairSpeed, downStairSpeed, turnSpeed, jumpHeight;
    [SerializeField] RectTransform rectT, icon;
    Canvas mapCanvas;
    GameObject mapCol;
    Rect rect;
    float speed;
    [SerializeField] InputActionProperty moveInput, turnInput, jumpInput;
    InputAction moveAction, turnAction, jumpAction;
    Vector2 moveAxis;
    float turnAxis;
    bool isTurning;
    float nextTurnTime;
    float cooldownTime = 0.25f;
    float deadzone = 0.5f;
    public static float snapTurn = 0;
    bool isGrounded;
    Vector2 lastPos;
    float vel;
    public static WaitForSeconds tenth = new WaitForSeconds(0.1f), fifth = new WaitForSeconds(0.2f), half = new WaitForSeconds(0.5f), one = new WaitForSeconds(1), two = new WaitForSeconds(2);
    public static bool tp, sitting;
    public static Seat seat;
    [SerializeField] Transform piano;
    //[SerializeField] GameObject lobby, ent, health, expl, manu;
    public static Player instance;
    bool canBump = true, climbing, airClimbing;
    public static UnityEngine.InputSystem.XR.XRController leftDevice, rightDevice;
    [SerializeField] Transform ladder;
    Collider[] ladderCols;
    int ladderColCount;
    [SerializeField] LayerMask body;
    ManuManager manuManager;
    public static GreeterBot greeterBot;
    Canvas[] tooltips;
    MeshRenderer[] tipDots;
    LineRenderer[] tipLines;
    int tooltipCount, tipDotCount, tipLineCount;
    [SerializeField] GameObject[] tooltipParents;
    bool mapTriggered;
    [SerializeField] SortingGroup windowSG;
    [SerializeField] Canvas[] enterUI, healthUI, explUI, manuUI;
    String[] enterNames, healthNames, explNames, manuNames;
    int[] enterOrder, healthOrder, explOrder, manuOrder;
    int enterCount, healthCount, explCount, manuCount;
    Location location = Location.Outside;
    Transform xr;

    void Awake()
    {
        instance = this;
        xr = transform.GetChild(0);
        rb = xr.GetComponent<Rigidbody>();
        col = xr.GetChild(2).GetComponent<CapsuleCollider>();
        colT = col.transform;
        AudioSource[] sources = xr.GetComponents<AudioSource>();
        audioSource = sources[0];
        walkSource = sources[1];
        bgSource = xr.GetChild(0).GetChild(7).GetComponent<AudioSource>();
        bgSource.volume = 0.03f;
        leftCol = leftJoint.GetComponentInChildren<BoxCollider>();
        rightCol = rightJoint.GetComponentInChildren<BoxCollider>();
        //leftRb = leftJoint.GetComponent<Rigidbody>();
        //rightRb = rightJoint.GetComponent<Rigidbody>();
        leftGrip = leftGripInput.action;
        rightGrip = rightGripInput.action;
        handAction = handInput.action;
        mapAction = mapInput.action;
        moveAction = moveInput.action;
        turnAction = turnInput.action;
        jumpAction = jumpInput.action;
        hideTipAction = hideTipInput.action;
        fadeCanvas.alpha = 0;
        grabClip = grab;
        buttonClip = button;
        bumpClips = objectBumps;
        //Door.rooms = new GameObject[] {lobby, ent, health, expl, manu};
        UIPanel.audioSource = UIButtonAudio.audioSource = audioSource;
        tileCount = tileLands.Length;
        metalCount = metalLands.Length;
        concreteCount = concreteLands.Length;
        grassCount = grassLands.Length;
        gravelCount = gravelLands.Length;
        speed = outSpeed;
        bumpCount = bumpClips.Length;
        //ent.SetActive(false);
        //health.SetActive(false);
        //expl.SetActive(false);
        //manu.SetActive(false);
        rect = rectT.rect;
        Transform mapT = rectT.parent;
        mapCanvas = mapT.GetComponent<Canvas>();
        mapCol = mapT.GetChild(4).gameObject;
        ladderCols = ladder.GetComponentsInChildren<Collider>();
        ladderColCount = ladderCols.Length;
        leftDevice = InputSystem.GetDevice<UnityEngine.InputSystem.XR.XRController>(CommonUsages.LeftHand);
        rightDevice = InputSystem.GetDevice<UnityEngine.InputSystem.XR.XRController>(CommonUsages.RightHand);
        NPC.tileWalk = tileWalk;
        NPC.concreteWalk = concreteWalk;
        NPC.grassWalk = grassWalk;
        NPC.gravelWalk = gravelWalk;
        NPC.metalWalk = metalWalk;
        NPC.groundMask = groundMask;
        leftPoke = leftInteractors[0].GetComponent<XRPokeInteractor>();
        rightPoke = rightInteractors[0].GetComponent<XRPokeInteractor>();
        leftTP = leftInteractors[3].GetComponent<XRInteractorLineVisual>();
        rightTP = rightInteractors[3].GetComponent<XRInteractorLineVisual>();
    }

    void Start()
    {
        Vector3 pos = head.position;
        lastPos = new Vector2(pos.x, pos.z);
        StartCoroutine(Initialize());
        StartCoroutine(CalculateVelocity());
        manuManager = ManuManager.instance;
        tooltips = tooltipParents[0].GetComponentsInChildren<Canvas>().Concat(tooltipParents[1].GetComponentsInChildren<Canvas>()).ToArray();
        tipDots = tooltipParents[0].GetComponentsInChildren<MeshRenderer>().Concat(tooltipParents[1].GetComponentsInChildren<MeshRenderer>()).ToArray();
        tipLines = tooltipParents[0].GetComponentsInChildren<LineRenderer>().Concat(tooltipParents[1].GetComponentsInChildren<LineRenderer>()).ToArray();
        tooltipCount = tooltips.Length;
        tipDotCount = tipDots.Length;
        tipLineCount = tipLines.Length;
        enterCount = enterUI.Length;
        healthCount = healthUI.Length;
        explCount = explUI.Length;
        manuCount = manuUI.Length;
        enterNames = new string[enterCount];
        healthNames = new string[healthCount];
        explNames = new string[explCount];
        manuNames = new string[manuCount];
        enterOrder = new int[enterCount];
        healthOrder = new int[healthCount];
        explOrder = new int[explCount];
        manuOrder = new int[manuCount];
        for (int i = 0; i < enterCount; i++)
        {
            Canvas canv = enterUI[i];
            enterNames[i] = canv.sortingLayerName;
            enterOrder[i] = canv.sortingOrder;
        }
        for (int i = 0; i < healthCount; i++)
        {
            Canvas canv = healthUI[i];
            healthNames[i] = canv.sortingLayerName;
            healthOrder[i] = canv.sortingOrder;
        }
        for (int i = 0; i < explCount; i++)
        {
            Canvas canv = explUI[i];
            explNames[i] = canv.sortingLayerName;
            explOrder[i] = canv.sortingOrder;
        }
        for (int i = 0; i < manuCount; i++)
        {
            Canvas canv = manuUI[i];
            manuNames[i] = canv.sortingLayerName;
            manuOrder[i] = canv.sortingOrder;
        }
        /*leftPos = new Vector3[4];
        leftRot = new Quaternion[4];
        rightPos = new Vector3[4];
        rightRot = new Quaternion[4];
        for (int i = 0; i < 4; i++) leftInteractors[i].GetLocalPositionAndRotation(out leftPos[i], out leftRot[i]);
        for (int i = 0; i < 4; i++) rightInteractors[i].GetLocalPositionAndRotation(out rightPos[i], out rightRot[i]);*/
    }

    void updateSG(Location loc)
    {
        Canvas canv;
        switch (location)
        {
            case Location.Entertainment:
                for (int i = 0; i < enterCount; i++)
                {
                    canv = enterUI[i];
                    canv.sortingLayerName = enterNames[i];
                    canv.sortingOrder = enterOrder[i];
                }
                break;
            case Location.Healthcare:
                for (int i = 0; i < healthCount; i++)
                {
                    canv = healthUI[i];
                    canv.sortingLayerName = healthNames[i];
                    canv.sortingOrder = healthOrder[i];
                }
                break;
            case Location.Exploration:
                for (int i = 0; i < explCount; i++)
                {
                    canv = explUI[i];
                    canv.sortingLayerName = explNames[i];
                    canv.sortingOrder = explOrder[i];
                }
                break;
            case Location.Manufacturing:
                for (int i = 0; i < manuCount; i++)
                {
                    canv = manuUI[i];
                    canv.sortingLayerName = manuNames[i];
                    canv.sortingOrder = manuOrder[i];
                }
                break;
        }
        location = loc;
        switch (location)
        {
            case Location.Entertainment:
                for (int i = 0; i < enterCount; i++)
                {
                    canv = enterUI[i];
                    canv.sortingLayerName = "Sorting";
                    canv.sortingOrder = 4;
                }
                break;
            case Location.Healthcare:
                for (int i = 0; i < healthCount; i++)
                {
                    canv = healthUI[i];
                    canv.sortingLayerName = "Sorting";
                    canv.sortingOrder = 4;
                }
                break;
            case Location.Exploration:
                for (int i = 0; i < explCount; i++)
                {
                    canv = explUI[i];
                    canv.sortingLayerName = "Sorting";
                    canv.sortingOrder = 4;
                }
                break;
            case Location.Manufacturing:
                for (int i = 0; i < manuCount; i++)
                {
                    canv = manuUI[i];
                    canv.sortingLayerName = "Sorting";
                    canv.sortingOrder = 4;
                }
                break;
        }
    }

    void Update()
    {
        if (hands)
        {
            leftHand.SetFloat("Grip", leftGrip.ReadValue<float>());
            rightHand.SetFloat("Grip", rightGrip.ReadValue<float>());
        }
        moveAxis = sitting? Vector2.zero : moveAction.ReadValue<Vector2>();
        turnAxis = turnAction.ReadValue<Vector2>().x;

        Vector3 start = colT.position;
        bool cast = Physics.Raycast(start + new Vector3(0, 0.5f, 0), Vector3.down, out RaycastHit hit, 0.55f, groundMask);
        if (!cast)
        {
            if (walkSource.isPlaying) walkSource.Stop();
            if (climbing) airClimbing = true;
        }
        else
        {
            //bool play = true;
            GameObject go = hit.transform.gameObject;
            if (go.CompareTag("Floor") || Vector2.SqrMagnitude(new Vector2(start.x, start.z)) <= 20.930625)
            {
                if (walkSource.clip != tileWalk)
                {
                    walkSource.volume = 0.3f;
                    walkSource.clip = tileWalk;
                }
                //else play = false;
                if (!isGrounded) audioSource.PlayOneShot(tileLands[UnityEngine.Random.Range(0, tileCount)], 0.6f);
                speed = inSpeed;
                if (location != Location.Inside)
                {
                    updateSG(Location.Inside);
                    windowSG.enabled = false;
                    greeterBot.Outside(false);
                }
            }
            else if (go.CompareTag("Entertainment"))
            {
                if (walkSource.clip != tileWalk)
                {
                    walkSource.volume = 0.3f;
                    walkSource.clip = tileWalk;
                }
                if (!isGrounded) audioSource.PlayOneShot(tileLands[UnityEngine.Random.Range(0, tileCount)], 0.6f);
                speed = inSpeed;
                if (location != Location.Entertainment) updateSG(Location.Entertainment);
            }
            else if (go.CompareTag("Healthcare"))
            {
                if (walkSource.clip != tileWalk)
                {
                    walkSource.volume = 0.3f;
                    walkSource.clip = tileWalk;
                }
                if (!isGrounded) audioSource.PlayOneShot(tileLands[UnityEngine.Random.Range(0, tileCount)], 0.6f);
                speed = inSpeed;
                if (location != Location.Healthcare) updateSG(Location.Healthcare);
            }
            else if (go.CompareTag("Exploration"))
            {
                if (walkSource.clip != tileWalk)
                {
                    walkSource.volume = 0.3f;
                    walkSource.clip = tileWalk;
                }
                if (!isGrounded) audioSource.PlayOneShot(tileLands[UnityEngine.Random.Range(0, tileCount)], 0.6f);
                speed = inSpeed;
                if (location != Location.Exploration) updateSG(Location.Exploration);
            }
            else if (go.CompareTag("Metal"))
            {
                if (walkSource.clip != metalWalk)
                {
                    walkSource.volume = 0.2f;
                    walkSource.clip = metalWalk;
                }
                //else play = false;
                if (!isGrounded) audioSource.PlayOneShot(metalLands[UnityEngine.Random.Range(0, metalCount)]);
                speed = inSpeed;
                if (location != Location.Manufacturing) updateSG(Location.Manufacturing);
            }
            else if (go.CompareTag("Grass"))
            {
                if (Vector2.SqrMagnitude(new Vector2(start.x, start.z)) <= 441)
                {
                    if (walkSource.clip != gravelWalk)
                    {
                        walkSource.volume = 0.2f;
                        walkSource.clip = gravelWalk;
                    }
                    //else play = false;
                    if (!isGrounded) audioSource.PlayOneShot(gravelLands[UnityEngine.Random.Range(0, gravelCount)], 0.5f);
                }
                else
                {
                    if (walkSource.clip != grassWalk)
                    {
                        walkSource.volume = 0.07f;
                        walkSource.clip = grassWalk;
                    }
                    //else play = false;
                    if (!isGrounded) audioSource.PlayOneShot(grassLands[UnityEngine.Random.Range(0, grassCount)], 0.4f);
                }
                speed = outSpeed;
                if (location != Location.Outside)
                {
                    location = Location.Outside;
                    windowSG.enabled = true;
                    greeterBot.Outside(true);
                }
            }
            else
            {
                if (walkSource.clip != concreteWalk)
                {
                    walkSource.volume = 0.3f;
                    walkSource.clip = concreteWalk;
                }
                //else play = false;
                if (!isGrounded) audioSource.PlayOneShot(concreteLands[UnityEngine.Random.Range(0, concreteCount)], 0.7f);
                if (go.CompareTag("Stair"))
                {
                    if (Vector3.Dot(head.forward, head.position) > 0) speed = stairSpeed;
                    else speed = downStairSpeed;
                }
                else speed = outSpeed;
                if (location != Location.Outside)
                {
                    location = Location.Outside;
                    windowSG.enabled = true;
                    greeterBot.Outside(true);
                }
            }
            if (vel > 0.5f)
            {
                if (!walkSource.isPlaying)
                {
                    walkSource.Play();
                    //if (play) walkSource.Play();
                    //else walkSource.UnPause();
                }
            }
            else if (walkSource.isPlaying) walkSource.Stop();


            if (jumpAction.WasPressedThisFrame())
            {
                //float handSpeed = ((leftRb.linearVelocity - rb.linearVelocity).sqrMagnitude + (rightRb.linearVelocity - rb.linearVelocity).sqrMagnitude) / 2;
                /*if (handSpeed > minHandSpeed)*/
                if (!sitting) rb.linearVelocity = Vector3.up * Mathf.Sqrt(2 * -Physics.gravity.y * jumpHeight);
                else seat.Exit(true);
            }
            airClimbing = false;
        }
        isGrounded = cast;
        bgSource.volume = Mathf.Clamp(0.002f * (Vector3.SqrMagnitude(piano.position - head.position) - 3), 0, 0.03f);
        
        if (mapAction.WasPressedThisFrame())
        {
            bool mapBool = !mapCanvas.enabled;
            mapCanvas.enabled = mapBool;
            mapCol.SetActive(mapBool);
            audioSource.PlayOneShot(mapActivate);
            mapTriggered = true;
        }
        if (mapCanvas.enabled)
        {
            Vector3 pos = head.position;
            float h = (15.035f - pos.z) / 31;
            if (h > 0.97f) h = 0.97f;
            else if (h < 0.03f) h = 0.03f;
            float w = (15.5f - pos.x) / 31;
            if (w > 0.97f) w = 0.97f;
            else if (w < 0.03f) w = 0.03f;
            icon.anchoredPosition = new Vector2(rect.xMin + rect.width * w, rect.yMin + rect.height * h);
            icon.localRotation = Quaternion.Euler(0, 0, -head.eulerAngles.y + 180);
        }
        if (hideTipAction.WasPressedThisFrame())
        {
            if (!hands)
            {
                tips = !tips;
                toggleTips(tips);
            }
            else
            {
                hands = false;
                tips = true;
                toggleHands(false);
                toggleTips(true);
            }
            audioSource.PlayOneShot(switchHands);
        }
        if (handAction.WasPressedThisFrame())
        {
            hands = !hands;
            toggleHands(hands);
            audioSource.PlayOneShot(switchHands);
        }
    }

    public void showMap()
    {
        if (mapTriggered) return;
        mapCanvas.enabled = true;
        mapCol.SetActive(true);
        audioSource.PlayOneShot(mapActivate);
    }

    void toggleHands(bool activate)
    {
        if (activate)
        {
            for (int i = 0; i < 4; i++)
            {
                leftInteractors[i].SetLocalPositionAndRotation(leftPos[i], leftRot[i]);
                rightInteractors[i].SetLocalPositionAndRotation(rightPos[i], rightRot[i]);
            }
            leftModels[1].SetActive(false);
            rightModels[1].SetActive(false);
            leftPokes[1].gameObject.SetActive(false);
            rightPokes[1].gameObject.SetActive(false);
            leftModels[0].SetActive(true);
            rightModels[0].SetActive(true);
            //leftPokes[0].gameObject.SetActive(true);
            //rightPokes[0].gameObject.SetActive(true);
            leftPoke.attachTransform = leftPokes[0];
            leftStabilizer.targetTransform = leftPokes[0];
            rightPoke.attachTransform = rightPokes[0];
            rightStabilizer.targetTransform = rightPokes[0];
            leftTP.lineOriginTransform = leftPokes[0];
            rightTP.lineOriginTransform = rightPokes[0];
            leftCol.center = new Vector3(-0.005f, -0.05f, -0.067f);
            leftCol.transform.localRotation = Quaternion.Euler(10, 0, 0);
            rightCol.center = new Vector3(0.005f, -0.05f, -0.067f);
            rightCol.transform.localRotation = Quaternion.Euler(10, 0, 0);
            if (tips) toggleTips(false);
        }
        else
        {
            for (int i = 0; i < 2; i++)
            {
                leftInteractors[i].SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                rightInteractors[i].SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            leftInteractors[2].SetLocalPositionAndRotation(new Vector3(0, 0, 0.01f), Quaternion.identity);
            rightInteractors[2].SetLocalPositionAndRotation(new Vector3(0, 0, 0.01f), Quaternion.identity);
            leftInteractors[3].SetLocalPositionAndRotation(new Vector3(0, -0.02f, -0.035f), Quaternion.identity);
            rightInteractors[3].SetLocalPositionAndRotation(new Vector3(0, -0.02f, -0.035f), Quaternion.identity);
            leftModels[0].SetActive(false);
            rightModels[0].SetActive(false);
            //leftPokes[0].gameObject.SetActive(false);
            //rightPokes[0].gameObject.SetActive(false);
            leftModels[1].SetActive(true);
            rightModels[1].SetActive(true);
            leftPokes[1].gameObject.SetActive(true);
            rightPokes[1].gameObject.SetActive(true);
            leftPoke.attachTransform = leftPokes[1];
            leftStabilizer.targetTransform = leftPokes[1];
            rightPoke.attachTransform = rightPokes[1];
            rightStabilizer.targetTransform = rightPokes[1];
            leftTP.lineOriginTransform = leftPokes[1];
            rightTP.lineOriginTransform = rightPokes[1];
            leftCol.center = new Vector3(-0.005f, -0.02f, -0.08f);
            leftCol.transform.localRotation = Quaternion.identity;
            rightCol.center = new Vector3(0.005f, -0.02f, -0.08f);
            rightCol.transform.localRotation = Quaternion.identity;
            if (tips) toggleTips(true);
        }
    }

    void toggleTips(bool activate)
    {
        if (activate)
        {
            for (int i = 0; i < tooltipCount; i++) tooltips[i].enabled = true;
            for (int i = 0; i < tipDotCount; i++) tipDots[i].enabled = true;
            for (int i = 0; i < tipLineCount; i++) tipLines[i].enabled = true;
        }
        else
        {
            for (int i = 0; i < tooltipCount; i++) tooltips[i].enabled = false;
            for (int i = 0; i < tipDotCount; i++) tipDots[i].enabled = false;
            for (int i = 0; i < tipLineCount; i++) tipLines[i].enabled = false;
        }
    }

    void FixedUpdate()
    {
        colT.localPosition = new Vector3(head.localPosition.x, 0, head.localPosition.z);
        if (!airClimbing)
        {
            col.height = Mathf.Clamp(head.position.y, minHeight, maxHeight);
            col.center = new Vector3(0, col.height / 2, 0);
        }
        leftJoint.targetPosition = xr.InverseTransformPoint(leftController.position);
        leftJoint.targetRotation = leftController.localRotation;
        rightJoint.targetPosition = xr.InverseTransformPoint(rightController.position);
        rightJoint.targetRotation = rightController.localRotation;
        headJoint.targetPosition = xr.InverseTransformPoint(head.position);
        
        Quaternion yaw = Quaternion.Euler(0, head.eulerAngles.y, 0);
        Vector3 direction = yaw * new Vector3(moveAxis.x, 0, moveAxis.y);
        Vector3 targetPos = rb.position + direction * speed * Time.fixedDeltaTime;
        Quaternion q = quaternion.identity;
        if (snapTurn == 0)
        {
            q = Quaternion.AngleAxis(turnSpeed * Time.fixedDeltaTime * turnAxis, Vector3.up);
            rb.MoveRotation(rb.rotation * q);
        }
        else
        {
            if (!isTurning && Time.time >= nextTurnTime && Mathf.Abs(turnAxis) > deadzone)
            {
                q = Quaternion.AngleAxis(Mathf.Sign(turnAxis) * snapTurn, Vector3.up);
                rb.MoveRotation(rb.rotation * q);
                isTurning = true;
                nextTurnTime = Time.time + cooldownTime;
            }
            else
            {
                isTurning = false;
            }
        }
        Vector3 newPosition = q * (targetPos - head.position) + head.position;
        rb.MovePosition(newPosition);
    }
    
    IEnumerator CalculateVelocity()
    {
        while (true)
        {
            if (isGrounded)
            {
                Vector3 pos = head.position;
                Vector2 currentPos = new Vector2(pos.x, pos.z);
                vel = ((currentPos - lastPos) * 10).sqrMagnitude;
                lastPos = currentPos;
                yield return tenth;
            }
            else yield return null;
        }
    }

    public void Teleport()
    {
        audioSource.PlayOneShot(teleport);
        StartCoroutine(Fade());
    }

    public void SetClimb(bool b)
    {
        if (b)
        {
            climbing = true;
            for (int i = 0; i < ladderColCount; i++) ladderCols[i].excludeLayers = body;
            if (!manuManager.carSwapped && manuManager.lastAssembled) manuManager.SwapCar();
        }
        else
        {
            climbing = false;
            for (int i = 0; i < ladderColCount; i++) ladderCols[i].excludeLayers = 0;
        }
    }

    IEnumerator Fade(float duration = 0.2f)
    {
        tp = rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.None;
        float elapsedTime = 0;
        float alpha = 0;
        while (alpha <= 0.99f)
        {
            alpha = fadeCanvas.alpha = elapsedTime / duration;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        alpha = fadeCanvas.alpha = 1;
        elapsedTime = 0;
        yield return twentiethSecond;
        if (sitting) seat.Exit(false);
        while (alpha >= 0.01f)
        {
            alpha = fadeCanvas.alpha = 1 - elapsedTime / duration;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        fadeCanvas.alpha = 0;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.isKinematic = false;
        //yield return halfSecond;
        tp = false;
    }

    IEnumerator Initialize()
    {
        yield return fifth;
        initialized = true;
    }

    public static void Grab()
    {
        audioSource.PlayOneShot(grabClip, 0.5f);
    }

    public static void Click()
    {
        audioSource.PlayOneShot(buttonClip, 0.9f);
    }

    public void Bump()
    {
        if (!canBump) return;
        canBump = false;
        StartCoroutine(BumpCooldown());
        audioSource.PlayOneShot(bumpClips[UnityEngine.Random.Range(0, bumpCount)], 0.4f);
    }

    public static void SendHapticsLeft(float amplitude, float duration)
    {
        if (leftDevice == null) return;
        var command = UnityEngine.InputSystem.XR.Haptics.SendHapticImpulseCommand.Create(0, amplitude, duration);
        leftDevice.ExecuteCommand(ref command);
    }

    public static void SendHapticsRight(float amplitude, float duration)
    {
        if (rightDevice == null) return;
        var command = UnityEngine.InputSystem.XR.Haptics.SendHapticImpulseCommand.Create(0, amplitude, duration);
        rightDevice.ExecuteCommand(ref command);
    }

    IEnumerator BumpCooldown()
    {
        yield return half;
        canBump = true;
    }
}
