using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceField : MonoBehaviour
{
    List<Vector2> bondList = new List<Vector2>();
    List<Vector3> angleList = new List<Vector3>();
    float kb = 1.0f;
    float ka = 1.0f;
    float standardDistance = 0.35f;
    float alphaNull = 1;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (this.GetComponent<GlobalCtrl>().forceField)
        {
            generateLists();
            forces();
        }
        scaleConnections();
    }

    /*
     * Generierung / Update der Bindungs- und Winkellisten.
     * Muss später um Torsionen und Impropers u.ä. erweitert werden
     * Update ggf. durch "Was ist neu" beschleunigen
     */ 
    void generateLists()
    {
        //Durchlauf durch alle Atome
        foreach(CarbonAtom c1 in GetComponent<GlobalCtrl>().list_curCarbonAtoms)
        {
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
                                else if (vec.y == c2._id)
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

        getAtomByID(bond.x).transform.localPosition += fc1 * 0.07f;
        getAtomByID(bond.y).transform.localPosition += fc2 * 0.07f;
        //print(getAtomByID(bond.x) + ": " + fc1);
        //print(getAtomByID(bond.y) + ": " + fc2);
    }

    void calcAngleForces(Vector3 angle)
    {
        Vector3 rb1 = getAtomByID(angle.x).transform.localPosition - getAtomByID(angle.y).transform.localPosition;
        Vector3 rb2 = getAtomByID(angle.z).transform.localPosition - getAtomByID(angle.y).transform.localPosition;

        float cosAlpha = (Vector3.Dot(rb1, rb2)) / (Vector3.Magnitude(rb1) * Vector3.Magnitude(rb2));
        float mAlpha = -ka * (Mathf.Acos(cosAlpha) - alphaNull) ;

        Vector3 fI = (mAlpha / (Vector3.Magnitude(rb1) * Mathf.Sqrt(1 - cosAlpha * cosAlpha))) * ((rb1 / Vector3.Magnitude(rb1)) - cosAlpha*(rb2 / Vector3.Magnitude(rb2)));
        Vector3 fK = (mAlpha / (Vector3.Magnitude(rb2) * Mathf.Sqrt(1 - cosAlpha * cosAlpha))) * ((rb2 / Vector3.Magnitude(rb2)) - cosAlpha * (rb1 / Vector3.Magnitude(rb1)));
        Vector3 fJ = -fI - fK;

        print("fI: " + fI);
        print("fK: " + fK);
        print("fJ: " + fJ);
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
