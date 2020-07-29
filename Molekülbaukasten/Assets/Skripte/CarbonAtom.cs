using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarbonAtom : MonoBehaviour
{
    public GrabController m_ActiveHand = null;
    public int _id;
    public float abstand;
    public bool isFull = false;
    private Vector3 l1Pos;
    private Vector3 l2Pos;
    private Vector3 r1Pos;
    private Vector3 r2Pos;
    public ConnectionStatus c0;
    public ConnectionStatus c1;
    public ConnectionStatus c2;
    public ConnectionStatus c3;


    public void f_Init(int id)
    {
        _id = id;
        this.name = "kohlenstoff" + _id;
        abstand = 1 / (Mathf.Sqrt(3));
        l1Pos = new Vector3(-abstand, -abstand, -abstand);
        l2Pos = new Vector3(abstand, -abstand, abstand);
        r1Pos = new Vector3(abstand, abstand, -abstand);
        r2Pos = new Vector3(-abstand, abstand, abstand);

        this.c0.transform.localPosition = l1Pos.normalized * transform.localScale.x * 3.5f;
        this.c1.transform.localPosition = l2Pos.normalized * transform.localScale.x * 3.5f;
        this.c2.transform.localPosition = r1Pos.normalized * transform.localScale.x * 3.5f;
        this.c3.transform.localPosition = r2Pos.normalized * transform.localScale.x * 3.5f;
        
    }
    public ConnectionStatus getConPoint(int i)
    {
        if (i == 0)
            return c0;
        else if (i == 1)
            return c1;
        else if (i == 2)
            return c2;
        else if (i == 3)
            return c3;
        else
            return c0;
    }

    public List<ConnectionStatus> getAllConPoints()
    {
        List<ConnectionStatus> allPoints = new List<ConnectionStatus>();
        allPoints.Add(c0);
        allPoints.Add(c1);
        allPoints.Add(c2);
        allPoints.Add(c3);
        return allPoints;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (this.c0.isConnected == true && this.c1.isConnected == true && this.c2.isConnected == true && this.c3.isConnected == true)
            isFull = true;
        else
            isFull = false;
    }
}
