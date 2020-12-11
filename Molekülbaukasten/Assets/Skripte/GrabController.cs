using System.Collections;
using System.Collections.Generic;
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
    
    // Start is called before the first frame update
    void Start()
    {
        m_pose = GetComponent<SteamVR_Behaviour_Pose>();
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
                    GameObject.Find("Molekül").transform.rotation = transform.rotation * Quaternion.Inverse(lastRotController) * lastRotationMolecule;
                    deltaPos = GameObject.Find("Molekül").transform.position - m_currentAtom.transform.position;
                    GameObject.Find("Molekül").transform.position = transform.position + deltaPos;
                } else
                {
                    m_currentAtom.transform.position = transform.position;
                }

            }
        }
        //Needed to connect 2 Atoms
        checkConnection();
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
        // Already held, check
        if (m_currentAtom.m_ActiveHand)
            m_currentAtom.m_ActiveHand.Drop();

        deltaPos = GameObject.Find("Molekül").transform.position - m_currentAtom.transform.position;
        //Grab single atom which is not connected to the molecule
        if (m_currentAtom != null && m_currentAtom.transform.parent == null)
        {
            m_currentAtom.transform.position = transform.position;
        }
        //Grab molecule
        else
        {
            //Transform whole molecule or single atom depending on which mode is active
            if (this.GetComponentInParent<GlobalCtrl>().allAtom)
            {
                GameObject.Find("Molekül").transform.position = transform.position + deltaPos;
            } else
            {
                m_currentAtom.transform.position = transform.position + deltaPos;
            }
                
        }
        //Fix Rotation
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

        // Create connection
        if (connectAtom != null)
        {
            //If min. distance is reached
            if (Vector3.Distance(connectAtom.transform.position, m_currentAtom.transform.position) <= GetComponentInParent<GlobalCtrl>().scale * 0.9f)
            {
                //Atoms are added to list
                List<Atom> senden = new List<Atom>();
                senden.Add(connectAtom);
                senden.Add(m_currentAtom);
                if(connectAtom == GameObject.Find("Molekül").GetComponent<EditMode>().fixedAtom)
                {
                    connectAtom.GetComponent<Renderer>().material.color = new Color(1, 0, 0, 1);
                } else
                {
                    connectAtom.setOriginalColor();
                }
                
                this.GetComponentInParent<GlobalCtrl>().createConnection(senden);
                senden.Clear();
            }
            connectAtom = null;
        }

        //Clear
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
            distance = (interactable.transform.position - transform.position).sqrMagnitude;
            if(distance < minDistance)
            {
                minDistance = distance;
                nearest = interactable;
            }
        }
        return nearest;
    }

    /*
     * Is called if one atom is hold near another to check if a connection is possible between them
     * 
     * */
    public void checkConnection()
    {
        if (!m_currentAtom)
            return;
        //Loop over all atoms
        List<Atom> ctrl = GetComponentInParent<GlobalCtrl>().list_curAtoms;
        foreach(Atom atom in ctrl)
        {
            //Checks distance between atoms
            float distance = Vector3.Distance(atom.transform.position, m_currentAtom.transform.position);
            if (distance <= GetComponentInParent<GlobalCtrl>().scale * 0.9f)
            {
                //Checks all connection points to see if they are already connected
                bool alreadyConnected = false;
                foreach(ConnectionStatus cs in atom.getAllConPoints())
                {
                    if (cs.otherAtomID == m_currentAtom._id)
                        alreadyConnected = true;
                }
                //If they aren't already connected, check for free connection point, if all points are alread connected no connection should be possible
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
                    //If a connection is possible, mark atom green
                    if (connect1 && connect2)
                    {
                        if (connectAtom != null)
                        {
                            if (connectAtom != atom)
                            {
                                connectAtom.setOriginalColor();
                            }
                        }
                        atom.GetComponent<Renderer>().material.color = new Color(0, 1, 0, 1);
                        connectAtom = atom;
                    }
                }
            }
            else
            {
                //Reset color
                if (atom != GameObject.Find("Molekül").GetComponent<EditMode>().fixedAtom)
                {
                    atom.setOriginalColor();
                }
            }
        }
    }
    
    
}
