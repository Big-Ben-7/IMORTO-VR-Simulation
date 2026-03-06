using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class Ball : MonoBehaviour
{
    [Header("Test")]
    [SerializeField] bool testing;
    [SerializeField] Transform testPos;
    [SerializeField] Vector3 testSpeed;
    [SerializeField] InputActionProperty resetBall;
    InputAction reset;
    Vector3 initialPos;
    Coroutine test;

    Rigidbody rb;
    Collider col;
    float g = Physics.gravity.magnitude;

    [Header("Table")]
    [SerializeField] Transform netMarker;
    float netPos;
    [SerializeField] float netHeight;
    [SerializeField] float tableHeight;
    [SerializeField] Transform returnMarker1, returnMarker2;
    float[] returnX = new float[2], returnZ = new float[2];
    [SerializeField] Transform leftCornerMarker, rightCornerMarker;
    Vector2 leftCorner, rightCorner;

    [Header("Bot")]
    [SerializeField] Transform middle;
    [SerializeField] Transform top;
    [SerializeField] Transform holder;
    [SerializeField] float midL, topL, holdL;
    [SerializeField] Transform baseMarker;
    Vector3 basePos;
    [SerializeField] float minSpeed, maxSpeed;
    float contactTime, currentTime;
    Vector3 contact;
    Coroutine hitCoroutine;
    Quaternion[] botRotations = new Quaternion[3];

    [Header("Audio/FX")]
    [SerializeField] AudioClip[] racketHits;
    [SerializeField] AudioClip table, floor, wall, net, bot, other;
    int racketLength;
    AudioSource audioSource;
    UnityEngine.XR.InputDevice rightDevice;
    UnityEngine.XR.InputDevice leftDevice;
    bool leftHaptics = true, rightHaptics = true;

    [Header("Booleans")]
    [SerializeField] bool botTrack;
    [SerializeField] bool hitting;
    [SerializeField] bool farSide;
    [SerializeField] bool serving = true;
    [SerializeField] LayerMask body;
    public static bool collidingTable;
    bool grabbed;
    string lastCollision = "";
    WaitForSeconds fifthSecond = new WaitForSeconds(0.2f);
    WaitForSeconds halfSecond = new WaitForSeconds(0.5f);
    Coroutine collisionCoroutine;

    IEnumerator Test()
    {
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        if (testPos.position != Vector3.zero) rb.position = testPos.position;
        else
        {
            rb.position = new Vector3(Random.Range(returnX[0], returnX[1]), netHeight + 0.5f, Random.Range(returnZ[0], returnZ[1]));
            //Debug.Log("Position: " + myT.position);
        }
        yield return new WaitForSeconds(1);
        rb.isKinematic = false;
        if (testSpeed != Vector3.zero) rb.linearVelocity = testSpeed;
        else
        {
            rb.linearVelocity = new Vector3(Random.Range(-30, 30), Random.Range(-5, 5), Random.Range(-60, 20));
            //Debug.Log(rb.linearVelocity);
        }
        ForceAim();
        farSide = false;
        test = null;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        initialPos = rb.position;
        reset = resetBall.action;
        audioSource = GetComponent<AudioSource>();
        racketLength = racketHits.Length;
        col = GetComponent<Collider>();
    }

    void Start()
    {
        if (testing && test == null) test = StartCoroutine(Test());
        basePos = baseMarker.position;
        leftCorner = new Vector2(leftCornerMarker.position.x, leftCornerMarker.position.z);
        rightCorner = new Vector2(rightCornerMarker.position.x, rightCornerMarker.position.z);
        returnX[0] = returnMarker1.position.x;
        returnX[1] = returnMarker2.position.x;
        returnZ[0] = returnMarker1.position.z;
        returnZ[1] = returnMarker2.position.z;
        netPos = netMarker.position.z;
        rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightDevice.isValid && rightDevice.TryGetHapticCapabilities(out HapticCapabilities capabilities) && capabilities.supportsImpulse) rightHaptics = true;
        else rightHaptics = false;
        leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (leftDevice.isValid && leftDevice.TryGetHapticCapabilities(out capabilities) && capabilities.supportsImpulse) leftHaptics = true;
        else leftHaptics = false;
    }

    void Update()
    {
        if (botTrack)
        {
            if (!collidingTable)
            {
                float botSpeed;
                if (contactTime <= currentTime) botSpeed = 1;
                else botSpeed  = Time.deltaTime / (contactTime - currentTime);
                middle.localRotation = Quaternion.Slerp(middle.localRotation, botRotations[0], botSpeed);
                top.localRotation = Quaternion.Slerp(top.localRotation, botRotations[1], botSpeed);
                //Vector3 look = new Vector3(botRotations[2].x, Quaternion.LookRotation(myT.position - holder.position, holder.up).eulerAngles.y, 0);
                holder.localRotation = Quaternion.Slerp(holder.localRotation, botRotations[2], botSpeed);
                currentTime += Time.deltaTime;
            }
        }
        else if (!hitting)
        {
            middle.localRotation = Quaternion.Slerp(middle.localRotation, Quaternion.identity, Time.deltaTime * 4);
            top.localRotation = Quaternion.Slerp(top.localRotation, Quaternion.identity, Time.deltaTime * 4);
            holder.localRotation = Quaternion.Slerp(holder.localRotation, Quaternion.identity, Time.deltaTime * 4);
        }
        //if (reset.WasPressedThisFrame()) Reset();
    }

    void OnTriggerEnter(Collider other)
    {
        GameObject go = other.gameObject;
        string name = go.name;
        switch (go.tag)
        {
            case "Table":
                //Hit net
                if (!grabbed && lastCollision != name)
                {
                    audioSource.PlayOneShot(net, Mathf.Clamp01(Mathf.Log10(rb.linearVelocity.magnitude + 1)));
                    lastCollision = name;
                    if (collisionCoroutine != null) StopCoroutine(collisionCoroutine);
                    collisionCoroutine = StartCoroutine(ResetLastCollision());
                    rb.linearVelocity = 0.5f * Vector3.Reflect(rb.linearVelocity, IsPastNet() ? Vector3.back : Vector3.forward);
                }
                if (botTrack) botTrack = false;
                break;
            case "BotRacket":
                if (!botTrack) break;
                if (!testing && serving) serving = false;
                //col.isTrigger = true;
                botTrack = false;
                Vector3 vel = SetReturnVel();
                rb.linearVelocity = vel;
                if (hitCoroutine != null)
                {
                    StopCoroutine(hitCoroutine);
                    hitting = false;
                }
                hitCoroutine = StartCoroutine(Hit(Quaternion.LookRotation(-vel.normalized, holder.up)));
                if (!grabbed && lastCollision != name)
                {
                    audioSource.PlayOneShot(racketHits[Random.Range(0, racketLength)], Mathf.Clamp01(Mathf.Log10(rb.linearVelocity.magnitude / 2 + 1)));
                    lastCollision = name;
                    if (collisionCoroutine != null) StopCoroutine(collisionCoroutine);
                    collisionCoroutine = StartCoroutine(ResetLastCollision());
                }
                break;
        }
    }

    /*void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "BotRacket") col.isTrigger = false;
    }*/

    void OnCollisionEnter(Collision collision)
    {
        if (!Player.initialized) return;
        GameObject go = collision.gameObject;
        string name = go.name;
        switch (go.tag)
        {
            case "Table":
                if (!grabbed && lastCollision != name)
                {
                    audioSource.PlayOneShot(table, Mathf.Clamp01(Mathf.Log10(collision.relativeVelocity.magnitude / 3 + 1)));
                    lastCollision = name;
                    if (collisionCoroutine != null) StopCoroutine(collisionCoroutine);
                    collisionCoroutine = StartCoroutine(ResetLastCollision());
                }
                break;
            case "Player":
                if (!grabbed && lastCollision != name)
                {
                    audioSource.PlayOneShot(racketHits[Random.Range(0, racketLength)], Mathf.Clamp01(Mathf.Log10(collision.relativeVelocity.magnitude / 2 + 1)));
                    lastCollision = name;
                    if (collisionCoroutine != null) StopCoroutine(collisionCoroutine);
                    collisionCoroutine = StartCoroutine(ResetLastCollision());
                }
                break;
            case "Floor":
                if (!grabbed && lastCollision != name)
                {
                    audioSource.PlayOneShot(floor, Mathf.Clamp01(Mathf.Log10(collision.relativeVelocity.magnitude / 3 + 1)));
                    lastCollision = name;
                    if (collisionCoroutine != null) StopCoroutine(collisionCoroutine);
                    collisionCoroutine = StartCoroutine(ResetLastCollision());
                }
                goto default;
            case "Wall":
                if (!grabbed && lastCollision != name)
                {
                    audioSource.PlayOneShot(wall, Mathf.Clamp01(Mathf.Log10(collision.relativeVelocity.magnitude / 3 + 1)));
                    lastCollision = name;
                    if (collisionCoroutine != null) StopCoroutine(collisionCoroutine);
                    collisionCoroutine = StartCoroutine(ResetLastCollision());
                }
                goto default;
            default:
                if (!grabbed && lastCollision != name && go.layer != 7)
                {
                    audioSource.PlayOneShot(other, Mathf.Clamp01(Mathf.Log10(collision.relativeVelocity.magnitude / 3 + 1)));
                    lastCollision = name;
                    if (collisionCoroutine != null) StopCoroutine(collisionCoroutine);
                    collisionCoroutine = StartCoroutine(ResetLastCollision());
                }
                if (farSide) farSide = false;
                if (botTrack) botTrack = false;
                if (testing)
                {
                    if (test == null) test = StartCoroutine(Test());
                }
                else if (!serving && go.tag != "Player") serving = true;
                break;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        switch (collision.gameObject.tag)
        {
            case "Table":
                if (IsPastNet())
                {
                    if (!farSide && rb.linearVelocity.y > 0)
                    {
                        farSide = botTrack = true;
                        RotateBot();
                    }
                    else botTrack = false;
                }
                else if (farSide) farSide = botTrack = false;
                break;
            case "Player":
                if (Racket.grabbed)
                {
                    if (farSide && !IsPastNet()) farSide = false;
                    switch(Racket.handeness)
                    {
                        case Racket.Handedness.Left:
                            if (leftHaptics) leftDevice.SendHapticImpulse(0, 0.75f, 0.1f);
                            break;
                        case Racket.Handedness.Right:
                            if (rightHaptics) rightDevice.SendHapticImpulse(0, 0.75f, 0.1f);
                            break;
                    }
                    ForceAim();
                }
                break;
        }
    }

    void ForceAim()
    {
        Vector3 pos = rb.position, vel = rb.linearVelocity;
        float theta = Mathf.Atan2(-vel.z, vel.x);
        float leftAng = Mathf.Atan2(pos.z - leftCorner.y, leftCorner.x - pos.x), rightAng = Mathf.Atan2(pos.z - rightCorner.y, rightCorner.x - pos.x);
        
        //Simple clamp
        /*if (leftAng - theta >= 1 || theta - rightAng >= 1)
        {
            rb.linearVelocity = vel.normalized * Mathf.Clamp(vel.magnitude / 5, 0, 12);
            return;
        }
        theta = Mathf.Clamp(theta, leftAng, rightAng);
        float mag = Mathf.Clamp(8 * Mathf.Sqrt(vel.x * vel.x + vel.z * vel.z) / 5, 0, 8);
        rb.linearVelocity = new Vector3(mag * Mathf.Cos(theta), Mathf.Clamp(vel.y / 10, -3, 3), -mag * Mathf.Sin(theta));*/

        //Return
        float vY = Mathf.Clamp(vel.y / 1.5f, -5, 5);
        float a = -0.5f * g, b = vY, c = pos.y - tableHeight;
        float d = b * b - 4 * a * c;
        if (leftAng - theta >= 1.5f || theta - rightAng >= 1.5f || d < 0)
        {
            rb.linearVelocity = vel.normalized * Mathf.Clamp(vel.magnitude / 1.5f, 0, 50);
            //Debug.Log(theta);
            return;
        }
        float t = (-b - Mathf.Sqrt(d)) / (2 * a);
        if (t < 0.3f) t = 0.3f;
        theta = Mathf.Clamp(theta, leftAng, rightAng);
        float sTheta = Mathf.Sin(theta);
        float d0, d1;
        if (serving)
        {
            float third = (pos.z - netPos) / 3;
            d0 = third / sTheta;
            d1 = 2 * third / sTheta;
        }
        else
        {
            d0 = (pos.z - netPos) / sTheta;
            d1 = (pos.z - leftCorner.y) / sTheta;
        }
        float v0 = d0 / t, v1 = d1 / t;
        float vD = Mathf.Clamp(v0 + v1 * Mathf.Sqrt(vel.x * vel.x + vel.z * vel.z) / 60, v0, v1);
        float vX = vD * Mathf.Cos(theta);
        float vZ = -vD * sTheta;
        rb.linearVelocity = new Vector3(vX, vY, vZ);
    }

    void RotateBot()
    {
        //Generate contact point
        Vector3 pos = rb.position;
        Vector3 vel = rb.linearVelocity;
        float yVel = vel.y;
        float t = yVel / g;
        Vector3 peak = new Vector3(vel.x*t + pos.x, -g/2*t*t + vel.y*t + pos.y, vel.z*t + pos.z);
        if (peak.y <= 2f)
        {
            t += 0.2f;
        }
        else if (peak.z > 0.5 * (leftCorner.y + netPos))
        {
            float d = Random.Range(peak.y - 2f, peak.y - 1.5f);
            t += Mathf.Sqrt(2 * d / g);
        }
        else
        {
            float d = Random.Range(peak.y - 2f, peak.y - 1.5f);
            t -= Mathf.Sqrt(2 * d / g);
        }
        contact = new Vector3(vel.x*t + pos.x, -g/2*t*t + vel.y*t + pos.y, vel.z*t + pos.z);
        Vector3 dir = contact - basePos;
        float d1 = dir.magnitude;
        Vector3 normal = new Plane(basePos, basePos + Vector3.up, contact).normal;

        //Find ball reflection angle
        Vector3 newVel = vel - Vector3.up * g * t;
        Vector3 projectedVel = Vector3.ProjectOnPlane(newVel, normal);
        float maxAng;
        if (d1 + holdL <= midL + topL) maxAng = 180;
        else maxAng = LOC(d1, holdL, midL + topL);
        float ballAng = Mathf.Min(270 - Vector3.SignedAngle(dir, projectedVel, -normal), maxAng);

        //Find middle and top angles
        float d2 = Mathf.Sqrt(d1 * d1 + holdL * holdL - 2 * d1 * holdL * Mathf.Cos(ballAng * Mathf.Deg2Rad));
        float theta = LOC(midL, topL, d2);
        float theta2 = LOC(midL, d2, topL);

        //Find rotation angles
        float midAngY = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        float elevationAngle = LOC(d1, d2, holdL);
        float midAngX = -(90 - theta2 - elevationAngle - Mathf.Atan2(dir.y, Mathf.Sqrt(dir.x * dir.x + dir.z * dir.z)) * Mathf.Rad2Deg);
        float topAng = theta - 90;
        float holdAng = 270 - theta - theta2 - elevationAngle - ballAng;
        botRotations[0] = Quaternion.Euler(midAngX, midAngY, 0);
        botRotations[1] = Quaternion.Euler(topAng, 0, 0);
        botRotations[2] = Quaternion.Euler(holdAng, 0, 0);
        currentTime = 0;
        contactTime = t - 0.2f;
        //StartCoroutine(SoundBot(contactTime / 4));
    }

    float LOC(float side1, float side2, float side3)
    {
        return Mathf.Acos(Mathf.Clamp((side1 * side1 + side2 * side2 - side3 * side3) / (2 * side1 * side2), -0.9999999999f, 0.9999999999f)) * Mathf.Rad2Deg;
    }

    /*IEnumerator SoundBot(float wait)
    {
        yield return new WaitForSeconds(wait);
        audioSource.PlayOneShot(bot, 0.1f);
    }*/

    Vector3 SetReturnVel()
    {
        //Set variables
        float y0 = contact.y, z0 = contact.z, y = tableHeight, h;
        float targetZ = Random.Range(returnZ[0], returnZ[1]);
        float v = Random.Range(minSpeed, maxSpeed);
        float a = Random.Range(returnX[0], returnX[1]) - contact.x, b = targetZ - z0;
        float x = Mathf.Sqrt(a * a + b * b);
        float k = (netPos - z0) / (targetZ - z0);
        float hMin = y0 * (1 - k) + k * y + 0.5f * g * k * (1 - k) * (x / v) * (x / v);
        //Debug.Log(v);
        if (hMin < netHeight + 0.1f)
        {
            h = netHeight + Random.Range(0.1f, 0.3f);
            ///Debug.Log("First If");
        }
        else if (hMin > tableHeight * 2)
        {
            h = Random.Range(y0 * (1 - k) + k * y + 2 * (netHeight-tableHeight), 2 * tableHeight);
            //Debug.Log("Second If");
        }
        else
        {
            h = hMin;
            ///Debug.Log("Third If");
        }

        //Set velocities
        float t = Mathf.Sqrt(2 * (h - k * y - y0 + k * y0) / (g * (1 - k) * k));
        float vy = (y - y0 + 0.5f * g * t * t) / t;
        float vd = x / t;
        float theta = Mathf.Atan2(b, a);
        return new Vector3(vd * Mathf.Cos(theta), vy, vd * Mathf.Sin(theta));
    }

    IEnumerator Hit(Quaternion rot)
    {
        hitting = true;
        while (Quaternion.Angle(holder.rotation, rot) >= 0.1f)
        {
            holder.rotation = Quaternion.Slerp(holder.rotation, rot, Time.deltaTime * 20);
            yield return null;
        }
        hitting = false;
        hitCoroutine = null;
    }

    bool IsPastNet()
    {
        if (rb.position.z >= netPos) return false;
        return true;
    }

    public void Reset()
    {
        rb.constraints = RigidbodyConstraints.FreezePosition;
        StopAllCoroutines();
        test = hitCoroutine = collisionCoroutine = null;
        rb.linearVelocity = Vector3.zero;
        farSide = botTrack = hitting = false;
        if (!testing) serving = true;
        lastCollision = "";
        rb.position = initialPos;
        rb.constraints = RigidbodyConstraints.None;
    }

    IEnumerator ResetLastCollision()
    {
        string t = lastCollision;
        yield return fifthSecond;
        if (lastCollision == t) lastCollision = "";
    }

    public void Grab()
    {
        farSide = botTrack = hitting = false;
        if (!testing) serving = true;
        grabbed = !grabbed;
        if (grabbed)
        {
            col.excludeLayers = body;
            rb.interpolation = RigidbodyInterpolation.None;
            Player.Grab();
        }
        else
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            StartCoroutine(Release());
        }
    }

    IEnumerator Release()
    {
        yield return halfSecond;
        if (!grabbed) col.excludeLayers = 0;
    }
}
