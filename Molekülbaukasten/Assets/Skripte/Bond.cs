using UnityEngine;

public class Bond : MonoBehaviour
{
    public int atom1ID;
    public int atom2ID;
    public string atomRefs2;
    public string order;


    public string generateRef2(int atom1, int atom2)
    {
        string ref2 = atom1 + " " + atom2;

        return ref2;
    }
}