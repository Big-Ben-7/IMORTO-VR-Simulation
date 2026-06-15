using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Tutorials.Core.Editor;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using Microsoft.MixedReality.Toolkit.Experimental.UI;

public class ManuManager : MonoBehaviour
{
    [SerializeField] float moveSpeed, slowSpeed, rotSpeed, timeInterval, laserSpeed, nozzleSpeed, nozzleSpeedSlow;
    [SerializeField] Transform[] parts;
    [SerializeField] GameObject[] preTires, postTires;
    [SerializeField] float[] moveSpeeds;
    [SerializeField] bool[] slowPart;
    [SerializeField] GameObject sheetPrefab;
    Rigidbody[] partRBs;
    int destinationCount;
    Vector3[] destinationPos;
    int[] partDestinations;
    Transform[] partMarkers, destinations, assemblyParts;
    MeshRenderer door1, door2;
    MeshRenderer[] partRends;
    List<MeshRenderer> lasercutRends = new List<MeshRenderer>();
    List<Collider> lasercutCols = new List<Collider>();
    int[] lasercutIndices;
    int cutCount;
    Collider[] partCols;
    GameObject[] sheets;
    ParticleSystem[] particles;
    int particleCount;
    [SerializeField] Transform stamper, painter, lasercutter, startMarker/*, endMarker, */, buttonParent, destinationParent, assemblyParent, suction1, suction2;
    [SerializeField] TMP_Text colorText, startText, paintQueueText;
    [SerializeField] Color[] masterButtonColors;
    [SerializeField] Color disabledColor;
    Transform tireParent, wholeCar, suctionParts1, suctionParts2;
    Material[][] wholeCarMats;
    List<int>[] partMats = new List<int>[] 
    {
        new List<int>() {4, 1}, 
        new List<int>() {4, 0}, 
        new List<int>() {4, 2}, 
        new List<int>() {4, 14},
        new List<int>() {5, 1, 10, 0, 12, 0},
        new List<int>() {7, 1, 10, 1, 12, 2},
        new List<int>() {5, 0},
        new List<int>() {7, 0},
        new List<int>() {4, 4},
        new List<int>() {4, 15},
        new List<int>() {0, 0, 9, 0},
        new List<int>() {2, 0, 9, 1},
        new List<int>() {0, 1},
        new List<int>() {2, 1},
        new List<int>() {4, 3},
        new List<int>() {4, 7},
        new List<int>() {4, 6},
    };
    string[] buttonNames;
    Color[] buttonColors;
    Transform carParent, partsParent;
    Animator assemblyAnim1, assemblyAnim2;
    Animation stampAnim, tireAnim, carAnim;
    AnimationState /*tireAssembly,*/ stampState;
    float stampZ, laserEnd;
    Vector3 laserMarker;
    Queue<int> availableIndices = new Queue<int>();
    LineRenderer[] lasers = new LineRenderer[4];
    Transform[] nozzles = new Transform[4];
    Vector3[] initialNozzlePos = new Vector3[4];
    bool[] lasersAvailable = new bool[4] {true, true, true, true};
    Vector3 startPos;
    Quaternion startRot;
    enum State {Idle, Moving, Stamping, ToLasercut, Stamped, ToPaint, Painted};
    State[] partStates;
    int partCount;
    bool paused, start = true, buttonPaused, firstStarted;
    WaitForSeconds particleWait = new WaitForSeconds(0.005f);
    WaitForSeconds wait = new WaitForSeconds(0.1667f);
    Color paintColor = Color.white;
    [SerializeField] Color blackColor;
    Rotate[] bars;
    int barCount;
    AudioSource beltSource, painterSource, stamperSource, laserSource, carSource;
    public AudioSource tireSource;
    [SerializeField] AudioClip paint, stamp, startUp, shutDown, tireSwap;//, carStart, carMove;
    List<string> paintQueue = new List<string>();
    public static ManuManager instance;
    public int tiresReady;
    public bool lastAssembled;
    bool assemblyComplete;
    bool tireAssembled;
    public bool stampTrigger;
    public bool carSwapped;
    [SerializeField] TMP_Text licenseInput, licensePlate, licenseKeyboard;

    void Awake()
    {
        instance = this;
        partCount = parts.Length;
        moveSpeeds = new float[partCount];
        slowPart = new bool[partCount];
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
        Transform finalCar = partsParent.GetChild(0);
        carAnim = finalCar.GetComponent<Animation>();
        wholeCar = finalCar.GetChild(0);
        carParent = partsParent.GetChild(1);
        Transform postConveyerParent = partsParent.GetChild(2);
        tireParent = partsParent.GetChild(3);
        assemblyAnim1 = assemblyParent.GetChild(1).GetComponent<Animator>();
        assemblyAnim2 = assemblyParent.GetChild(2).GetComponent<Animator>();
        for (int i = 0; i < partCount; i++) 
        {
            Transform partT = parts[i];
            moveSpeeds[i] = moveSpeed;
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
            assemblyParts[i] = postConveyerParent.GetChild(i);
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
        laserEnd = lasercutter.GetChild(10).position.z;
        for (int i = 0; i < 4; i++)
        {
            availableIndices.Enqueue(i);
            lasers[i] = lasercutter.GetChild(i + 1).GetComponent<LineRenderer>();
            Transform nozzle = nozzles[i] = lasercutter.GetChild(i + 6);
            initialNozzlePos[i] = nozzle.position;
        }
        carSource = wholeCar.GetChild(0).GetComponent<AudioSource>();
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
            t.GetComponent<RectTransform>().anchoredPosition = new Vector2(pos, -0.175f);
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
        colorText.SetText("Selected Paint Color: Auto");
        bars = transform.parent.GetChild(0).GetComponentsInChildren<Rotate>();
        barCount = bars.Length;
        tireAnim = tireParent.GetChild(0).GetComponent<Animation>();
        //tireAssembly = tireAnim["TireAssembly"];
        tireSource = tireParent.GetChild(3).GetComponent<AudioSource>();
        stampState = stampAnim["Stamp"];
        MeshRenderer[] assembledRends = wholeCar.GetComponentsInChildren<MeshRenderer>();
        int n = assembledRends.Length;
        wholeCarMats = new Material[n][];
        for (int i = 0; i < n; i++) wholeCarMats[i] = assembledRends[i].materials;
        suctionParts1 = suction1.GetChild(1);
        suctionParts2 = suction2.GetChild(1);
        door1 = assemblyParts[4].GetComponent<MeshRenderer>();
        door2 = assemblyParts[10].GetComponent<MeshRenderer>();
    }

    IEnumerator SendParts()
    {
        for (int i = 0; i < partCount; i++)
        {
            Transform partT = parts[i];
            partT.position += startPos - partMarkers[i].position;
            partT.gameObject.SetActive(true);
            partStates[i] = State.Moving;
            float t = 0;
            while (t < timeInterval)
            {
                while (paused || buttonPaused) yield return null;
                t += Time.deltaTime;
                yield return null;
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
            if (d == destinationCount - 1)
            {
                partStates[i] = State.Idle;
                partRBs[i].isKinematic = true;
                Color color = partRends[i].materials[0].color;
                parts[i].gameObject.SetActive(false);
                Transform t = assemblyParts[i];
                MeshRenderer rend = t.GetComponent<MeshRenderer>();
                Material[] mats = rend.materials;
                int n = mats.Length;
                for (int j = 0; j < n; j++) mats[j].color = color;
                t.gameObject.SetActive(true);
                (i % 2 == 0 ? assemblyAnim1 : assemblyAnim2).SetInteger("Part", i);
            }
            else
            {
                if (d == destinationCount - 5) slowPart[i] = true;
                partDestinations[i] ++;
            }
            return;
        }
        Vector3 diffN = new Vector3(diff.x, 0, diff.z).normalized;
        Rigidbody rb = partRBs[i];
        float speed = moveSpeeds[i];
        if (slowPart[i])
        {
            speed = moveSpeeds[i] = speed - slowSpeed * Time.deltaTime;
            if (speed < 0.1f) speed = moveSpeeds[i] = 0.1f;
        }
        rb.MovePosition(rb.position + diffN * speed * Time.fixedDeltaTime);
        Quaternion rot = rb.rotation;
        Vector3 markerF = marker.forward;
        markerF = new Vector3(markerF.x, 0, markerF.z).normalized;
        rb.MoveRotation(Quaternion.RotateTowards(rot, Quaternion.FromToRotation(markerF, diffN) * rot, rotSpeed * Time.deltaTime));
        //.Log(destinations[d] + ", " + destinations[d].InverseTransformPoint(partMarkers[i].position).y);
    }

    void Update()
    {
        if (assemblyComplete && !tireAssembled && tiresReady == 4)
        {
            tireAssembled = true;
            tireSource.Play();
            tireAnim.Play();
        }
        if (lastAssembled && !carSwapped && !door1.isVisible && !door2.isVisible) SwapCar();
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
                    //for (int j = 0; j < 6; j++) bars[j].rotate = false;
                    StartCoroutine(Stamp(i));
                    break;
                case State.ToLasercut:
                    movePart(i);
                    if (partMarkers[i].position.z >= laserMarker.z)
                    {
                        partStates[i] = State.Stamped;
                        StartCoroutine(Lasercut(i));
                    }
                    break;
                case State.Stamped:
                    movePart(i);
                    if (partMarkers[i].position.z >= laserEnd)
                    {
                        partStates[i] = State.ToPaint;
                        paintQueue.Add(parts[i].gameObject.name);
                        UpdatePaintQueue();
                    }
                    break;
                case State.ToPaint:
                    movePart(i);
                    if (partDestinations[i] >= 7)
                    {
                        partStates[i] = State.Painted;
                        Color c = paintColor;
                        switch (i)
                        {
                            case 6 or 7 or 12 or 13:
                                if (c == Color.white) c = blackColor;
                                break;
                        }
                        StartCoroutine(SetColor(i, c));
                        for (int j = 0; j < particleCount; j++) particles[j].Play();
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

    IEnumerator SetColor(int i, Color color)
    {
        yield return particleWait;
        for (int j = 0; j < particleCount; j++)
        {
            ParticleSystem.MainModule main = particles[j].main;
            main.startColor = color;
        }
        MeshRenderer rend = partRends[i];
        if (color != Color.white)
        {
            Material[] mats = rend.materials;
            int n = mats.Length;
            for (int j = 0; j < n; j++) mats[j].color = color;
            List<int> matIndices = partMats[i];
            n = matIndices.Count;
            for (int j = 0; j < n; j += 2) wholeCarMats[matIndices[j]][matIndices[j + 1]].color = color;
        }
        painterSource.PlayOneShot(paint);
        paintQueue.RemoveAt(0);
        UpdatePaintQueue();
    }
    IEnumerator Stamp(int i)
    {
        if (!stampAnim.IsPlaying("Stamp")) stampAnim.Play();
        while (buttonPaused || !stampTrigger) yield return null;
        stampTrigger = false;
        stamperSource.PlayOneShot(stamp);
        yield return wait;
        while (buttonPaused) yield return null;
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
        for (int j = 0; j < 3; j++) yield return wait;
        while (buttonPaused) yield return null;
        paused = false;
        //if (!buttonPaused) for (int j = 0; j < 6; j++) bars[j].rotate = true;
        partStates[i] = laserIndex != -1 ? State.ToLasercut : State.Stamped;
    }

    void UpdatePaintQueue()
    {
        string str = "Vacant";
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
            colorText.SetText("Selected Paint Color: Auto");
        }
        else
        {
            paintColor = buttonColors[i];
            colorText.SetText("Selected Paint Color: " + buttonNames[i]);
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
            else
            {
                assemblyAnim1.speed = assemblyAnim2.speed = stampState.speed = 1;
                //if (!tireAssembled) tireAssembly.speed = 1;
            }
            start = buttonPaused = false;
            for (int j = 0; j < barCount; j++) bars[j].SetRotate(true, 360);
            startText.SetText("Halt Production");
            startText.color = masterButtonColors[1];
            beltSource.Play();
            beltSource.PlayOneShot(startUp);
        }
        else
        {
            buttonPaused = start = true;
            for (int j = 0; j < barCount; j++) bars[j].SetRotate(false, 360);
            startText.SetText("Initiate Production");
            startText.color = masterButtonColors[0];
            beltSource.Stop();
            beltSource.PlayOneShot(shutDown);
            assemblyAnim1.speed = assemblyAnim2.speed = stampState.speed = 0;
            //if (!tireAssembled) tireAssembly.speed = 0;
        }
        Player.Click();
    }

    public void CompleteAssembly()
    {
        assemblyComplete = true;
        startText.transform.parent.GetComponent<Button>().interactable = false;
        startText.SetText("Assembly Complete");
        startText.color = masterButtonColors[2];
        Transform t = licenseInput.transform.parent.parent;
        t.GetComponent<Button>().interactable = false;
        NonNativeKeyboard.Instance.Close();
        t.GetComponent<Image>().color = disabledColor;
        if (buttonPaused) return;
        buttonPaused = true;
        for (int j = 0; j < barCount; j++) bars[j].SetRotate(false, 360);
        beltSource.Stop();
        beltSource.PlayOneShot(shutDown);
    }

    public void Suction(int i)
    {
        Transform t = assemblyParts[i];
        if (i % 2 == 0)
        {
            t.SetParent(suction1);
            suctionParts1.GetChild(i / 2).GetLocalPositionAndRotation(out Vector3 p, out Quaternion r);
            t.SetLocalPositionAndRotation(p, r);
        }
        else
        {
            t.SetParent(suction2);
            suctionParts2.GetChild(i / 2).GetLocalPositionAndRotation(out Vector3 p, out Quaternion r);
            t.SetLocalPositionAndRotation(p, r);
        }
        //assemblyAnim.SetInteger("Part", -1);
    }

    public void UnSuction(int i)
    {
        Transform partT = assemblyParts[i];
        partT.SetParent(carParent);
        carParent.GetChild(i).GetPositionAndRotation(out Vector3 p, out Quaternion r);
        partT.SetPositionAndRotation(p, r);
        if (i == partCount - 1)
        {
            lastAssembled = true;
            if (!carSwapped && tiresReady == 4) SwapCar();
        }
    }

    public void SwapTires()
    {
        for (int i = 0; i < 4; i++)
        {
            preTires[i].SetActive(false);
            postTires[i].SetActive(true);
        }
        carSource.PlayOneShot(tireSwap);
        carAnim.Play();
    }

    public void SwapCar()
    {
        if (carSwapped || !lastAssembled) return;
        carSwapped = true;
        carParent.gameObject.SetActive(false);
        wholeCar.gameObject.SetActive(true);
    }

    public void SetLicense()
    {
        string str = Regex.Replace(licenseKeyboard.text, @"[\x00-\x1F\u200B\uFEFF]", "");
        //Debug.Log(str + str.IsNullOrWhiteSpace());
        if (!str.IsNullOrWhiteSpace()) str = str.Substring(0, Math.Min(str.Length, 9)).Trim();
        else str = "IMORTO";
        //Debug.Log(str);
        licenseInput.SetText(str);
        str = string.Join(" ", str.ToCharArray());
        licensePlate.SetText(str);
    }
}
