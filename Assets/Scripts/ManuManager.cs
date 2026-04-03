using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ManuManager : MonoBehaviour
{
    [SerializeField] float moveSpeed, rotSpeed, timeInterval, laserSpeed, nozzleSpeed, nozzleSpeedSlow;
    [SerializeField] Transform[] parts;
    [SerializeField] GameObject sheetPrefab;
    Rigidbody[] partRBs;
    int destinationCount;
    Vector3[] destinationPos;
    int[] partDestinations;
    Transform[] partMarkers, destinations, assemblyParts;
    MeshRenderer[] partRends;
    List<MeshRenderer> lasercutRends = new List<MeshRenderer>();
    List<Collider> lasercutCols = new List<Collider>();
    int[] lasercutIndices;
    int cutCount;
    Collider[] partCols;
    GameObject[] sheets;
    ParticleSystem[] particles;
    int particleCount;
    [SerializeField] Transform stamper, painter, lasercutter, startMarker/*, endMarker, */, buttonParent, destinationParent, assemblyParent, suction;
    [SerializeField] TMP_Text colorText, startText, paintQueueText;
    string[] buttonNames;
    Color[] buttonColors;
    Transform carParent, partsParent;
    Animator assemblyAnim;
    Animation stampAnim;
    float stampZ, endX;
    Vector3 laserMarker;
    Queue<int> availableIndices = new Queue<int>();
    LineRenderer[] lasers = new LineRenderer[4];
    Transform[] nozzles = new Transform[4];
    Vector3[] initialNozzlePos = new Vector3[4];
    bool[] lasersAvailable = new bool[4] {true, true, true, true};
    Vector3 startPos;
    Quaternion startRot;
    enum State {Idle, Moving, Stamping, ToLasercut, Stamped, Painted};
    State[] partStates;
    int partCount;
    bool paused, start = true, buttonPaused, firstStarted;
    WaitForSeconds particleWait = new WaitForSeconds(0.005f);
    WaitForSeconds wait = new WaitForSeconds(0.1667f);
    WaitForSeconds partWait;
    Color paintColor = Color.white;
    Rotate[] bars;
    int barCount;
    AudioSource beltSource, painterSource, stamperSource, laserSource;
    [SerializeField] AudioClip paint, stamp, startUp, shutDown;
    List<string> paintQueue = new List<string>();
    public static ManuManager instance;

    void Awake()
    {
        instance = this;
        partWait = new WaitForSeconds(timeInterval);
        partCount = parts.Length;
        partStates = new State[partCount];
        partMarkers = new Transform[partCount];
        partRends = new MeshRenderer[partCount];
        partCols = new Collider[partCount];
        sheets = new GameObject[partCount];
        partRBs = new Rigidbody[partCount];
        lasercutIndices = new int[partCount];
        int lasercutIndex = 0;
        startMarker.GetPositionAndRotation(out startPos, out startRot);
        assemblyParts = new Transform[partCount];
        partsParent = assemblyParent.GetChild(0);
        carParent = partsParent.GetChild(1);
        assemblyAnim = assemblyParent.GetComponent<Animator>();
        for (int i = 0; i < partCount; i++) 
        {
            Transform partT = parts[i];
            partT.gameObject.SetActive(false);
            MeshRenderer rend = partRends[i] = partT.GetComponent<MeshRenderer>();
            rend.enabled = false;
            Collider col = partCols[i] = partT.GetComponent<Collider>();
            col.enabled = false;
            if (partT.childCount > 1)
            {
                lasercutIndices[i] = lasercutIndex ++;
                Transform t = partT.GetChild(1).GetChild(0);
                rend = t.GetComponent<MeshRenderer>();
                rend.enabled = false;
                lasercutRends.Add(rend);
                col = t.GetComponent<Collider>();
                col.enabled = false;
                lasercutCols.Add(col);
            }
            else lasercutIndices[i] = -1;
            Transform marker = partMarkers[i] = partT.GetChild(0);
            partRBs[i] = partT.GetComponent<Rigidbody>();
            sheets[i] = Instantiate(sheetPrefab, marker.position, startRot, partT);
            assemblyParts[i] = partsParent.GetChild(i+2);
        }
        destinationCount = destinationParent.childCount;
        destinations = new Transform[destinationCount];
        destinationPos = new Vector3[destinationCount];
        for (int i = 0; i < destinationCount; i++)
        {
            Transform d = destinations[i] = destinationParent.GetChild(destinationCount - i - 1);
            destinationPos[i] = d.position;
        }
        partDestinations = new int[partCount];
        beltSource = transform.GetChild(0).GetComponent<AudioSource>();
        stampAnim = stamper.GetComponent<Animation>();
        //endX = endMarker.position.x;
        stampZ = stamper.position.z;
        stamperSource = stamper.GetChild(0).GetComponent<AudioSource>();
        laserMarker = lasercutter.GetChild(0).position;
        for (int i = 0; i < 4; i++)
        {
            availableIndices.Enqueue(i);
            lasers[i] = lasercutter.GetChild(i + 1).GetComponent<LineRenderer>();
            Transform nozzle = nozzles[i] = lasercutter.GetChild(i + 6);
            initialNozzlePos[i] = nozzle.position;
        }
        laserSource = lasercutter.GetChild(5).GetComponent<AudioSource>();
        painterSource = painter.GetComponent<AudioSource>();
        particles = painter.GetComponentsInChildren<ParticleSystem>();
        particleCount = particles.Length;
        int childCount = buttonParent.childCount;
        buttonColors = new Color[childCount];
        buttonNames = new string[childCount];
        int row1 = (int) (childCount / 2.0f + 0.5f);
        float pos = -0.43f, inc = 0.86f / (row1 - 1);
        for (int i = 0; i < row1; i++)
        {
            Transform t = buttonParent.GetChild(i);
            t.GetComponent<RectTransform>().anchoredPosition = new Vector2(pos, -0.155f);
            buttonNames[i] = t.gameObject.name;
            buttonColors[i] = t.GetComponent<Image>().color;
            pos += inc;
        }
        pos = -0.43f;
        for (int i = row1; i < childCount; i++)
        {
            Transform t = buttonParent.GetChild(i);
            t.GetComponent<RectTransform>().anchoredPosition = new Vector2(pos, -0.275f);
            buttonNames[i] = t.gameObject.name;
            buttonColors[i] = t.GetComponent<Image>().color;
            pos += inc;
        }
        for (int j = 0; j < particleCount; j++)
        {
            ParticleSystem.MainModule main = particles[j].main;
            main.startColor = Color.white;
        }
        colorText.SetText("Selected Paint Color: None");
        bars = transform.parent.GetChild(0).GetComponentsInChildren<Rotate>();
        barCount = bars.Length;
    }

    IEnumerator SendParts()
    {
        for (int i = 0; i < partCount; i++)
        {
            while (paused || buttonPaused) yield return null;
            Transform partT = parts[i];
            partT.position += startPos - partMarkers[i].position;
            partT.gameObject.SetActive(true);
            partStates[i] = State.Moving;
            yield return partWait;
        }
    }

    public void movePart(int i)
    {
        int d = partDestinations[i];
        Transform marker = partMarkers[i];
        Vector3 diff = destinationPos[d] - marker.position;
        if (Mathf.Abs(diff.x) < 0.02f && Mathf.Abs(diff.z) < 0.02f)
        {
            if (d == destinationCount - 1)
            {
                partStates[i] = State.Idle;
                Color color = partRends[i].materials[0].color;
                parts[i].gameObject.SetActive(false);
                Transform t = parts[i] = assemblyParts[i];
                MeshRenderer rend = t.GetComponent<MeshRenderer>();
                Material[] mats = rend.materials;
                int n = mats.Length;
                for (int j = 0; j < n; j++) mats[j].color = color;
                t.gameObject.SetActive(true);
                assemblyAnim.SetInteger("Part", i);
            }
            else partDestinations[i] ++;
            return;
        }
        Vector3 diffN = new Vector3(diff.x, 0, diff.z).normalized;
        Rigidbody rb = partRBs[i];
        rb.MovePosition(rb.position + diffN * moveSpeed * Time.fixedDeltaTime);
        Quaternion rot = rb.rotation;
        Vector3 markerF = marker.forward;
        markerF = new Vector3(markerF.x, 0, markerF.z).normalized;
        rb.MoveRotation(Quaternion.RotateTowards(rot, Quaternion.FromToRotation(markerF, diffN) * rot, rotSpeed * Time.deltaTime));
        //.Log(destinations[d] + ", " + destinations[d].InverseTransformPoint(partMarkers[i].position).y);
    }

    void FixedUpdate()
    {
        if (buttonPaused) return;
        for (int i = 0; i < partCount; i++)
        {
            switch(partStates[i])
            {
                case State.Idle: break;
                case State.Moving:
                    if (paused) break;
                    movePart(i);
                    if (partMarkers[i].position.z >= stampZ) partStates[i] = State.Stamping;
                    break;
                case State.Stamping:
                    if (paused) break;
                    paused = true;
                    for (int j = 0; j < 6; j++) bars[j].rotate = false;
                    StartCoroutine(Stamp(i));
                    break;
                case State.Stamped:
                    movePart(i);
                    if (partDestinations[i] >= 7)
                    {
                        partStates[i] = State.Painted;
                        StartCoroutine(SetColor(partRends[i], paintColor));
                        for (int j = 0; j < particleCount; j++) particles[j].Play();
                    }
                    break;
                case State.ToLasercut:
                    movePart(i);
                    if (partMarkers[i].position.z >= laserMarker.z)
                    {
                        partStates[i] = State.Stamped;
                        StartCoroutine(Lasercut(i));
                    }
                    break;
                case State.Painted:
                    movePart(i);
                    break;
            }
        }
    }

    IEnumerator Lasercut(int i)
    {
        cutCount ++;
        int c = cutCount;
        int laserIndex = lasercutIndices[i];
        MeshRenderer rend = lasercutRends[laserIndex];
        Transform t = rend.transform.parent;
        Animation anim = t.GetComponent<Animation>();
        anim[t.gameObject.name].speed *= laserSpeed;
        anim.Play();
        TrailRendererLocal trl = t.GetComponent<TrailRendererLocal>();
        int n = trl.targetCount;
        int[] indices = new int[n];
        Transform[] spheres = trl.targets;
        for (int j = 0; j < n; j++)
        {
            int index = indices[j] = availableIndices.Dequeue();
            lasersAvailable[index] = false;
            LineRenderer laser = lasers[index];
            laser.enabled = true;
        }
        if (!laserSource.isPlaying) laserSource.Play();
        trl.ToggleTrail(true);
        while (anim.isPlaying)
        {
            for (int j = 0; j < n; j++)
            {
                Vector3 spherePos = spheres[j].position;
                int index = indices[j];
                Transform nozzle = nozzles[index];
                Vector3 nozzlePos = nozzle.position = Vector3.Lerp(nozzle.position, new Vector3(spherePos.x, laserMarker.y, spherePos.z), nozzleSpeed * Time.deltaTime);
                lasers[index].SetPositions(new Vector3[] {nozzlePos, spherePos});
            }
            //trl.MyUpdate();
            yield return null;
        }
        if (cutCount == c) laserSource.Stop();
        cutCount --;
        trl.ToggleTrail(false);
        for (int j = 0; j < n; j++)
        {
            int index = indices[j];
            availableIndices.Enqueue(index);
            lasersAvailable[index] = true;
            lasers[index].enabled = false;
        }
        /*partCols[i].enabled = */
        /*lasercutCols[laserIndex].enabled =*/
        rend.enabled = false;
        partRends[i].enabled = true;
        while (true)
        {
            bool canBreak = true;
            for (int j = 0; j < n; j++)
            {
                int index = indices[j];
                if (lasersAvailable[index])
                {
                    Transform nozzle = nozzles[index];
                    nozzle.position = Vector3.Lerp(nozzle.position, initialNozzlePos[index], nozzleSpeedSlow * Time.deltaTime);
                    canBreak = false;
                }
            }
            if (canBreak) break;
            yield return null;
        }
    }

    IEnumerator SetColor(MeshRenderer rend, Color color)
    {
        yield return particleWait;
        if (color != Color.white)
        {
            Material[] mats = rend.materials;
            int n = mats.Length;
            for (int i = 0; i < n; i++) mats[i].color = color;
        }
        painterSource.PlayOneShot(paint);
        paintQueue.RemoveAt(0);
        UpdatePaintQueue();
    }
    IEnumerator Stamp(int i)
    {
        if (!stampAnim.IsPlaying("Stamp")) stampAnim.Play();
        yield return wait;
        yield return wait;
        stamperSource.PlayOneShot(stamp);
        yield return wait;
        Destroy(sheets[i]);
        parts[i].position += new Vector3(0, 0.02f, 0);
        int laserIndex = lasercutIndices[i];
        if (laserIndex != -1)
        {
            lasercutCols[laserIndex].enabled = true;
            lasercutRends[laserIndex].enabled = true;
        }
        else
        {
            partCols[i].enabled = true;
            partRends[i].enabled = true;
        }
        yield return wait;
        paintQueue.Add(parts[i].gameObject.name);
        UpdatePaintQueue();
        for (int j = 0; j < 2; j++) yield return wait;
        paused = false;
        if (!buttonPaused) for (int j = 0; j < 6; j++) bars[j].rotate = true;
        partStates[i] = laserIndex != -1 ? State.ToLasercut : State.Stamped;
    }

    void UpdatePaintQueue()
    {
        string str = "Clear";
        switch(paintQueue.Count)
        {
            case 0: break;
            case 1:
                str = paintQueue[0];
                break;
            case 2:
                str = paintQueue[0] + ", " + paintQueue[1];
                break;
            default:
                str = paintQueue[0] + ", " + paintQueue[1] + " . . .";
                break;
        }
        paintQueueText.SetText("PAINT NEXT QUEUE: " + str);
    }

    public void SetPaint(int i)
    {
        if (paintColor == buttonColors[i])
        {
            paintColor = Color.white;
            for (int j = 0; j < particleCount; j++)
            {
                ParticleSystem.MainModule main = particles[j].main;
                main.startColor = Color.white;
            }
            colorText.SetText("Selected Paint Color: None");
        }
        else
        {
            paintColor = buttonColors[i];
            colorText.SetText("Selected Paint Color: " + buttonNames[i]);
            for (int j = 0; j < particleCount; j++)
            {
                ParticleSystem.MainModule main = particles[j].main;
                main.startColor = paintColor;
            }
        }
        Player.Click();
    }

    public void StartConveyer()
    {
        if (start)
        {
            if (!firstStarted)
            {
                StartCoroutine(SendParts());
                firstStarted = true;
            }
            start = buttonPaused = false;
            for (int j = 0; j < barCount; j++) bars[j].SetRotate(true, 360);
            startText.SetText("Halt Production");
            beltSource.Play();
            beltSource.PlayOneShot(startUp);
        }
        else
        {
            buttonPaused = start = true;
            for (int j = 0; j < barCount; j++) bars[j].SetRotate(false, 360);
            startText.SetText("Initiate Production");
            beltSource.Stop();
            beltSource.PlayOneShot(shutDown);
        }
        Player.Click();
    }

    public void Suction(int i)
    {
        parts[i].SetParent(suction);
    }

    public void UnSuction(int i)
    {
        Transform partT = parts[i];
        partT.SetParent(carParent);
        carParent.GetChild(i).GetPositionAndRotation(out Vector3 p, out Quaternion r);
        partT.SetPositionAndRotation(p, r);
    }
}
