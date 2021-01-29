using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Valve.VR;

public class GrabController : MonoBehaviour
{
    public SteamVR_Action_Boolean triggerklicked = SteamVR_Input.GetBooleanAction("Triggerklick");
    private SteamVR_Behaviour_Pose m_pose = null;
    
    private Atom m_currentAtom = null;
    private Atom connectAtom = null;
    public List<Atom> m_Interactables = new List<Atom>();
    private bool isPressed = false;
    private Vector3 lastPosController;
    private Vector3 deltaPos;
    private Quaternion lastRotController;
    private Quaternion lastRotationMolecule;

    StreamWriter Log;
    Logger1 logger;

    // Start is called before the first frame update
    void Start()
    {

        m_pose = GetComponent<SteamVR_Behaviour_Pose>();
        //if(this.name == "Controller (left)")
        //{
        //    logger = new Logger1(@"C:\Users\vruser\Documents\Molecular-Construction-Kit\MyLog.log");
        //    //Log = File.CreateText("totalLog.txt");
        //    logger.WriteLine("Start");
        //}

    }

    // Update is called once per frame
    void Update()
    {
        //Trigger Down
        if (triggerklicked.GetStateDown(m_pose.inputSource))
        {
            Pickup();
        }
        //Trigger Up

        if (triggerklicked.GetStateUp(m_pose.inputSource))
        {            
            Drop();
            isPressed = false;
        }
        //During Trigger pressed
        if (isPressed)
        {
            //Grab single atom which is not connected to the molecule
            if (m_currentAtom != null && m_currentAtom.transform.parent == null)
            {
                m_currentAtom.transform.position = transform.position;
            }
            //Grab molecule
            else
            {
                //Transform whole molecule or single atom depending on which mode is active
                if(this.GetComponentInParent<GlobalCtrl>().allAtom)
                {

                    m_currentAtom.transform.parent.transform.rotation = transform.rotation * Quaternion.Inverse(lastRotController) * lastRotationMolecule;
                    deltaPos = m_currentAtom.transform.parent.transform.position - m_currentAtom.transform.position;
                    m_currentAtom.transform.parent.transform.position = transform.position + deltaPos;
                    //GameObject.Find("Molekül").transform.rotation = transform.rotation * Quaternion.Inverse(lastRotController) * lastRotationMolecule;
                    //deltaPos = GameObject.Find("Molekül").transform.position - m_currentAtom.transform.position;
                    //GameObject.Find("Molekül").transform.position = transform.position + deltaPos;
                } else
                {
                    if(m_currentAtom != null)
                        m_currentAtom.transform.position = transform.position;
                }

            }
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Atom"))
        {
            return;
        }
        m_Interactables.Add(other.gameObject.GetComponent<Atom>());

    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.CompareTag("Atom"))
        {
            return;
        }
        m_Interactables.Remove(other.gameObject.GetComponent<Atom>());
    }

    public void Pickup()
    {
        m_currentAtom = GetNearestAtom();

        //Null check
        if (!m_currentAtom)
            return;
        m_currentAtom.grabbed = true;
        // Already held, check
        if (m_currentAtom.m_ActiveHand)
            m_currentAtom.m_ActiveHand.Drop();

        deltaPos = GameObject.Find("atomworld").transform.position - m_currentAtom.transform.parent.transform.position;


        //Transform whole molecule or single atom depending on which mode is active
        if (this.GetComponentInParent<GlobalCtrl>().allAtom)
        {
            m_currentAtom.transform.parent.transform.position = transform.position + deltaPos;
        }
        else
        {
            m_currentAtom.transform.position = transform.position + deltaPos;
        }



        //Fix Rotation
        lastRotationMolecule = m_currentAtom.transform.parent.transform.rotation;
        lastPosController = transform.position;
        lastRotController = transform.rotation;
        isPressed = true;
    }

    public void Drop()
    {
        // Null check
        if (!m_currentAtom)
            return;


        if (transform.GetComponentInParent<GlobalCtrl>().collision)
        {
            List<int> conList = new List<int>();
            conList.Add(GetComponentInParent<GlobalCtrl>().collider1.c0.otherAtomID);
            conList.Add(GetComponentInParent<GlobalCtrl>().collider2.c0.otherAtomID);
            conList.Add(GetComponentInParent<GlobalCtrl>().collider1.c0.otherPointID);
            conList.Add(GetComponentInParent<GlobalCtrl>().collider2.c0.otherPointID);
            GetComponentInParent<GlobalCtrl>().createConnection(conList);


            GetComponentInParent<GlobalCtrl>().list_curAtoms.Remove(GetComponentInParent<GlobalCtrl>().collider1);
            GetComponentInParent<GlobalCtrl>().list_curAtoms.Remove(GetComponentInParent<GlobalCtrl>().collider2);
            m_Interactables.Remove(GetComponentInParent<GlobalCtrl>().collider1);
            m_Interactables.Remove(GetComponentInParent<GlobalCtrl>().collider2);

            Destroy(GetComponentInParent<GlobalCtrl>().collider1.gameObject);
            Destroy(GetComponentInParent<GlobalCtrl>().collider2.gameObject);
            Destroy(GameObject.Find("dummycon" + GetComponentInParent<GlobalCtrl>().collider1._id));
            Destroy(GameObject.Find("dummycon" + GetComponentInParent<GlobalCtrl>().collider2._id));

            GetComponentInParent<GlobalCtrl>().collision = false;
            GetComponentInParent<GlobalCtrl>().collider1 = null;
            GetComponentInParent<GlobalCtrl>().collider2 = null;
            conList.Clear();
        }

        //Clear
        m_currentAtom.grabbed = false;
        m_currentAtom.m_ActiveHand = null;
        m_currentAtom = null;
    }
    
    /*
     * Calculates distance from controller to all other atoms
     * returns: nearest atom 
     * 
     * */
    private Atom GetNearestAtom()
    {
        Atom nearest = null;
        float minDistance = float.MaxValue;
        float distance = 0.0f;

        foreach(Atom interactable in m_Interactables)
        {
            if(interactable != null)
            {
                distance = (interactable.transform.position - transform.position).sqrMagnitude;
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = interactable;
                }
            }
            
        }
        return nearest;
    }
    
}
