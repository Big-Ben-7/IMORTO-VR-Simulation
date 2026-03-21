using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ManuManager : MonoBehaviour
{
    [SerializeField] float moveSpeed, rotSpeed, timeInterval, laserSpeed, nozzleSpeed;
    [SerializeField] Transform[] parts;
    [SerializeField] GameObject sheetPrefab;
    Rigidbody[] partRBs;
    int destinationCount;
    Vector3[] destinationPos;
    int[] partDestinations;
    Transform[] partMarkers, destinations;
    MeshRenderer[] partRends;
    List<MeshRenderer> lasercutRends = new List<MeshRenderer>();
    int[] lasercutIndices;
    int cutCount;
    Collider[] partCols;
    GameObject[] sheets;
    ParticleSystem[] particles;
    int particleCount;
    [SerializeField] Transform stamper, painter, lasercutter, startMarker, endMarker, buttonParent, destinationParent;
    [SerializeField] TMP_Text colorText, startText, paintQueueText;
    string[] buttonNames;
    Color[] buttonColors;
    Animation stampAnim;
    float stampZ, endX;
    Vector3 laserMarker;
    Queue<int> availableIndices = new Queue<int>();
    LineRenderer[] lasers = new LineRenderer[4];
    Transform[] nozzles = new Transform[4];
    Vector3[] initialNozzlePos = new Vector3[4];
    bool[] lasersAvailable = new bool[4] {true, true, true, true};
    Vector3 startPos;
    enum State {Idle, Moving, Stamping, ToLasercut, Stamped, Painted};
    State[] partStates;
    int partCount;
    bool paused, start = true, buttonPaused, firstStarted;
    WaitForSeconds particleWait = new WaitForSeconds(0.005f);
    WaitForSeconds wait = new WaitForSeconds(0.1667f);
    Color paintColor = Color.white;
    Rotate[] bars;
    int barCount;
    AudioSource beltSource, painterSource, stamperSource, laserSource;
    [SerializeField] AudioClip paint, stamp, startUp, shutDown;
    List<string> paintQueue = new List<string>();

    void Awake()
    {
        partCount = parts.Length;
        partStates = new State[partCount];
        partMarkers = new Transform[partCount];
        partRends = new MeshRenderer[partCount];
        partCols = new Collider[partCount];
        sheets = new GameObject[partCount];
        partRBs = new Rigidbody[partCount];
        lasercutIndices = new int[partCount];
        int lasercutIndex = 0;
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
                rend = partT.GetChild(1).GetChild(0).GetComponent<MeshRenderer>();
                rend.enabled = false;
                lasercutRends.Add(rend);
            }
            else lasercutIndices[i] = -1;
            Transform marker = partMarkers[i] = partT.GetChild(0);
            partRBs[i] = partT.GetComponent<Rigidbody>();
            sheets[i] = Instantiate(sheetPrefab, marker.position, Quaternion.identity, partT);
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
        startPos = startMarker.position;
        endX = endMarker.position.x;
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
            partT.position = startPos - partMarkers[i].localPosition;
            partT.gameObject.SetActive(true);
            partStates[i] = State.Moving;
            float t = 0;
            while (t < timeInterval)
            {
                yield return null;
                t += Time.deltaTime;
            }
        }
    }

    public void movePart(int i)
    {
        int d = partDestinations[i];
        Transform marker = partMarkers[i];
        Vector3 diff = destinationPos[d] - marker.position;
        if (Mathf.Abs(diff.x) < 0.02f && Mathf.Abs(diff.z) < 0.02f)
        {
            partDestinations[i] ++;
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
                    if (partMarkers[i].position.x < endX)
                    {
                        partStates[i] = State.Idle;
                        parts[i].gameObject.SetActive(false);
                    }
                    break;
            }
        }
    }

    IEnumerator Lasercut(int i)
    {
        cutCount ++;
        int c = cutCount;
        MeshRenderer rend = lasercutRends[lasercutIndices[i]];
        Transform t = rend.transform.parent;
        Animation anim = t.GetComponent<Animation>();
        anim[t.gameObject.name].speed *= laserSpeed;
        anim.Play();
        Transform sphere1, sphere2 = null;
        int index1 = availableIndices.Dequeue(), index2 = -1;
        lasersAvailable[index1] = false;
        LineRenderer laser1 = lasers[index1], laser2 = null;
        Transform nozzle1 = nozzles[index1], nozzle2 = null;
        laser1.enabled = true;
        if (t.childCount == 5)
        {
            sphere1 = t.GetChild(3);
            sphere2 = t.GetChild(4);
            index2 = availableIndices.Dequeue();
            lasersAvailable[index2] = false;
            laser2 = lasers[index2];
            laser2.enabled = true;
            nozzle2 = nozzles[index2];
        }
        else sphere1 = t.GetChild(2);
        if (!laserSource.isPlaying) laserSource.Play();
        while (anim.isPlaying)
        {
            Vector3 spherePos = sphere1.position;
            Vector3 nozzlePos = nozzle1.position = Vector3.Lerp(nozzle1.position, new Vector3(spherePos.x, laserMarker.y, spherePos.z), nozzleSpeed * Time.deltaTime);
            laser1.SetPositions(new Vector3[] {nozzlePos, spherePos});
            if (index2 != -1)
            {
                spherePos = sphere2.position;
                nozzlePos = nozzle2.position = Vector3.Lerp(nozzle2.position, new Vector3(spherePos.x, laserMarker.y, spherePos.z), nozzleSpeed * Time.deltaTime);
                laser2.SetPositions(new Vector3[] {nozzlePos, spherePos});
            }
            yield return null;
        }
        if (cutCount == c) laserSource.Stop();
        cutCount --;
        availableIndices.Enqueue(index1);
        lasersAvailable[index1] = true;
        laser1.enabled = false;
        if (index2 != -1)
        {
            availableIndices.Enqueue(index2);
            lasersAvailable[index2] = true;
            laser2.enabled = false;
        }
        rend.enabled = false;
        partRends[i].enabled = true;
        while (true)
        {
            bool b1 = lasersAvailable[index1], b2 = index2 != -1 && lasersAvailable[index2];
            if (b1) nozzle1.position = Vector3.Lerp(nozzle1.position, initialNozzlePos[index1], nozzleSpeed * Time.deltaTime);
            if (b2) nozzle2.position = Vector3.Lerp(nozzle2.position, initialNozzlePos[index2], nozzleSpeed * Time.deltaTime);
            else if (!b1 && !b2) break;
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
        parts[i].position += new Vector3(0, 0.1f, 0);
        partCols[i].enabled = true;
        (lasercutIndices[i] != -1 ? lasercutRends[lasercutIndices[i]] : partRends[i]).enabled = true;
        yield return wait;
        paintQueue.Add(parts[i].gameObject.name);
        UpdatePaintQueue();
        for (int j = 0; j < 2; j++) yield return wait;
        paused = false;
        if (!buttonPaused) for (int j = 0; j < 6; j++) bars[j].rotate = true;
        partStates[i] = lasercutIndices[i] != -1 ? State.ToLasercut : State.Stamped;
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
            for (int j = 0; j < barCount; j++) bars[j].rotate = true;
            startText.SetText("Halt Production");
            beltSource.Play();
            beltSource.PlayOneShot(startUp);
        }
        else
        {
            buttonPaused = start = true;
            for (int j = 0; j < barCount; j++) bars[j].rotate = false;
            startText.SetText("Initiate Production");
            beltSource.Stop();
            beltSource.PlayOneShot(shutDown);
        }
        Player.Click();
    }
}
