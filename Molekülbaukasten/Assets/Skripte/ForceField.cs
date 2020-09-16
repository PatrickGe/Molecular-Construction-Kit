using System.Collections;
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

    Vector3 test;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        test = new Vector3(0, 0, 0);
        if (this.GetComponent<GlobalCtrl>().forceField)
        {
            bondList.Clear();
            angleList.Clear();
            generateLists();
            forces();
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
        foreach(CarbonAtom c1 in GetComponent<GlobalCtrl>().list_curCarbonAtoms)
        {
            //Bewegungsmap füllen
            if (!movement.ContainsKey(c1._id))
            {
                movement.Add(c1._id, new Vector3(0, 0, 0));
            }
            //Vergleich mit allen Atomen
            foreach(CarbonAtom c2 in GetComponent<GlobalCtrl>().list_curCarbonAtoms)
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

        //Winkelkräfte viel zu stark, sobald mehrere Atome darauf einwirken, evtl normalisieren oder weiter verkleinern?
        if((angleAlpha <= 170.0f || angleAlpha >= 190.0f) && (angleAlpha >= 5.0f))
        {
            movement[(int)angle.x] += fI * 0.0007f;
            movement[(int)angle.y] += fK * 0.0007f;
            movement[(int)angle.z] += fJ * 0.0007f;
        }
        
        print("fI: " + fI * 0.0007f);
        print("fK: " + fK * 0.0007f);
        print("fJ: " + fJ * 0.0007f);

        //Map erstellen Key: Atom ID Value: bondVector + angleVector
        //Am Ende in applyForces() alle Kräfte zu Bewegungen machen
    }

    void applyForces()
    {
        foreach(var pair in movement)
        {
            getAtomByID(pair.Key).transform.localPosition += pair.Value;
            test += pair.Value;
        }
        print(test);
    }


    public void scaleConnections()
    {
        foreach(CarbonAtom atom in this.GetComponent<GlobalCtrl>().list_curCarbonAtoms)
        {
            foreach(ConnectionStatus carbonCP in atom.getAllConPoints())
            {
                if (carbonCP.isConnected)
                {
                    CarbonAtom carbonConnected = GameObject.Find("kohlenstoff" + carbonCP.otherAtomID).GetComponent<CarbonAtom>();

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



    public CarbonAtom getAtomByID(float id)
    {
        foreach(CarbonAtom c1 in GetComponent<GlobalCtrl>().list_curCarbonAtoms)
        {
            if (c1._id == (int)id)
                return c1;
        }

        return null;
    }
}
