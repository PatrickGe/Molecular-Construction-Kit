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
    public bool initMove = false;
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
                GameObject.Find("Molekül").transform.position = transform.position + deltaPos;
            }
            
            lastRotationMolecule = GameObject.Find("Molekül").transform.rotation;
            lastPosController = transform.position;
            lastRotController = transform.rotation;
            isPressed = true;
            initMove = true;
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
                GameObject.Find("Molekül").transform.rotation = transform.rotation * Quaternion.Inverse(lastRotController) * lastRotationMolecule ;
                deltaPos = GameObject.Find("Molekül").transform.position - m_currentAtom.transform.position;
                GameObject.Find("Molekül").transform.position = transform.position + deltaPos;
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
        ////Get nearest Interactable
        //m_currentAtom = GetNearestCarbonAtom();
        ////Null check
        //if (!m_currentAtom)
        //    return;
        //// Already held, check
        //if (m_currentAtom.m_ActiveHand)
        //    m_currentAtom.m_ActiveHand.Drop();
        //// Position
        //if (m_currentAtom != null && m_currentAtom.transform.parent != null)
        //{
        //    Vector3 localMove = -m_currentAtom.transform.localPosition;
        //    GameObject.Find("Molekül").transform.position = m_currentAtom.transform.position;
        //    m_currentAtom.transform.localPosition = new Vector3(0, 0, 0);
        //    posDiff = transform.position - m_currentAtom.transform.position + localMove;
        //    foreach (Transform t in GameObject.Find("Molekül").GetComponentInChildren<Transform>())
        //    {
        //        if (t != m_currentAtom)
        //        {
        //            t.transform.localPosition += posDiff;
        //        }
        //    }
        //    //lastRotController = transform.rotation.eulerAngles;
        //    startRot = transform.rotation.eulerAngles;
        //}
        //m_currentAtom.transform.position = transform.position;
        ////Set active hand
        //m_currentAtom.m_ActiveHand = this;
    }

    public void Drop()
    {
        // Null check
        if (!m_currentAtom)
            return;
        // Verbindung erstellen
        if(connectAtom != null)
        {
            if(Vector3.Distance(connectAtom.transform.position, m_currentAtom.transform.position) <= 0.25)
            {
                List<CarbonAtom> senden = new List<CarbonAtom>();
                senden.Add(connectAtom);
                senden.Add(m_currentAtom);
                connectAtom.GetComponent<Renderer>().material.color = new Color(0, 0, 0, 1);
                this.GetComponentInParent<GlobalCtrl>().verbindungErstellen(senden);
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
            if(distance < minDistance)
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

            if (distance<= 0.25)
            {
                if(atom != m_currentAtom)
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
                            if(connectAtom != atom)
                            {
                                connectAtom.GetComponent<Renderer>().material.color = new Color(0, 0, 0, 1);
                            }
                        }
                        atom.GetComponent<Renderer>().material.color = new Color(0, 1, 0, 1);
                        connectAtom = atom;
                    }
                }  
            } else {
                atom.GetComponent<Renderer>().material.color = new Color(0, 0, 0, 1);
            }
        }
    }
    
    
}
