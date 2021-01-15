using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System;
using UnityEngine;

public class ForceField : MonoBehaviour
{
    public struct BondTerm
    {
        public int Atom1; public int Atom2; public float kBond; public float Req;
    }
    public List<BondTerm> bondList = new List<BondTerm>();

    struct AngleTerm
    {
        public int Atom1; public int Atom2; public int Atom3; public float kAngle; public float Aeq;
    }
    List<AngleTerm> angleList = new List<AngleTerm>();

    //public List<Vector2> bondList = new List<Vector2>();
    //public List<Vector3> angleList = new List<Vector3>();
    
    List<int> atomList = new List<int>();
    List<float> atomMass = new List<float>();
    List<string> atomType = new List<string>();
    List<Vector3> position = new List<Vector3>();
    List<Vector3> forces = new List<Vector3>();
    public List<Vector3> movement = new List<Vector3>();
    int nAtoms;

    //float scalingFactor = 154 / (154 / GetComponent<GlobalCtrl>().scale); // with this, 154 pm are equivalent to 0.35 m in the model
    // note that the forcefield works in the atomic scale (i.e. all distances measure in pm)
    // we scale back when applying the movements to the actual objects
    float scalingfactor;
    float timeFactor = 0.25f;

    // constants for bond terms
    const float kb = 3.0f;    // standard value
    const float kbCC = 4.0f;
    const float kbCH = 6.0f;
    const float reqStd = 154f; // standard value
    const float reqCC = 154f;
    const float reqCH = 108f;
    const float reqHH = 78f;

    const float kbCX = 10.0f;
    const float reqCX = 80f;
    // constants for angle terms
    float ka = 360.0f; // standard value (must be this large ... or even larger!)
    //float standardDistance = 154f; // integrate into new bondList
    float alphaNull = 109.4712f; // integrate into new angleList

    // counter for frames (debug only)
    int frame = 0;

    // for Debugging; level = 100 only input coords + output movements
    //                level = 1000 more details on forces
    //                level = 10000 maximum detail level
    StreamWriter FFlog;
    const int LogLevel = 000;

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
        scalingfactor = GetComponent<GlobalCtrl>().scale / 154f;
        
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

		    // COMMENT2a: how could we check that the FF needs an update
            generateLists();
            generateFF();
            applyFF();
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
        atomType.Clear();
        movement.Clear();
        forces.Clear();
        position.Clear();
        nAtoms = 0;
        // cycle Atoms
        foreach(Atom At in GetComponent<GlobalCtrl>().list_curAtoms)
        {
            nAtoms++;
            atomList.Add(At._id);
            atomMass.Add(At.mass);
            atomType.Add(At.type);
            // Get atoms and scale to new unit system (in pm)
            position.Add((At.transform.localPosition*(1f/ scalingfactor)));
            forces.Add(new Vector3(0.0f, 0.0f, 0.0f));
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
        bondList.Clear();
        angleList.Clear();
        // set topology array
        bool[,] topo = new bool[nAtoms, nAtoms];
        for (int iAtom = 0; iAtom < nAtoms; iAtom++)
        {
            for (int jAtom = 0; jAtom < nAtoms; jAtom++)
            {
                topo[iAtom, jAtom] = false;
            }
        }

        {
            int iAtom = 0;
            foreach (Atom At1 in GetComponent<GlobalCtrl>().list_curAtoms)
            {
                // cycle through connection points
                foreach (ConnectionStatus conPoint in At1.getAllConPoints())
                {
                    // get current atom index by comparison to entries in atomList
                    int jAtom = -1;
                    for (int kAtom = 0; kAtom < nAtoms; kAtom++)
                    {
                        if (atomList[kAtom] == conPoint.otherAtomID)
                        {
                            jAtom = kAtom;
                            break;
                        }
                    }
                    if (jAtom >= 0)
                    {
                        topo[iAtom, jAtom] = true;
                        topo[jAtom, iAtom] = true;
                    }
                }
                iAtom++;
            }
        }

        // now set all FF terms
        // pairwise terms, run over unique atom pairs
        for (int iAtom = 0; iAtom<nAtoms; iAtom++)
        {
            for (int jAtom = 0; jAtom < iAtom; jAtom++)
            {
                if (topo[iAtom, jAtom])
                {
                    BondTerm newBond = new BondTerm();
                    newBond.Atom1 = jAtom;
                    newBond.Atom2 = iAtom;
                    if (atomType[iAtom] == "C" && atomType[jAtom] == "C")
                    {
                        newBond.kBond = kbCC;
                        newBond.Req = reqCC;
                    }
                    else if (atomType[iAtom] == "C" && atomType[jAtom] == "H" ||
                             atomType[iAtom] == "H" && atomType[jAtom] == "C")
                    {
                        newBond.kBond = kbCH;
                        newBond.Req = reqCH;
                    }
                    // RENEW THIS LATER
                    else if (atomType[iAtom] == "C" && atomType[jAtom] == "DUMMY" ||
                             atomType[iAtom] == "DUMMY" && atomType[jAtom] == "C")
                    {
                        newBond.kBond = kbCH;
                        newBond.Req = reqCH;
                    }
                    else if (atomType[iAtom] == "H" && atomType[jAtom] == "H")
                    {
                        newBond.kBond = kb;
                        newBond.Req = reqHH;
                    }
                    else // take defaults for the time being
                    {
                        newBond.kBond = kb;
                        newBond.Req = reqStd;
                    }
                    bondList.Add(newBond);
                }
                // else ... set here non-bonded interactions
            }

        }
        if (LogLevel >= 1000)
        {
            FFlog.WriteLine("Bond terms:");
            FFlog.WriteLine(" Atom1  Atom2      kBond           Req");
            foreach (BondTerm bond in bondList)
            {
                FFlog.WriteLine(string.Format(" {0,4} - {1,4}   {2,12:f3}  {3,12:f3}",
                    bond.Atom1,bond.Atom2,bond.kBond,bond.Req));
            }
        }

        // angle terms
        // run over unique bond pairs
        foreach (BondTerm bond1 in bondList)
        {
            foreach(BondTerm bond2 in bondList)
            {
                // if we reached the same atom pair, we can skip
                if (bond1.Atom1 == bond2.Atom1 && bond1.Atom2 == bond2.Atom2) break;

                int idx=-1, jdx=-1, kdx=-1;
                if (bond1.Atom1 == bond2.Atom1)
                {
                    idx = bond1.Atom2; jdx = bond1.Atom1; kdx = bond2.Atom2;
                }
                else if (bond1.Atom1 == bond2.Atom2)
                {
                    idx = bond1.Atom2; jdx = bond1.Atom1; kdx = bond2.Atom1;
                }
                else if (bond1.Atom2 == bond2.Atom1)
                {
                    idx = bond1.Atom1; jdx = bond1.Atom2; kdx = bond2.Atom2;
                }
                else if (bond1.Atom2 == bond2.Atom2)
                {
                    idx = bond1.Atom1; jdx = bond1.Atom2; kdx = bond2.Atom1;
                }
                if (idx>-1) // if anything was found: set term
                {
                    AngleTerm newAngle = new AngleTerm();
                    newAngle.Atom1 = kdx;  // I put kdx->Atom1 and idx->Atom3 just for aesthetical reasons ;)
                    newAngle.Atom2 = jdx;
                    newAngle.Atom3 = idx;
                    // currently only this angle type:
                    newAngle.kAngle = ka;
                    newAngle.Aeq = alphaNull;
                    angleList.Add(newAngle);
                }
            }
        }
        if (LogLevel >= 1000)
        {
            FFlog.WriteLine("Angle terms:");
            FFlog.WriteLine(" Atom1  Atom2  Atom3    kAngle           Aeq");
            foreach (AngleTerm angle in angleList)
            {
                FFlog.WriteLine(string.Format(" {0,4} - {1,4} - {2,4}  {3,12:f3}  {4,12:f3}",
                    angle.Atom1, angle.Atom2, angle.Atom3, angle.kAngle, angle.Aeq));
            }
        }



    }

    // evaluate the ForceField and compute update of positions
    // to enhance stability, do more than one timestep for each actual update
    // in applyMovements, finally the actual objects are updated
    void applyFF()
    {
        int nTimeSteps = 4;
        for (int istep = 0; istep < nTimeSteps; istep++)
        {
            //Loop Bond List
            foreach (BondTerm bond in bondList)
            {
                calcBondForces(bond);
            }

            //Loop Angle List
            foreach (AngleTerm angle in angleList)
            {
                calcAngleForces(angle);
            }

            calcMovements();
        }
        applyMovements();
    }

    // calculate bond forces
    void calcBondForces(BondTerm bond)
    {
        if (LogLevel >= 1000) FFlog.WriteLine("calcBondForces for {0} - {1}", (int)bond.Atom1, (int)bond.Atom2);
        //bond vector
        Vector3 rb = position[bond.Atom1] - position[bond.Atom2];
        //force on this bond vector
        float delta = rb.magnitude - bond.Req;
        float fb = -bond.kBond * delta;
        if (LogLevel >= 1000) FFlog.WriteLine("dist: {0,12:f3}  dist0: {1,12:f3}  --  force = {2,14:f5} ",rb.magnitude,bond.Req,fb);
        //separate the forces on the two atoms
        Vector3 fc1 =  fb * (rb / Vector3.Magnitude(rb)); // could use rb.normalized
        Vector3 fc2 = -fb * (rb / Vector3.Magnitude(rb));

        if (LogLevel >= 10000)
        {
            FFlog.WriteLine(string.Format("force for atom {0,3}:  ( {1,14:f6} ; {2,14:f6} ; {3,14:f6} )", bond.Atom1, fc1.x, fc1.y, fc1.z));
            FFlog.WriteLine(string.Format("force for atom {0,3}:  ( {1,14:f6} ; {2,14:f6} ; {3,14:f6} )", bond.Atom2, fc2.x, fc2.y, fc2.z));
        }

        forces[bond.Atom1] += fc1;
        forces[bond.Atom2] += fc2;

        if (LogLevel >= 10000)
        {
            FFlog.WriteLine("Updated forces:");
            for (int iAtom = 0; iAtom < nAtoms; iAtom++)
            {
                FFlog.WriteLine(string.Format(" {0,4:d}  -  {1,5:d}   {2,14:f6} {3,14:f6}  {4,14:f6}",
                    iAtom, atomList[iAtom],
                    forces[iAtom].x, forces[iAtom].y, forces[iAtom].z));
            }
        }
    }

    // calculate angle forces
    void calcAngleForces(AngleTerm angle)
    {
        if (LogLevel >= 1000) FFlog.WriteLine("calcAngleForces for {0} - {1} - {2}", angle.Atom1, angle.Atom2, angle.Atom3);
        Vector3 rb1 = position[angle.Atom1] - position[angle.Atom2];
        Vector3 rb2 = position[angle.Atom3] - position[angle.Atom2];

        float cosAlpha = (Vector3.Dot(rb1, rb2)) / (Vector3.Magnitude(rb1) * Vector3.Magnitude(rb2));
        //float angleAlpha = Mathf.Acos(cosAlpha) * (180 / Mathf.PI);
        float mAlpha = angle.kAngle * (Mathf.Acos(cosAlpha) * (180.0f / Mathf.PI) - angle.Aeq);

        Vector3 fI = (mAlpha / (Vector3.Magnitude(rb1) * Mathf.Sqrt(1.0f - cosAlpha * cosAlpha))) * ((rb2 / Vector3.Magnitude(rb2)) - cosAlpha * (rb1 / Vector3.Magnitude(rb1)));
        Vector3 fK = (mAlpha / (Vector3.Magnitude(rb2) * Mathf.Sqrt(1.0f - cosAlpha * cosAlpha))) * ((rb1 / Vector3.Magnitude(rb1)) - cosAlpha * (rb2 / Vector3.Magnitude(rb2)));
        Vector3 fJ = -fI - fK;

        if (LogLevel >= 1000) FFlog.WriteLine("angle: {0,12:f3}  angle0: {1,12:f3}  --  moment = {2,14:f5} ", Mathf.Acos(cosAlpha) * (180.0f / Mathf.PI), angle.Aeq, mAlpha);

        if (LogLevel >= 10000)
        {
            FFlog.WriteLine(string.Format("force for atom {0,3}:  ( {1,14:f6} ; {2,14:f6} ; {3,14:f6} )", angle.Atom1, fI.x, fI.y, fI.z));
            FFlog.WriteLine(string.Format("force for atom {0,3}:  ( {1,14:f6} ; {2,14:f6} ; {3,14:f6} )", angle.Atom2, fJ.x, fJ.y, fJ.z));
            FFlog.WriteLine(string.Format("force for atom {0,3}:  ( {1,14:f6} ; {2,14:f6} ; {3,14:f6} )", angle.Atom3, fK.x, fK.y, fK.z));
        }

        forces[angle.Atom1] += fI;
        forces[angle.Atom2] += fJ; 
        forces[angle.Atom3] += fK;

        if (LogLevel >= 10000)
        {
            FFlog.WriteLine("Updated forces:");
            for (int iAtom = 0; iAtom < nAtoms; iAtom++)
            {
                FFlog.WriteLine(string.Format(" {0,4:d}  -  {1,5:d}   {2,14:f6} {3,14:f6}  {4,14:f6}",
                    iAtom, atomList[iAtom],
                    forces[iAtom].x, forces[iAtom].y, forces[iAtom].z));
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
                    forces[iAtom].x, forces[iAtom].y, forces[iAtom].z, atomMass[iAtom]));
            }
        }


        // force -> momentum change: divide by mass
        // momentum change to position change: apply time factor
        for (int iAtom = 0; iAtom < nAtoms; iAtom++)
        {
            // negative masses flag a fixed atom
            if (atomMass[iAtom] > 0.0f)
            {
                forces[iAtom] *= timeFactor/atomMass[iAtom];
            }
            else
            {
                forces[iAtom] = new Vector3(0.0f, 0.0f, 0.0f);
            }
        }


        // check for too long steps:
        float MaxMove = 10f;
        float moveMaxNorm = 0f; // norm of movement vector
        for (int iAtom = 0; iAtom < nAtoms; iAtom++)
        {
            float moveNorm = Vector3.Magnitude(forces[iAtom]);
            moveMaxNorm = Mathf.Max(moveMaxNorm, moveNorm);
        }
        if (moveMaxNorm > MaxMove)
        {
            float scaleMove = MaxMove / moveMaxNorm;
            if (LogLevel >= 100) FFlog.WriteLine("moveMaxNorm was {0:f3} - scaling by {1:f10}", moveMaxNorm, scaleMove);
            
            for (int iAtom = 0; iAtom < nAtoms; iAtom++)
            {
                forces[iAtom] *= scaleMove;
            }
        }

        if (LogLevel >= 100)
        {
            FFlog.WriteLine("Computed movements:");
            for (int iAtom = 0; iAtom < nAtoms; iAtom++)
            {
                FFlog.WriteLine(string.Format(" {0,4:d}  -  {1,5:d}   {2,14:f6} {3,14:f6}  {4,14:f6}",
                    iAtom, atomList[iAtom],
                    forces[iAtom].x, forces[iAtom].y, forces[iAtom].z));
            }
        }

        // update position and total movement:
        for (int iAtom = 0; iAtom < nAtoms; iAtom++)
        {
            movement[iAtom] += forces[iAtom];
            position[iAtom] += forces[iAtom];
        }

    }


    void applyMovements()
    {
        for (int iAtom = 0; iAtom < nAtoms; iAtom++)
        {
            // get atom identified and update the actual object
            // scale to Unity's unit system
            int atID = atomList[iAtom];
            getAtomByID(atID).transform.localPosition += movement[iAtom]* scalingfactor;
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
                    print(carbonConnected.type);
                    if(carbonConnected.type == "DUMMY")
                    {
                        //Fehler hier
                        print(carbonCP.conID);
                        Transform connection = GameObject.Find("dummycon" + carbonCP.conID).transform;
                        connection.localScale = new Vector3(connection.localScale.x, connection.localScale.y, distance / 2);
                        connection.transform.position = atom.transform.position;
                        connection.transform.LookAt(carbonConnected.transform.position);
                    } else
                    {
                        Transform connection = GameObject.Find("con" + carbonCP.conID).transform;
                        connection.localScale = new Vector3(connection.localScale.x, connection.localScale.y, distance / 2);
                        connection.transform.position = atom.transform.position;
                        connection.transform.LookAt(carbonConnected.transform.position);
                    }


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
