using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SenseGloveCs;

/// <summary> Attach this object to the Sense Glove to allow it to select an object in the room. </summary>
public class Auswahlgeste: MonoBehaviour
{

    //----------------------------------------------------------------------------------------------------------------------------------
    // Public Properties

    #region Properties

    /// <summary> The camerRig object that controls the position of the VR setup within the scene, including that of the camera_eye. </summary>
    [Header("Required Components")]
    [Tooltip("The camerRig object that controls the position of the VR setup within the scene, including that of the camera_eye.")]
    public GameObject cameraRig;

    /// <summary> The HMD within the cameraRig. </summary>
    [Tooltip("The HMD within the cameraRig.")]
    public GameObject camera_eye;

    /// <summary> The origin of the pointer, which extends from its forward (Z) axis. </summary>
    [Tooltip("The origin of the pointer, which extends from its forward (Z) axis.")]
    public Transform pointerOriginZ;

    /// <summary> The glove to use for selection. </summary>
    [Tooltip("The glove to use for selection.")]
    public SenseGlove_HandModel senseGlove;

    /// <summary> A collider placed in the hand that is used to detect gestures. </summary>
    [Tooltip("A collider placed in the hand that is used to detect fingers.")]
    public SenseGlove_FingerDetector gestureDetector;

    /// <summary> The cooldown (in seconds) in between selections </summary>
    [Tooltip("The cooldown (in seconds) in between selections")]
    public float coolDown = 1.0f;

    /// <summary> The layers that this Selector uses for collision. </summary>
    [Header("Collision Options")]
    [Tooltip("The layers that this Selector uses for collision.")]
    public LayerMask collisionLayers;

    /// <summary> The way that the selected position is calculated </summary>
    [Tooltip("The way that the selected position is calculated ")]
    public Util.PointerOption pointerStyle = Util.PointerOption.StraightLine;

    ///// <summary> The maximum distance (in m) that the user can teleport in one go. </summary>
    //[Tooltip("The maximum distance (in m) that the user can teleport in one go.")]
    //private float maxDistance = 10; //private since it has not been implemented yet.

    /// <summary> Whether or not the cameraRig can move in the y-direction. </summary>
    [Tooltip("Whether or not the cameraRig can move in the y-direction.")]
    public bool ignoreY = false;

    /// <summary> The color of the pointer / indicator </summary>
    [Header("Display Options (Optional)")]
    [Tooltip("The color of the pointer / indicator ")]
    public Color indicatorColor = Color.green;

    /// <summary> An optional GameObject that appears (in the hand) to indicate that selection is active. </summary>
    [Tooltip("An optional GameObject that appears (for example in the hand) to indicate that selection is active.")]
    public Renderer activeIndicator;

    /// <summary> Highlighters that are set to active while the selector is online. </summary>
    [Tooltip("Highlighters (to aid navigation) that are set to active while the pointer is active.")]
    public List<Renderer> selectionHighlights = new List<Renderer>();

    //----------------------------------------------------------------------------------------------------------------------------------
    // Private Properties

    /// <summary> Determines if the selector is currently active. </summary>
    private bool isActive = false;

    /// <summary> The gameobject representing the pointer from the origin to the endPoint. </summary>
    private GameObject pointer;

    /// <summary> A sphere that appears at the desired location, if any is available. </summary>
    private GameObject endPointTracker;

    /// <summary> Optional, used to disable the teleporter while the user is interacting with an object. </summary>
    private SenseGlove_GrabScript grabScript;

    /// <summary> Timer for the cooldown of the teleport </summary>
    private float coolDownTimer = 0;
    
    /// <summary> Timer to activate the laser </summary>
    private float activateTimer = 0; //uses isactivated

    /// <summary> Time (seconds) that the user must point with the index finger before the pointer activates. </summary>
    public static float activationTime = .2f;



    /// <summary> Timer to selection. </summary>
    private float selecionTimer = 0;

    /// <summary> Time (seconds) that the user must press with their thumb before the selection activates. </summary>
    public static float selectionTime = .2f;

    /// <summary> Indicates that the user has selected using the fingerDetector. </summary>
    private bool hasSelected = false;

    /// <summary> Thickness of the laser if using a Straight Line style. </summary>
    private static readonly float laserThickness = 0.002f;

    /// <summary> The diameter of the sphere that indicates where the player will teleport to. </summary>
    private static readonly float endPointDiameter = 0.05f;


    RaycastHit hit;

    SenseGlove_Object senseGlove_Object;

    #endregion Properties

    //----------------------------------------------------------------------------------------------------------------------------------
    // Utility Methods

    #region Utility

    /// <summary> Create new Pointer objects based on the chosen style. </summary>
    protected void CreatePointer()
    {
        Material ptrMaterial = new Material(Shader.Find("Unlit/Color"));
        ptrMaterial.SetColor("_Color", indicatorColor);

        //create the pointer
        if (this.pointerStyle == Util.PointerOption.StraightLine)
        {
            this.pointer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            this.pointer.transform.parent = null;
            this.pointer.transform.localScale = new Vector3(laserThickness, laserThickness, -20f);

            this.pointer.transform.position = this.pointerOriginZ.position + (this.pointerOriginZ.rotation * new Vector3(0, 0, 10f));
            this.pointer.transform.parent = this.pointerOriginZ;
            this.pointer.transform.localRotation = Quaternion.identity;

            GameObject.Destroy(pointer.GetComponent<BoxCollider>()); //remove boxCollider so no collision calculation is done.

            pointer.GetComponent<MeshRenderer>().material = ptrMaterial;

            //Setup the end point
            this.endPointTracker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            this.endPointTracker.name = "SelectionLocation";
            GameObject.Destroy(endPointTracker.GetComponent<SphereCollider>());
            endPointTracker.transform.parent = null;
            endPointTracker.transform.localScale = new Vector3(endPointDiameter, endPointDiameter, endPointDiameter);
            endPointTracker.GetComponent<MeshRenderer>().material = ptrMaterial;
        }
        

    }

    /// <summary> Set the pointer and its active indicator to true/false. </summary>
    /// <param name="active"></param>
    public void SetPointer(bool active)
    {
        this.pointer.SetActive(active);
        if (this.endPointTracker != null)
        {
            this.endPointTracker.SetActive(active);
        }
        if (this.activeIndicator != null)
        {
            this.activeIndicator.enabled = active;
        }
    }

    /// <summary> Activate / Deactivate the selection HighLights </summary>
    /// <param name="active"></param>
    public void SetHighlights(bool active)
    {
        for (int i = 0; i < this.selectionHighlights.Count; i++)
        {
            this.selectionHighlights[i].enabled = active;
        }
    }

    //----------------------------------------------------------------------------------------------------------------------------------
    // Selection Methods

    /// <summary> Activate the selector manually, untill you call a disable function. </summary>
    public void ActivateSelector()
    {
        this.SetPointer(true);
        this.SetHighlights(true);
        this.isActive = true;
    }

    /// <summary> Disable the selector untill you call ActivateSelector. </summary>
    public void DisableSelector()
    {
        this.SetPointer(false);
        this.SetHighlights(false);
        this.isActive = false;
        this.activateTimer = 0;
    }
    
    /// <summary> Calculates the desired position of the selector based on the chosen PointerOption </summary>
    /// <returns></returns>
    public bool CalculateDesiredPos(out Vector3 newPos)
    {
        newPos = Vector3.zero;
        bool validHit = false;

        switch (this.pointerStyle)
        {
            case Util.PointerOption.StraightLine:

                Ray raycast = new Ray(this.pointerOriginZ.position, this.pointerOriginZ.forward);
                
                bool bHit = Physics.Raycast(raycast, out hit, 100f, this.collisionLayers);
                //hit.collider liefert den Name des getroffenen Objekts
                float d = 20f;
                
                if (bHit)
                {
                    Vector3 destination = hit.point;
                    //Vector3 rigPos = this.cameraRig.transform.position;
                    //Vector3 dir2D = new Vector3(destination.x - rigPos.x, 0, destination.z - rigPos.z);

                    //if (dir2D.magnitude > this.maxDistance)
                    //{
                    //    destination = rigPos + (dir2D.normalized * this.maxDistance);
                    //    destination.y = hit.point.y;
                    //}
                    newPos = destination;
                    d = (newPos - pointerOriginZ.position).magnitude; //size (m between points)
                    validHit = true;
                }

                //update pointer
                this.pointer.transform.parent = null;
                this.pointer.transform.localScale = new Vector3(laserThickness, laserThickness, d);
                this.pointer.transform.position = this.pointerOriginZ.position + (this.pointerOriginZ.rotation * new Vector3(0, 0, (d / 2.0f)));
                this.pointer.transform.parent = this.pointerOriginZ;

                return validHit;
        }

        newPos = Vector3.zero;
        return false;
    }

    /// <summary> SelectionConfirmed to the position of the endPointTracker, but only if it is active on a valid position. </summary>
    public void SelectionConfirmed()
    {
        if (this.IsActive() && this.endPointTracker.activeInHierarchy)
        {
            if(hit.collider.name == "Öffnen" && (guiSave.activeInHierarchy == false))
            {
                senseGlove_Object.SendBuzzCmd(new bool[5] { true, false, false, false, false }, 100, 50);
                GameObject.Find("Molekül").GetComponent<EditMode>().editMode = false;
                GameObject.Find("UI0").transform.position = new Vector3(0, 0, 0);
                GameObject.Find("UI1").transform.position = new Vector3(0, 0, 0);
                GameObject.Find("UI2").transform.position = new Vector3(0, 0, 0);
                GameObject.Find("UI3").transform.position = new Vector3(0, 0, 0);
                GameObject.Find("Molekül").SetActive(false);
                this.GetComponent<GlobalCtrl>().loadGUILoad();
                guiLoad.SetActive(true);
            } else if(hit.collider.name == "Speichern" && guiSave.activeInHierarchy == false)
            {
                senseGlove_Object.SendBuzzCmd(new bool[5] { true, false, false, false, false }, 100, 50);
                guiSave.SetActive(true);
                GameObject.Find("Molekül").GetComponent<EditMode>().editMode = false;
                GameObject.Find("UI0").transform.position = new Vector3(0, 0, 0);
                GameObject.Find("UI1").transform.position = new Vector3(0, 0, 0);
                GameObject.Find("UI2").transform.position = new Vector3(0, 0, 0);
                GameObject.Find("UI3").transform.position = new Vector3(0, 0, 0);
                GameObject.Find("Molekül").SetActive(false);
            } else if(hit.collider.name == "PeriodensystemC" && (guiSave.activeInHierarchy == false)){
                senseGlove_Object.SendBuzzCmd(new bool[5] { true, false, false, false, false }, 100, 50);
                SendMessage("kohlenstoffErstellen");
                //Methode von NeuesGameObjekt aufrufen: neuer Kohlenstoff
            } else if (hit.collider.name == "recycle bin" && (guiSave.activeInHierarchy == false))
            {
                senseGlove_Object.SendBuzzCmd(new bool[5] { true, false, false, false, false }, 100, 50);
                this.GetComponent<GlobalCtrl>().recycle();
            } else if (hit.collider.name.StartsWith("atom") && (guiSave.activeInHierarchy == false))
            {
                senseGlove_Object.SendBuzzCmd(new bool[5] { true, false, false, false, false }, 100, 50);
                //einzelnes Kohlenstoff Atom ausgewählt -- Bearbeitungsmodus öffnen
                    if (GameObject.Find("Molekül").GetComponent<EditMode>().editMode == false)
                    {
                        GameObject.Find("Molekül").GetComponent<EditMode>().editMode = true;
                        GameObject.Find("Molekül").GetComponent<EditMode>().fixedAtom = hit.collider.gameObject.GetComponent<Atom>();
                        hit.collider.gameObject.GetComponent<Renderer>().material.color = new Color32(255, 0, 0, 255);
                        hit.collider.gameObject.GetComponent<SenseGlove_Grabable>().editMarker = true;
                    }
                    else if (GameObject.Find("Molekül").GetComponent<EditMode>().editMode == true && hit.collider.gameObject.GetComponent<Atom>()._id == GameObject.Find("Molekül").GetComponent<EditMode>().fixedAtom._id)
                    {
                        hit.collider.gameObject.GetComponent<SenseGlove_Grabable>().editMarker = false;
                        GameObject.Find("Molekül").GetComponent<EditMode>().editMode = false;
                        GameObject.Find("Molekül").GetComponent<EditMode>().fixedAtom = null;
                        hit.collider.gameObject.GetComponent<Renderer>().material.color = new Color32(0, 0, 0, 255);
                    }
            }
        }
    }

    /// <summary> Returns true if the laser should activate. </summary>
    /// <returns></returns>
    public virtual bool ShouldActivate()
    {
        if (this.gestureDetector != null && (this.senseGlove == null || this.senseGlove.senseGlove.GloveReady))
        {
            if (this.grabScript == null || (this.grabScript != null && !this.grabScript.IsTouching()))
            {
                return !this.gestureDetector.TouchedBy(Finger.Index)
                    && this.gestureDetector.TouchedBy(Finger.Middle)
                    && this.gestureDetector.TouchedBy(Finger.Ring)
                    && this.gestureDetector.TouchedBy(Finger.Little);
            }
        }
        return false;
    }

    /// <summary> Returns true if we should select. </summary>
    /// <returns></returns>
    public virtual bool ShouldSelect()
    {
        if (this.gestureDetector != null) //no need to check if active, because otherwise the selector is not even active.
        {
            return this.gestureDetector.TouchedBy(Finger.Thumb);
        }
        return false;
    }

    #endregion Utility

    //----------------------------------------------------------------------------------------------------------------------------------
    // Variable Access

    #region Variables

    /// <summary> Check if the selector is currently active. </summary>
    /// <returns></returns>
    public bool IsActive()
    {
        return this.isActive;
    }
    
    /// <summary> Check if the cooldown timer is currently running </summary>
    /// <returns></returns>
    public bool IsOnCoolDown()
    {
        return this.coolDownTimer < this.coolDown;
    }

    public Object[] allGameObjects;
    public GameObject guiSave;
    public GameObject guiLoad;

    #endregion Variables

    //----------------------------------------------------------------------------------------------------------------------------------
    // Monobehaviour

    #region Monobehaviour

    // Use this for initialization
    protected virtual void Start ()
    {
        if (this.senseGlove != null)
        {
            senseGlove_Object = this.GetComponent<SenseGlove_Object>();
            this.grabScript = this.senseGlove.gameObject.GetComponent<SenseGlove_GrabScript>();
        }
        CreatePointer();
        SetPointer(false);
        SetHighlights(false);
        this.coolDownTimer = this.coolDown; //set to equal so we can select right away.
        allGameObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject));
        foreach (GameObject obj in allGameObjects)
        {
            if (obj.name == "GUISave")
            {
                guiSave = obj;
            }
            if(obj.name == "GUILoad")
            {
                guiLoad = obj;
            }
        }
    }

    // Update is called once per frame
    protected virtual void Update ()
    {
        if (this.coolDownTimer < this.coolDown)
        {
            this.coolDownTimer += Time.deltaTime;
            if (this.endPointTracker.activeInHierarchy) { this.endPointTracker.SetActive(false); } //disable end point (and with it, teleport)
        }
        else
        {
            //check activation gestures
            if (this.gestureDetector != null)
            {
                bool shouldActivate = this.ShouldActivate();
                if (shouldActivate && !this.isActive)
                {
                    if (this.activateTimer < activationTime)
                    {
                        this.activateTimer += Time.deltaTime;
                    }
                    else
                    {
                        this.ActivateSelector();
                    }
                }
                else if (!shouldActivate && this.isActive)
                {
                    this.DisableSelector();
                }
            }

            if (this.IsActive())
            {
                //calculate the desired position and put the endPoint tracker over there
                Vector3 newPos;
                if (CalculateDesiredPos(out newPos))
                {
                    if (!this.endPointTracker.activeInHierarchy) { this.endPointTracker.SetActive(true); }
                    this.endPointTracker.transform.position = newPos;
                }
                else
                {
                    if (this.endPointTracker.activeInHierarchy) { this.endPointTracker.SetActive(false); }
                }


                //check selection gesture
                if (this.gestureDetector != null)
                {
                    bool shouldSelect = this.ShouldSelect();
                    if (shouldSelect && !hasSelected)
                    {
                        if (this.selecionTimer < selectionTime)
                        {
                            this.selecionTimer += Time.deltaTime;
                        }
                        else
                        {
                            this.SelectionConfirmed();
                            this.hasSelected = true;
                        }
                    }
                    else if (!shouldSelect)
                    {
                        this.hasSelected = false;
                    }
                }


            }
        }
        
	}

    #endregion Monobehaviour

}