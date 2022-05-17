using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/**
  Base class for TrackedObjects in the Real/Virtual-Reality scene
 of the simulator
 */
public class TrackedObject : MonoBehaviour
{

    public string trackerName = "";

    public bool isLocal = false;

    // For now
    [HideInInspector]
    public Vector3 offset = new Vector3(100, 100, 100);

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void PerformUpdate()
    {
        TrackerBehavior tracker;
        if (MainManager.Instance.trackerManager.GetTracker(trackerName, out tracker))
        {
            if (isLocal)
            {
                transform.localPosition = tracker.GetTranslation();
                transform.localRotation = tracker.GetRotation();
            }
            else
            {
                transform.position = tracker.GetTranslation() + offset;
                transform.rotation = tracker.GetRotation();
            }
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        PerformUpdate();
    }

    void LateUpdate()
    {
        PerformUpdate();
    }
}
