using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System;
using UnityEngine;

public class ForceField : MonoBehaviour
{
    // COMMENT1
    // bonds and angles will have individual force constants and equilibrium values
    // thus we have to use other structures (or classes?) here, e.g.
    //struct CovBond
    //{
    //    public int Atom1; public int Atom2; public float kBond; public float r_eq;
    //}
    //List<CovBond> bondList = new List<CovBond>(); 

    public List<Vector2> bondList = new List<Vector2>();
    public List<Vector3> angleList = new List<Vector3>();
    
    List<int> atomList = new List<int>();
    List<float> atomMass = new List<float>();
    List<Vector3> position = new List<Vector3>();
    public List<Vector3> movement = new List<Vector3>();
    int nAtoms;

    //float scalingFactor = 154 / (154 / GetComponent<GlobalCtrl>().scale); // with this, 154 pm are equivalent to 0.35 m in the model
    // note that the forcefield works in the atomic scale (i.e. all distances measure in pm)
    // we scale back when applying the movements to the actual objects

    float kb = 3.0f;    // should be integrated into new bondList structure
    float ka = 120.0f; // should be integrated into new angleList structure (must be this large ... or even larger!)
    float standardDistance = 154f; // integrate into new bondList
    float alphaNull = 109.4712f; // integrate into new angleList
    int frame = 0;

    // for Debugging; level = 100 only input coords + output movements
    //                level = 1000 more details on forces
    //                level = 10000 maximum detail level
    StreamWriter FFlog;
    int LogLevel = 0;

    // Start is called before the first frame update
    void Start()
    {
        if (LogLevel > 0)
        {
            FFlog = File.CreateText("logfile.txt");
            FFlog.WriteLine("ForceField logfile");
            FFlog.WriteLine("Log starts at " + Time.time.ToString("f6"));
            FFlog.WriteLine("LogLevel = " + LogLevel);
        }
        ;

        standardDistance = 154f * GetComponent<GlobalCtrl>().scale; // * scalingFactor;
        
    }
    void OnApplicationQuit()
    {
        if (LogLevel > 0)
        {
            FFlog.WriteLine("Log ends at " + Time.time.ToString("f6"));
            FFlog.Close();
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        frame += 1;
        if (LogLevel >= 100 && this.GetComponent<GlobalCtrl>().forceField)
        {
            FFlog.WriteLine("Current frame: " + frame);
            FFlog.WriteLine("Current time:  " + Time.time.ToString("f6") + "  Delta: " + Time.deltaTime.ToString("f6"));
        }
        // If the forcefield is active, update all connections and forces, else only update connections
        if (this.GetComponent<GlobalCtrl>().forceField)
        {

		    // COMMENT2a: how could we check that the lists need an update?
            bondList.Clear();
            angleList.Clear();
            generateLists();
            generateFF();
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
        // init lists
        atomList.Clear();
        atomMass.Clear();
        movement.Clear();
        position.Clear();
        nAtoms = 0;
        // cycle Atoms
        foreach(Atom At in GetComponent<GlobalCtrl>().list_curAtoms)
        {
            nAtoms++;
            atomList.Add(At._id);
            // TODO: put actual masses here (which should be part of Atom object)
            atomMass.Add(At.mass);
            // Get atoms and scale to new unit system (in pm)
            position.Add((At.transform.localPosition*(1f/ GetComponent<GlobalCtrl>().scale)));
            movement.Add(new Vector3(0.0f, 0.0f, 0.0f));
        }
        // TODO: when FF is not generated in each frame, we have to check that the atomList matches!
        if (LogLevel >= 100)
        {
            FFlog.WriteLine("Current positions:");
            for (int iAtom = 0; iAtom < nAtoms; iAtom++)
            {
                FFlog.WriteLine(string.Format(" {0,4:d}  -  {1,5:d}   {2,14:f6} {3,14:f6}  {4,14:f6}",
                    iAtom, atomList[iAtom],
                    position[iAtom].x, position[iAtom].y, position[iAtom].z));
            }
        }
    }


    void generateFF()
    {
        int iAtom = -1;
        //cycle through all atoms
        foreach(Atom c1 in GetComponent<GlobalCtrl>().list_curAtoms)
        {
            iAtom++;
            int jAtom = -1;
            //compare with all atoms
            foreach(Atom c2 in GetComponent<GlobalCtrl>().list_curAtoms)
            {
                jAtom++;
                //cycle through all connection points
                foreach(ConnectionStatus conPoint in c1.getAllConPoints())
                {
                    //if a connection exists, add to list
                    if(conPoint.otherAtomID == c2._id)
                    {
                        if(!bondList.Contains(new Vector2(iAtom, jAtom)) &&
                           !bondList.Contains(new Vector2(jAtom, iAtom)))
                        {
                            //angle list
                            foreach (Vector2 vec in bondList)
                            {
                                if (vec.x == iAtom)
                                {
                                    angleList.Add(new Vector3(vec.y, iAtom, jAtom));
                                } else if (vec.x == jAtom)
                                {
                                    angleList.Add(new Vector3(vec.y, jAtom, iAtom));
                                }
                                else if (vec.y == iAtom)
                                {
                                    angleList.Add(new Vector3(vec.x, iAtom, jAtom));
                                }
                                else if (vec.y == jAtom)
                                {
                                    angleList.Add(new Vector3(vec.x, jAtom, iAtom));
                                }
                            }
                            //bond list
                            bondList.Add(new Vector2(iAtom, jAtom));
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
        if (LogLevel >= 1000) FFlog.WriteLine("calcBondForces for {0} - {1}", (int)bond.x, (int)bond.y);
        //bond vector
        Vector3 rb = position[(int)bond.x] - position[(int)bond.y];
        //force on this bond vector
        float delta = rb.magnitude - standardDistance;
        //float fb = -kb * (Vector3.Magnitude(rb) - standardDistance);
        float fb = -kb * delta;
        if (LogLevel >= 1000) FFlog.WriteLine("dist: {0,12:f3}  dist0: {1,12:f3}  --  force = {2,14:f5} ",rb.magnitude,standardDistance,fb);
        //separate the forces on the two atoms
        Vector3 fc1 =  fb * (rb / Vector3.Magnitude(rb));
        Vector3 fc2 = -fb * (rb / Vector3.Magnitude(rb));

        if (LogLevel >= 10000)
        {
            FFlog.WriteLine(string.Format("force for atom {0,3}:  ( {1,14:f6} ; {2,14:f6} ; {3,14:f6} )", (int)bond.x, fc1.x, fc1.y, fc1.z));
            FFlog.WriteLine(string.Format("force for atom {0,3}:  ( {1,14:f6} ; {2,14:f6} ; {3,14:f6} )", (int)bond.y, fc2.x, fc2.y, fc2.z));
        }

        movement[(int)bond.x] += fc1;
        movement[(int)bond.y] += fc2;

        if (LogLevel >= 10000)
        {
            FFlog.WriteLine("Updated forces:");
            for (int iAtom = 0; iAtom < nAtoms; iAtom++)
            {
                FFlog.WriteLine(string.Format(" {0,4:d}  -  {1,5:d}   {2,14:f6} {3,14:f6}  {4,14:f6}",
                    iAtom, atomList[iAtom],
                    movement[iAtom].x, movement[iAtom].y, movement[iAtom].z));
            }
        }
    }

    // calculate angle forces
    void calcAngleForces(Vector3 angle)
    {
        if (LogLevel >= 1000) FFlog.WriteLine("calcAngleForces for {0} - {1} - {2}", (int)angle.x, (int)angle.y, (int)angle.z);
        Vector3 rb1 = position[(int)angle.x] - position[(int)angle.y];
        Vector3 rb2 = position[(int)angle.z] - position[(int)angle.y];

        float cosAlpha = (Vector3.Dot(rb1, rb2)) / (Vector3.Magnitude(rb1) * Vector3.Magnitude(rb2));
        //float angleAlpha = Mathf.Acos(cosAlpha) * (180 / Mathf.PI);
        float mAlpha = ka * (Mathf.Acos(cosAlpha) * (180.0f / Mathf.PI) - alphaNull);

        // check: rb1 and rb2 were mixed up in vector part
        Vector3 fI = (mAlpha / (Vector3.Magnitude(rb1) * Mathf.Sqrt(1.0f - cosAlpha * cosAlpha))) * ((rb2 / Vector3.Magnitude(rb2)) - cosAlpha * (rb1 / Vector3.Magnitude(rb1)));
        Vector3 fK = (mAlpha / (Vector3.Magnitude(rb2) * Mathf.Sqrt(1.0f - cosAlpha * cosAlpha))) * ((rb1 / Vector3.Magnitude(rb1)) - cosAlpha * (rb2 / Vector3.Magnitude(rb2)));
        Vector3 fJ = -fI - fK;

        if (LogLevel >= 1000) FFlog.WriteLine("angle: {0,12:f3}  angle0: {1,12:f3}  --  moment = {2,14:f5} ", Mathf.Acos(cosAlpha) * (180.0f / Mathf.PI), alphaNull, mAlpha);

        if (LogLevel >= 10000)
        {
            FFlog.WriteLine(string.Format("force for atom {0,3}:  ( {1,14:f6} ; {2,14:f6} ; {3,14:f6} )", (int)angle.x, fI.x, fI.y, fI.z));
            FFlog.WriteLine(string.Format("force for atom {0,3}:  ( {1,14:f6} ; {2,14:f6} ; {3,14:f6} )", (int)angle.y, fJ.x, fJ.y, fJ.z));
            FFlog.WriteLine(string.Format("force for atom {0,3}:  ( {1,14:f6} ; {2,14:f6} ; {3,14:f6} )", (int)angle.z, fK.x, fK.y, fK.z));
        }

        movement[(int)angle.x] += fI;
        movement[(int)angle.y] += fJ; //  fJ and fK were interchanged ....
        movement[(int)angle.z] += fK;

        if (LogLevel >= 10000)
        {
            FFlog.WriteLine("Updated forces:");
            for (int iAtom = 0; iAtom < nAtoms; iAtom++)
            {
                FFlog.WriteLine(string.Format(" {0,4:d}  -  {1,5:d}   {2,14:f6} {3,14:f6}  {4,14:f6}",
                    iAtom, atomList[iAtom],
                    movement[iAtom].x, movement[iAtom].y, movement[iAtom].z));
            }
        }

    }

    // turn forces into movements and apply sanity checks 
    void calcMovements()
    {
        
        if (LogLevel >= 1000)
        {
            FFlog.WriteLine("Computed forces and applicable masses:");
            for (int iAtom = 0; iAtom < nAtoms; iAtom++)
            {
                FFlog.WriteLine(string.Format(" {0,4:d}  -  {1,5:d}   {2,14:f6} {3,14:f6}  {4,14:f6}    m = {5,9:f3} ",
                    iAtom, atomList[iAtom],
                    movement[iAtom].x, movement[iAtom].y, movement[iAtom].z, atomMass[iAtom]));
            }
        }

        Vector3 CurCOM = new Vector3(0, 0, 0); // current center of mass
        
        foreach(var coord in position)
        {
            CurCOM += coord;  // times (relative) mass of the atom (to be implemented)
        }
        CurCOM = CurCOM / (float)nAtoms;


        // force -> momentum change: divide by mass
        // momentum change to position change: apply time factor
        float timeFactor = 2f;
        for (int iAtom = 0; iAtom < nAtoms; iAtom++)
        {
            // negative masses flag a fixed atom
            if (atomMass[iAtom] > 0.0f)
            {
                movement[iAtom] *= timeFactor/atomMass[iAtom];
            }
            else
            {
                movement[iAtom] = new Vector3(0.0f, 0.0f, 0.0f);
            }
        }

        // check angular momentum ... if required
        //Vector3 AngMom = new Vector3(0, 0, 0);
        //for (int iAtom = 0; iAtom < nAtoms; iAtom++)
        //{
        //    Vector3 vel = movement[iAtom];
        //    Vector3 pos = position[iAtom] - CurCOM; // maybe this can be done more cleanly
        //    AngMom.x += pos.y * vel.z; // times (relative) mass
        //    AngMom.y += pos.z * vel.x;
        //    AngMom.z += pos.x * vel.y;
        //}
        //
		// so far, we have computed the angular momentum of the structure, need now code to apply a damping of the rotation
		// AK will take care of that

        // check for too long steps:
        float MaxMove = 10f;
        float moveMaxNorm = 0f; // norm of movement vector
        for (int iAtom = 0; iAtom < nAtoms; iAtom++)
        {
            float moveNorm = Vector3.Magnitude(movement[iAtom]);
            moveMaxNorm = Mathf.Max(moveMaxNorm, moveNorm);
        }
        if (moveMaxNorm > MaxMove)
        {
            float scaleMove = MaxMove / moveMaxNorm;
            if (LogLevel >= 100) FFlog.WriteLine("moveMaxNorm was {0:f3} - scaling by {1:f10}", moveMaxNorm, scaleMove);
            
            for (int iAtom = 0; iAtom < nAtoms; iAtom++)
            {
                movement[iAtom] *= scaleMove;
            }
        }

        if (LogLevel >= 100)
        {
            FFlog.WriteLine("Computed movements:");
            for (int iAtom = 0; iAtom < nAtoms; iAtom++)
            {
                FFlog.WriteLine(string.Format(" {0,4:d}  -  {1,5:d}   {2,14:f6} {3,14:f6}  {4,14:f6}",
                    iAtom, atomList[iAtom],
                    movement[iAtom].x, movement[iAtom].y, movement[iAtom].z));
            }
        }


    }


    void applyMovements()
    {
        for (int iAtom = 0; iAtom < nAtoms; iAtom++)
        {
            // get atom identified and update the actual object
            // scale to Unity's unit system
            int atID = atomList[iAtom];
            getAtomByID(atID).transform.localPosition += movement[iAtom]* GetComponent<GlobalCtrl>().scale;            
        }
    }

    // connections between atoms get scaled new as soon as the position of an atom gets updated
    public void scaleConnections()
    {
        foreach(Atom atom in this.GetComponent<GlobalCtrl>().list_curAtoms)
        {
            foreach(ConnectionStatus carbonCP in atom.getAllConPoints())
            {
                if (carbonCP.isConnected)
                {
                    Atom carbonConnected = getAtomByID(carbonCP.otherAtomID);
                    float distance = Vector3.Distance(atom.transform.position, carbonConnected.transform.position);
                    Transform connection = GameObject.Find("con" + carbonCP.conID).transform;
                    connection.localScale = new Vector3(connection.localScale.x, connection.localScale.y, distance/2);
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
