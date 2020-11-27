using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceField : MonoBehaviour
{
    // COMMENT1
    // bonds and angles will have individual force constants and equilibrium values
    // thus we have to use other structures (or classes?) here, e.g.
    // public struct CovBond
    // {
    //    public int Atom1; public int Atom2; public float kBond; public float r_eq;
    // }
    // public List<CovBond> NewBondList = new List<CovBond>(); // not sure this contructor works

    public List<Vector2> bondList = new List<Vector2>();
    List<Vector3> angleList = new List<Vector3>();
    // COMMENT2
    // for control of our movements, we need a list with all atom positions
    Dictionary<int, Vector3> position = new Dictionary<int, Vector3>();
    Dictionary<int, Vector3> movement = new Dictionary<int, Vector3>();

    float scalingFactor = 1f/440f; // with this, 154 pm are equivalent to 0.35 m in the model
    float kb = 1.0f;    // should be integrated into new bondList structure
    float ka = 0.0001f; // should be integrated into new angleList structure
    float standardDistance; // integrate into new bondList
    float alphaNull = 109.4712f; // integrate into new angleList

    // Start is called before the first frame update
    void Start()
    {
        standardDistance = 154f * scalingFactor;
    }

    // Update is called once per frame
    void Update()
    {
        // If the forcefield is active, update all connections and forces, else only update connections
        if (this.GetComponent<GlobalCtrl>().forceField)
        {
		    // COMMENT2a: how could we check that the lists need an update?
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
     * generation / update of the connection and angle lists.
     * Will be extended later with torsions and impropers etc.
     * maybe speed up update with "what is new"
     */ 
    void generateLists()
    {
        //clear previous movement and position map
        movement.Clear();
        position.Clear();
        //cycle through all atoms
        foreach(Atom c1 in GetComponent<GlobalCtrl>().list_curAtoms)
        {
            //fill movement map
            if (!movement.ContainsKey(c1._id))
            {
                movement.Add(c1._id, new Vector3(0, 0, 0));
                position.Add(c1._id, c1.transform.localPosition);
            }
            //compare with all atoms
            foreach(Atom c2 in GetComponent<GlobalCtrl>().list_curAtoms)
            {
                //cycle through all connection points
                foreach(ConnectionStatus conPoint in c1.getAllConPoints())
                {
                    //if a connection exists, add to list
                    if(conPoint.otherAtomID == c2._id)
                    {
                        if(!bondList.Contains(new Vector2(c1._id, c2._id)) && !bondList.Contains(new Vector2(c2._id, c1._id)))
                        {
                            //angle list
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
                            //bond list
                            bondList.Add(new Vector2(c1._id, c2._id));
                        }
                        
                        break;
                    }
                }
            }
        }
    }

    //for all connections in the corresponding lists, the forces are calculated.
    // at the end, the method to update the positions of the atoms is called
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

        calcMovements();

        applyMovements();
    }

    // COMMENT3: instead of fetching the atom coordinates here, the info on list 'positions' could be used
    // calculate bond forces
    void calcBondForces(Vector2 bond)
    {
        
        //bond vector
        Vector3 rb = getAtomByID(bond.x).transform.localPosition - getAtomByID(bond.y).transform.localPosition;
        //force on this bond vector
        float fb =  -kb *  (Vector3.Magnitude(rb) - standardDistance) ;
        //separate the forces on the two atoms
        Vector3 fc1 = fb * (rb / Vector3.Magnitude(rb));
        Vector3 fc2 = -fb * (rb / Vector3.Magnitude(rb));

        // COMMENT4: the scaling (by 0.07) should be applied later. In principle the scaling will depend on the atomic masses
        movement[(int)bond.x] += fc1 * 0.07f;
        movement[(int)bond.y] += fc2 * 0.07f;
    }

    // calculate angle forces
    void calcAngleForces(Vector3 angle)
    {
        Vector3 rb1 = getAtomByID(angle.x).transform.localPosition - getAtomByID(angle.y).transform.localPosition;
        Vector3 rb2 = getAtomByID(angle.z).transform.localPosition - getAtomByID(angle.y).transform.localPosition;

        float cosAlpha = (Vector3.Dot(rb1, rb2)) / (Vector3.Magnitude(rb1) * Vector3.Magnitude(rb2));
        float angleAlpha = Mathf.Acos(cosAlpha) * (180 / Mathf.PI);
        float mAlpha = -ka * (Mathf.Acos(cosAlpha) * (180 / Mathf.PI) - alphaNull);

        Vector3 fI = (mAlpha / (Vector3.Magnitude(rb1) * Mathf.Sqrt(1 - cosAlpha * cosAlpha))) * ((rb1 / Vector3.Magnitude(rb1)) - cosAlpha*(rb2 / Vector3.Magnitude(rb2)));
        Vector3 fK = (mAlpha / (Vector3.Magnitude(rb2) * Mathf.Sqrt(1 - cosAlpha * cosAlpha))) * ((rb2 / Vector3.Magnitude(rb2)) - cosAlpha * (rb1 / Vector3.Magnitude(rb1)));
        Vector3 fJ = -fI - fK;

        // angle forces much too strong, maybe mistake or scale them down again
        if (true/*(angleAlpha <= 170.0f || angleAlpha >= 190.0f) && (angleAlpha >= 5.0f)*/)
        {
            movement[(int)angle.x] += fI * 0.07f;
            movement[(int)angle.y] += fK * 0.07f;
            movement[(int)angle.z] += fJ * 0.07f;

            
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

    // turn forces into movements and apply sanity checks 
    void calcMovements()
    {
        //COMMENT5
        // in principle, the scaling by atomic masses and by a time-step factor should be applied here
        // currently, this is the factor 0.07f applied above


        //COMMENT6

        Vector3 CurCOM = new Vector3(0, 0, 0); // current center of mass
        int nAtoms = position.Count;
        foreach(var pair in position)
        {
            CurCOM += pair.Value;  // times (relative) mass of the atom (to be implemented)
        }
        CurCOM = CurCOM / (float)nAtoms;
        Vector3 AngMom = new Vector3(0, 0, 0);
        foreach(var pair in movement)
        {
            Vector3 vel = pair.Value;
            Vector3 pos = position[pair.Key] - CurCOM; // maybe this can be done more cleanly
            AngMom.x += pos.y * vel.z; // times (relative) mass
            AngMom.y += pos.z * vel.x;
            AngMom.z += pos.x * vel.y;
        }
        //print(AngMom.x + "  :  " + AngMom.y  + "  :  " + AngMom.z);
		// so far, we have computed the angular momentum of the structure, need now code to apply a damping of the rotation
		// AK will take care of that

        // check for too long steps:
        float MaxMove = 1f; // 0.01f; // to be checked; with 1f practically disabled
        float moveMaxNorm = 0f; // norm of movement vector
        foreach (var pair in movement)
        {
            float moveNorm = Vector3.SqrMagnitude(pair.Value);
            moveMaxNorm = Mathf.Max(moveMaxNorm, moveNorm);
        }
        if (moveMaxNorm > MaxMove)
        {
            float scaleMove = MaxMove / moveMaxNorm;
            foreach (var pair in movement)
            {
                movement[pair.Key] = pair.Value * scaleMove;
            }
        }

    }

    // COMMENT7
    // renamed this one to distinguish forces and movements (=volocities x timestep)
    // apply movement to each atom
    void applyMovements()
    {

        Vector3 test = new Vector3(0, 0, 0);
        foreach(var pair in movement)
        {
            getAtomByID(pair.Key).transform.localPosition += pair.Value;
            test += pair.Value;
            //print(pair.Key);
            //print(pair.Value.x + "  :  " + pair.Value.y + "  :  " + pair.Value.z);

            //print("Leer");
        }

        //print(test.x + "  :  " + test.y + "  :  " + test.z);
    }

    // connections between atoms get scaled new as soon as the position of an atom gets updated
    public void scaleConnections()
    {
        print("here");
        foreach(Atom atom in this.GetComponent<GlobalCtrl>().list_curAtoms)
        {
            print("1: " + atom);
            foreach(ConnectionStatus carbonCP in atom.getAllConPoints())
            {
                print("2: " + atom);
                if (carbonCP.isConnected)
                {
                    print(carbonCP + "  :  " + atom);
                    Atom carbonConnected = GameObject.Find("atom" + carbonCP.otherAtomID).GetComponent<Atom>();

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


    // returns the atom with the given ID 
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
