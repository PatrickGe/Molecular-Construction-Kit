using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class ControllerInteractions : MonoBehaviour
{
    public SteamVR_Action_Boolean triggerklicked = SteamVR_Input.GetBooleanAction("Triggerklick");
    private SteamVR_Behaviour_Pose m_pose = null;

    private Atom m_currentAtom = null;
    public List<Atom> m_Interactables = new List<Atom>();
    private bool isPressed = false;
    private Vector3 deltaPos;


    private Quaternion lastRotationMolecule;
    private Quaternion lastRotController;
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
            pickup();
        }
        //Trigger Up
        if (triggerklicked.GetStateUp(m_pose.inputSource))
        {
            drop();
            isPressed = false;
        }

        //During Trigger pressed
        if (isPressed)
        {
            whilePressed();
            
        }
    }

    private void drop()
    {
        
    }

    private void pickup()
    {
        
    }

    private void whilePressed()
    {

        //Transform whole molecule or single atom depending on which mode is active
        if (this.GetComponentInParent<GlobalCtrl>().allAtom)
        {
            GameObject.Find("Molekül").transform.rotation = transform.rotation * Quaternion.Inverse(lastRotController) * lastRotationMolecule;
            deltaPos = GameObject.Find("Molekül").transform.position - m_currentAtom.transform.position;
            GameObject.Find("Molekül").transform.position = transform.position + deltaPos;
        }
        else
        {
            //maybe add Dummys to move them with their atom
            m_currentAtom.transform.position = transform.position;
            
        }

        //Needed to connect 2 Atoms
        checkConnection();
    }

    private void checkConnection()
    {
        
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


}
