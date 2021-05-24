using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class HandAction : MonoBehaviour
{


    // a reference to the hand
    [Header("Hand configuration")]
    public SteamVR_Input_Sources handType;


    [Header("Calibration interface")]

    /// <summary>
    /// spaamTargetManager is a singleton set during runtime in the AR scene (so there is no direct way of linking it
    /// in the VR scene)
    /// </summary>
    public SteamVR_Action_Boolean Trigger;
    public bool GetPointWhenTrigger = true;
    [HideInInspector]
    public AugmentedRealityCalibrationManager spaamTargetManager;
    public RealWorldCalibrationManager spaamSolver;


    [Header("Holding objects")]
    public SteamVR_Action_Single Squeeze;

    public float triggerThreshold = 0.5f;
    public bool canHoldObjects = true;
    public GrabbableObject objectInHand = null;
    [HideInInspector]
    public List<GrabbableObject> closeGameObjects = new List<GrabbableObject>();


    void Start()
    {
        Squeeze.AddOnChangeListener(SqueezeDown, handType);
        Trigger.AddOnChangeListener(TriggerDown, handType);

    }

    void Update()
    {
        if (!spaamTargetManager)
        {
            spaamTargetManager = AugmentedRealityCalibrationManager.Instance;
        }
    }

    public void SqueezeDown(SteamVR_Action_Single fromAction, SteamVR_Input_Sources fromSource, float newAxis, float newDelta)
    {
        if (!canHoldObjects) return;
        if (fromSource == handType)
        {
            //Debug.Log("Trigger down: " + newAxis);
            if (newAxis > triggerThreshold)
            {
                if (objectInHand == null)
                {
                    // Find closest 
                    var closest = GetClosestGameObject();
                    if (closest != null)
                    {
                        objectInHand = closest;
                        objectInHand.Grab(this.transform);
                    }
                }
            }
            else
            {
                if (objectInHand)
                {
                    // Release
                    objectInHand.Release(this.transform);
                    objectInHand = null;
                }
            }
        }
    }

    public void TriggerDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool state)
    {
        //Debug.Log("trigger down");
        if (fromSource == handType)
        {
            //Debug.Log("trigger down 1");
            if (!state)
            {
                //Debug.Log("trigger down 2");
                if (GetPointWhenTrigger)
                {
                    spaamSolver.PerformAlignment();
                }
            }
            
        }
    }

    void OnTriggerEnter(Collider other)
    {
        GrabbableObject go = other.GetComponent<GrabbableObject>();
        if (go)
        {
            closeGameObjects.Add(go);
        }
    }

    void OnTriggerExit(Collider other)
    {
        GrabbableObject go = other.GetComponent<GrabbableObject>();
        if (go)
        {
            closeGameObjects.Remove(go);
        }
    }

    GrabbableObject GetClosestGameObject()
    {
        GrabbableObject closestGameObject = null;
        float distance = float.MaxValue;
        foreach (GrabbableObject GameObj in closeGameObjects)
        {
            if (GameObj.enabled)
            { 
                // only consider objects that we can grab
                if ((GameObj.transform.position - transform.position).sqrMagnitude < distance && GameObj.CanGrab)
                {
                    closestGameObject = GameObj;
                    distance = (GameObj.transform.position - transform.position).sqrMagnitude;
                }
            }
        }

        return closestGameObject;
    }
}
