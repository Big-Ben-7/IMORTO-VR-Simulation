using UnityEngine;

public class PlayerCollider : MonoBehaviour
{
    [SerializeField] MyHandedness hand;

    void OnCollisionEnter(Collision collision)
    {
        GameObject go = collision.gameObject;
        if (go.layer == 0)
        {
            //if (!go.CompareTag("Racket")) Player.instance.Bump();
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
