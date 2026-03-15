using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

public class ExplorationManager : MonoBehaviour, IPointerClickHandler
{
    [Header("Rover")]
    [SerializeField] Transform[] rockTs;
    NavMeshObstacle[] rockObstacles = new NavMeshObstacle[5];
    [SerializeField] RectTransform[] buttons;
    bool[] disable = new bool[] {false, false, false, false, false};
    [SerializeField] Transform[] scans;
    Canvas[] scanCanvases = new Canvas[5];
    [SerializeField] Transform target;
    [SerializeField] RectTransform targetRing;
    [SerializeField] GameObject[] mapColors;
    RectTransform[] colorRects = new RectTransform[5];
    [SerializeField] Transform colorMarkers;
    Vector3[] colorPos = new Vector3[5];
    Animator[] animators = new Animator[6];
    [SerializeField] LayerMask terrainLayer, platformLayer, rockLayer;
    [SerializeField] Transform rover;
    Animator roverAnim;
    [SerializeField] float roverRotSpeed;
    [SerializeField] Camera roverCam, heliCam;
    Canvas block;
    [SerializeField] Transform lens;
    [SerializeField] float laserSpeed;
    LineRenderer[] lasers;
    int laserCount;
    Transform ingenuity;
    Transform heli;
    float initialSize;
    [SerializeField] GameObject screen;
    [SerializeField] AudioClip scanClip, drillClip, foundClip, successClip, errorClip, uiClip, colorClip, ringClip, startClip, gravel;
    NavMeshAgent roverAgent;
    //Rigidbody rb;
    Transform[] wheels = new Transform[3];
    //Transform[] indWheels = new Transform[6];
    AudioSource roverSource;
    int rockIndex = -1;
    int rocksScanned;
    bool traveling, inCoroutine, canClick = true;
    RectTransform rectT;
    Rect rect;
    WaitForSeconds second;
    WaitForSeconds halfSecond;
    WaitForSeconds fifthSecond;
    [SerializeField] Transform[] joysticks;
    Transform origin;
    Transform[] marks = new Transform[2];
    Vector3[] initialPos = new Vector3[2];
    [SerializeField] LayerMask body;
    [SerializeField] float deadZone, speed;
    Collider[] cols = new Collider[2];
    bool[] grabbed = new bool[] {false, false};
    enum State {Idle, Launch, Fly, Reset};
    State state = State.Idle;
    Vector3 launchPos;
    [SerializeField] Transform ingenuityTarget, bounds1, bounds2;
    float xMin, xMax, yMin, yMax, zMin, zMax;
    Vector3 launchTarget;
    AudioSource heliSource;
    [SerializeField] AudioClip launchClip;
    Animator heliAnim;
    bool cooldown;

    [Header("Solar System")]
    [SerializeField] Transform solarSystem;
    [SerializeField] float[] revSpeeds = new float[8], rotSpeeds = new float[9];
    Transform[] planetPivots = new Transform[8], planetAnchors = new Transform[8], planets = new Transform[8];
    Transform sun;

    void Awake()
    {
        rectT = GetComponent<RectTransform>();
        rect = rectT.rect;
        roverAgent = rover.GetComponent<NavMeshAgent>();
        //rb = rover.GetComponent<Rigidbody>();
        roverAnim = rover.GetComponent<Animator>();
        //roverAgent.updateRotation = false;
        roverSource = rover.GetComponent<AudioSource>();
        lasers = lens.GetComponentsInChildren<LineRenderer>();
        laserCount = lasers.Length;
        for (int i = 0; i < laserCount; i++) lasers[i].enabled = false;
        Transform p = rover.GetChild(0);
        cols[0] = joysticks[0].GetComponent<Collider>();
        cols[1] = joysticks[1].GetComponent<Collider>();
        origin = joysticks[0].parent.GetChild(1);
        Transform t = marks[0] = joysticks[0].GetChild(0);
        initialPos[0] = origin.InverseTransformPoint(t.position);
        t = marks[1] = joysticks[1].GetChild(0);
        initialPos[1] = origin.InverseTransformPoint(t.position);
        for (int i = 0; i < 3; i++) wheels[i] = p.GetChild(i+4);
        //for (int i = 0; i < 6; i++) indWheels[i] = p.GetChild(i+7);
        for (int i = 0; i < 5; i++)
        {
            rockObstacles[i] = rockTs[i].GetComponent<NavMeshObstacle>();
            /*RectTransform rectT = */colorRects[i] = mapColors[i].GetComponent<RectTransform>();
            //if (Physics.Raycast(heliCam.ViewportPointToRay(Rect.PointToNormalized(rect, rectT.anchoredPosition)), out RaycastHit hit, 20, terrainLayer)) colorPos[i] = hit.point;
            animators[i] = mapColors[i].GetComponent<Animator>();
            scanCanvases[i] = scans[i].GetChild(1).GetComponent<Canvas>();
            colorPos[i] = colorMarkers.GetChild(i).position;
        }
        animators[5] = targetRing.GetComponent<Animator>();
        heli = heliCam.transform;
        ingenuity = heli.parent;
        heliSource = ingenuity.GetComponent<AudioSource>();
        heliAnim = ingenuity.GetComponent<Animator>();
        block = ingenuity.GetChild(1).GetComponent<Canvas>();
        for (int i = 0; i < 8; i++)
        {
            Transform pivot = planetPivots[i] = solarSystem.GetChild(i+1);
            Transform anchor = planetAnchors[i] = pivot.GetChild(0);
            planets[i] = anchor.GetChild(0);
        }
        sun = solarSystem.GetChild(0);
        second = Player.one;
        halfSecond = Player.half;
        fifthSecond = Player.fifth;
    }
    
    void Start()
    {
        launchPos = ingenuity.position;
        initialSize = heliCam.orthographicSize;
        launchTarget = ingenuityTarget.position;
        xMin = bounds1.position.x;
        xMax = bounds2.position.x;
        yMin = bounds1.position.y;
        yMax = bounds2.position.y;
        zMin = bounds1.position.z;
        zMax = bounds2.position.z;
    }

    void Update()
    {
        switch (state)
        {
            case State.Idle:
                break;
            case State.Launch:
                Vector3 pos = ingenuity.position;
                ingenuity.position = Vector3.MoveTowards(pos, launchTarget, Time.deltaTime);
                Vector3 d =  launchTarget - ingenuity.position;
                if (Mathf.Abs(d.x) < 0.01f && Mathf.Abs(d.y) < 0.01f && Mathf.Abs(d.z) < 0.01f)
                {
                    ingenuity.position = launchTarget;
                    state = State.Fly;
                }
                break;
            case State.Fly:
                d = origin.InverseTransformPoint(marks[0].position) - initialPos[0];
                float dy = (origin.InverseTransformPoint(marks[1].position) - initialPos[1]).z;
                if (Mathf.Abs(d.x) < deadZone) d.x = 0;
                else d.x -= deadZone;
                if (Mathf.Abs(d.z) < deadZone) d.z = 0;
                else d.z -= deadZone;
                if (Mathf.Abs(dy) < deadZone) dy = 0;
                else dy -= deadZone;
                pos = ingenuity.position;
                if (pos.y > yMax)
                {
                    dy = 0;
                    pos.y = yMax;
                }
                else if (pos.y < yMin)
                {
                    dy = 0;
                    pos.y = yMin;
                }
                if (pos.z > zMax)
                {
                    d.x = 0;
                    pos.z = zMax;
                }
                else if (pos.z < zMin)
                {
                    d.x = 0;
                    pos.z = zMin;
                }
                if (pos.x > xMax)
                {
                    d.z = 0;
                    pos.x = xMax;
                }
                else if (pos.x < xMin)
                {
                    d.z = 0;
                    pos.x = xMin;
                }
                ingenuity.position = pos;
                ingenuity.Translate(new Vector3(d.z, dy, -d.x) * speed * Time.deltaTime);
                break;
            case State.Reset:
                pos = ingenuity.position;
                ingenuity.position = Vector3.MoveTowards(pos, launchPos, Time.deltaTime);
                d = launchPos - ingenuity.position;
                if (Mathf.Abs(d.x) < 0.01f && Mathf.Abs(d.y) < 0.01f && Mathf.Abs(d.z) < 0.01f)
                {
                    ingenuity.position = launchPos;
                    heliSource.Stop();
                    heliAnim.SetTrigger("Reset");
                    state = State.Idle;
                    screen.SetActive(false);
                    roverCam.enabled = false;
                    block.enabled = true;
                }
                break;
        }
        float scaleFactor = heli.position.y / 3;
        if (heliCam.enabled) heliCam.orthographicSize = initialSize * scaleFactor;
        for (int i = 0; i < 5; i++)
        {
            Vector3 vp = heliCam.WorldToViewportPoint(rockTs[i].position);
            float x = vp.x, y = vp.y;
            //if (vp.z <= 0 || x > 1 || x < 0 || y > 1 || y < 0 || disable[i]) buttons[i].gameObject.SetActive(false);
            if (disable[i]) buttons[i].gameObject.SetActive(false);
            else
            {
                if (!buttons[i].gameObject.activeSelf) buttons[i].gameObject.SetActive(true);
                buttons[i].anchoredPosition = PosToPoint(rect, new Vector2(x, y) + (y < 0.5f ? Vector2.up : Vector2.down) * 0.12f / scaleFactor);
            }
            if (mapColors[i].activeSelf)
            {
                colorRects[i].anchoredPosition = PosToPoint(rect, heliCam.WorldToViewportPoint(colorPos[i]));
                colorRects[i].localScale = Vector3.one / scaleFactor;
            }
            if (targetRing.gameObject.activeSelf)
            {
                targetRing.anchoredPosition = PosToPoint(rect, heliCam.WorldToViewportPoint(target.position));
                targetRing.localScale = Vector3.one / scaleFactor;
            }
        }
        if (traveling)
        {
            for (int i = 0; i < 3; i++)
            {
                wheels[i].Rotate(Vector3.forward, roverAgent.velocity.magnitude * 572.957795131f * Time.deltaTime);
            }
            if (!inCoroutine && roverAgent.remainingDistance <= roverAgent.stoppingDistance && (!roverAgent.hasPath || roverAgent.velocity.sqrMagnitude < 1e-5f))
            {
                traveling = false;
                if (rockIndex != -1)
                {
                    if (rockIndex == 5) StartCoroutine(Drill());
                    else StartCoroutine(ScanRock());
                }
                else
                {
                    roverSource.Stop();
                    heliCam.enabled = true;
                    screen.SetActive(true);
                    roverCam.enabled = false;
                    canClick = true;
                }
            }
        }
        /*if (!roverAgent.hasPath) return;
        rb.AddTorque(Vector3.Cross(rover.forward, roverAgent.steeringTarget - rover.position).normalized * roverRotSpeed, ForceMode.VelocityChange);
        /*float roverSpeed = roverAgent.velocity.magnitude;
        if (roverAgent.remainingDistance <= roverAgent.stoppingDistance && (!roverAgent.hasPath || roverSpeed < 0.1))
            return;
        Vector3 direction = roverAgent.pathEndPosition - rover.position;
        if (direction.sqrMagnitude < 0.1)
            return;
        Vector3[] hitPoints = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            if (Physics.Raycast(indWheels[i].position, Vector3.down, out RaycastHit hit, 5, terrainLayer)) hitPoints[i] = hit.point;
        }
        Array.Sort(hitPoints, (a, b) => a.y.CompareTo(b.y));
        Vector3 lowPoint = hitPoints[0];
        Vector3 r0 = (lowPoint-hitPoints[4]).normalized;
        Vector3 r1 = (lowPoint-hitPoints[5]).normalized;
        float xd = Vector3.Dot(r0, r1);
        if (xd > 0.95 || xd < -0.95) lowPoint = hitPoints[1];
        Vector3 normal = new Plane(hitPoints[4], lowPoint, hitPoints[5]).normal;
        if (normal.y < 0) normal *= -1;
        float cx = Vector3.Dot(normal, rover.up);
        if (cx < 0.95) cx = 0.05f/(1-cx);
        else cx = 1.0f;
        rover.rotation = Quaternion.Slerp(rover.rotation, Quaternion.LookRotation(direction, rover.up), roverRotSpeed * Time.deltaTime);
        rover.rotation =Quaternion.Slerp(rover.rotation, rover.rotation * Quaternion.FromToRotation(rover.up, normal), cx);
        /*if (Physics.Raycast(rover.position, -rover.up, out RaycastHit hit, 5, terrainLayer))
        {
            Debug.Log("rotating");
            if (Physics.Raycast(indWheels[4].position, -indWheels[4].up, out RaycastHit hit1, 5, terrainLayer)) Debug.Log("2");
            if (Physics.Raycast(indWheels[5].position, -indWheels[5].up, out RaycastHit hit2, 5, terrainLayer)) Debug.Log("3");
            rover.rotation = Quaternion.Slerp(rover.rotation, Quaternion.LookRotation(rockTs[1].position-rover.position, new Plane(hit.point, hit1.point, hit2.point).normal), roverRotSpeed * Time.deltaTime);
        }*/
        for (int i = 0; i < 8; i++)
        {
            float speed = revSpeeds[i] * Time.deltaTime;
            planetPivots[i].Rotate(Vector3.down, speed);
            planetAnchors[i].Rotate(Vector3.down, -speed);
            planets[i].Rotate(Vector3.down, rotSpeeds[i] * Time.deltaTime);
        }
        sun.Rotate(Vector3.down, rotSpeeds[8] * Time.deltaTime);
    }

    Vector2 PosToPoint(Rect rect, Vector2 point)
    {
        return new Vector2( rect.xMin + rect.width * point.x, rect.yMin + rect.height * point.y );
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
        heliAnim.SetTrigger("Launch");
        heliSource.PlayOneShot(launchClip, 0.2f);
        heliSource.Play();
        heliCam.enabled = true;
        screen.SetActive(true);
        block.enabled = false;
        state = State.Launch;
    }

    IEnumerator ResetCooldown()
    {
        yield return fifthSecond;
        cooldown = false;
    }

    public void Reset()
    {
        if (state == State.Idle || state == State.Reset) return;
        state = State.Reset;
    }

    IEnumerator Drill()
    {
        inCoroutine = true;
        roverAnim.SetTrigger("Drill");
        yield return new WaitForSeconds(0.75f);
        roverSource.PlayOneShot(drillClip, 0.15f);
        yield return new WaitForSeconds(2.5f);
        roverSource.Stop();
        heliCam.enabled = true;
        screen.SetActive(true);
        roverCam.enabled = false;
        if ((rover.position - target.position).sqrMagnitude < 1)
        {
            target.GetChild(0).gameObject.SetActive(true);
            roverSource.PlayOneShot(foundClip, 0.1f);
            roverSource.PlayOneShot(uiClip, 0.1f);
            yield return halfSecond;
            roverSource.PlayOneShot(successClip, 0.15f);
            for(int i = 0; i < 5; i++)
            {
                animators[i].SetTrigger("Unfill");
                scans[i].gameObject.SetActive(false);
                disable[i] = false;
            }
            animators[5].SetTrigger("Unfill");
            yield return second;
            for(int i = 0; i < 5; i++) mapColors[i].SetActive(false);
            targetRing.gameObject.SetActive(false);
            rockIndex = -1;
            rocksScanned = 0;
        }
        else
        {
            roverSource.PlayOneShot(errorClip, 0.05f);
        }
        inCoroutine = false;
        canClick = true;
    }

    IEnumerator ScanRock()
    {
        inCoroutine = true;
        roverSource.PlayOneShot(scanClip, 0.05f);
        Transform scan = scans[rockIndex];
        Transform rockT = rockTs[rockIndex];
        Vector3 rockPos = rockT.position;
        scan.SetPositionAndRotation(rockPos + 0.65f * Vector3.up, Quaternion.Euler(0, -90, 0));
        rocksScanned ++;
        Vector3 lensPos = lens.position;
        //Vector3 hor = Vector3.Cross(rockPos - lensPos, Vector3.up).normalized * laserSpeed;
        Vector3[] offsets = new Vector3[] {Vector3.up * laserSpeed, Vector3.down * laserSpeed};//, hor, -hor};
        Vector3[] targets = new Vector3[] {rockPos, rockPos};//, rockPos, rockPos};
        for (int i = 0; i < laserCount; i++) 
        {
            LineRenderer laser = lasers[i];
            laser.enabled = true;
            laser.SetPosition(0, lensPos);
            laser.SetPosition(1, rockPos);
        }
        float t = 0;
        while (t < 2.377)
        {
            for (int i = 0; i < laserCount; i++) 
            {
                LineRenderer laser = lasers[i];
                Vector3 newTarget = targets[i] + offsets[i] * Time.deltaTime;
                if (Physics.Raycast(lensPos, (newTarget - lensPos).normalized, out RaycastHit hit, 20, rockLayer) && hit.collider.transform == rockT)
                {
                    laser.SetPosition(1, hit.point);
                    targets[i] = newTarget;
                }
                else
                {
                    offsets[i] *= -1;
                    targets[i] += offsets[i] * Time.deltaTime;
                }
            }
            t += Time.deltaTime;
            yield return null;
        }
        for (int i = 0; i < laserCount; i++) lasers[i].enabled = false;
        scanCanvases[rockIndex].enabled = true;
        scan.gameObject.SetActive(true);
        roverSource.PlayOneShot(uiClip, 0.1f);
        yield return halfSecond;
        roverSource.Stop();
        heliCam.enabled = true;
        screen.SetActive(true);
        roverCam.enabled = false;
        mapColors[rockIndex].SetActive(true);
        animators[rockIndex].SetTrigger("Fill");
        rockObstacles[rockIndex].enabled = true;
        rockIndex = -1;
        roverSource.PlayOneShot(colorClip, 0.06f);
        if (rocksScanned == 5)
        {
            roverAgent.stoppingDistance = 0.1f;
            rockIndex = 5;
            yield return second;
            targetRing.gameObject.SetActive(true);
            animators[5].SetTrigger("Fill");
            roverSource.PlayOneShot(ringClip, 0.1f);
        }
        yield return halfSecond;
        inCoroutine = false;
        canClick = true;
    }

    public void RockClick(int idx)
    {
        if (inCoroutine || !canClick || scans[idx].gameObject.activeSelf) return;
        canClick = false;
        rockIndex = idx;
        //Vector3 rockPos = rockTs[idx].position;
        //Vector3 agentPos = rover.position;
        disable[idx] = true;
        rockObstacles[idx].enabled = false;
        roverAgent.stoppingDistance = 1.4f;
        roverAgent.SetDestination(rockTs[idx].GetChild(0).position);// + (agentPos - rockPos).normalized);
        roverSource.PlayOneShot(startClip, 0.4f);
        roverSource.Play();
        roverSource.PlayOneShot(gravel, 0.15f);
        StartCoroutine(ToggleCamera());
    }

    public void Grab(int i)
    {
        grabbed[i] = !grabbed[i];
        if (grabbed[i]) cols[i].excludeLayers = body;
        else cols[i].excludeLayers = 0;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (inCoroutine || !canClick || !RectTransformUtility.ScreenPointToLocalPointInRectangle(rectT, eventData.position, eventData.pressEventCamera, out Vector2 point)) return;
        if (Physics.Raycast(heliCam.ViewportPointToRay(Rect.PointToNormalized(rect, point)), out RaycastHit hit, 20, platformLayer))
        {
            if (hit.collider.gameObject.layer != 11) return;
            canClick = false;
            roverAgent.stoppingDistance = 0.01f;
            Vector3 hitPoint = hit.point;
            roverAgent.SetDestination(hitPoint);
            float minDistance = 1000;
            int idx = -1;
            for (int i = 0; i < 5; i++)
            {
                Transform rockT = rockTs[i];
                float d = Vector3.SqrMagnitude(rockT.position - hitPoint);
                if (d < 4 && !scans[i].gameObject.activeSelf)
                {
                    if (d < minDistance)
                    {
                        minDistance = d;
                        idx = i;
                    }
                }
            }
            if (idx != -1)
            {
                rockIndex = idx;
                disable[idx] = true;
            }
            roverSource.PlayOneShot(startClip, 0.4f);
            roverSource.Play();
            roverSource.PlayOneShot(gravel, 0.15f);
            StartCoroutine(ToggleCamera());
        }
    }

    IEnumerator ToggleCamera()
    {
        while (roverAgent.pathPending) yield return null;
        yield return null;
        traveling = true;
        while (roverAgent.remainingDistance > 3) yield return null;
        screen.SetActive(false);
        roverCam.enabled = true;
        heliCam.enabled = false;
    }
}
