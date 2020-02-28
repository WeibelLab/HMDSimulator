using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackedObject : MonoBehaviour
{

    public string trackerName = "";

    // For now
    public Vector3 offset = new Vector3(100, 100, 100);

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        TrackerBehavior tracker;
        if (MainManager.Instance.trackerManager.GetTracker(trackerName, out tracker))
        {
            transform.position = tracker.GetTranslation() + offset;
            transform.rotation = tracker.GetRotation();
        }
    }
}
