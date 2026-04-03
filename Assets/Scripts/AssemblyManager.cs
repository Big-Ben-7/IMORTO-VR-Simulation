using UnityEngine;

public class AssemblyManager : MonoBehaviour
{
    Animator anim;
    void Awake()
    {
        anim = GetComponent<Animator>();
    }
    public void Suction(int i)
    {
        ManuManager.instance.Suction(i);
    }
    public void UnSuction(int i)
    {
        ManuManager.instance.UnSuction(i);
    }
    public void Reset()
    {
        anim.SetInteger("Part", -1);
    }
}
