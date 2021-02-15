using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using Valve.VR;

public struct connectInfo
{
    public bool isConnected;
    public int otherAtomID;
    public int otherPointID;
}

public struct atomData
{
    public int id;
    public string type;
    public Vector3 pos;
}

public struct bondData
{
    public string bondRef;
    public string order;
}

public struct moleculeData
{
    public List<atomData> atomArray;
    public List<bondData> bondArray;

}

public struct mergedData
{
    public List<atomData> aData;
    public List<bondData> bData;
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
    public List<bondData> list_bondData = new List<bondData>();
    public List<mergedData> mergeList = new List<mergedData>();
    public List<Atom> list_curAtoms = new List<Atom>();
    public GameObject atomprefab;
    public GameObject dummyprefab;
    public GameObject dummycon;
    public GameObject atomcon;

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
    public bool forceField = true;
    public bool collision;
    public Atom collider1;
    public Atom collider2;


    public Dictionary<int, Vector3> atomMap = new Dictionary<int, Vector3>();


    // Start is called before the first frame update
    void Start()
    {
        //THIS IS THE GLOBAL SCALE FACTOR IN METER TO SCALE THE MOLECULE. Do not change during runtime!
        //The scale describes exactly the length of a single bond. Atoms are half of the scale size.
        //Example: scale of 0.2f renders an atom with a diameter of 10cm and the bonds in the molecule will have a length of 20cm
        scale = 0.1f;  // 0.05f to 0.1f is what a practical user will find satisfying
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
            }
        }
        DirectoryInfo dir = new DirectoryInfo(Application.dataPath + "/MoleculeFiles/");
        info = dir.GetFiles("*.*");

    }

    // Update is called once per frame
    void Update()
    {
        //Loads and updates GUI
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

    public void createDummy(Atom mainAtom, int conID)
    {
        _id += 1;
        GameObject dummyatom = Instantiate(dummyprefab, new Vector3(0, 0, 0), Quaternion.identity);
        dummyatom.GetComponent<Atom>().f_InitDummy(_id, mainAtom._id, conID);
        Vector3 offset = new Vector3(0, 0.05f, 0);
        if (conID == 0)
        {
            dummyatom.transform.position = mainAtom.transform.localPosition + Quaternion.Euler(0, 0, 0) * offset;
        }
        else if(conID == 1)
        {
            dummyatom.transform.position = mainAtom.transform.localPosition + Quaternion.Euler(54.74f, 60, 180) * offset;
        }
        else if(conID == 2)
        {
            dummyatom.transform.position = mainAtom.transform.localPosition + Quaternion.Euler(-54.74f, 0, 180) * offset;
        }
        else if (conID == 3)
        {
            dummyatom.transform.position = mainAtom.transform.localPosition + Quaternion.Euler(-54.74f, 120, 180) * offset;
        }
        
        dummyatom.transform.localScale = new Vector3(scale * 0.2f, scale * 0.2f, scale * 0.2f);
        list_curAtoms.Add(dummyatom.GetComponent<Atom>());
        dummyatom.transform.parent = mainAtom.transform.parent;
        mainAtom.getConPoint(conID).otherAtomID = dummyatom.GetComponent<Atom>()._id;
        mainAtom.getConPoint(conID).otherPointID = 0;
        mainAtom.getConPoint(conID).isConnected = true;
        mainAtom.getConPoint(conID).conID = dummyatom.GetComponent<Atom>()._id;
        dummyatom.GetComponent<Atom>().getConPoint(0).conID = dummyatom.GetComponent<Atom>()._id;
        createDummyCon(mainAtom, dummyatom.GetComponent<Atom>());
    }

    public void createDummyCon(Atom mainAtom, Atom dummyAtom)
    {
        //_conID += 1;
        GameObject connection = Instantiate(dummycon, mainAtom.transform.position, Quaternion.identity);
        connection.transform.LookAt(dummyAtom.transform.position);
        connection.transform.parent = mainAtom.transform.parent;
        connection.transform.name = "dummycon" + dummyAtom._id;
        float distance = Vector3.Distance(mainAtom.transform.localPosition, dummyAtom.transform.localPosition);
        connection.transform.localScale = new Vector3(scale * 0.5f, scale * 0.5f, distance / 2);
        connection.GetComponent<Bond>().atom1ID = mainAtom._id;
        connection.GetComponent<Bond>().atom2ID = dummyAtom._id;
    }

    public void createCarbon(Vector3 pos)
    {   
        _id += 1;
        GameObject molecule = new GameObject();
        molecule.transform.parent = GameObject.Find("atomworld").transform;
        molecule.transform.name = "molecule" + _id;

        GameObject carbonatom = Instantiate(atomprefab, new Vector3(0, 1, 0.5f), Quaternion.identity);
        carbonatom.GetComponent<Atom>().f_InitCarbon(_id);
        carbonatom.transform.position = pos + new Vector3(0, 0, 0.2f);
        carbonatom.transform.localScale = new Vector3(scale * 0.5f, scale * 0.5f, scale * 0.5f);

        carbonatom.transform.parent = molecule.transform;
        list_curAtoms.Add(carbonatom.GetComponent<Atom>());
        int i = 0;
        foreach(ConnectionStatus conPoint in carbonatom.GetComponent<Atom>().getAllConPoints())
        {
            createDummy(carbonatom.GetComponent<Atom>(), i);
            i++;
        }
    }
    public void createHydrogen(Vector3 pos)
    {
        _id += 1;
        GameObject molecule = new GameObject();
        molecule.transform.parent = GameObject.Find("atomworld").transform;
        molecule.transform.name = "molecule" + _id;

        GameObject hydrogen = Instantiate(atomprefab, new Vector3(0, 1, 0.5f), Quaternion.identity);
        hydrogen.GetComponent<Atom>().f_InitHydrogen(_id);
        hydrogen.transform.position = pos + new Vector3(0, 0, 0.2f);
        hydrogen.transform.localScale = new Vector3(scale * 0.3f, scale * 0.3f, scale * 0.3f);

        hydrogen.transform.parent = molecule.transform;
        list_curAtoms.Add(hydrogen.GetComponent<Atom>());
        int i = 0;
        foreach (ConnectionStatus conPoint in hydrogen.GetComponent<Atom>().getAllConPoints())
        {
            createDummy(hydrogen.GetComponent<Atom>(), i);
            i++;
        }
    }

    public void createConnection(List<int> conList)
    {
        _conID += 1;
        Atom conAtom0 = getAtomByID(conList[0]);
        Atom conAtom1 = getAtomByID(conList[1]);
        int otherPoint0 = conList[2];
        int otherPoint1 = conList[3];
        //Transform parents
        if(conAtom1.transform.parent != conAtom0.transform.parent)
        {
            Transform oldParent = conAtom1.transform.parent;
            while (oldParent.childCount > 0)
            {
                oldParent.GetChild(0).transform.SetParent(conAtom0.transform.parent);
            }
            Destroy(oldParent.transform.gameObject);
        }


        
        GameObject connection = Instantiate(atomcon, conAtom0.transform.position, Quaternion.identity);
        connection.transform.LookAt(conAtom1.transform.position);
        connection.transform.parent = conAtom0.transform.parent;
        connection.transform.name = "con" + _conID;
        float distance = Vector3.Distance(conAtom1.transform.localPosition, conAtom0.transform.localPosition);

        connection.transform.localScale = new Vector3(connection.transform.localScale.x, connection.transform.localScale.y, distance/2);
        connection.GetComponent<Bond>().atom1ID = conAtom0._id;
        connection.GetComponent<Bond>().atom2ID = conAtom1._id;
        //additional information is set, so each atom knows the id of it's connected atoms
        conAtom0.getConPoint(otherPoint0).otherAtomID = conAtom1._id;
        conAtom1.getConPoint(otherPoint1).otherAtomID = conAtom0._id;
        conAtom0.getConPoint(otherPoint0).conID = _conID;
        conAtom1.getConPoint(otherPoint1).conID = _conID;

    }


    public Vector3 scaleToAngst(Vector3 pos)
    {
        Vector3 angstVec = (pos * (scale / 154f)) / 100f;

        return angstVec;
    }

    public List<mergedData> saveMolecule()
    {
        mergedData m;
        for (int i = 0; i < GameObject.Find("atomworld").transform.childCount; i++)
        {
            
            foreach(Transform trans in GameObject.Find("atomworld").transform.GetChild(i))
            {
                if(trans.TryGetComponent(out Atom at))
                {
                    atomData a;
                    a.id = at._id;
                    a.type = at.type;
                    a.pos = scaleToAngst(at.transform.localPosition);
                    list_atomData.Add(a);
                }

                if (trans.TryGetComponent(out Bond bd))
                {
                    bondData b;
                    b.bondRef = bd.generateRef2(bd.atom1ID, bd.atom2ID);
                    b.order = "1";
                    list_bondData.Add(b);
                }
            }

            //list_atomData.Clear();
            //list_bondData.Clear();
        }

        m.aData = list_atomData;
        m.bData = list_bondData;

        mergeList.Add(m);

        return mergeList;
    }

    public void loadMolecule(List<atomData> list)
    {
    //    _conID = 0;
    //    //reset molecule
    //    loadGUI = false;
    //    molecule.SetActive(true);
    //    GameObject.Find("GUILoad").SetActive(false);
    //    destroyMolecule();
    //    list_curAtoms.Clear();
    //    //get the atom data for each atom in the saved list and instantiate the atoms with their data
    //    foreach (atomData atom in list)
    //    {
    //        GameObject atomObj = Instantiate(atomprefab, atom.pos, Quaternion.Euler(atom.rot));
    //        Atom atomDef = atomObj.GetComponent<Atom>();
    //        //NEEDS REWORK HERE BECAUSE OF NEW ATOM STRUCTURE

    //        //atomDef.f_Init(atom.id);
    //        atomDef.transform.parent = GameObject.Find("Molekül").transform;
    //        atomDef.c0.isConnected = atom.info0.isConnected;
    //        atomDef.c1.isConnected = atom.info1.isConnected;
    //        atomDef.c2.isConnected = atom.info2.isConnected;
    //        atomDef.c3.isConnected = atom.info3.isConnected;

    //        atomDef.c0.otherAtomID = atom.info0.otherAtomID;
    //        atomDef.c1.otherAtomID = atom.info1.otherAtomID;
    //        atomDef.c2.otherAtomID = atom.info2.otherAtomID;
    //        atomDef.c3.otherAtomID = atom.info3.otherAtomID;

    //        atomDef.c0.otherPointID = atom.info0.otherPointID;
    //        atomDef.c1.otherPointID = atom.info1.otherPointID;
    //        atomDef.c2.otherPointID = atom.info2.otherPointID;
    //        atomDef.c3.otherPointID = atom.info3.otherPointID;
    //        list_curAtoms.Add(atomDef);
    //        foreach(ConnectionStatus c in atomDef.getAllConPoints())
    //        {
    //            c.conID = -1;
    //        }
    //    }

    //    //loop all atoms and their connections, if they have a connection --> create it
    //    int tempID = 0;
    //    foreach (Atom atom in list_curAtoms)
    //    {
    //        foreach (ConnectionStatus cp in atom.getAllConPoints())
    //        {
    //            if (cp.isConnected == true)
    //            {
                    
    //                // find connected atom: Find atom p, where p.id = otherAtomID
    //                otherAtom = list_curAtoms.Find(p=>p._id==cp.otherAtomID);
    //                otherCP = otherAtom.getConPoint(cp.otherPointID);
    //                //if the connection hasn't already been created in the same loop because it was part of the "other atom" before, create it now
    //                if (cp.conID == -1)
    //                {
    //                    _conID += 1;
    //                    GameObject connection = Instantiate(atomcon, atom.transform.position, Quaternion.identity);
    //                    connection.transform.LookAt(otherAtom.transform.position);
    //                    connection.transform.parent = GameObject.Find("Molekül").transform;
    //                    connection.transform.name = "con" + _conID;
    //                    float distance = Vector3.Distance(atom.transform.position, otherAtom.transform.position);
    //                    connection.transform.localScale = new Vector3(connection.transform.localScale.x, connection.transform.localScale.y, connection.transform.localScale.z + ((distance - 0.35f) * 1));
    //                    cp.conID = _conID;
    //                    otherCP.conID = _conID;
    //                    cp.gameObject.SetActive(false);
    //                    otherCP.gameObject.SetActive(false);
    //                }
    //            }
    //            // set full attribute if needed
    //            if (atom.c0.isConnected == true && atom.c1.isConnected == true && atom.c2.isConnected == true && atom.c3.isConnected == true)
    //            {
    //                atom.isFull = true;
    //            }
    //        }
    //        // update atomID
    //        if(atom._id > tempID)
    //        {
    //            tempID = atom._id;
    //        }
    //    }
    //    //replace atom ID, if a new atom is created now, the ID starts at _id+1, so that each atomID is unique
    //    _id = tempID;
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
            list_bondData.Clear();
            mergeList.Clear();

            CFileHelper.SaveData(Application.dataPath + "/MoleculeFiles/" + changingText.text + ".xml", saveMolecule());

            changingText.text = "Name eingeben";
            GameObject.Find("GUISave").SetActive(false);
            //molecule.SetActive(true);
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


    // returns the atom with the given ID 
    public Atom getAtomByID(float id)
    {
        foreach (Atom c1 in list_curAtoms)
        {
            if (c1._id == (int)id)
                return c1;
        }

        return null;
    }
}
