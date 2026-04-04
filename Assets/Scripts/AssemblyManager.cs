using UnityEngine;

public class AssemblyManager : MonoBehaviour
{
    public void Suction(int i)
    {
        ManuManager.instance.Suction(i);
    }
    public void UnSuction(int i)
    {
        ManuManager.instance.UnSuction(i);
    }
}
