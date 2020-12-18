using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Atom : MonoBehaviour
{
    public GrabController m_ActiveHand = null;
    public int _id;
    public float abstand;
    public bool isFull = false;
    public float mass;
    public string type;
    private Vector3 l1Pos;
    private Vector3 l2Pos;
    private Vector3 r1Pos;
    private Vector3 r2Pos;
    public ConnectionStatus c0;
    public ConnectionStatus c1;
    public ConnectionStatus c2;
    public ConnectionStatus c3;

    //optimal distance and bond force const.
    public float r0;
    public float kb;

    //public float c0c1;
    //public float c0c2;
    //public float c0c3;
    //public float c1c2;
    //public float c1c3;
    //public float c2c3;

    private List<Atom> allAtoms = new List<Atom>();

    internal void f_InitDummy(int id, int atomid, int conID)
    {
        _id = id;
        this.type = "DUMMY";
        this.name = "dummy" + _id;
        this.mass = 0;
        //ConnectionStatus conDummy = new ConnectionStatus();
        //conDummy.otherAtomID = atomid;
        //conDummy.otherPointID = conID;
        //conDummy.isConnected = true;
        //this.c1.gameObject.SetActive(false);
        //this.c2.gameObject.SetActive(false);
        //this.c3.gameObject.SetActive(false);
        this.c0.isConnected = true;
        this.c0.otherAtomID = atomid;
        this.c0.otherPointID = conID;
        this.c1.usable = false;
        this.c2.usable = false;
        this.c3.usable = false;
        print("init Dummy");
    }


    public void f_InitCarbon(int id)
    {
        _id = id;
        this.type = "C";
        this.name = "atom" + _id;
        this.mass = 12f;

        //abstand = 1 / (Mathf.Sqrt(3));
        //l1Pos = new Vector3(-abstand, -abstand, -abstand);
        //l2Pos = new Vector3(abstand, -abstand, abstand);
        //r1Pos = new Vector3(abstand, abstand, -abstand);
        //r2Pos = new Vector3(-abstand, abstand, abstand);

        //this.c0.transform.localPosition = l1Pos.normalized * transform.localScale.x * 3.5f;
        //this.c1.transform.localPosition = l2Pos.normalized * transform.localScale.x * 3.5f;
        //this.c2.transform.localPosition = r1Pos.normalized * transform.localScale.x * 3.5f;
        //this.c3.transform.localPosition = r2Pos.normalized * transform.localScale.x * 3.5f;
        
    }

    public void f_InitHydrogen(int id)
    {
        this._id = id;
        this.type = "H";
        this.name = "atom" + _id;
        this.mass = 1f;
        this.gameObject.GetComponent<Renderer>().material.color = new Color32(232, 232, 232, 1);
        this.c1.usable = false;
        this.c2.usable = false;
        this.c3.usable = false;
    }

    //public float getAngle(ConnectionStatus cx , ConnectionStatus cy)
    //{
    //    if ((cx == c0 && cy == c1) || (cy == c0 && cx == c1))
    //        return c0c1;
    //    else if ((cx == c0 && cy == c2) || (cy == c0 && cx == c2))
    //        return c0c2;
    //    else if ((cx == c0 && cy == c3) || (cy == c0 && cx == c3))
    //        return c0c3;
    //    else if ((cx == c1 && cy == c2) || (cy == c1 && cx == c2))
    //        return c1c2;
    //    else if ((cx == c1 && cy == c3) || (cy == c1 && cx == c3))
    //        return c1c3;
    //    else if ((cx == c2 && cy == c3) || (cy == c2 && cx == c3))
    //        return c2c3;
    //    else
    //        return -1.0f;     
    //}

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
        if (this.c0.usable)
        {
            allPoints.Add(c0);
        }
        if (this.c1.usable)
        {
            allPoints.Add(c1);
        }
        if (this.c2.usable)
        {
            allPoints.Add(c2);
        }
        if (this.c3.usable)
        {
            allPoints.Add(c3);
        }
        return allPoints;
    }

    public void setOriginalColor()
    {
        if(this.type == "C")
        {
            this.gameObject.GetComponent<Renderer>().material.color = new Color32(0, 0, 0, 1);
        } else if(this.type == "H")
        {
            this.gameObject.GetComponent<Renderer>().material.color = new Color32(232, 232, 232, 1);
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }



    // Update is called once per frame
    void Update()
    {
        bool fullTest = true;
        if (!this.name.StartsWith("dummy"))
        {
            foreach (ConnectionStatus conPoint in this.getAllConPoints())
            {
                if (!conPoint.isConnected)
                    fullTest = false;
            }
            isFull = fullTest;
        }


        //allAtoms = GameObject.Find("Camera").GetComponent<GlobalCtrl>().list_curAtoms;
        
        //if (this.c0.isConnected && this.c1.isConnected)
        //    c0c1 = Vector3.Angle(allAtoms.Find(p => p._id == c0.otherAtomID).transform.localPosition - this.transform.localPosition, allAtoms.Find(p => p._id == c1.otherAtomID).transform.localPosition - this.transform.localPosition);

        //if(this.c0.isConnected && this.c2.isConnected)
        //    c0c2 = Vector3.Angle(allAtoms.Find(p => p._id == c0.otherAtomID).transform.localPosition - this.transform.localPosition, allAtoms.Find(p => p._id == c2.otherAtomID).transform.localPosition - this.transform.localPosition);

        //if (this.c0.isConnected && this.c3.isConnected)
        //    c0c3 = Vector3.Angle(allAtoms.Find(p => p._id == c0.otherAtomID).transform.localPosition - this.transform.localPosition, allAtoms.Find(p => p._id == c3.otherAtomID).transform.localPosition - this.transform.localPosition);

        //if (this.c1.isConnected && this.c2.isConnected)
        //    c1c2 = Vector3.Angle(allAtoms.Find(p => p._id == c1.otherAtomID).transform.localPosition - this.transform.localPosition, allAtoms.Find(p => p._id == c2.otherAtomID).transform.localPosition - this.transform.localPosition);

        //if (this.c1.isConnected && this.c3.isConnected)
        //    c1c3 = Vector3.Angle(allAtoms.Find(p => p._id == c1.otherAtomID).transform.localPosition - this.transform.localPosition, allAtoms.Find(p => p._id == c3.otherAtomID).transform.localPosition - this.transform.localPosition);

        //if (this.c2.isConnected && this.c3.isConnected)
        //    c2c3 = Vector3.Angle(allAtoms.Find(p => p._id == c2.otherAtomID).transform.localPosition - this.transform.localPosition, allAtoms.Find(p => p._id == c3.otherAtomID).transform.localPosition - this.transform.localPosition);

        //if (GameObject.Find("Molekül").GetComponent<EditMode>().editMode)
        //{
        //    print(this + "  " + c0c1 + "  " + c0c2 + "  " + c0c3 + "  " + c1c2 + "  " + c1c3 + "  " + c2c3);
        //}
    }
}
