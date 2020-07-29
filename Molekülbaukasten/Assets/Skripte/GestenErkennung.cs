using SenseGloveCs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GestenErkennung : MonoBehaviour
{

    public SenseGlove_FingerDetector fingerZuFaust;
    public bool thumb = false;
    public bool index = false;
    public bool middle = false;
    public bool ring = false;
    public bool little = false;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        this.zeigenGeste();
    }


    public void zeigenGeste()
    {
        if (this.fingerZuFaust != null)
        {
            // nur bei Änderungen der Finger (Einrollen / Ausrollen) wird das nachfolgende ausgeführt
            if (index != this.fingerZuFaust.TouchedBy(Finger.Index) || middle != this.fingerZuFaust.TouchedBy(Finger.Middle) || ring != this.fingerZuFaust.TouchedBy(Finger.Ring) || little != this.fingerZuFaust.TouchedBy(Finger.Little))
            {
                index = this.fingerZuFaust.TouchedBy(Finger.Index);
                middle = this.fingerZuFaust.TouchedBy(Finger.Middle);
                ring = this.fingerZuFaust.TouchedBy(Finger.Ring);
                little = this.fingerZuFaust.TouchedBy(Finger.Little);


                //funktioniert, Werte werden true, wenn Finger eingerollt ist
                //Debug.Log("Index" + this.fingerZuFaust.TouchedBy(Finger.Index));
                //Debug.Log("Middle" + this.fingerZuFaust.TouchedBy(Finger.Middle));
                //Debug.Log("Ring" + this.fingerZuFaust.TouchedBy(Finger.Ring));
                //Debug.Log("Little" + this.fingerZuFaust.TouchedBy(Finger.Little));

            }
            
            
        }
        
    }
}
