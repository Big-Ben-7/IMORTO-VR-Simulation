using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class Ball : MonoBehaviour
{
    [Header("Testing")]
    [SerializeField] bool testing;
    [SerializeField] Transform testPos;
    [SerializeField] Vector3 testSpeed;
    //[SerializeField] InputActionProperty resetBall;
    //InputAction reset;
    Vector3 initialPos;
    Coroutine test;

    Rigidbody rb;
    Collider col;
    float g = Physics.gravity.magnitude;

    //Table
    public static float netPos, netHeight, tableHeight;
    public static float[] returnX = new float[2], returnZ = new float[2];//, launchZ = new float[2];
    public static Vector2 leftCorner, rightCorner;

    //Bot
    public static Transform middle, top, holder;
    public static float midL, topL, holdL;
    public static Transform racket;
    public static Vector3 basePos;
    public static float minSpeed, maxSpeed, easyMinSpeed, easyMaxSpeed, hardMinSpeed, hardMaxSpeed;
    float contactTime, currentTime;
    Vector3 contact;
    Coroutine hitCoroutine;
    Quaternion[] botRotations = new Quaternion[3];
    public static int difficulty = 0;

    //Audio/FX
    public static AudioClip[] racketHits;
    public static AudioClip table, floor, wall, net, bot, other, launch;
    public static int racketLength;
    AudioSource audioSource;

    [Header("Booleans")]
    [SerializeField] bool botTrack;
    [SerializeField] bool hitting;
    [SerializeField] bool farSide;
    [SerializeField] bool serving = true;
    public static LayerMask body, defaultLayer;
    public static bool collidingTable;
    bool grabbed;
    string lastCollision = "";
    public static WaitForSeconds fifthSecond;
    public static WaitForSeconds halfSecond;
    Coroutine collisionCoroutine;
    public static Ball instance;
    public static List<Ball> nextActive;
    bool active;

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
        //reset = resetBall.action;
        audioSource = GetComponent<AudioSource>();
        col = GetComponent<Collider>();
    }

    void Start()
    {
        if (testing && test == null) test = StartCoroutine(Test());
    }

    public void Activate(Vector3 pos, /*float v*/ Vector3 vel/*, Transform refT*/)
    {
        //instance = this;
        farSide = botTrack = hitting = false;
        serving = true;
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        col.excludeLayers = defaultLayer;
        rb.position = pos;
        rb.isKinematic = false;
        rb.linearVelocity = vel;
        StartCoroutine(Release(fifthSecond));
        /*float randomZ = Random.Range(launchZ[0], launchZ[1]);
        Vector3 refPos = refT.position;
        rb.linearVelocity = CalcReturnVel(pos.x, pos.y, pos.z, tableHeight, randomZ, v, (randomZ - refPos.z)/Mathf.Tan(refT.rotation.eulerAngles.y * Mathf.Deg2Rad) + refPos.x);*/
        ForceAim();
        audioSource.PlayOneShot(launch, 0.03f);
    }

    void Update()
    {
        if (instance != this)
        {
            //if (botTrack) botTrack = false;
            //if (hitting) hitting = false;
            return;
        }
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
        else if (!hitting) ResetBot();
        //if (reset.WasPressedThisFrame()) Reset();
    }

    public static void ResetBot()
    {
        if (Quaternion.Angle(holder.rotation, Quaternion.identity) < 0.1f) return;
        middle.localRotation = Quaternion.Slerp(middle.localRotation, Quaternion.identity, Time.deltaTime * 4);
        top.localRotation = Quaternion.Slerp(top.localRotation, Quaternion.identity, Time.deltaTime * 4);
        holder.localRotation = Quaternion.Slerp(holder.localRotation, Quaternion.identity, Time.deltaTime * 4);
    }

    void OnTriggerEnter(Collider other)
    {
        GameObject go = other.gameObject;
        string name = go.name;
        //Hits net
        if (go.CompareTag("Table"))
        {
            if (!grabbed && lastCollision != name)
            {
                audioSource.PlayOneShot(net, Mathf.Clamp01(Mathf.Log10(rb.linearVelocity.magnitude + 1)));
                lastCollision = name;
                if (collisionCoroutine != null) StopCoroutine(collisionCoroutine);
                collisionCoroutine = StartCoroutine(ResetLastCollision());
                rb.linearVelocity = 0.5f * Vector3.Reflect(rb.linearVelocity, IsPastNet() ? Vector3.back : Vector3.forward);
            }
            if (botTrack) botTrack = false;
            SetNextActive();
        }
        else if (go.CompareTag("BotRacket"))
        {
            if (!grabbed && lastCollision != name)
            {
                audioSource.PlayOneShot(racketHits[Random.Range(0, racketLength)], Mathf.Clamp01(Mathf.Log10(rb.linearVelocity.magnitude / 2 + 1)));
                lastCollision = name;
                if (collisionCoroutine != null) StopCoroutine(collisionCoroutine);
                collisionCoroutine = StartCoroutine(ResetLastCollision());
            }
            if (!botTrack) return;
            if (!testing && serving) serving = false;
            //col.isTrigger = true;
            botTrack = false;

            if (instance != this) return;
            float v, randomX;
            switch (difficulty)
            {
                //No mercy: fast, random return
                case 3:
                    v = Random.Range(hardMinSpeed, hardMaxSpeed);
                    randomX = Random.Range(leftCorner.x, rightCorner.x);
                    break;
                //Tough: random return
                case 2:
                    v = Random.Range(minSpeed, maxSpeed);
                    randomX = Random.Range(returnX[0], returnX[1]);
                    break;
                //Serious: rough aimed return
                case 1:
                    v = Random.Range(minSpeed, maxSpeed);
                    randomX = Mathf.Clamp(racket.position.x - Random.Range(-0.1f, 0.25f), returnX[0], returnX[1]);
                    break;
                //Friendly: slow, aimed return
                default:
                    v = Random.Range(easyMinSpeed, easyMaxSpeed);
                    randomX = Mathf.Clamp(racket.position.x - Random.Range(0.1f, 0.15f), returnX[0], returnX[1]);
                    break;
            }
            Vector3 vel = CalcReturnVel(contact.x, contact.y, contact.z, tableHeight, Random.Range(returnZ[0], returnZ[1]), v, randomX);
            rb.linearVelocity = vel;
            if (hitCoroutine != null)
            {
                StopCoroutine(hitCoroutine);
                hitting = false;
            }
            hitCoroutine = StartCoroutine(Hit(Quaternion.LookRotation(-vel.normalized, holder.up)));
        }
    }

    /*void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("BotRacket")) col.isTrigger = false;
    }*/

    void OnCollisionEnter(Collision collision)
    {
        if (!Player.initialized) return;
        GameObject go = collision.gameObject;
        string name = go.name;
        if (go.CompareTag("Table"))
        {
            if (!grabbed && lastCollision != name)
            {
                audioSource.PlayOneShot(table, Mathf.Clamp01(Mathf.Log10(collision.relativeVelocity.magnitude / 3 + 1)));
                lastCollision = name;
                if (collisionCoroutine != null) StopCoroutine(collisionCoroutine);
                collisionCoroutine = StartCoroutine(ResetLastCollision());
            }
            return;
        }
        else if (go.CompareTag("Racket"))
        {
            if (!grabbed && lastCollision != name)
            {
                audioSource.PlayOneShot(racketHits[Random.Range(0, racketLength)], Mathf.Clamp01(Mathf.Log10(collision.relativeVelocity.magnitude / 2 + 1)));
                lastCollision = name;
                if (collisionCoroutine != null) StopCoroutine(collisionCoroutine);
                collisionCoroutine = StartCoroutine(ResetLastCollision());
            }
            return;
        }
        else if (go.CompareTag("Floor"))
        {
            if (!grabbed && lastCollision != name)
            {
                audioSource.PlayOneShot(floor, Mathf.Clamp01(Mathf.Log10(collision.relativeVelocity.magnitude / 3 + 1)));
                lastCollision = name;
                if (collisionCoroutine != null) StopCoroutine(collisionCoroutine);
                collisionCoroutine = StartCoroutine(ResetLastCollision());
            }
        }
        else if (go.CompareTag("Wall"))
        {
            if (!grabbed && lastCollision != name)
            {
                audioSource.PlayOneShot(wall, Mathf.Clamp01(Mathf.Log10(collision.relativeVelocity.magnitude / 3 + 1)));
                lastCollision = name;
                if (collisionCoroutine != null) StopCoroutine(collisionCoroutine);
                collisionCoroutine = StartCoroutine(ResetLastCollision());
            }
        }
        else if (!grabbed && lastCollision != name && go.layer != 7)
        {
            audioSource.PlayOneShot(other, Mathf.Clamp01(Mathf.Log10(collision.relativeVelocity.magnitude / 3 + 1)));
            lastCollision = name;
            if (collisionCoroutine != null) StopCoroutine(collisionCoroutine);
            collisionCoroutine = StartCoroutine(ResetLastCollision());
        }

        if (farSide) farSide = false;
        if (botTrack) botTrack = false;
        SetNextActive();
        if (testing)
        {
            if (/*instance == this &&*/ test == null) test = StartCoroutine(Test());
        }
        else if (!serving) serving = true;
    }

    void OnCollisionExit(Collision collision)
    {
        GameObject go = collision.gameObject;
        if (go.CompareTag("Table"))
        {
            if (IsPastNet())
            {
                if (!farSide && rb.linearVelocity.y > 0)
                {
                    farSide = true;
                    //if (instance == this)
                    //{
                    botTrack = true;
                    RotateBot();
                    if (instance == null) instance = this;
                    else
                    {
                        nextActive.Add(this);
                        active = true;
                    }
                    //}
                }
                else botTrack = false;
            }
            else
            {
                if (botTrack) botTrack = false;
                if (farSide) farSide = false;
                SetNextActive();
            }
        }
        else if (go.CompareTag("Racket"))
        {
            if (Racket.grabbed)
            {
                if (farSide && !IsPastNet()) farSide = false;
                SetNextActive();
                switch(Racket.hand)
                {
                    case MyHandedness.Left:
                        Player.SendHapticsLeft(1.5f, 0.15f);
                        break;
                    case MyHandedness.Right:
                        Player.SendHapticsRight(1.5f, 0.15f);
                        break;
                }
                ForceAim();
            }
        }
    }

    void ForceAim()
    {
        //if (instance != this) return;
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
        //if (instance != this) return;
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

    Vector3 CalcReturnVel(float x0, float y0, float z0, float y, float targetZ, float v, float randomX)
    {
        float h, a = randomX - x0, b = targetZ - z0;
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
            if (instance != this) break;
            holder.rotation = Quaternion.Slerp(holder.rotation, rot, Time.deltaTime * 20);
            yield return null;
        }
        hitting = false;
        hitCoroutine = null;
        SetNextActive();
    }

    void SetNextActive()
    {
        if (instance == this)
        {
            if (nextActive.Count > 0)
            {
                instance = nextActive[0];
                nextActive.RemoveAt(0);
                instance.active = false;
            }
            else instance = null;
        }
        else if (active)
        {
            nextActive.Remove(this);
            active = false;
        }
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
            StartCoroutine(Release(halfSecond));
        }
    }

    IEnumerator Release(WaitForSeconds wait)
    {
        yield return wait;
        if (!grabbed) col.excludeLayers = 0;
    }
}
