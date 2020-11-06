﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceField : MonoBehaviour
{
    public List<Vector2> bondList = new List<Vector2>();
    List<Vector3> angleList = new List<Vector3>();
    Dictionary<int, Vector3> movement = new Dictionary<int, Vector3>();

    float kb = 1.0f;
    float ka = 1.0f;
    float standardDistance = 0.35f;
    float alphaNull = 109.4712f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Wenn das Kraftfeld aktiv ist, sämtliche Bindungen und Kräfte updaten,
        // sonst nur Bindungen aktualisieren
        if (this.GetComponent<GlobalCtrl>().forceField)
        {
            bondList.Clear();
            angleList.Clear();
            generateLists();
            forces();
            scaleConnections();
        } else
        {
            scaleConnections();
        }
        
    }

    /*
     * Generierung / Update der Bindungs- und Winkellisten.
     * Muss später um Torsionen und Impropers u.ä. erweitert werden
     * Update ggf. durch "Was ist neu" beschleunigen
     */ 
    void generateLists()
    {
        //vorherige Bewegungsmap leeren
        movement.Clear();
        //Durchlauf durch alle Atome
        foreach(Atom c1 in GetComponent<GlobalCtrl>().list_curAtoms)
        {
            //Bewegungsmap füllen
            if (!movement.ContainsKey(c1._id))
            {
                movement.Add(c1._id, new Vector3(0, 0, 0));
            }
            //Vergleich mit allen Atomen
            foreach(Atom c2 in GetComponent<GlobalCtrl>().list_curAtoms)
            {
                //Durchlauf durch alle ConnectionPoints
                foreach(ConnectionStatus conPoint in c1.getAllConPoints())
                {
                    //Wenn Verbindung besteht, zu Listen hinzufügen
                    if(conPoint.otherAtomID == c2._id)
                    {
                        if(!bondList.Contains(new Vector2(c1._id, c2._id)) && !bondList.Contains(new Vector2(c2._id, c1._id)))
                        {
                            //Winkelliste
                            foreach (Vector2 vec in bondList)
                            {
                                if (vec.x == c1._id)
                                {
                                    angleList.Add(new Vector3(vec.y, c1._id, c2._id));
                                } else if (vec.x == c2._id)
                                {
                                    angleList.Add(new Vector3(vec.y, c2._id, c1._id));
                                }
                                else if (vec.y == c1._id)
                                {
                                    angleList.Add(new Vector3(vec.x, c1._id, c2._id));
                                }
                                else if (vec.y == c2._id)
                                {
                                    angleList.Add(new Vector3(vec.x, c2._id, c1._id));
                                }
                            }
                            //Bindungsliste
                            bondList.Add(new Vector2(c1._id, c2._id));
                        }
                        
                        break;
                    }
                }
            }
        }
    }

    // Für alle Bindungen in den jeweiligen Listen werden die Kräfte berechnet,
    // am Ende wird die Methode zum Aktualisieren der Position aufgerufen.
    void forces()
    {
        //Loop Bond List
        foreach(Vector2 bond in bondList)
        {
            calcBondForces(bond);
        }

        //Loop Angle List
        foreach(Vector3 angle in angleList)
        {
            calcAngleForces(angle);
        }

        applyForces();
    }

    // Berechnet Bindungskräfte
    void calcBondForces(Vector2 bond)
    {
        
        //Bindungsvektor
        Vector3 rb = getAtomByID(bond.x).transform.localPosition - getAtomByID(bond.y).transform.localPosition;
        //Kraft entlang diesem Bindungsvektor
        float fb =  -kb *  (Vector3.Magnitude(rb) - standardDistance) ;
        //Kräfte auf die beiden Atome verteilt
        Vector3 fc1 = fb * (rb / Vector3.Magnitude(rb));
        Vector3 fc2 = -fb * (rb / Vector3.Magnitude(rb));

        movement[(int)bond.x] += fc1 * 0.07f;
        movement[(int)bond.y] += fc2 * 0.07f;
    }

    // Berechnet Winkelkräfte
    void calcAngleForces(Vector3 angle)
    {
        Vector3 rb1 = getAtomByID(angle.x).transform.localPosition - getAtomByID(angle.y).transform.localPosition;
        Vector3 rb2 = getAtomByID(angle.z).transform.localPosition - getAtomByID(angle.y).transform.localPosition;

        float cosAlpha = (Vector3.Dot(rb1, rb2)) / (Vector3.Magnitude(rb1) * Vector3.Magnitude(rb2));
        float angleAlpha = Mathf.Acos(cosAlpha) * (180 / Mathf.PI);
        float mAlpha = -ka * (Mathf.Acos(cosAlpha) * (180 / Mathf.PI) - alphaNull) ;

        Vector3 fI = (mAlpha / (Vector3.Magnitude(rb1) * Mathf.Sqrt(1 - cosAlpha * cosAlpha))) * ((rb1 / Vector3.Magnitude(rb1)) - cosAlpha*(rb2 / Vector3.Magnitude(rb2)));
        Vector3 fK = (mAlpha / (Vector3.Magnitude(rb2) * Mathf.Sqrt(1 - cosAlpha * cosAlpha))) * ((rb2 / Vector3.Magnitude(rb2)) - cosAlpha * (rb1 / Vector3.Magnitude(rb1)));
        Vector3 fJ = -fI - fK;
        float angleFactor = 0.001f;
        // Winkelkräfte viel zu stark, sobald mehrere Atome darauf einwirken, evtl normalisieren oder weiter verkleinern?
        if ((angleAlpha <= 170.0f || angleAlpha >= 190.0f) && (angleAlpha >= 5.0f))
        {
            movement[(int)angle.x] += fI * 0.07f * angleFactor;
            movement[(int)angle.y] += fK * 0.07f * angleFactor;
            movement[(int)angle.z] += fJ * 0.07f * angleFactor;
        }
        // else
        //{
        //    float lineX = Mathf.Abs(getAtomByID(angle.x).transform.localPosition.x - getAtomByID(angle.z).transform.localPosition.x);
        //    float lineY = Mathf.Abs(getAtomByID(angle.x).transform.localPosition.y - getAtomByID(angle.z).transform.localPosition.y);
        //    float lineZ = Mathf.Abs(getAtomByID(angle.x).transform.localPosition.z - getAtomByID(angle.z).transform.localPosition.z);

        //    if (lineX <= lineY && lineX <= lineZ)
        //    {
        //        movement[(int)angle.x] = new Vector3(0.1f, 0, 0);
        //    }
        //    else if (lineY < lineX && lineY < lineZ)
        //    {
        //        movement[(int)angle.x] = new Vector3(0, 0.1f, 0);
        //    }
        //    else if (lineZ < lineX && lineZ < lineY)
        //    {
        //        movement[(int)angle.x] = new Vector3(0, 0, 0.1f);
        //    }
        //}
       
    }

    // Kräfte als Bewegung der einzelnen Atome umsetzen
    void applyForces()
    {
        foreach(var pair in movement)
        {
            getAtomByID(pair.Key).transform.localPosition += pair.Value;
        }
    }

    // Atombindungen werden neu skaliert sobald Atome bewegt werden
    public void scaleConnections()
    {
        foreach(Atom atom in this.GetComponent<GlobalCtrl>().list_curAtoms)
        {
            foreach(ConnectionStatus carbonCP in atom.getAllConPoints())
            {
                if (carbonCP.isConnected)
                {
                    Atom carbonConnected = GameObject.Find("kohlenstoff" + carbonCP.otherAtomID).GetComponent<Atom>();

                    float distance = Vector3.Distance(atom.transform.position, carbonConnected.transform.position);
                    float distanceDiff = distance - standardDistance;
                    Transform connection = GameObject.Find("con" + carbonCP.conID).transform;
                    connection.localScale = new Vector3(connection.localScale.x, connection.localScale.y, 1 + (distanceDiff* 2.5f));
                    connection.transform.position = atom.transform.position;
                    connection.transform.LookAt(carbonConnected.transform.position);

                }
            }
        }
    }


    // Liefert bei gegebener ID das dazugehörige Atom zurück
    public Atom getAtomByID(float id)
    {
        foreach(Atom c1 in GetComponent<GlobalCtrl>().list_curAtoms)
        {
            if (c1._id == (int)id)
                return c1;
        }

        return null;
    }
}
