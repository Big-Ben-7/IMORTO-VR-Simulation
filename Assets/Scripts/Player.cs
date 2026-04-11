using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
public enum MyHandedness {Left, Right, None};
public class Player : MonoBehaviour
{
    [SerializeField] InputActionProperty leftGripInput, rightGripInput, mapInput;
    InputAction leftGrip, rightGrip, mapAction;
    [SerializeField] Animator leftHand, rightHand;
    [SerializeField] AudioClip teleport, grab, button, tileWalk, /*woodWalk,*/ metalWalk, concreteWalk, grassWalk, gravelWalk, mapActivate;
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
    //WaitForSeconds halfSecond = new WaitForSeconds(0.5f);

    [Header("Physics")]
    //CapsuleCollider col;
    CapsuleCollider col;
    public static Rigidbody rb;
    Transform colT;
    [SerializeField] Transform head, leftController, rightController;
    [SerializeField] ConfigurableJoint headJoint, leftJoint, rightJoint;
    [SerializeField] float minHeight = 0.5f, maxHeight = 2;

    [Header("Movement")]
    [SerializeField] LayerMask groundMask;
    [SerializeField] float outSpeed, inSpeed, stairSpeed, downStairSpeed, turnSpeed, jumpHeight;
    [SerializeField] RectTransform rectT, icon;
    Canvas mapCanvas;
    Collider mapCol;
    Rect rect;
    float speed;
    [SerializeField] InputActionProperty moveInput, turnInput, jumpInput;
    InputAction moveAction, turnAction, jumpAction;
    Vector2 moveAxis;
    float turnAxis;
    bool isGrounded;
    Vector2 lastPos;
    float vel;
    public static WaitForSeconds tenth = new WaitForSeconds(0.1f), fifth = new WaitForSeconds(0.2f), half = new WaitForSeconds(0.5f), one = new WaitForSeconds(1), two = new WaitForSeconds(2);
    public static bool tp, sitting;
    public static Seat seat;
    [SerializeField] Transform piano;
    //[SerializeField] GameObject lobby, ent, health, expl, manu;
    public static Player instance;
    bool canBump = true, climbing;
    public static UnityEngine.InputSystem.XR.XRController leftDevice, rightDevice;
    [SerializeField] Transform ladder;
    Collider[] ladderCols;
    int ladderColCount;
    [SerializeField] LayerMask body;
    ManuManager manuManager;

    void Awake()
    {
        instance = this;
        Transform xr = transform.GetChild(0);
        rb = xr.GetComponent<Rigidbody>();
        col = xr.GetChild(2).GetComponent<CapsuleCollider>();
        colT = col.transform;
        AudioSource[] sources = xr.GetComponents<AudioSource>();
        audioSource = sources[0];
        walkSource = sources[1];
        bgSource = xr.GetChild(0).GetChild(7).GetComponent<AudioSource>();
        bgSource.volume = 0.03f;
        //leftRb = leftJoint.GetComponent<Rigidbody>();
        //rightRb = rightJoint.GetComponent<Rigidbody>();
        leftGrip = leftGripInput.action;
        rightGrip = rightGripInput.action;
        mapAction = mapInput.action;
        moveAction = moveInput.action;
        turnAction = turnInput.action;
        jumpAction = jumpInput.action;
        fadeCanvas.alpha = 0;
        grabClip = grab;
        buttonClip = button;
        bumpClips = objectBumps;
        //Door.rooms = new GameObject[] {lobby, ent, health, expl, manu};
        UIPanel.audioSource = audioSource;
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
        mapCanvas = rectT.parent.GetComponent<Canvas>();
        mapCol = mapCanvas.GetComponent<Collider>();
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
    }

    void Start()
    {
        Vector3 pos = head.position;
        lastPos = new Vector2(pos.x, pos.z);
        StartCoroutine(Initialize());
        StartCoroutine(CalculateVelocity());
        manuManager = ManuManager.instance;
    }

    void Update()
    {
        leftHand.SetFloat("Grip", leftGrip.ReadValue<float>());
        rightHand.SetFloat("Grip", rightGrip.ReadValue<float>());
        moveAxis = sitting? Vector2.zero : moveAction.ReadValue<Vector2>();
        turnAxis = turnAction.ReadValue<Vector2>().x;

        Vector3 start = colT.position;
        bool cast = Physics.Raycast(start + new Vector3(0, 0.5f, 0), Vector3.down, out RaycastHit hit, 0.55f, groundMask);
        if (!cast)
        {
            if (walkSource.isPlaying) walkSource.Stop();
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
                if (!isGrounded) audioSource.PlayOneShot(tileLands[Random.Range(0, tileCount)], 0.6f);
                speed = inSpeed;
            }
            else if (go.CompareTag("Metal"))
            {
                if (walkSource.clip != metalWalk)
                {
                    walkSource.volume = 0.2f;
                    walkSource.clip = metalWalk;
                }
                //else play = false;
                if (!isGrounded) audioSource.PlayOneShot(metalLands[Random.Range(0, metalCount)]);
                speed = inSpeed;
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
                    if (!isGrounded) audioSource.PlayOneShot(gravelLands[Random.Range(0, gravelCount)], 0.5f);
                }
                else
                {
                    if (walkSource.clip != grassWalk)
                    {
                        walkSource.volume = 0.07f;
                        walkSource.clip = grassWalk;
                    }
                    //else play = false;
                    if (!isGrounded) audioSource.PlayOneShot(grassLands[Random.Range(0, grassCount)], 0.4f);
                }
                speed = outSpeed;
            }
            else
            {
                if (walkSource.clip != concreteWalk)
                {
                    walkSource.volume = 0.3f;
                    walkSource.clip = concreteWalk;
                }
                //else play = false;
                if (!isGrounded) audioSource.PlayOneShot(concreteLands[Random.Range(0, concreteCount)], 0.7f);
                if (go.CompareTag("Stair"))
                {
                    if (Vector3.Dot(head.forward, head.position) > 0) speed = stairSpeed;
                    else speed = downStairSpeed;
                }
                else speed = outSpeed;
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
            if (climbing)
            {
                climbing = false;
                for (int i = 0; i < ladderColCount; i++) ladderCols[i].excludeLayers = 0;
            }
        }
        isGrounded = cast;
        bgSource.volume = Mathf.Clamp(0.002f * (Vector3.SqrMagnitude(piano.position - head.position) - 3), 0, 0.03f);
        
        if (mapAction.WasPressedThisFrame())
        {
            mapCanvas.enabled = mapCol.enabled = !mapCanvas.enabled;
            audioSource.PlayOneShot(mapActivate);
        }
        if (mapCanvas.enabled)
        {
            Vector3 pos = head.position;
            icon.anchoredPosition = new Vector2(rect.xMin + rect.width * Mathf.Clamp01((15.5f - pos.x) / 31), rect.yMin + rect.height * Mathf.Clamp01((15.035f - pos.z) / 31));
            icon.localRotation = Quaternion.Euler(0, 0, -head.eulerAngles.y + 180);
        }
    }

    void FixedUpdate()
    {
        colT.localPosition = new Vector3(head.localPosition.x, 0, head.localPosition.z);
        if (!climbing)
        {
            col.height = Mathf.Clamp(head.position.y, minHeight, maxHeight);
            col.center = new Vector3(0, col.height / 2, 0);
        }

        Vector3 localPos = leftController.localPosition;
        leftJoint.targetPosition = new Vector3(localPos.x, leftController.position.y, localPos.z);
        leftJoint.targetRotation = leftController.localRotation;
        localPos = rightController.localPosition;
        rightJoint.targetPosition = new Vector3(localPos.x, rightController.position.y, localPos.z);
        rightJoint.targetRotation = rightController.localRotation;
        localPos = head.localPosition;
        headJoint.targetPosition = new Vector3(localPos.x, head.position.y, localPos.z);
        
        Quaternion yaw = Quaternion.Euler(0, head.eulerAngles.y, 0);
        Vector3 direction = yaw * new Vector3(moveAxis.x, 0, moveAxis.y);
        Vector3 targetPos = rb.position + direction * speed * Time.fixedDeltaTime;
        Quaternion q = Quaternion.AngleAxis(turnSpeed * Time.fixedDeltaTime * turnAxis, Vector3.up);
        rb.MoveRotation(rb.rotation * q);
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

    public void SetClimb()
    {
        climbing = true;
        for (int i = 0; i < ladderColCount; i++) ladderCols[i].excludeLayers = body;
        if (!manuManager.carSwapped && manuManager.lastAssembled) manuManager.SwapCar();
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
        audioSource.PlayOneShot(bumpClips[Random.Range(0, bumpCount)], 0.4f);
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
