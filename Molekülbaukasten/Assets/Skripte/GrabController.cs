using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class GrabController : MonoBehaviour
{
    public SteamVR_Action_Boolean triggerklicked = SteamVR_Input.GetBooleanAction("Triggerklick");
    public SteamVR_Behaviour_Pose m_pose = null;

    public CarbonAtom m_currentAtom = null;
    public CarbonAtom connectAtom = null;
    public List<CarbonAtom> m_Interactables = new List<CarbonAtom>();
    public bool isPressed = false;
    public Vector3 lastPosController;
    public Vector3 deltaPos;
    public Vector3 posDiff;
    public Quaternion lastRotController;
    public Vector3 rotDiff;
    public Vector3 startRot;
    public Quaternion lastRotationMolecule;
    
    // Start is called before the first frame update
    void Start()
    {
        m_pose = GetComponent<SteamVR_Behaviour_Pose>();
    }

    // Update is called once per frame
    void Update()
    {
        //Down
        if (triggerklicked.GetStateDown(m_pose.inputSource))
        {
            Pickup();
        }
        //Up
        if (triggerklicked.GetStateUp(m_pose.inputSource))
        {
            Drop();
            isPressed = false;
        }
        if (isPressed)
        {
            if (m_currentAtom != null && m_currentAtom.transform.parent == null)
            {
                m_currentAtom.transform.position = transform.position;
            }
            else
            {
                if(this.GetComponentInParent<GlobalCtrl>().allAtom/*GameObject.Find("Molekül").GetComponent<EditMode>().editMode == false*/)
                {
                    GameObject.Find("Molekül").transform.rotation = transform.rotation * Quaternion.Inverse(lastRotController) * lastRotationMolecule;
                    deltaPos = GameObject.Find("Molekül").transform.position - m_currentAtom.transform.position;
                    GameObject.Find("Molekül").transform.position = transform.position + deltaPos;
                } else
                {
                    
                    //GameObject.Find("Molekül").GetComponent<EditMode>().regroupAtoms(m_currentAtom);
                    //GameObject.Find("editTeil").transform.rotation = transform.rotation * Quaternion.Inverse(lastRotController) * lastRotationMolecule;
                    //deltaPos = GameObject.Find("editTeil").transform.position - m_currentAtom.transform.position;
                    //GameObject.Find("editTeil").transform.position = transform.position + deltaPos;

                    m_currentAtom.transform.position = transform.position;
                }

            }
        }
        //Needed to connect 2 Atoms
        checkConnection();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Kohlenstoff"))
        {
            return;
        }
        m_Interactables.Add(other.gameObject.GetComponent<CarbonAtom>());

    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.CompareTag("Kohlenstoff"))
        {
            return;
        }
        m_Interactables.Remove(other.gameObject.GetComponent<CarbonAtom>());
    }

    public void Pickup()
    {
        m_currentAtom = GetNearestCarbonAtom();
        //Null check
        if (!m_currentAtom)
            return;
        // Already held, check
        if (m_currentAtom.m_ActiveHand)
            m_currentAtom.m_ActiveHand.Drop();

        deltaPos = GameObject.Find("Molekül").transform.position - m_currentAtom.transform.position;
        if (m_currentAtom != null && m_currentAtom.transform.parent == null)
        {
            m_currentAtom.transform.position = transform.position;
        }
        else
        {
            if (this.GetComponentInParent<GlobalCtrl>().allAtom)
            {
                GameObject.Find("Molekül").transform.position = transform.position + deltaPos;
            } else
            {
                m_currentAtom.transform.position = transform.position + deltaPos;
            }
                
        }

        lastRotationMolecule = GameObject.Find("Molekül").transform.rotation;
        lastPosController = transform.position;
        lastRotController = transform.rotation;
        isPressed = true;
    }

    public void Drop()
    {
        // Null check
        if (!m_currentAtom)
            return;

        if(GameObject.Find("Molekül").GetComponent<EditMode>().editMode == true)
        {
            while(GameObject.Find("editTeil").transform.GetChildCount()>0)
            {
                GameObject.Find("editTeil").transform.GetChild(0).parent = GameObject.Find("Molekül").transform;
            }
            GameObject.Find("editTeil").transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        // Verbindung erstellen
        if (connectAtom != null)
        {
            if (Vector3.Distance(connectAtom.transform.position, m_currentAtom.transform.position) <= 0.25)
            {
                List<CarbonAtom> senden = new List<CarbonAtom>();
                senden.Add(connectAtom);
                senden.Add(m_currentAtom);
                if(connectAtom == GameObject.Find("Molekül").GetComponent<EditMode>().fixedAtom)
                {
                    connectAtom.GetComponent<Renderer>().material.color = new Color(1, 0, 0, 1);
                } else
                {
                    connectAtom.GetComponent<Renderer>().material.color = new Color(0, 0, 0, 1);
                }
                
                this.GetComponentInParent<GlobalCtrl>().verbindungErstellen(senden);
                senden.Clear();
            }
            connectAtom = null;
        }

        //Clear
        m_currentAtom.m_ActiveHand = null;
        m_currentAtom = null;
    }

    private CarbonAtom GetNearestCarbonAtom()
    {
        CarbonAtom nearest = null;
        float minDistance = float.MaxValue;
        float distance = 0.0f;

        foreach(CarbonAtom interactable in m_Interactables)
        {
            distance = (interactable.transform.position - transform.position).sqrMagnitude;
            if(distance < minDistance && interactable != GameObject.Find("Molekül").GetComponent<EditMode>().fixedAtom)
            {
                minDistance = distance;
                nearest = interactable;
            }
        }
        return nearest;
    }

    public void checkConnection()
    {
        if (!m_currentAtom)
            return;

        List<CarbonAtom> ctrl = GetComponentInParent<GlobalCtrl>().list_curCarbonAtoms;
        foreach(CarbonAtom atom in ctrl)
        {
            float distance = Vector3.Distance(atom.transform.position, m_currentAtom.transform.position);
            if (distance <= 0.25)
            {
                bool alreadyConnected = false;
                foreach(ConnectionStatus cs in atom.getAllConPoints())
                {
                    if (cs.otherAtomID == m_currentAtom._id)
                        alreadyConnected = true;
                }
                if (atom != m_currentAtom && alreadyConnected == false)
                {
                    bool connect1 = false;
                    bool connect2 = false;
                    foreach (ConnectionStatus con in atom.getAllConPoints())
                    {
                        if (con.isConnected == false)
                            connect1 = true;
                    }
                    foreach (ConnectionStatus con in m_currentAtom.getAllConPoints())
                    {
                        if (con.isConnected == false)
                            connect2 = true;
                    }

                    if (connect1 && connect2)
                    {
                        if (connectAtom != null)
                        {
                            if (connectAtom != atom)
                            {
                                connectAtom.GetComponent<Renderer>().material.color = new Color(0, 0, 0, 1);
                            }
                        }
                        atom.GetComponent<Renderer>().material.color = new Color(0, 1, 0, 1);
                        connectAtom = atom;
                    }
                }
            }
            else
            {
                if (atom != GameObject.Find("Molekül").GetComponent<EditMode>().fixedAtom)
                {
                    atom.GetComponent<Renderer>().material.color = new Color(0, 0, 0, 1);
                }
            }
        }
    }
    
    
}
