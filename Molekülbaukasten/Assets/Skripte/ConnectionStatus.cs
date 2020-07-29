using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectionStatus : MonoBehaviour
{
    public bool isConnected = false;
    public int otherAtomID;
    public int otherPointID;
    public int conID;
    public connectInfo m_info;

    // Start is called before the first frame update
    void Start()
    {
        if(isConnected == false)
        {
            otherAtomID = -1;
            otherPointID = -1;
            conID = -1;
        }
    }

    // Update is called once per frame
    void Update()
    { 

    }
}
