using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ManuManager : MonoBehaviour
{
    [SerializeField] float moveSpeed, rotSpeed, timeInterval;
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
    Collider[] partCols;
    GameObject[] sheets;
    ParticleSystem[] particles;
    int particleCount;
    [SerializeField] Transform stamper, painter, lasercutter, startMarker, endMarker, buttonParent, destinationParent;
    [SerializeField] TMP_Text colorText, startText, paintQueueText;
    string[] buttonNames;
    Color[] buttonColors;
    Animation stampAnim;
    float stampZ, laserZ, endX;
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
    AudioSource painterSource, stamperSource, beltSource;
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
        laserZ = lasercutter.GetChild(0).position.z;
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
                    if (partMarkers[i].position.z >= laserZ)
                    {
                        partStates[i] = State.Stamped;
                        lasercutRends[lasercutIndices[i]].enabled = false;
                        partRends[i].enabled = true;
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
        (lasercutIndices[i] >= 0 ? lasercutRends[lasercutIndices[i]] : partRends[i]).enabled = true;
        yield return wait;
        paintQueue.Add(parts[i].gameObject.name);
        UpdatePaintQueue();
        for (int j = 0; j < 2; j++) yield return wait;
        paused = false;
        if (!buttonPaused) for (int j = 0; j < 6; j++) bars[j].rotate = true;
        partStates[i] = lasercutIndices[i] >= 0 ? State.ToLasercut : State.Stamped;
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
