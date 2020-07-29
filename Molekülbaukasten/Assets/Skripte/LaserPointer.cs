using UnityEngine;
using System.Collections;

namespace Valve.VR.Extras
{
    public class LaserPointer : MonoBehaviour
    {
        public SteamVR_Behaviour_Pose pose;
        public SteamVR_Action_Vector2 trackpadtouch = SteamVR_Input.GetVector2Action("TrackpadTouch");
        public SteamVR_Action_Boolean trackpadklicked = SteamVR_Input.GetBooleanAction("TrackpadKlick");
        public SteamVR_Action_Vibration vibration;
        public bool active = true;
        public float thickness = 0.002f;
        public GameObject holder;
        public GameObject pointer;
        bool isActive = false;
        public bool addRigidBody = false;
        public Object[] allGameObjects;
        public GameObject guiSave;
        public GameObject guiLoad;


        private void Start()
        {
            if (pose == null)
                pose = this.GetComponent<SteamVR_Behaviour_Pose>();
            if (pose == null)
                Debug.LogError("No SteamVR_Behaviour_Pose component found on this object", this);

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
            }
        }

        private void Update()
        {
            if (!isActive)
            {
                isActive = true;
                this.transform.GetChild(0).gameObject.SetActive(true);
            }
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
            if (trackpadklicked.stateDown && bHit && touchPos.y >= 0.5)
            {
                if (hit.collider.name == "Öffnen" && (guiSave.activeInHierarchy == false) && (guiLoad.activeInHierarchy == false))
                {
                    vibration.Execute(0, 0.2f, 1, 1, this.pose.inputSource);
                    GameObject.Find("Molekül").GetComponent<EditMode>().editMode = false;
                    GameObject.Find("UI0").transform.position = new Vector3(0, 0, 0);
                    GameObject.Find("UI1").transform.position = new Vector3(0, 0, 0);
                    GameObject.Find("UI2").transform.position = new Vector3(0, 0, 0);
                    GameObject.Find("UI3").transform.position = new Vector3(0, 0, 0);
                    GameObject.Find("Molekül").SetActive(false);
                    print(this.GetComponentInParent<GlobalCtrl>());
                    this.GetComponentInParent<GlobalCtrl>().loadGUILoad();
                    guiLoad.SetActive(true);
                }
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
                else if (hit.collider.name == "PeriodensystemC" && (guiSave.activeInHierarchy == false) && (guiLoad.activeInHierarchy == false))
                {
                    vibration.Execute(0, 0.2f, 1, 1, this.pose.inputSource);
                    this.GetComponentInParent<GlobalCtrl>().kohlenstoffErstellen(this.transform.position);
                }
                else if (hit.collider.name == "recycle bin" && (guiSave.activeInHierarchy == false) && (guiLoad.activeInHierarchy == false))
                {
                    print(GameObject.Find("Molekül"));
                    print(GameObject.Find("Molekül").GetComponent<EditMode>());
                    print(GameObject.Find("Molekül").GetComponent<EditMode>().editMode);
                    vibration.Execute(0, 0.2f, 1, 1, this.pose.inputSource);
                    this.GetComponentInParent<GlobalCtrl>().recycle();
                }
                else if (hit.collider.name.StartsWith("kohlenstoff") && (guiSave.activeInHierarchy == false) && (guiLoad.activeInHierarchy == false))
                {
                    vibration.Execute(0, 0.2f, 1, 1, this.pose.inputSource);
                    //einzelnes Kohlenstoff Atom ausgewählt -- Bearbeitungsmodus öffnen
                    if (GameObject.Find("Molekül").GetComponent<EditMode>().editMode == false)
                    {
                        GameObject.Find("Molekül").GetComponent<EditMode>().editMode = true;
                        GameObject.Find("Molekül").GetComponent<EditMode>().fixedAtom = hit.collider.gameObject.GetComponent<CarbonAtom>();
                        hit.collider.gameObject.GetComponent<Renderer>().material.color = new Color32(255, 0, 0, 255);
                        hit.collider.gameObject.GetComponent<SenseGlove_Grabable>().editMarker = true;
                    }
                    else if (GameObject.Find("Molekül").GetComponent<EditMode>().editMode == true && hit.collider.gameObject.GetComponent<CarbonAtom>()._id == GameObject.Find("Molekül").GetComponent<EditMode>().fixedAtom._id)
                    {
                        hit.collider.gameObject.GetComponent<SenseGlove_Grabable>().editMarker = false;
                        GameObject.Find("Molekül").GetComponent<EditMode>().editMode = false;
                        GameObject.Find("Molekül").GetComponent<EditMode>().fixedAtom = null;
                        hit.collider.gameObject.GetComponent<Renderer>().material.color = new Color32(0, 0, 0, 255);
                    }
                }

            }
        }
    }
}