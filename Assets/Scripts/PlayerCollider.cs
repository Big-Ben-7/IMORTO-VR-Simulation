using System;
using UnityEngine;

public class PlayerCollider : MonoBehaviour
{
    [SerializeField] MyHandedness hand;
    [SerializeField] LayerMask cols;
    [SerializeField] String[] tags;
    int tagCount;
    int val;

    void Awake()
    {
        val = cols.value;
        tagCount = tags.Length;
    }

    void OnCollisionEnter(Collision collision)
    {
        GameObject go = collision.gameObject;
        if ((val & 1 << go.layer) != 0)
        {
            if (go.layer == 15) Player.instance.Bump();
            else
            {
                for (int i = 0; i < tagCount; i++)
                {
                    if (go.CompareTag(tags[i]))
                    {
                        Player.instance.Bump();
                        break;
                    }
                }
            }
            switch (hand)
            {
                case MyHandedness.Left:
                    Player.SendHapticsLeft(0.5f, 0.1f);
                    break;
                case MyHandedness.Right:
                    Player.SendHapticsRight(0.5f, 0.1f);
                    break;
            }
        }
    }
}
