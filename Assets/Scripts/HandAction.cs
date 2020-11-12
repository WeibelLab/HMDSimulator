using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class HandAction : MonoBehaviour
{

    // a reference to the action
    public SteamVR_Action_Single Squeeze;
    public SteamVR_Action_Boolean GrabGrip;
    public SteamVR_Action_Boolean MenuClick;

    // a reference to the hand
    public SteamVR_Input_Sources handType;

    public List<GrabbableObject> closeGameObjects = new List<GrabbableObject>();

    public float axis = 0;

    public float triggerThreshold = 0.5f;

    public GrabbableObject objectInHand = null;

    /// <summary>
    /// spaamTargetManager is a singleton set during runtime in the AR scene (so there is no direct way of linking it
    /// in the VR scene)
    /// </summary>
    [HideInInspector]
    public SPAAMTargetManager spaamTargetManager;

    public SPAAMSolver spaamSolver;

    public Transform calibrationCube;

    void Start()
    {
        Squeeze.AddOnChangeListener(TriggerDown, handType);
        GrabGrip.AddOnStateDownListener(GripDown, handType);
        MenuClick.AddOnStateDownListener(MenuDown, handType);
    }

    void Update()
    {
        if (!spaamTargetManager)
        {
            spaamTargetManager = SPAAMTargetManager.Instance;
        }
    }

    public void TriggerDown(SteamVR_Action_Single fromAction, SteamVR_Input_Sources fromSource, float newAxis, float newDelta)
    {
        if (fromSource == handType)
        {
            axis = newAxis;
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

    public void GripDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        //Debug.Log("gripdown");
        if (fromSource == handType)
        {
            if (!spaamTargetManager.initialized)
            {
                spaamTargetManager.InitializePosition();
            }
            else
            {
                # if !UNITY_EDITOR
                Vector3 targetPosition = spaamTargetManager.PerformAlignment();
                Vector3 objectPosition = calibrationCube.position; //TODO
                spaamSolver.PerformAlignment(objectPosition, targetPosition);
                #endif
            }
        }
    }

    public void MenuDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        Debug.Log("MenuDown");
        if (fromSource == handType)
        {
            if (!spaamTargetManager.initialized)
            {
                spaamTargetManager.InitializePosition();
            }
            else
            {
                spaamSolver.Solve();
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
            if ((GameObj.transform.position - transform.position).sqrMagnitude < distance)
            {
                closestGameObject = GameObj;
                distance = (GameObj.transform.position - transform.position).sqrMagnitude;
            }
        }

        return closestGameObject;
    }
}
