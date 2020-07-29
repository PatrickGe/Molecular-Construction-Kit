using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerknüpfungAtome : MonoBehaviour
{
    float distance = 1;
    public int redCol;
    public int greenCol;
    public int blueCol;
    public bool startedFlashing = false;
    public bool flashingIn = true;
    public bool CR_running = false;
    public bool alreadyGrabbed = false;
    GameObject zwischenSpeicher;
    GameObject[] alleKohlenstoff;
    SenseGlove_Grabable grabable;
    List<CarbonAtom> senden;

    // Start is called before the first frame update
    void Start()
    {
        senden = new List<CarbonAtom>();
        grabable = GetComponent<SenseGlove_Grabable>();
    }

    // Update is called once per frame
    void Update()
    {

        alleKohlenstoff = GameObject.FindGameObjectsWithTag("Kohlenstoff");
        if (grabable.IsGrabbed())
        {
            
            for (int j = 1; j <= alleKohlenstoff.Length; j++)
            {
                if (grabable.name != alleKohlenstoff[j - 1].name && grabable.transform.parent.name != "Molekül")
                {
                    if (grabable.GetComponent<CarbonAtom>().c0.otherAtomID != alleKohlenstoff[j - 1].GetComponent<CarbonAtom>()._id &&
                        grabable.GetComponent<CarbonAtom>().c1.otherAtomID != alleKohlenstoff[j - 1].GetComponent<CarbonAtom>()._id &&
                        grabable.GetComponent<CarbonAtom>().c2.otherAtomID != alleKohlenstoff[j - 1].GetComponent<CarbonAtom>()._id &&
                        grabable.GetComponent<CarbonAtom>().c3.otherAtomID != alleKohlenstoff[j - 1].GetComponent<CarbonAtom>()._id) {
                        alreadyGrabbed = true;
                        distance = Vector3.Distance(grabable.transform.position, alleKohlenstoff[j - 1].transform.position);
                        if (distance <= 0.25 && (alleKohlenstoff[j - 1].transform.parent != null || alleKohlenstoff.Length <= 2) && !alleKohlenstoff[j - 1].GetComponent<CarbonAtom>().isFull)
                        {
                            zwischenSpeicher = alleKohlenstoff[j - 1];
                            alleKohlenstoff[j - 1].GetComponent<Renderer>().material.color = new Color32((byte)redCol, (byte)greenCol, (byte)blueCol, 255);
                            if (startedFlashing == false)
                            {
                                startedFlashing = true;
                                StartCoroutine(FlashObject(distance));
                            }
                            break;
                        }
                        else if (CR_running = true && distance >= 0.25)
                        {
                            startedFlashing = false;
                            StopCoroutine(FlashObject(distance));
                            CR_running = false;
                            if (GameObject.Find("Molekül").GetComponent<EditMode>().editMode == true && GameObject.Find("Molekül").GetComponent<EditMode>().fixedAtom._id != alleKohlenstoff[j - 1].GetComponent<CarbonAtom>()._id)
                            {
                                alleKohlenstoff[j - 1].GetComponent<Renderer>().material.color = new Color32(0, 0, 0, 255);
                            } else if (GameObject.Find("Molekül").GetComponent<EditMode>().editMode == false)
                            {
                                alleKohlenstoff[j - 1].GetComponent<Renderer>().material.color = new Color32(0, 0, 0, 255);
                            }
                        }
                    }
                }
            }

        }
        else if (!grabable.IsGrabbed() && alreadyGrabbed == true)
        {
            alreadyGrabbed = false;
            StopCoroutine(FlashObject(distance));
            startedFlashing = false;
            CR_running = false;

            if (zwischenSpeicher != null && distance <= 0.25 && zwischenSpeicher.GetComponent<CarbonAtom>().isFull == false)
            {
                senden.Clear();
                senden.Add(zwischenSpeicher.GetComponent<CarbonAtom>());
                senden.Add(grabable.GetComponent<CarbonAtom>());
                grabable.transform.parent = GameObject.Find("Molekül").transform;
                if(zwischenSpeicher.GetComponent<CarbonAtom>() == GameObject.Find("Molekül").GetComponent<EditMode>().fixedAtom)
                    zwischenSpeicher.GetComponent<Renderer>().material.color = new Color32(255, 0, 0, 255);
                else
                    zwischenSpeicher.GetComponent<Renderer>().material.color = new Color32(0, 0, 0, 255);
                SendMessage("verbindungErstellen", senden);
                zwischenSpeicher = null;
            }
        }        
    }




    IEnumerator FlashObject(float dist)
    {
        CR_running = true;
        while (dist <= 0.25)
        {
            yield return new WaitForSeconds(0.05f);
            if(flashingIn == true){
                if (greenCol <= 30){
                    flashingIn = false;
                } else {
                    greenCol -= 25;
                    blueCol -= 1;
                }
            }
            if(flashingIn == false) {
                if (greenCol >= 250){
                    flashingIn = true;
                } else {
                    greenCol += 25;
                    blueCol += 1;
                }
            }
            
            CR_running = false;
        }
        CR_running = false;
    }
}
