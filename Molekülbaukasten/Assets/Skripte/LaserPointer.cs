using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Valve.VR.Extras
{
    public class LaserPointer : MonoBehaviour
    {
        private SteamVR_Behaviour_Pose pose;
        public SteamVR_Action_Vector2 trackpadtouch = SteamVR_Input.GetVector2Action("TrackpadTouch");
        public SteamVR_Action_Boolean trackpadklicked = SteamVR_Input.GetBooleanAction("TrackpadKlick");
        public SteamVR_Action_Vibration vibration;
        private float thickness = 0.002f;
        private GameObject holder;
        private GameObject pointer;
        private bool isActive = false;
        private bool addRigidBody = false;
        public Object[] allGameObjects;
        public GameObject guiSave;
        public GameObject guiLoad;
        private GameObject molecule;

        private void Start()
        {
            if (pose == null)
                pose = this.GetComponent<SteamVR_Behaviour_Pose>();
            if (pose == null)
                Debug.LogError("No SteamVR_Behaviour_Pose component found on this object", this);
            //Transform controller
            holder = new GameObject();
            holder.transform.parent = this.transform;
            holder.transform.localPosition = Vector3.zero;
            holder.transform.localRotation = Quaternion.Euler(30, 0,0);
            pointer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pointer.transform.parent = holder.transform;
            pointer.transform.localScale = new Vector3(thickness, thickness, 100f);
            pointer.transform.localPosition = new Vector3(0f, 0f, 50f);
            pointer.transform.localRotation = Quaternion.identity;
            BoxCollider collider = pointer.GetComponent<BoxCollider>();
            if (addRigidBody)
            {
                if (collider)
                {
                    collider.isTrigger = true;
                }
                Rigidbody rigidBody = pointer.AddComponent<Rigidbody>();
                rigidBody.isKinematic = true;
            }
            else
            {
                if (collider)
                {
                    Object.Destroy(collider);
                }
            }
            Material newMaterial = new Material(Shader.Find("Unlit/Color"));
            newMaterial.SetColor("_Color", Color.green);
            pointer.GetComponent<MeshRenderer>().material = newMaterial;

            //Transform GUI Options + Molecule
            allGameObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject));
            foreach (GameObject obj in allGameObjects)
            {
                if (obj.name == "GUISave")
                {
                    guiSave = obj;
                }
                if (obj.name == "GUILoad")
                {
                    guiLoad = obj;
                }
                if (obj.name == "Molekül")
                {
                    molecule = obj;
                }
            }
        }

        private void Update()
        {
            if (!isActive)
            {
                isActive = true;
                this.transform.GetChild(0).gameObject.SetActive(true);
            }
            //IF Trackpad is touched in the front mittle of the trackpad, the laserpointer appears
            Vector2 touchPos = trackpadtouch.GetAxis(SteamVR_Input_Sources.Any);
            if (touchPos.y >= 0.5)
            {
                this.transform.GetChild(1).GetChild(0).gameObject.SetActive(true);
            } else
            {
                this.transform.GetChild(1).GetChild(0).gameObject.SetActive(false);
            }
            Transform rayTrans = transform.GetChild(1).transform;
            rayTrans.localRotation = Quaternion.Euler(30, 0, 0);
            Ray raycast = new Ray(transform.position, rayTrans.forward);
            RaycastHit hit;
            bool bHit = Physics.Raycast(raycast, out hit);
            //Klick on the trackpad confirms the selection, target gets executed
            if (trackpadklicked.stateDown && bHit && touchPos.y >= 0.5)
            {
                //Open an already saved molecule
                if (hit.collider.name == "Öffnen" && (guiSave.activeInHierarchy == false) && (guiLoad.activeInHierarchy == false))
                {
                    vibration.Execute(0, 0.2f, 1, 1, this.pose.inputSource);
                    GameObject.Find("Molekül").GetComponent<EditMode>().editMode = false;
                    GameObject.Find("UI0").transform.position = new Vector3(0, 0, 0);
                    GameObject.Find("UI1").transform.position = new Vector3(0, 0, 0);
                    GameObject.Find("UI2").transform.position = new Vector3(0, 0, 0);
                    GameObject.Find("UI3").transform.position = new Vector3(0, 0, 0);
                    GameObject.Find("Molekül").SetActive(false);
                    this.GetComponentInParent<GlobalCtrl>().loadGUILoad();
                    guiLoad.SetActive(true);
                }
                //Save created molecule
                else if (hit.collider.name == "Speichern" && guiSave.activeInHierarchy == false && (guiLoad.activeInHierarchy == false))
                {
                    vibration.Execute(0, 0.2f, 1, 1, this.pose.inputSource);
                    guiSave.SetActive(true);
                    GameObject.Find("Molekül").GetComponent<EditMode>().editMode = false;
                    GameObject.Find("UI0").transform.position = new Vector3(0, 0, 0);
                    GameObject.Find("UI1").transform.position = new Vector3(0, 0, 0);
                    GameObject.Find("UI2").transform.position = new Vector3(0, 0, 0);
                    GameObject.Find("UI3").transform.position = new Vector3(0, 0, 0);
                    GameObject.Find("Molekül").SetActive(false);
                }
                //Create new carbon atom
                else if (hit.collider.name == "PeriodensystemC" && (guiSave.activeInHierarchy == false) && (guiLoad.activeInHierarchy == false))
                {
                    vibration.Execute(0, 0.2f, 1, 1, this.pose.inputSource);
                    this.GetComponentInParent<GlobalCtrl>().kohlenstoffErstellen(this.transform.position);
                }
                //Delete atoms or whole molecule
                else if (hit.collider.name == "recycle bin" && (guiSave.activeInHierarchy == false) && (guiLoad.activeInHierarchy == false))
                {
                    vibration.Execute(0, 0.2f, 1, 1, this.pose.inputSource);
                    this.GetComponentInParent<GlobalCtrl>().recycle();
                }
                //Mark an existing atom to edit / delete it
                else if (hit.collider.name.StartsWith("kohlenstoff") && (guiSave.activeInHierarchy == false) && (guiLoad.activeInHierarchy == false))
                {
                    vibration.Execute(0, 0.2f, 1, 1, this.pose.inputSource);
                    //if it isn't selected before,start edit mode
                    if (GameObject.Find("Molekül").GetComponent<EditMode>().editMode == false)
                    {
                        GameObject.Find("Molekül").GetComponent<EditMode>().editMode = true;
                        GameObject.Find("Molekül").GetComponent<EditMode>().fixedAtom = hit.collider.gameObject.GetComponent<Atom>();
                        hit.collider.gameObject.GetComponent<Renderer>().material.color = new Color32(255, 0, 0, 255);
                        hit.collider.gameObject.GetComponent<SenseGlove_Grabable>().editMarker = true;
                    }
                    //if it already was selected before, close edit mode
                    else if (GameObject.Find("Molekül").GetComponent<EditMode>().editMode == true && hit.collider.gameObject.GetComponent<Atom>()._id == GameObject.Find("Molekül").GetComponent<EditMode>().fixedAtom._id)
                    {
                        hit.collider.gameObject.GetComponent<SenseGlove_Grabable>().editMarker = false;
                        GameObject.Find("Molekül").GetComponent<EditMode>().editMode = false;
                        GameObject.Find("Molekül").GetComponent<EditMode>().fixedAtom = null;
                        hit.collider.gameObject.GetComponent<Renderer>().material.color = new Color32(0, 0, 0, 255);
                    }
                }
                //Select the whole molecule -- moves as a solid structure
                else if((hit.collider.name == "AllAtoms") && (guiSave.activeInHierarchy == false) && (guiLoad.activeInHierarchy == false))
                {
                    GameObject.Find("AllAtoms").transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                    GameObject.Find("SingleAtom").transform.localScale = new Vector3(0.07f, 0.1f, 0.07f);
                    this.GetComponentInParent<GlobalCtrl>().allAtom = true;
                }
                //Select single atom -- moves independent from the molecule
                else if ((hit.collider.name == "SingleAtom") && (guiSave.activeInHierarchy == false) && (guiLoad.activeInHierarchy == false))
                {
                    GameObject.Find("AllAtoms").transform.localScale = new Vector3(0.07f, 0.1f, 0.07f);
                    GameObject.Find("SingleAtom").transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                    this.GetComponentInParent<GlobalCtrl>().allAtom = false;
                }
                //Turn off the forcefield, no forces are calculated
                else if ((hit.collider.name == "ButtonOFF") && (guiSave.activeInHierarchy == false) && (guiLoad.activeInHierarchy == false))
                {
                    GameObject.Find("ButtonON").transform.localPosition = new Vector3(4.75f, 1.6f, 6.5f);
                    GameObject.Find("ButtonOFF").transform.localPosition = new Vector3(4.775f, 1.6f, 6.53f);
                    this.GetComponentInParent<GlobalCtrl>().forceField = true;
                }
                //Turn on the forcefiled, forces are calculated, atoms are going to move if forces are big enough
                else if ((hit.collider.name == "ButtonON") && (guiSave.activeInHierarchy == false) && (guiLoad.activeInHierarchy == false))
                {
                    GameObject.Find("ButtonOFF").transform.localPosition = new Vector3(4.75f, 1.6f, 6.5f);
                    GameObject.Find("ButtonON").transform.localPosition = new Vector3(4.775f, 1.6f, 6.53f);
                    this.GetComponentInParent<GlobalCtrl>().forceField = false;
                }
                //Typing on virtual keyboard
                else if(hit.collider.transform.parent != null && hit.collider.transform.parent.name == "Tastatur")
                {
                    GlobalCtrl text = this.transform.root.GetComponent<GlobalCtrl>();
                    text.textChange(hit.collider.gameObject);
                }
                //Scrollbar and update in GUI, if "open created molecule" is active
                else if (hit.collider.transform.parent != null && hit.collider.transform.parent.name == "GUILoad")
                {
                    if(hit.collider.name == "Down")
                    {
                        GlobalCtrl.updown += 1;
                    }
                    else if(hit.collider.name == "Up" && GlobalCtrl.updown >= 1)
                    {
                        GlobalCtrl.updown -= 1;
                    }
                    else if (hit.collider.name.StartsWith("Platz"))
                    {
                        List<atomData> loadFile = new List<atomData>();
                        loadFile = (List<atomData>)CFileHelper.LoadData(Application.dataPath + "/MoleculeFiles/" + hit.collider.transform.GetChild(0).GetComponent<Text>().text, typeof(List<atomData>));
                        this.transform.root.GetComponent<GlobalCtrl>().loadMolecule(loadFile);
                    }
                    else if(hit.collider.name == "ExitLoadGui")
                    {
                        GlobalCtrl.loadGUI = false;
                        GameObject.Find("GUILoad").SetActive(false);
                        molecule.SetActive(true);
                    }
                }
            }
        }
    }
}