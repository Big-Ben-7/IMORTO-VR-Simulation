using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.ProBuilder.MeshOperations;

public class BacteriaSpray : XRGrabInteractable
{
    [SerializeField] GameObject bacteriaPrefab, bacteriaQuad;
    [SerializeField] LayerMask body, sprayMask;//, sprayableMask;
    [SerializeField] Transform bot, leftWall, rightWall, backWall, frontWall, dome;
    float leftBound, rightBound, backBound, frontBound, radius;
    [SerializeField] float botRotSpeed;
    [SerializeField] Volume volume;
    [SerializeField] AudioClip hit, spray, buzz;
    AudioSource audioSource, botSource;
    GameObject volumeObject;
    UniversalAdditionalCameraData cameraData;
    NavMeshAgent botAgent;
    Material mat;
    Transform squirt;
    Collider col;
    bool grabbed;
    string lastCollision = "";
    Coroutine collisionCoroutine;
    WaitForSeconds fifthSecond = new WaitForSeconds(0.2f);
    WaitForSeconds second = new WaitForSeconds(1);
    WaitForSeconds twoSeconds = new WaitForSeconds(2);
    WaitForSeconds halfSecond = new WaitForSeconds(0.5f);
    Vector3 initialPos;
    Quaternion initialRot;
    Vector3 currentHitPoint;
    List<Vector3> hitPoints = new List<Vector3>();
    List<Vector3> destinations = new List<Vector3>();
    List<GameObject> bacterias = new List<GameObject>();
    List<float> stoppingDists = new List<float>();
    enum State { Idle, Moving, Returning, Acting};
    State botState = State.Idle;
    Transform healthcare;

    protected override void Awake()
    {
        base.Awake();
        audioSource = GetComponent<AudioSource>();
        col = GetComponent<Collider>();
        volumeObject = volume.gameObject;
        volumeObject.SetActive(false);
        botAgent = bot.GetComponent<NavMeshAgent>();
        botSource = bot.GetComponent<AudioSource>();
        mat = bot.GetChild(0).GetChild(2).GetComponent<MeshRenderer>().material;
        mat.DisableKeyword("_EMISSION");
        Camera.main.TryGetComponent<UniversalAdditionalCameraData>(out cameraData);
        cameraData.renderPostProcessing = false;
        squirt = transform.GetChild(6);
        healthcare = transform.parent;
    }

    void Start()
    {
        initialPos = bot.position;
        initialPos = new Vector3(initialPos.x, 0, initialPos.z);
        initialRot = bot.rotation;
        leftBound = leftWall.position.x;
        rightBound = rightWall.position.x;
        backBound = backWall.position.z;
        frontBound = frontWall.position.z;
        radius = dome.localScale.x / 25 + 0.1f;
        radius *= radius;
    }

    void Update()
    {
        switch (botState)
        {
            case State.Moving:
                if (botAgent.remainingDistance <= botAgent.stoppingDistance && (!botAgent.hasPath || botAgent.velocity.sqrMagnitude < 1e-5f))
                {
                    botState = State.Acting;
                    StartCoroutine(UVGlow());
                }
                break;
            case State.Returning:
                if (destinations.Count > 0)
                {
                    botState = State.Moving;
                    botAgent.stoppingDistance = stoppingDists[0];
                    stoppingDists.RemoveAt(0);
                    botAgent.SetDestination(destinations[0]);
                    destinations.RemoveAt(0);
                    currentHitPoint = hitPoints[0];
                    hitPoints.RemoveAt(0);
                    if (!botSource.isPlaying) botSource.Play();
                }
                else if (botAgent.remainingDistance <= botAgent.stoppingDistance && (!botAgent.hasPath || botAgent.velocity.sqrMagnitude < 1e-5f))
                {
                    botState = State.Idle;
                    StartCoroutine(ResetRot());
                }
                break;
            case State.Idle:
                if (destinations.Count > 0)
                {
                    botState = State.Moving;
                    botAgent.stoppingDistance = stoppingDists[0];
                    stoppingDists.RemoveAt(0);
                    botAgent.SetDestination(destinations[0]);
                    destinations.RemoveAt(0);
                    currentHitPoint = hitPoints[0];
                    hitPoints.RemoveAt(0);
                    if (!botSource.isPlaying) botSource.Play();
                }
                break;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!Player.initialized) return;
        GameObject go = collision.gameObject;
        if (go.layer == 7) return;
        string name = go.name;
        if (!grabbed && lastCollision != name)
        {
            audioSource.PlayOneShot(hit, Mathf.Clamp01(Mathf.Log10(0.6f * collision.relativeVelocity.magnitude + 1)));
            lastCollision = name;
            if (collisionCoroutine != null) StopCoroutine(collisionCoroutine);
            collisionCoroutine = StartCoroutine(ResetLastCollision());
        }
    }

    IEnumerator ResetLastCollision()
    {
        string t = lastCollision;
        yield return fifthSecond;
        if (lastCollision == t) lastCollision = "";
    }

    protected override void OnActivated(ActivateEventArgs args)
    {
        base.OnActivated(args);
        RaycastHit hit;
        Vector3 pos = squirt.position;
        if (Physics.Raycast(squirt.position, squirt.forward, out hit, 3, sprayMask))
        {
            GameObject go = hit.collider.gameObject;
            //if ((sprayableMask.value & (1 << go.layer)) == 0) return;
            if (!go.CompareTag("Sprayable") || pos.x < leftBound || pos.x > rightBound || pos.z < backBound || pos.z > frontBound || Vector2.SqrMagnitude(new Vector2(pos.x, pos.z)) < radius) return;
            Vector3 up = squirt.TransformDirection(new Vector3(Random.Range(-1, 1), Random.Range(-1, 1), 0).normalized);
            float randomSize = Random.Range(0.1f, 0.5f);
            Vector3 hitPoint = hit.point, normal = hit.normal;
            //GameObject bacteria = Instantiate(bacteriaQuad, hitPoint + 0.01f * normal, Quaternion.LookRotation(-normal, up), healthcare);
            //bacteria.transform.localScale = randomSize * Vector3.one;
            GameObject bacteria;
            if (go.name == "Pane")
            {
                bacteria = Instantiate(bacteriaQuad, hitPoint + 0.01f * normal, Quaternion.LookRotation(-normal, up), healthcare);
                bacteria.transform.localScale = randomSize * Vector3.one;
            }
            else
            {
                bacteria = Instantiate(bacteriaPrefab, hitPoint + (squirt.position - hitPoint).normalized * 0.3f, Quaternion.LookRotation(squirt.forward, up), healthcare);
                bacteria.GetComponent<DecalProjector>().size = new Vector3(randomSize, randomSize, 3);
            }
            bacteria.GetComponent<Animator>().SetTrigger("FadeIn");
            hitPoints.Add(hitPoint);
            bacterias.Add(bacteria);
            stoppingDists.Add(go.layer == 8? 1 : 0.01f);
            destinations.Add(new Vector3(hitPoint.x, 0, hitPoint.z));
            audioSource.PlayOneShot(spray);
        }
        //else audioSource.PlayOneShot(spray, 0.1f);
    }

    IEnumerator RotateBotToward(Quaternion targetRot, bool interruptable = false)
    {
        Quaternion rot = bot.rotation;
        while (Quaternion.Angle(rot, targetRot) > 1)
        {
            if (interruptable && botState == State.Moving) break;
            bot.rotation = rot = Quaternion.Slerp(rot, targetRot, Time.deltaTime * botRotSpeed);
            yield return null;
        }
    }

    IEnumerator ResetRot()
    {
        yield return StartCoroutine(RotateBotToward(initialRot, true));
        if (botSource.isPlaying) botSource.Stop();
    }
    IEnumerator UVGlow()
    {
        Vector3 dir = currentHitPoint - bot.position;
        StartCoroutine(RotateBotToward(Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z))));
        yield return second;
        //if (botSource.isPlaying) botSource.Stop();
        botSource.PlayOneShot(buzz, 0.2f);
        cameraData.renderPostProcessing = true;
        volumeObject.SetActive(true);
        mat.EnableKeyword("_EMISSION");

        float weight = 0;
        while (weight < 1)
        {
            volume.weight = weight;
            weight += Time.deltaTime * 0.75f;
            yield return null;
        }
        weight = volume.weight = 1;
        GameObject bacteria = bacterias[0];
        bacterias.RemoveAt(0);
        bacteria.GetComponent<Animator>().SetTrigger("FadeOut");
        Destroy(bacteria, 2.5f);
        Vector3 botPos = bot.position;
        int n = bacterias.Count - 1;
        for(int i = n; i >= 0; i--)
        {
            if (Vector3.Distance(hitPoints[i], botPos) > 3) continue;
            bacteria = bacterias[i];
            bacteria.GetComponent<Animator>().SetTrigger("FadeOut");
            Destroy(bacteria, 2.5f);
            bacterias.RemoveAt(i);
            stoppingDists.RemoveAt(i);
            destinations.RemoveAt(i);
            hitPoints.RemoveAt(i);
        }
        yield return twoSeconds;
        while (weight > 0)
        {
            volume.weight = weight;
            weight -= Time.deltaTime * 0.75f;
            yield return null;
        }
        volume.weight = 0;

        mat.DisableKeyword("_EMISSION");
        volumeObject.SetActive(false);
        cameraData.renderPostProcessing = false;
        yield return halfSecond;
        botAgent.SetDestination(initialPos);
        botState = State.Returning;
        //if (!botSource.isPlaying) botSource.Play();
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        if (args.interactorObject is XRSocketInteractor)
        {
            if (Player.initialized) audioSource.PlayOneShot(hit, 0.4f);
            return;
        }
        grabbed = true;
        col.excludeLayers = body;
        Player.Grab();
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        if (args.interactorObject is XRSocketInteractor) return;
        grabbed = false;
        StartCoroutine(Release());
    }

    IEnumerator Release()
    {
        yield return second;
        if (!grabbed) col.excludeLayers = 0;
    }
}
