using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EditMode : MonoBehaviour
{
    public bool editMode = false;
    public CarbonAtom fixedAtom;
    public CarbonAtom carbonConnected;
    public ConnectionStatus fixedCP;
    public ConnectionStatus otherCP;
    public ConnectionStatus carbonCP;
    public Transform nextCP;
    public Vector3 targetPoint;
    public int angleBetweenAtoms;
    float distance = 0;
    float carbonDist = 0;
    float standardDistance = 0.35f;
    float standardScale = 1;
    float distanceDiff = 0;
    float force = 0;
    float constantForceFormula = 1f;
    public Queue<int> moveNext = new Queue<int>();
    public List<int> alreadyMoved = new List<int>();

    private List<CarbonAtom> allCarbonAtoms = new List<CarbonAtom>();
    
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (editMode == true)
        {
            if (/*GameObject.Find("editTeil").transform.parent.name == "Molekül"*/true)
            {
                allCarbonAtoms = GameObject.Find("Camera").GetComponent<GlobalCtrl>().list_curCarbonAtoms;
                //forceField();
                applyForce(fixedAtom);
                alreadyMoved.Clear();
                
                foreach(ConnectionStatus cs in fixedAtom.getAllConPoints())
                {
                    if (cs.isConnected == true)
                    {
                        carbonConnected = GameObject.Find("kohlenstoff" + cs.otherAtomID).GetComponent<CarbonAtom>();
                        GameObject.Find("UI" + cs.name).transform.position = carbonConnected.transform.position + new Vector3(0, 0.1f, 0);
                        GameObject.Find("UI" + cs.name).transform.GetChild(0).GetComponent<Text>().text = showRotation(cs).ToString() + "°";
                    }
                }
            }
            else
            {
                //Alle Connection Punkte des Fixierten Atoms werden angeschaut und bei Abstandsveränderung wird die Verbindung neu skaliert
                foreach (ConnectionStatus fixedCP in fixedAtom.getAllConPoints())
                {
                    if (fixedCP.isConnected == true)
                    {
                        carbonConnected = GameObject.Find("kohlenstoff" + fixedCP.otherAtomID).GetComponent<CarbonAtom>();
                        if (carbonConnected.transform.parent.name == "editTeil")
                        {
                            otherCP = carbonConnected.getConPoint(fixedCP.otherPointID);
                            distance = Vector3.Distance(fixedAtom.transform.position, carbonConnected.transform.position);
                            distanceDiff = distance - standardDistance;
                            Transform connection = GameObject.Find("con" + fixedCP.conID).transform;
                            connection.localScale = new Vector3(connection.localScale.x, connection.localScale.y, standardScale + (distanceDiff * 2.5f));
                            connection.transform.position = fixedAtom.transform.position;
                            connection.transform.LookAt(carbonConnected.transform.position);

                        }
                        GameObject.Find("UI" + fixedCP.name).transform.position = carbonConnected.transform.position + new Vector3(0, 0.1f, 0);
                        GameObject.Find("UI" + fixedCP.name).transform.GetChild(0).GetComponent<Text>().text = showRotation(fixedCP).ToString() + "°";
                    }

                }
            }
        } else
        {
            GameObject.Find("UI0").transform.position = new Vector3(0, 0, 0);
            GameObject.Find("UI1").transform.position = new Vector3(0, 0, 0);
            GameObject.Find("UI2").transform.position = new Vector3(0, 0, 0);
            GameObject.Find("UI3").transform.position = new Vector3(0, 0, 0);
        }
    }

    public void regroupAtoms(CarbonAtom investigatedAtom)
    {
        if (investigatedAtom.transform.parent.name != "editTeil" && investigatedAtom != fixedAtom)
        {
            investigatedAtom.transform.parent = GameObject.Find("editTeil").transform;
            //Cycle through all 4 childs
            foreach (ConnectionStatus cs in investigatedAtom.getAllConPoints())
            {
                if (cs.isConnected == true)
                {
                    GameObject.Find("con" + cs.conID).transform.parent = GameObject.Find("editTeil").transform;
                    CarbonAtom next = GameObject.Find("kohlenstoff" + cs.otherAtomID).GetComponent<CarbonAtom>();
                    regroupAtoms(next);
                }
            }
        }
    }

    public void forceField()
    {        
        foreach(CarbonAtom atom in allCarbonAtoms)
        {
            if(GameObject.Find("Camera").GetComponent<GlobalCtrl>().atomMap.TryGetValue(atom._id, out Vector3 goal))
            {
                Vector3 move = goal - atom.transform.localPosition;
                atom.transform.localPosition += move * 0.05f;
            }
            

            foreach(ConnectionStatus carbonCP in atom.getAllConPoints())
            {
                if (carbonCP.isConnected)
                {
                    carbonConnected = GameObject.Find("kohlenstoff" + carbonCP.otherAtomID).GetComponent<CarbonAtom>();

                    distance = Vector3.Distance(atom.transform.position, carbonConnected.transform.position);
                    distanceDiff = distance - standardDistance;
                    Transform connection = GameObject.Find("con" + carbonCP.conID).transform;
                    connection.localScale = new Vector3(connection.localScale.x, connection.localScale.y, standardScale + (distanceDiff * 2.5f));
                    connection.transform.position = atom.transform.position;
                    connection.transform.LookAt(carbonConnected.transform.position);
                    //if(carbonConnected != fixedAtom)
                    //{


                    //    Vector3 endPos = atom.transform.localPosition + Quaternion.Euler(atom.transform.rotation.eulerAngles) * carbonCP.transform.localPosition.normalized * standardDistance;
                    //    //Vector3 endPos = atom.transform.position + Quaternion.Euler(atom.transform.rotation.eulerAngles) * ((carbonCP.transform.localPosition / carbonCP.transform.parent.localScale.x) * 0.1f);
                    //    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    //    cube.transform.parent = GameObject.Find("Molekül").transform;
                    //    cube.transform.localPosition = endPos;
                    //    cube.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                    //}
                }
            }
        }
    }

    public void applyForce(CarbonAtom atom)
    {
        //Einzelne Atome rekursiv aufrufen. Startpunkt: FixedAtom, von da aus über jedes Child die angrenzenden Atome mit force bewegen.
        //Breitensuche mit Queue verwenden
        foreach (ConnectionStatus nextCP in atom.getAllConPoints())
        {
            if (nextCP.isConnected == true && !(alreadyMoved.Contains(nextCP.otherAtomID)))
            {
                moveNext.Enqueue(nextCP.otherAtomID);
                alreadyMoved.Add(nextCP.otherAtomID);
            }
        }
        foreach (ConnectionStatus carbonCP in atom.getAllConPoints())
        {
            if (carbonCP.isConnected == true)
            {
                //Wenn größer als Mindestabstand - direkte Verbindung zu fixedAtom
                carbonConnected = GameObject.Find("kohlenstoff" + carbonCP.otherAtomID).GetComponent<CarbonAtom>();
                if (carbonConnected._id != fixedAtom._id)
                {
                    Vector3 carbonVec = carbonConnected.transform.localPosition - atom.transform.localPosition;
                    targetPoint = atom.transform.localPosition + ((standardDistance) * carbonVec.normalized);
                    carbonConnected.transform.localPosition += calculateForce(atom, carbonConnected) * (targetPoint - carbonConnected.transform.localPosition);
                }
                //Scale Connections
                distance = Vector3.Distance(atom.transform.position, carbonConnected.transform.position);
                distanceDiff = distance - standardDistance;
                Transform connection = GameObject.Find("con" + carbonCP.conID).transform;
                connection.localScale = new Vector3(connection.localScale.x, connection.localScale.y, standardScale + (distanceDiff * 2.5f));
                connection.transform.position = atom.transform.position;
                connection.transform.LookAt(carbonConnected.transform.position);
            }
        }
        foreach (GameObject otherGO in GameObject.FindGameObjectsWithTag("Kohlenstoff"))
        {
            CarbonAtom otherAtom = otherGO.GetComponent<CarbonAtom>();
            distance = Vector3.Distance(atom.transform.localPosition, otherAtom.transform.localPosition);
            //Wenn kleiner als Mindestabstand und fixedAtom nicht beteiligt
            if ((distance < standardDistance) && (otherAtom._id != fixedAtom._id) && (atom._id != fixedAtom._id))
            {
                Vector3 carbonVec = otherAtom.transform.localPosition - atom.transform.localPosition;
                targetPoint = atom.transform.localPosition + ((standardDistance) * carbonVec.normalized);
                otherAtom.transform.localPosition += 0.5f * calculateForce(atom, otherAtom) * (targetPoint - otherAtom.transform.localPosition);
                atom.transform.localPosition -= 0.5f * calculateForce(atom, otherAtom) * (targetPoint - otherAtom.transform.localPosition);
            }
            // Wenn kleiner als Mindestabstand und atom = fixedAtom
            else if ((distance < standardDistance) && (atom._id == fixedAtom._id))
            {
                Vector3 carbonVec = otherAtom.transform.localPosition - atom.transform.localPosition;
                targetPoint = atom.transform.localPosition + ((standardDistance) * carbonVec.normalized);
                otherAtom.transform.localPosition += calculateForce(atom, otherAtom) * (targetPoint - otherAtom.transform.localPosition);
            }
            // Wenn kleiner als Mindestabstand und verbundenes Atom = fixedAtom
            else if ((distance < standardDistance) && (otherAtom._id == fixedAtom._id))
            {
                Vector3 carbonVec = otherAtom.transform.localPosition - atom.transform.localPosition;
                targetPoint = atom.transform.localPosition + ((standardDistance) * carbonVec.normalized);
                atom.transform.localPosition += calculateForce(atom, otherAtom) * (targetPoint - otherAtom.transform.localPosition);
            }
        }
        if (moveNext.Count > 0)
        {
            applyForce(GameObject.Find("kohlenstoff" + moveNext.Dequeue()).GetComponent<CarbonAtom>());
        }

    }

    public float calculateForce(CarbonAtom atom, CarbonAtom carbonConnected)
    {
        carbonDist = Vector3.Distance(targetPoint, carbonConnected.transform.position);
        force = 0.5f * constantForceFormula * Mathf.Pow(carbonDist, 2);
        if (force < 0.025f)
        {
            force = 0.025f;
        }
        if (carbonDist < 0)
        {
            force = -force;
        }
        return force;
    }

    public int showRotation(ConnectionStatus conPoint)
    {
        angleBetweenAtoms = 0;
        if (conPoint.isConnected == true)
        {
            ConnectionStatus point;
            ConnectionStatus otherPoint;
            CarbonAtom conAtom = GameObject.Find("kohlenstoff" + conPoint.otherAtomID).GetComponent<CarbonAtom>();
            Vector3 vecPlane = conAtom.transform.position - fixedAtom.transform.position;
            if (fixedAtom.getConPoint(0) != conPoint)
            {
                point = fixedAtom.getConPoint(0);
            }
            else
            {
                point = fixedAtom.getConPoint(1);
            }
            Vector3 vecPoint = point.transform.position - fixedAtom.transform.position;
            vecPoint = Vector3.ProjectOnPlane(vecPoint, vecPlane);

            if (conAtom.getConPoint(0).otherAtomID != fixedAtom._id)
            {
                otherPoint = conAtom.getConPoint(0);
            }
            else
            {
                otherPoint = conAtom.getConPoint(1);
            }
            Vector3 otherVec = otherPoint.transform.position - conAtom.transform.position;
            otherVec = Vector3.ProjectOnPlane(otherVec, vecPlane);
            angleBetweenAtoms = Mathf.RoundToInt(Vector3.Angle(otherVec, vecPoint));
            if (conAtom.GetComponent<SenseGlove_Grabable>().IsGrabbed() == false)
            {
                
                if (Mathf.RoundToInt(angleBetweenAtoms) % 10 != 0)
                {
                    int rest = angleBetweenAtoms % 10;
                    conAtom.transform.RotateAround(conAtom.transform.position, vecPlane, rest);
                    vecPoint = point.transform.position - fixedAtom.transform.position;
                    vecPoint = Vector3.ProjectOnPlane(vecPoint, vecPlane);
                    otherVec = otherPoint.transform.position - conAtom.transform.position;
                    otherVec = Vector3.ProjectOnPlane(otherVec, vecPlane);
                    angleBetweenAtoms = Mathf.RoundToInt(Vector3.Angle(otherVec, vecPoint));
                    if(angleBetweenAtoms % 10 != 0)
                    {
                        conAtom.transform.RotateAround(conAtom.transform.position, vecPlane, -2*rest);
                    }
                    if(rest > 5)
                    {
                        conAtom.transform.RotateAround(conAtom.transform.position, vecPlane, 10);
                    }
                    vecPoint = point.transform.position - fixedAtom.transform.position;
                    vecPoint = Vector3.ProjectOnPlane(vecPoint, vecPlane);
                    otherVec = otherPoint.transform.position - conAtom.transform.position;
                    otherVec = Vector3.ProjectOnPlane(otherVec, vecPlane);
                    angleBetweenAtoms = Mathf.RoundToInt(Vector3.Angle(otherVec, vecPoint));
                }
                
            }
            return Mathf.RoundToInt(angleBetweenAtoms);
        }
        return 0;
    }
}
