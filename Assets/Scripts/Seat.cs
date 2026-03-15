using UnityEngine;
using TMPro;
using System.Collections;

public class Seat : MonoBehaviour
{
    Collider col;
    Rigidbody rb;
    [SerializeField] int resetFrames, resetFrames2;
    [SerializeField] LayerMask body;//, defaultLayer;]
    //LayerMask initialExclude;
    [SerializeField] float sitHeight, exitDistance;
    [SerializeField] Transform camOffset, origin, cam, attach;
    Vector3 attachPos, initialPos;
    Vector2 attachDir;
    [SerializeField] Canvas canvas;
    [SerializeField] TMP_Text seatText;
    void Awake()
    {
        col = GetComponent<Collider>();
        attachPos = attach.position;
        Vector3 forward = attach.forward;
        attachDir = new Vector2(forward.x, forward.z);
        seatText.SetText("[CLICK ICON TO SIT]");
    }

    void Start()
    {
        rb = Player.rb;
    }

    public void Sit()
    {
        col.excludeLayers = body;
        //initialExclude = rb.excludeLayers;
        //rb.excludeLayers = defaultLayer;
        //rb.linearVelocity = Vector3.zero;
        rb.isKinematic = Player.sitting = true;
        canvas.enabled = false;
        Vector3 camPos = cam.position, pos = rb.position;
        //rb.linearVelocity = Vector3.zero;
        Player.seat = this;
        seatText.SetText("[JUMP (A) TO EXIT SEAT]");
        initialPos = camOffset.localPosition;
        rb.position = new Vector3(attachPos.x - camPos.x + pos.x, pos.y, attachPos.z - camPos.z + pos.z);
        camOffset.localPosition = new Vector3(0, sitHeight, 0);
        StartCoroutine(ResetRB(resetFrames));
    }
    IEnumerator ResetRB(int n)
    {
        for (int i = 0; i < n; i++) yield return null;
        //rb.excludeLayers = initialExclude;
        rb.isKinematic = false;
    }

    IEnumerator ResetRB2(int n, Vector3 d)
    {
        for (int i = 0; i < n; i++) yield return null;
        //rb.excludeLayers = initialExclude;
        rb.position += d;
        rb.isKinematic = false;
    }

    public void Exit(bool move)
    {
        rb.isKinematic = true;
        Player.sitting = false;
        camOffset.localPosition = initialPos;
        Vector3 d = Vector3.zero;
        if (move)
        {
            Vector3 forward = cam.forward;
            float angle = Vector2.SignedAngle(attachDir, new Vector2(forward.x, forward.z));
            switch (angle)
            {
                case > 0 and < 90:
                    d = -attach.right * exitDistance;
                    break;
                case <= 0 and > -90:
                    d = attach.right * exitDistance;
                    break;
                default:
                    d = forward * exitDistance;
                    break;
            }
            //rb.linearVelocity = Vector3.zero;
        }
        canvas.enabled = true;
        col.excludeLayers = 0;
        seatText.SetText("[CLICK ICON TO SIT]");
        Player.seat = null;
        StartCoroutine(ResetRB2(resetFrames2, d));
        //rb.isKinematic = false;
    }
}
