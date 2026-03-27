using UnityEngine;

public class PlayerCollider : MonoBehaviour
{
    [SerializeField] MyHandedness hand;
    [SerializeField] LayerMask cols;
    int val;

    void Awake()
    {
        val = cols.value;
    }

    void OnCollisionEnter(Collision collision)
    {
        GameObject go = collision.gameObject;
        if ((val & 1 << go.layer) != 0)
        {
            if (!go.CompareTag("Racket") && !go.CompareTag("PokeInteractable")) Player.instance.Bump();
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
