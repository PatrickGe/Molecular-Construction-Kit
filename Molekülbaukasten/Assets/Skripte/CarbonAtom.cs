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

    public float c0c1;
    public float c0c2;
    public float c0c3;
    public float c1c2;
    public float c1c3;
    public float c2c3;

    private List<CarbonAtom> allCarbonAtoms = new List<CarbonAtom>();

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

    public float getAngle(ConnectionStatus cx , ConnectionStatus cy)
    {
        if ((cx == c0 && cy == c1) || (cy == c0 && cx == c1))
            return c0c1;
        else if ((cx == c0 && cy == c2) || (cy == c0 && cx == c2))
            return c0c2;
        else if ((cx == c0 && cy == c3) || (cy == c0 && cx == c3))
            return c0c3;
        else if ((cx == c1 && cy == c2) || (cy == c1 && cx == c2))
            return c1c2;
        else if ((cx == c1 && cy == c3) || (cy == c1 && cx == c3))
            return c1c3;
        else if ((cx == c2 && cy == c3) || (cy == c2 && cx == c3))
            return c2c3;
        else
            return -1.0f;     
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

        allCarbonAtoms = GameObject.Find("Camera").GetComponent<GlobalCtrl>().list_curCarbonAtoms;
        
        if (this.c0.isConnected && this.c1.isConnected)
            c0c1 = Vector3.Angle(allCarbonAtoms.Find(p => p._id == c0.otherAtomID).transform.localPosition - this.transform.localPosition, allCarbonAtoms.Find(p => p._id == c1.otherAtomID).transform.localPosition - this.transform.localPosition);

        if(this.c0.isConnected && this.c2.isConnected)
            c0c2 = Vector3.Angle(allCarbonAtoms.Find(p => p._id == c0.otherAtomID).transform.localPosition - this.transform.localPosition, allCarbonAtoms.Find(p => p._id == c2.otherAtomID).transform.localPosition - this.transform.localPosition);

        if (this.c0.isConnected && this.c3.isConnected)
            c0c3 = Vector3.Angle(allCarbonAtoms.Find(p => p._id == c0.otherAtomID).transform.localPosition - this.transform.localPosition, allCarbonAtoms.Find(p => p._id == c3.otherAtomID).transform.localPosition - this.transform.localPosition);

        if (this.c1.isConnected && this.c2.isConnected)
            c1c2 = Vector3.Angle(allCarbonAtoms.Find(p => p._id == c1.otherAtomID).transform.localPosition - this.transform.localPosition, allCarbonAtoms.Find(p => p._id == c2.otherAtomID).transform.localPosition - this.transform.localPosition);

        if (this.c1.isConnected && this.c3.isConnected)
            c1c3 = Vector3.Angle(allCarbonAtoms.Find(p => p._id == c1.otherAtomID).transform.localPosition - this.transform.localPosition, allCarbonAtoms.Find(p => p._id == c3.otherAtomID).transform.localPosition - this.transform.localPosition);

        if (this.c2.isConnected && this.c3.isConnected)
            c2c3 = Vector3.Angle(allCarbonAtoms.Find(p => p._id == c2.otherAtomID).transform.localPosition - this.transform.localPosition, allCarbonAtoms.Find(p => p._id == c3.otherAtomID).transform.localPosition - this.transform.localPosition);

        //if (GameObject.Find("Molekül").GetComponent<EditMode>().editMode)
        //{
        //    print(this + "  " + c0c1 + "  " + c0c2 + "  " + c0c3 + "  " + c1c2 + "  " + c1c3 + "  " + c2c3);
        //}
    }
}
