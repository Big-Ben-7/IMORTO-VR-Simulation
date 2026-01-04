using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

public class RoverController : MonoBehaviour, IPointerClickHandler
{

    [SerializeField] Transform[] rockTs;
    NavMeshObstacle[] rockObstacles = new NavMeshObstacle[5];
    [SerializeField] RectTransform[] buttons;
    bool[] disable = new bool[] {false, false, false, false, false};
    [SerializeField] Transform[] scans;
    Canvas[] scanCanvases = new Canvas[5];
    [SerializeField] Transform target;
    [SerializeField] RectTransform targetRing;
    [SerializeField] GameObject[] mapColors;
    Animator[] animators = new Animator[6];
    [SerializeField] LayerMask terrainLayer, platformLayer, rockLayer;
    [SerializeField] Transform rover;
    Animator roverAnim;
    [SerializeField] float roverRotSpeed;
    [SerializeField] Camera roverCam, heliCam;
    [SerializeField] Transform lens;
    [SerializeField] float laserSpeed;
    LineRenderer[] lasers;
    int laserCount;
    Transform heli;
    float initialHeight, initialSize;
    [SerializeField] GameObject screen;
    [SerializeField] AudioClip scanClip, drillClip, foundClip, successClip, errorClip, uiClip, colorClip, ringClip, startClip, gravel;
    NavMeshAgent roverAgent;
    Transform[] wheels = new Transform[3];
    Transform[] indWheels = new Transform[6];
    AudioSource roverSource;
    int rockIndex = -1;
    int rocksScanned;
    bool traveling, inCoroutine, canClick = true;
    RectTransform rectT;
    Rect rect;
    WaitForSeconds second = new WaitForSeconds(1);
    WaitForSeconds halfSecond = new WaitForSeconds(0.5f);

    void Awake()
    {
        rectT = GetComponent<RectTransform>();
        rect = rectT.rect;
        roverAgent = rover.GetComponent<NavMeshAgent>();
        roverAnim = rover.GetComponent<Animator>();
        //roverAgent.updateRotation = false;
        roverSource = rover.GetComponent<AudioSource>();
        lasers = lens.GetComponentsInChildren<LineRenderer>();
        laserCount = lasers.Length;
        for (int i = 0; i < laserCount; i++) lasers[i].enabled = false;
        Transform p = rover.GetChild(0);
        for (int i = 0; i < 3; i++) wheels[i] = p.GetChild(i+4);
        for (int i = 0; i < 6; i++) indWheels[i] = p.GetChild(i+7);
        for (int i = 0; i < 5; i++)
        {
            rockObstacles[i] = rockTs[i].GetComponent<NavMeshObstacle>();
            animators[i] = mapColors[i].GetComponent<Animator>();
            scanCanvases[i] = scans[i].GetChild(1).GetComponent<Canvas>();
        }
        animators[5] = targetRing.GetComponent<Animator>();
        heli = heliCam.transform;
        initialHeight = heli.position.y;
        initialSize = heliCam.orthographicSize;
    }

    void Update()
    {
        if (heliCam.enabled) heliCam.orthographicSize = initialSize * heli.position.y / initialHeight;
        for (int i = 0; i < 5; i++)
        {
            Vector3 vp = heliCam.WorldToViewportPoint(rockTs[i].position);
            float x = vp.x, y = vp.y;
            if (vp.z <= 0 || x > 1 || x < 0 || y > 1 || y < 0 || disable[i]) buttons[i].gameObject.SetActive(false);
            else if (!disable[i])
            {
                if (!buttons[i].gameObject.activeSelf) buttons[i].gameObject.SetActive(true);
                buttons[i].anchoredPosition = Rect.NormalizedToPoint(rect, new Vector2(x, y) + (y < 0.5f ? Vector2.up : Vector2.down) * 0.12f);
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
        /*//Quaternion newRot;
        //if (roverAgent.hasPath && roverAgent.velocity.sqrMagnitude > 0.01f) newRot = Quaternion.LookRotation(roverAgent.velocity.normalized, rover.up);
        //else newRot = Quaternion.LookRotation(roverAgent.destination, rover.up);
        Vector3 normal = Vector3.zero;
        for (int i = 0; i < 6; i++)
        {
            if (Physics.Raycast(indWheels[i].position, Vector3.down, out RaycastHit hit, 5, terrainLayer)) normal += hit.normal;
        }
        normal = -normal.normalized;
        Vector3 forward = roverAgent.hasPath && roverAgent.velocity.sqrMagnitude > 0.01f? roverAgent.velocity : rover.forward;
        forward = Vector3.ProjectOnPlane(forward, normal).normalized;
        rover.rotation = Quaternion.Slerp(rover.rotation, Quaternion.LookRotation(forward, normal), roverRotSpeed * Time.deltaTime);*/
        /*
        Vector3[] hitPoints = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            if (Physics.Raycast(indWheels[i].position, Vector3.down, out RaycastHit hit, 5, terrainLayer)) hitPoints[i] = hit.point;
        }
        rover.rotation = Quaternion.Slerp(rover.rotation, rover.rotation * Quaternion.FromToRotation(rover.up, new Plane(hitPoints[0], hitPoints[3], hitPoints[5]).normal), roverRotSpeed * Time.deltaTime);*/
        /*if (Physics.Raycast(rover.position + Vector3.up * 0.05f, -rover.up, out RaycastHit hit, 5, terrainLayer))
        {
            rover.rotation = Quaternion.Slerp(rover.rotation, rover.rotation * Quaternion.FromToRotation(rover.up, hit.normal), 10 * Time.deltaTime);
        }*/
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
            roverSource.PlayOneShot(foundClip, 0.15f);
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
            targetRing.anchoredPosition = Rect.NormalizedToPoint(rect, heliCam.WorldToViewportPoint(target.position));
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
