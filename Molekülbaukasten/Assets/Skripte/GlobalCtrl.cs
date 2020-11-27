using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public struct connectInfo
{
    public bool isConnected;
    public int otherAtomID;
    public int otherPointID;
}

public struct atomData
{
    public int id;
    public connectInfo info0;
    public connectInfo info1;
    public connectInfo info2;
    public connectInfo info3;
    public Vector3 pos;
    public Vector3 rot;
}

public class GlobalCtrl : MonoBehaviour
{
    public Object[] allGameObjects;
    private int _id;
    private static int _conID;
    private static GlobalCtrl instance;
    public static GlobalCtrl Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<GlobalCtrl>();
            return instance;
        }
    }
    
    public static int updown = 0;
    private static int openSize = 0;
    private List<string> open = new List<string>();
    public List<atomData> list_atomData = new List<atomData>();
    public List<Atom> list_curAtoms = new List<Atom>();
    public GameObject KohlenstoffPrefab;
    public GameObject VerbindungCC;

    private GameObject molecule;
    private GameObject up;
    private GameObject down;
    private Vector3[,] position;
    public FileInfo[] info;

    public Text changingText;
    public static bool loadGUI = false;

    Atom otherAtom;
    ConnectionStatus otherCP;
    public ConnectionStatus childSelected;
    public ConnectionStatus childGrabbedSelected;
    public float scale;
    public float winkelDiff;
    public bool allAtom = true;
    public bool forceField = false;

    public Dictionary<int, Vector3> atomMap = new Dictionary<int, Vector3>();

    float standardDistance = 0.35f;

    // Start is called before the first frame update
    void Start()
    {
        //position = new Vector3[1000, 1000];
        scale = 0.1f;
        allGameObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject));
        foreach (GameObject obj in allGameObjects)
        {
            if (obj.name == "Molekül")
            {
                molecule = obj;
            }
            else if (obj.name == "Up")
            {
                up = obj;
            }
            else if (obj.name == "Down")
            {
                down = obj;
            } else if (obj.name.StartsWith("atom"))
            {
                list_curAtoms.Add(obj.GetComponent<Atom>());
            }
        }
        DirectoryInfo dir = new DirectoryInfo(Application.dataPath + "/MoleculeFiles/");
        info = dir.GetFiles("*.*");

    }

    // Update is called once per frame
    void Update()
    {
        if (loadGUI == true)
        {
            if (updown < 1)
                up.SetActive(false);
            else
                up.SetActive(true);

            if (updown > openSize - 5)
                down.SetActive(false);
            else
                down.SetActive(true);

            for (int i = 0; i <= 3; i++)
            {
                if (open.Count > 0)
                {
                    if (open[i + updown] != "")
                        GameObject.Find("GUILoad").transform.GetChild(i).GetChild(0).GetComponent<Text>().text = open[i + updown].Substring(open[i + updown].LastIndexOf("\\") + 1);

                }

            }

        }
    }

    public void createCarbon(Vector3 pos)
    {   
        _id += 1;
        GameObject kohlenstoffatom = Instantiate(KohlenstoffPrefab, new Vector3(0, 1, 0.5f), Quaternion.identity);
        kohlenstoffatom.GetComponent<Atom>().f_InitCarbon(_id);
        kohlenstoffatom.transform.position = pos + new Vector3(0, 0, 0.2f);
        list_curAtoms.Add(kohlenstoffatom.GetComponent<Atom>());
    }
    public void createHydrogen(Vector3 pos)
    {
        _id += 1;
        GameObject wasserstoffatom = Instantiate(KohlenstoffPrefab, new Vector3(0, 1, 0.5f), Quaternion.identity);
        wasserstoffatom.GetComponent<Atom>().f_InitHydrogen(_id);
        wasserstoffatom.transform.position = pos + new Vector3(0, 0, 0.2f);
        list_curAtoms.Add(wasserstoffatom.GetComponent<Atom>());
    }

    public void createConnection(List<Atom> senden)
    {
        _conID += 1;
        senden[0].transform.parent = GameObject.Find("Molekül").transform;
        senden[1].transform.parent = GameObject.Find("Molekül").transform;
        //set position here
        foreach (ConnectionStatus childofGrabbedLoop in senden[1].getAllConPoints())
        {
            if (childofGrabbedLoop.isConnected == false)
            {
                foreach (ConnectionStatus childLoop in senden[0].getAllConPoints())
                {
                    if (childLoop.isConnected == false)
                    {
                        if (childGrabbedSelected == null || childSelected == null)
                        {
                            childGrabbedSelected = childofGrabbedLoop;
                            childSelected = childLoop;
                        }
                        if (Vector3.Distance(childLoop.transform.position, childofGrabbedLoop.transform.position) <= Vector3.Distance(childGrabbedSelected.transform.position, childSelected.transform.position))
                        {
                            childGrabbedSelected = childofGrabbedLoop;
                            childSelected = childLoop;
                        }
                    }
                }
            }
        }
        //newly addes atom gets rotated to vector, it's child object is shown as connected
        childGrabbedSelected.isConnected = true;
        childSelected.isConnected = true;

        // positioning of the linked atoms
        senden[1].transform.position = senden[0].transform.position + Quaternion.Euler(senden[0].transform.rotation.eulerAngles) * ((childSelected.transform.localPosition / childSelected.transform.parent.localScale.x) * scale);
        Vector3 direction = childSelected.transform.position - senden[1].transform.position;
        Quaternion rotation = Quaternion.FromToRotation(childGrabbedSelected.transform.localPosition, direction);
        senden[1].transform.rotation = rotation;

        //create the visual connection between them
        childGrabbedSelected.gameObject.GetComponent<Renderer>().material.color = Color.clear;
        childSelected.gameObject.GetComponent<Renderer>().material.color = Color.clear;
        GameObject connection = Instantiate(VerbindungCC, senden[0].transform.position, Quaternion.identity);
        connection.transform.LookAt(senden[1].transform.position);
        connection.transform.parent = GameObject.Find("Molekül").transform;
        connection.transform.name = "con" + _conID;
        float distance = Vector3.Distance(childSelected.transform.position, childGrabbedSelected.transform.position);
        if (GameObject.Find("Molekül").GetComponent<EditMode>().editMode == true)
        {
            connection.transform.localScale = new Vector3(connection.transform.localScale.x, connection.transform.localScale.y, connection.transform.localScale.z + (distance * 1));
        }
        //additional information is set, so each atom knows the id of it's connected atoms
        childSelected.otherAtomID = senden[1].GetComponent<Atom>()._id;
        childGrabbedSelected.otherAtomID = senden[0].GetComponent<Atom>()._id;
        childSelected.conID = _conID;
        childGrabbedSelected.conID = _conID;
        int.TryParse(childGrabbedSelected.name, out childSelected.otherPointID);
        int.TryParse(childSelected.name, out childGrabbedSelected.otherPointID);

        //reset used variables
        childGrabbedSelected = null;
        childSelected = null;
        //Alle Atome und deren Abstände zu den Nachbarn berechnen
        //for (int i = 0; i <= list_curAtoms.Count - 1; i++)
        //{
        //    for (int j = 0; j <= list_curAtoms.Count - 1; j++)
        //    {
        //        position[i, j] = list_curAtoms[j].transform.position - list_curAtoms[i].transform.position;
        //    }
        //}
    }

    public List<atomData> saveMolecule()
    {
        // add each atom and it's information to the list, which is then saved as XML
        foreach (Atom child in molecule.GetComponentsInChildren<Atom>())
        {
            atomData a;
            a.id = child._id;
            a.info0.isConnected = child.c0.isConnected;
            a.info1.isConnected = child.c1.isConnected;
            a.info2.isConnected = child.c2.isConnected;
            a.info3.isConnected = child.c3.isConnected;

            a.info0.otherAtomID = child.c0.otherAtomID;
            a.info1.otherAtomID = child.c1.otherAtomID;
            a.info2.otherAtomID = child.c2.otherAtomID;
            a.info3.otherAtomID = child.c3.otherAtomID;

            a.info0.otherPointID = child.c0.otherPointID;
            a.info1.otherPointID = child.c1.otherPointID;
            a.info2.otherPointID = child.c2.otherPointID;
            a.info3.otherPointID = child.c3.otherPointID;
            a.pos = child.transform.position;
            a.rot = child.transform.eulerAngles;
            list_atomData.Add(a);
        }

        return list_atomData;
    }

    public void loadMolecule(List<atomData> list)
    {
        _conID = 0;
        //reset molecule
        loadGUI = false;
        molecule.SetActive(true);
        GameObject.Find("GUILoad").SetActive(false);
        destroyMolecule();
        list_curAtoms.Clear();
        //get the atom data for each atom in the saved list and instantiate the atoms with their data
        foreach (atomData atom in list)
        {
            GameObject atomObj = Instantiate(KohlenstoffPrefab, atom.pos, Quaternion.Euler(atom.rot));
            Atom atomDef = atomObj.GetComponent<Atom>();
            //NEEDS REWORK HERE BECAUSE OF NEW ATOM STRUCTURE

            //atomDef.f_Init(atom.id);
            atomDef.transform.parent = GameObject.Find("Molekül").transform;
            atomDef.c0.isConnected = atom.info0.isConnected;
            atomDef.c1.isConnected = atom.info1.isConnected;
            atomDef.c2.isConnected = atom.info2.isConnected;
            atomDef.c3.isConnected = atom.info3.isConnected;

            atomDef.c0.otherAtomID = atom.info0.otherAtomID;
            atomDef.c1.otherAtomID = atom.info1.otherAtomID;
            atomDef.c2.otherAtomID = atom.info2.otherAtomID;
            atomDef.c3.otherAtomID = atom.info3.otherAtomID;

            atomDef.c0.otherPointID = atom.info0.otherPointID;
            atomDef.c1.otherPointID = atom.info1.otherPointID;
            atomDef.c2.otherPointID = atom.info2.otherPointID;
            atomDef.c3.otherPointID = atom.info3.otherPointID;
            list_curAtoms.Add(atomDef);
            foreach(ConnectionStatus c in atomDef.getAllConPoints())
            {
                c.conID = -1;
            }
        }

        //loop all atoms and their connections, if they have a connection --> create it
        int tempID = 0;
        foreach (Atom atom in list_curAtoms)
        {
            foreach (ConnectionStatus cp in atom.getAllConPoints())
            {
                if (cp.isConnected == true)
                {
                    
                    // find connected atom: Find atom p, where p.id = otherAtomID
                    otherAtom = list_curAtoms.Find(p=>p._id==cp.otherAtomID);
                    otherCP = otherAtom.getConPoint(cp.otherPointID);
                    //if the connection hasn't already been created in the same loop because it was part of the "other atom" before, create it now
                    if (cp.conID == -1)
                    {
                        _conID += 1;
                        GameObject connection = Instantiate(VerbindungCC, atom.transform.position, Quaternion.identity);
                        connection.transform.LookAt(otherAtom.transform.position);
                        connection.transform.parent = GameObject.Find("Molekül").transform;
                        connection.transform.name = "con" + _conID;
                        float distance = Vector3.Distance(atom.transform.position, otherAtom.transform.position);
                        connection.transform.localScale = new Vector3(connection.transform.localScale.x, connection.transform.localScale.y, connection.transform.localScale.z + ((distance - 0.35f) * 1));
                        cp.conID = _conID;
                        otherCP.conID = _conID;
                        cp.gameObject.SetActive(false);
                        otherCP.gameObject.SetActive(false);
                    }
                }
                // set full attribute if needed
                if (atom.c0.isConnected == true && atom.c1.isConnected == true && atom.c2.isConnected == true && atom.c3.isConnected == true)
                {
                    atom.isFull = true;
                }
            }
            // update atomID
            if(atom._id > tempID)
            {
                tempID = atom._id;
            }
        }
        //replace atom ID, if a new atom is created now, the ID starts at _id+1, so that each atomID is unique
        _id = tempID;
    }

    public void loadGUILoad()
    {
        open.Clear();
        //get all Paths of the XML Files
        foreach (FileInfo file in info)
        {
            if (file.ToString().Contains(".xml.meta") == false)
            {
                open.Add(file.ToString());
            }
        }
        while (open.Count < 4)
        {
            open.Add("NO MORE FILES");
        }
        openSize = open.Count;
        loadGUI = true;
    }

    public void textChange(GameObject key)
    {
        //text typed on the virtual keyboard by the user is put together to one string here
        //different special cases with the standard case, just adding the character, below
        changingText.text = GameObject.Find("GUISave").transform.GetChild(0).GetComponent<Text>().text;
        string s = key.name;
        if (changingText.text == "Name eingeben")
        {
            changingText.text = "";
        }
        if (s == "Delete")
        {
            if (changingText.text.Length > 1)
                changingText.text = changingText.text.Substring(0, changingText.text.Length - 1);
            else
                changingText.text = "Name eingeben";
        }
        else if (s == "Space")
        {
            changingText.text = changingText.text + " ";
        }
        else if (s == "Exit")
        {
            changingText.text = "Name eingeben";
            GameObject.Find("GUISave").SetActive(false);
            molecule.SetActive(true);
        }
        else if (s == "OK")
        {
            list_atomData.Clear();
            saveMolecule();
            CFileHelper.SaveData(Application.dataPath + "/MoleculeFiles/" + changingText.text + ".xml", list_atomData);

            changingText.text = "Name eingeben";
            GameObject.Find("GUISave").SetActive(false);
            molecule.SetActive(true);
        }
        else
        {
            changingText.text = changingText.text + s;
        }
    }

    public void destroyMolecule()
    {
        //Deletes the whole molecule and it's connections
        foreach (Atom child in molecule.GetComponentsInChildren<Atom>())
        {
            Destroy(child.gameObject);
            list_curAtoms.Remove(child);
        }
        foreach (GameObject con in GameObject.FindGameObjectsWithTag("VerbindungCC"))
        {
            Destroy(con);
        }
    }

    public void recycle()
    {
        //select if one atom is marked red or not
        // if a single atom is marked red, this one gets deleted, else the whole molecule will be deleted
        if(GameObject.Find("Molekül").GetComponent<EditMode>().editMode == false)
        {
            destroyMolecule();
            foreach(Atom c in list_curAtoms)
            {
                Destroy(c.gameObject);
            }
            list_curAtoms.Clear();
            foreach (GameObject con in GameObject.FindGameObjectsWithTag("VerbindungCC"))
            {
                Destroy(con);
            }
            atomMap.Clear();
        } else
        {
            Atom fixedAtom = GameObject.Find("Molekül").GetComponent<EditMode>().fixedAtom;
            GameObject.Find("Molekül").GetComponent<EditMode>().fixedAtom = null;
            GameObject.Find("Molekül").GetComponent<EditMode>().editMode = false;
            foreach (ConnectionStatus connection in fixedAtom.getAllConPoints())
            {
                if(connection.isConnected == true)
                {
                    Atom otherAtom = list_curAtoms.Find(p => p._id == connection.otherAtomID);
                    ConnectionStatus otherPoint = otherAtom.getConPoint(connection.otherPointID);
                    otherPoint.isConnected = false;
                    otherPoint.otherAtomID = -1;
                    otherPoint.otherPointID = -1;
                    otherPoint.conID = -1;
                    otherPoint.gameObject.GetComponent<Renderer>().material.color = Color.clear;
                    Destroy(GameObject.Find("con" + connection.conID));
                }
            }
            Destroy(fixedAtom.gameObject);
            list_curAtoms.Remove(fixedAtom);
            atomMap.Remove(fixedAtom._id);
        }
    }
}
