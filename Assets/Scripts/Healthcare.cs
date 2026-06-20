using TMPro;
using UnityEngine;

public class Healthcare : MonoBehaviour
{
    [Header("Da Vinci")]
    [SerializeField] Transform[] joysticks;
    [SerializeField] Transform[] arms;
    [SerializeField] float speed, deadZone;
    [SerializeField] LayerMask body;
    [SerializeField] TMP_Text armText;
    [SerializeField] int arm = -1;
    Transform[] attaches = new Transform[3];
    Vector3[] initialPos = new Vector3[3];
    Collider[] cols = new Collider[3];
    Transform[] midArms = new Transform[4], lowArms = new Transform[4], needles = new Transform[4];
    bool[] grabbed = new bool[3];
    Vector3[] armVectors1 = new Vector3[] {Vector3.down, Vector3.down, Vector3.up, Vector3.up};
    Vector3[] needleVectors2 = new Vector3[] {Vector3.back, Vector3.forward, Vector3.back, Vector3.forward};
    GameObject[,,] arrows = new GameObject[4, 3, 2];

    /*[Header("Triggers")]
    [SerializeField] Camera mainCam;
    [SerializeField] MeshRenderer screen;
    VideoPlayer videoPlayer;
    [SerializeField] Material videoMat;*/
    //[SerializeField] Drone drone;
    //bool canPlay = true, playing = false;

    void Awake()
    {
        //videoPlayer = screen.GetComponent<VideoPlayer>();
        for (int i = 0; i < 4; i++)
        {
            if (i < 3)
            {
                cols[i] = joysticks[i].GetComponent<Collider>();
                attaches[i] = joysticks[i].GetChild(0);
                initialPos[i] = attaches[i].position;
            }

            Transform arm = arms[i];
            Transform mid = midArms[i] = arm.GetChild(0);
            Transform low = lowArms[i] = mid.GetChild(0);
            Transform needle = needles[i] = low.GetChild(0);

            arrows[i, 0, 0] = arm.parent.GetChild(i+7).gameObject;
            arrows[i, 0, 1] = arm.GetChild(1).gameObject;
            arrows[i, 1, 0] = low.GetChild(1).gameObject;
            arrows[i, 1, 1] = mid.GetChild(1).gameObject;
            arrows[i, 2, 0] = needle.GetChild(0).gameObject;
            arrows[i, 2, 1] = low.GetChild(2).gameObject;
        }
        //screen.material = videoMat;
        //videoPlayer.Play();
    }

    void Update()
    {
        /*if (canPlay)
        {
            if (GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(mainCam), screen.bounds))
            {
                if (!playing)
                {
                    playing = true;
                    screen.material = videoMat;
                    videoPlayer.Play();
                }
            }
            else if (playing)
            {
                playing = false;
                videoPlayer.Pause();
            }
        }*/

        if (arm == -1) return;
        Quaternion needleRot = needles[arm].rotation;
        bool lowX = true;

        Vector3 d = (attaches[0].position - initialPos[0]) * speed * Time.deltaTime;
        float dead = deadZone * speed * Time.deltaTime;
        if (d.x > dead) arms[arm].Rotate(armVectors1[arm] * (d.x - dead), Space.World);
        else if (d.x < -dead) arms[arm].Rotate(armVectors1[arm] * (d.x + dead), Space.World);
        if (d.z > dead) midArms[arm].Rotate(Vector3.up * (d.z - dead), Space.World);
        else if (d.z < -dead) midArms[arm].Rotate(Vector3.up * (d.z + dead), Space.World);
        
        d = (attaches[1].position - initialPos[1]) * speed * Time.deltaTime;
        if (d.x > dead) lowArms[arm].Rotate(midArms[arm].TransformDirection(Vector3.left) * (d.x - dead), Space.World);
        else if (d.x < -dead) lowArms[arm].Rotate(midArms[arm].TransformDirection(Vector3.left) * (d.x + dead), Space.World);
        else lowX = false;
        if (d.z > dead) lowArms[arm].Rotate(Vector3.forward * (d.z - dead));
        else if (d.z < -dead) lowArms[arm].Rotate(Vector3.forward * (d.z + dead));
        
        d = (attaches[2].position - initialPos[2]) * speed * Time.deltaTime;
        if (d.x > dead) needles[arm].Rotate(lowArms[arm].TransformDirection(Vector3.left) * (d.x - dead), Space.World);
        else if (d.x < -dead) needles[arm].Rotate(lowArms[arm].TransformDirection(Vector3.left) * (d.x + dead), Space.World);
        else if (lowX) needles[arm].rotation = needleRot;
        if (d.z > dead) needles[arm].Rotate(needleVectors2[arm] * (d.z - dead));
        else if (d.z < -dead) needles[arm].Rotate(needleVectors2[arm] * (d.z + dead));
    }

    public void Hover(int n)
    {
        if (arm == -1) return;
        arrows[arm, n, 0].SetActive(true);
        arrows[arm, n, 1].SetActive(true);
    }

    public void ExitHover(int n)
    {
        if (arm == -1) return;
        arrows[arm, n, 0].SetActive(false);
        arrows[arm, n, 1].SetActive(false);
    }

    public void SetArm(int a)
    {
        int lastArm = arm;
        if (arm != a) arm = a;
        else arm = -1;
        switch(arm)
        {
            case -1:
                armText.SetText("Push button to select arm");
                break;
            case 0:
                armText.SetText("Selected Arm: Bottom-Left");
                break;
            case 1:
                armText.SetText("Selected Arm: Top-Left");
                break;
            case 2:
                armText.SetText("Selected Arm: Bottom-Right");
                break;
            case 3:
                armText.SetText("Selected Arm: Top-Right");
                break;
        }
        if (lastArm != -1)
        {
            for (int n = 0; n < 3; n++)
            {
                GameObject go = arrows[lastArm, n, 0];
                if (go.activeSelf) go.SetActive(false);
                go = arrows[lastArm, n, 1];
                if (go.activeSelf) go.SetActive(false);
            }
        }
        Player.Click();
    }

    /*public void ReleasedArmJoystick()
    {
        Vector3 d = attaches[2].position - initialPos[2];
        if (d.z > deadZone)
        {
            if (d.x > deadZone) SetArm(2);
            else if (d.x < -deadZone) SetArm(3);
            else SetArm(-1);
        }
        else if (d.z < -deadZone)
        {
            if (d.x > deadZone) SetArm(0);
            else if (d.x < -deadZone) SetArm(1);
            else SetArm(-1);
        }
        else SetArm(-1);
        if (arm == -1) return;
    }*/

    public void Grab(int i)
    {
        grabbed[i] = !grabbed[i];
        if (grabbed[i]) cols[i].excludeLayers = body;
        else cols[i].excludeLayers = 0;
    }

    /*void OnTriggerEnter(Collider other)
    {
        //drone.Reset();
        if (!other.gameObject.CompareTag("Player")) return;
        if (!canPlay) canPlay = true;
    }*/

    /*void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.CompareTag("Player")) return;
        if (canPlay) canPlay = false;
        if (playing)
        {
            playing = false;
            videoPlayer.Pause();
        }
    }*/
}
