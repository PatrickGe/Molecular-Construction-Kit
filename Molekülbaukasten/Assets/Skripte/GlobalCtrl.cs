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
    public static int openSize = 0;
    public List<string> open = new List<string>();
    public List<atomData> list_atomData = new List<atomData>();
    public List<CarbonAtom> list_curCarbonAtoms = new List<CarbonAtom>();
    public GameObject KohlenstoffPrefab;
    public GameObject VerbindungCC;
    public GameObject hand;
    public GameObject molecule;
    public GameObject up;
    public GameObject down;
    public Vector3[,] position;
    FileInfo[] info;

    public Text changingText;
    public static bool loadGUI = false;

    CarbonAtom otherAtom;
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
        position = new Vector3[1000, 1000];
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
            } else if (obj.name.StartsWith("kohlenstoff"))
            {
                list_curCarbonAtoms.Add(obj.GetComponent<CarbonAtom>());
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
                    print(open.Count);
                    if (open[i + updown] != "")
                        GameObject.Find("GUILoad").transform.GetChild(i).GetChild(0).GetComponent<Text>().text = open[i + updown].Substring(open[i + updown].LastIndexOf("\\") + 1);

                }

            }

        }
    }

    public void kohlenstoffErstellen(Vector3 pos)
    {   //fertig
        _id += 1;
        GameObject kohlenstoffatom = Instantiate(KohlenstoffPrefab, new Vector3(0, 1, 0.5f), Quaternion.identity);
        kohlenstoffatom.GetComponent<CarbonAtom>().f_Init(_id);
        kohlenstoffatom.transform.position = pos + new Vector3(0, 0, 0.2f);
        list_curCarbonAtoms.Add(kohlenstoffatom.GetComponent<CarbonAtom>());
    }

    public void verbindungErstellen(List<CarbonAtom> senden)
    {
        _conID += 1;
        senden[0].transform.parent = GameObject.Find("Molekül").transform;
        senden[1].transform.parent = GameObject.Find("Molekül").transform;
        //Position hier setzen
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

        // Neu hinzugefügtes Atom zu gewähltem Vektor rotieren, Kind als connected anzeigen.
        childGrabbedSelected.isConnected = true;
        childSelected.isConnected = true;
        if (/*GameObject.Find("Molekül").GetComponent<EditMode>().editMode == false*/true)
        {
            senden[1].transform.position = senden[0].transform.position + Quaternion.Euler(senden[0].transform.rotation.eulerAngles) * ((childSelected.transform.localPosition / childSelected.transform.parent.localScale.x) * scale);
            Vector3 direction = childSelected.transform.position - senden[1].transform.position;
            Quaternion rotation = Quaternion.FromToRotation(childGrabbedSelected.transform.localPosition, direction);
            senden[1].transform.rotation = rotation;
        }
        //Verbindung erstellen
        childGrabbedSelected.gameObject.SetActive(false);
        childSelected.gameObject.SetActive(false);
        GameObject connection = Instantiate(VerbindungCC, senden[0].transform.position, Quaternion.identity);
        connection.transform.LookAt(senden[1].transform.position);
        connection.transform.parent = GameObject.Find("Molekül").transform;
        connection.transform.name = "con" + _conID;
        float distance = Vector3.Distance(childSelected.transform.position, childGrabbedSelected.transform.position);
        if (GameObject.Find("Molekül").GetComponent<EditMode>().editMode == true)
        {
            connection.transform.localScale = new Vector3(connection.transform.localScale.x, connection.transform.localScale.y, connection.transform.localScale.z + (distance * 1));
        }
        //Verbundenen Atome und Punkte auf den Atomen setzen
        childSelected.otherAtomID = senden[1].GetComponent<CarbonAtom>()._id;
        childGrabbedSelected.otherAtomID = senden[0].GetComponent<CarbonAtom>()._id;
        childSelected.conID = _conID;
        childGrabbedSelected.conID = _conID;
        int.TryParse(childGrabbedSelected.name, out childSelected.otherPointID);
        int.TryParse(childSelected.name, out childGrabbedSelected.otherPointID);

        //addToMap(senden[0], senden[1]);
        //addToMap(senden[1], senden[1]);

        childGrabbedSelected = null;
        childSelected = null;
        //Alle Atome und deren Abstände zu den Nachbarn berechnen
        for (int i = 0; i <= list_curCarbonAtoms.Count - 1; i++)
        {
            for (int j = 0; j <= list_curCarbonAtoms.Count - 1; j++)
            {
                position[i, j] = list_curCarbonAtoms[j].transform.position - list_curCarbonAtoms[i].transform.position;
            }
        }
    }

    public void addToMap(CarbonAtom c, CarbonAtom cGrabbed)
    {
        //Wenn Wert noch nicht enthalten ist
        if(!atomMap.TryGetValue(c._id, out Vector3 pos))
        {
            atomMap.Add(c._id, c.transform.localPosition);
        }
        //Wenn er schon enthalten ist
        else
        {
            if(atomMap.TryGetValue(c._id, out Vector3 posOld) && c == cGrabbed)
            {
                Vector3 newPos = posOld + (c.transform.localPosition - posOld)* 0.5f;
                print(c);
                print("alt: " + posOld);
                print("neu: " + c.transform.localPosition);
                
                atomMap[c._id] = newPos;
                print("mittel: " + atomMap[c._id]);
            }

        }
    }

    public Vector3[,] positionVector
    {
        get
        {
            return position;
        }
        set
        {
            position = value;
        }
    }

    public List<atomData> saveMolecule()
    {
        foreach (CarbonAtom child in molecule.GetComponentsInChildren<CarbonAtom>())
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
        //Molekül zurücksetzen
        loadGUI = false;
        molecule.SetActive(true);
        GameObject.Find("GUILoad").SetActive(false);
        destroyMolecule();
        list_curCarbonAtoms.Clear();
        foreach (atomData atom in list)
        {
            GameObject atomObj = Instantiate(KohlenstoffPrefab, atom.pos, Quaternion.Euler(atom.rot));
            CarbonAtom atomDef = atomObj.GetComponent<CarbonAtom>();
            atomDef.f_Init(atom.id);
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
            list_curCarbonAtoms.Add(atomDef);
            foreach(ConnectionStatus c in atomDef.getAllConPoints())
            {
                c.conID = -1;
            }
        }


        foreach (CarbonAtom atom in list_curCarbonAtoms)
        {
            foreach (ConnectionStatus cp in atom.getAllConPoints())
            {
                if (cp.isConnected == true)
                {
                    
                    
                    otherAtom = list_curCarbonAtoms.Find(p=>p._id==cp.otherAtomID);
                    otherCP = otherAtom.getConPoint(cp.otherPointID);
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
                if (atom.c0.isConnected == true && atom.c1.isConnected == true && atom.c2.isConnected == true && atom.c3.isConnected == true)
                {
                    atom.isFull = true;
                }

            }
        }


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
        foreach (CarbonAtom child in molecule.GetComponentsInChildren<CarbonAtom>())
        {
            Destroy(child.gameObject);
            list_curCarbonAtoms.Remove(child);
        }
        foreach (GameObject con in GameObject.FindGameObjectsWithTag("VerbindungCC"))
        {
            Destroy(con);
        }
    }

    public void recycle()
    {
        if(GameObject.Find("Molekül").GetComponent<EditMode>().editMode == false)
        {
            destroyMolecule();
            foreach(CarbonAtom c in list_curCarbonAtoms)
            {
                Destroy(c.gameObject);
            }
            list_curCarbonAtoms.Clear();
            foreach (GameObject con in GameObject.FindGameObjectsWithTag("VerbindungCC"))
            {
                Destroy(con);
            }
            atomMap.Clear();
        } else
        {
            CarbonAtom fixedAtom = GameObject.Find("Molekül").GetComponent<EditMode>().fixedAtom;
            GameObject.Find("Molekül").GetComponent<EditMode>().fixedAtom = null;
            GameObject.Find("Molekül").GetComponent<EditMode>().editMode = false;
            foreach (ConnectionStatus connection in fixedAtom.getAllConPoints())
            {
                if(connection.isConnected == true)
                {
                    CarbonAtom otherAtom = list_curCarbonAtoms.Find(p => p._id == connection.otherAtomID);
                    ConnectionStatus otherPoint = otherAtom.getConPoint(connection.otherPointID);
                    otherPoint.isConnected = false;
                    otherPoint.otherAtomID = -1;
                    otherPoint.otherPointID = -1;
                    otherPoint.conID = -1;
                    otherPoint.gameObject.SetActive(true);
                    Destroy(GameObject.Find("con" + connection.conID));
                }
            }
            Destroy(fixedAtom.gameObject);
            list_curCarbonAtoms.Remove(fixedAtom);
            atomMap.Remove(fixedAtom._id);
        }
    }
}
