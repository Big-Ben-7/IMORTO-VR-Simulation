using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ManuManager : MonoBehaviour
{
    [SerializeField] float moveSpeed, timeInterval;
    [SerializeField] Transform[] parts;
    [SerializeField] GameObject sheetPrefab;
    Transform[] partMarkers;
    MeshRenderer[] partRends;
    Collider[] partCols;
    GameObject[] sheets;
    ParticleSystem[] particles;
    int particleCount;
    [SerializeField] Transform stamper, painter, startMarker, endMarker, buttonParent;
    [SerializeField] TMP_Text colorText, startText;
    string[] buttonNames;
    Color[] buttonColors;
    Animation stampAnim;
    float stampZ, paintZ, endZ;
    Vector3 startPos;
    enum State {Idle, Moving, Stamping, Stamped, Painted};
    State[] partStates;
    int partCount;
    bool paused, start = true, buttonPaused, firstStarted;
    WaitForSeconds particleWait = new WaitForSeconds(0.005f);
    WaitForSeconds wait = new WaitForSeconds(0.5833f);
    Color paintColor;
    Rotate[] bars;
    int barCount;
    AudioSource painterSource, stamperSource, beltSource;
    [SerializeField] AudioClip paint, stamp;

    void Awake()
    {
        partCount = parts.Length;
        partStates = new State[partCount];
        partMarkers = new Transform[partCount];
        partRends = new MeshRenderer[partCount];
        partCols = new Collider[partCount];
        sheets = new GameObject[partCount];
        for (int i = 0; i < partCount; i++) 
        {
            Transform partT = parts[i];
            Transform marker = partMarkers[i] = partT.GetChild(0);
            MeshRenderer rend = partRends[i] = partT.GetComponent<MeshRenderer>();
            rend.enabled = false;
            Collider col = partCols[i] = partT.GetComponent<Collider>();
            col.enabled = false;
            sheets[i] = Instantiate(sheetPrefab, marker.position, Quaternion.identity, partT);
        }
        beltSource = transform.GetChild(0).GetComponent<AudioSource>();
        stampAnim = stamper.GetComponent<Animation>();
        startPos = startMarker.position;
        endZ = endMarker.position.z - 0.01f;
        stampZ = stamper.position.z - 0.01f;
        stamperSource = stamper.GetChild(0).GetComponent<AudioSource>();
        paintZ = painter.position.z - 0.01f;
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
            t.GetComponent<RectTransform>().anchoredPosition = new Vector2(pos, -0.08f);
            buttonNames[i] = t.gameObject.name;
            buttonColors[i] = t.GetComponent<Image>().color;
            pos += inc;
        }
        pos = -0.43f;
        for (int i = row1; i < childCount; i++)
        {
            Transform t = buttonParent.GetChild(i);
            t.GetComponent<RectTransform>().anchoredPosition = new Vector2(pos, -0.2f);
            buttonNames[i] = t.gameObject.name;
            buttonColors[i] = t.GetComponent<Image>().color;
            pos += inc;
        }
        paintColor = buttonColors[0];
        colorText.SetText("Selected Paint Color: " + buttonNames[0]);
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

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < partCount; i++)
        {
            switch(partStates[i])
            {
                case State.Idle: break;
                case State.Moving:
                    if (paused || buttonPaused) break;
                    if (partMarkers[i].position.z < stampZ) parts[i].position += new Vector3(0, 0, moveSpeed * Time.deltaTime);
                    else partStates[i] = State.Stamping;
                    break;
                case State.Stamping:
                    if (paused || buttonPaused) break;
                    paused = true;
                    partStates[i] = State.Stamping;
                    for (int j = 0; j < barCount; j++) bars[j].rotate = false;
                    StartCoroutine(Stamp(i));
                    break;
                case State.Stamped:
                    if (buttonPaused) break;
                    if (partMarkers[i].position.z < paintZ) parts[i].position += new Vector3(0, 0, moveSpeed * Time.deltaTime);
                    else
                    {
                        partStates[i] = State.Painted;
                        StartCoroutine(SetColor(partRends[i], paintColor));
                        for (int j = 0; j < particleCount; j++) particles[j].Play();
                    }
                    break;
                case State.Painted:
                    if (buttonPaused) break;
                    if (partMarkers[i].position.z < endZ) parts[i].position += new Vector3(0, 0, moveSpeed * Time.deltaTime);
                    else
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
        Material[] mats = rend.materials;
        int n = mats.Length;
        for (int i = 0; i < n; i++) mats[i].color = color;
        painterSource.PlayOneShot(paint);
    }
    IEnumerator Stamp(int i)
    {
        if (!stampAnim.IsPlaying("Stamp")) stampAnim.Play();
        stamperSource.PlayOneShot(stamp);
        yield return wait;
        Destroy(sheets[i]);
        parts[i].position += new Vector3(0, 0.1f, 0);
        partRends[i].enabled = partCols[i].enabled = true;
        yield return wait;
        paused = false;
        for (int j = 0; j < barCount; j++) bars[j].rotate = true;
        partStates[i] = State.Stamped;
    }

    public void SetPaint(int i)
    {
        paintColor = buttonColors[i];
        colorText.SetText("Selected Paint Color: " + buttonNames[i]);
        for (int j = 0; j < particleCount; j++)
        {
            ParticleSystem.MainModule main = particles[j].main;
            main.startColor = paintColor;
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
            startText.SetText("Pause Conveyer Belt");
            beltSource.Play();
        }
        else
        {
            buttonPaused = start = true;
            for (int j = 0; j < barCount; j++) bars[j].rotate = false;
            startText.SetText("Activate Conveyer Belt");
            beltSource.Stop();
        }
        Player.Click();
    }
}
