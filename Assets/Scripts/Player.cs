using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField] InputActionProperty leftGripInput, rightGripInput;
    InputAction leftGrip, rightGrip;
    [SerializeField] Animator leftHand, rightHand;
    [SerializeField] AudioClip teleport, grab, button;
    [SerializeField] AudioClip tileWalk, concreteWalk, grassWalk, gravelWalk;
    [SerializeField] AudioClip[] tileLands, concreteLands, grassLands, gravelLands;
    int tileCount, concreteCount, grassCount, gravelCount;
    [SerializeField] CanvasGroup fadeCanvas;
    public static bool initialized;
    public static AudioSource audioSource;
    public static AudioClip grabClip, buttonClip;
    AudioSource walkSource, bgSource;
    WaitForSeconds twentiethSecond = new WaitForSeconds(0.05f);
    //WaitForSeconds halfSecond = new WaitForSeconds(0.5f);

    [Header("Physics")]
    [SerializeField] CapsuleCollider col;
    [SerializeField] Transform head, leftController, rightController;
    [SerializeField] ConfigurableJoint headJoint, leftJoint, rightJoint;
    [SerializeField] float minHeight = 0.5f, maxHeight = 2;

    [Header("Movement")]
    [SerializeField] float outSpeed = 4.5f, inSpeed = 3, turnSpeed, jumpHeight;
    float speed;
    [SerializeField] InputActionProperty moveInput, turnInput, jumpInput;
    [SerializeField] LayerMask groundMask;
    Rigidbody rb;
    Vector2 moveAxis;
    float turnAxis;
    bool isGrounded;
    Vector2 lastPos;
    float vel;
    WaitForSeconds velocityWait = new WaitForSeconds(0.1f);
    public static bool tp;

    [Header("Other")]
    [SerializeField] Transform piano;
    [SerializeField] GameObject lobby, ent, health, expl, manu;

    void Awake()
    {
        Transform xr = transform.GetChild(0);
        rb = xr.GetComponent<Rigidbody>();
        AudioSource[] sources = xr.GetComponents<AudioSource>();
        audioSource = sources[0];
        walkSource = sources[1];
        bgSource = xr.GetChild(0).GetChild(7).GetComponent<AudioSource>();
        bgSource.volume = 0.03f;
        //leftRb = leftJoint.GetComponent<Rigidbody>();
        //rightRb = rightJoint.GetComponent<Rigidbody>();
        leftGrip = leftGripInput.action;
        rightGrip = rightGripInput.action;
        fadeCanvas.alpha = 0;
        grabClip = grab;
        buttonClip = button;
        Door.rooms = new GameObject[] {lobby, ent, health, expl, manu};
        UIPanel.audioSource = audioSource;
        tileCount = tileLands.Length;
        concreteCount = concreteLands.Length;
        grassCount = grassLands.Length;
        gravelCount = gravelLands.Length;
        speed = outSpeed;
        //ent.SetActive(false);
        //health.SetActive(false);
        //expl.SetActive(false);
        //manu.SetActive(false);
    }

    void Start()
    {
        Vector3 pos = head.position;
        lastPos = new Vector2(pos.x, pos.z);
        StartCoroutine(Initialize());
        StartCoroutine(CalculateVelocity());
    }

    void Update()
    {
        leftHand.SetFloat("Grip", leftGrip.ReadValue<float>());
        rightHand.SetFloat("Grip", rightGrip.ReadValue<float>());
        moveAxis = moveInput.action.ReadValue<Vector2>();
        turnAxis = turnInput.action.ReadValue<Vector2>().x;

        Vector3 start = col.transform.TransformPoint(col.center);
        bool cast = Physics.Raycast(start, Vector3.down, out RaycastHit hit, col.height / 2 + 0.05f, groundMask);
        if (!cast)
        {
            if (walkSource.isPlaying) walkSource.Stop();
        }
        else
        {
            //bool play = true;
            GameObject go = hit.transform.gameObject;
            if (go.CompareTag("Floor"))
            {
                if (walkSource.clip != tileWalk)
                {
                    walkSource.volume = 0.4f;
                    walkSource.clip = tileWalk;
                }
                //else play = false;
                if (!isGrounded) audioSource.PlayOneShot(tileLands[Random.Range(0, tileCount)]);
                speed = inSpeed;
            }
            else if (go.CompareTag("Grass"))
            {
                if (Vector2.SqrMagnitude(new Vector2(start.x, start.z)) <= 441)
                {
                    if (walkSource.clip != gravelWalk)
                    {
                        walkSource.volume = 0.6f;
                        walkSource.clip = gravelWalk;
                    }
                    //else play = false;
                    if (!isGrounded) audioSource.PlayOneShot(gravelLands[Random.Range(0, gravelCount)]);
                }
                else
                {
                    if (walkSource.clip != grassWalk)
                    {
                        walkSource.volume = 0.05f;
                        walkSource.clip = grassWalk;
                    }
                    //else play = false;
                    if (!isGrounded) audioSource.PlayOneShot(grassLands[Random.Range(0, grassCount)], 0.3f);
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
                if (!isGrounded) audioSource.PlayOneShot(concreteLands[Random.Range(0, concreteCount)], 0.5f);
                //if (go.CompareTag("Stair")) speed = stairSpeed;
                speed = outSpeed;
            }
            if (vel > 0.01f)
            {
                if (!walkSource.isPlaying)
                {
                    walkSource.Play();
                    //if (play) walkSource.Play();
                    //else walkSource.UnPause();
                }
            }
            else if (walkSource.isPlaying) walkSource.Stop();


            if (jumpInput.action.WasPressedThisFrame())
            {
                //float handSpeed = ((leftRb.linearVelocity - rb.linearVelocity).sqrMagnitude + (rightRb.linearVelocity - rb.linearVelocity).sqrMagnitude) / 2;
                /*if (handSpeed > minHandSpeed)*/
                rb.linearVelocity = Vector3.up * Mathf.Sqrt(2 * -Physics.gravity.y * jumpHeight);
            }
        }
        isGrounded = cast;
        bgSource.volume = Mathf.Clamp(0.002f * (Vector3.SqrMagnitude(piano.position - head.position) - 3), 0, 0.03f);
    }

    void FixedUpdate()
    {
        col.height = Mathf.Clamp(head.position.y, minHeight, maxHeight);
        col.center = new Vector3(head.localPosition.x, col.height / 2, head.localPosition.z);
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
                yield return velocityWait;
            }
            else yield return null;
        }
    }

    public void Teleport()
    {
        audioSource.PlayOneShot(teleport);
        StartCoroutine(Fade());
    }

    IEnumerator Fade(float duration = 0.2f)
    {
        tp = rb.isKinematic = true;
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
        while (alpha >= 0.01f)
        {
            alpha = fadeCanvas.alpha = 1 - elapsedTime / duration;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        fadeCanvas.alpha = 0;
        rb.isKinematic = false;
        //yield return halfSecond;
        tp = false;
    }

    IEnumerator Initialize()
    {
        yield return velocityWait;
        initialized = true;
    }

    public static void Grab()
    {
        audioSource.PlayOneShot(grabClip, 0.5f);
    }

    public static void Click()
    {
        audioSource.PlayOneShot(buttonClip, 0.7f);
    }
}
