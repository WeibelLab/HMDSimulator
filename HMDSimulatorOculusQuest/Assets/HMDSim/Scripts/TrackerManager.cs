using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


/// <summary>
/// All trackers go thorough this class
/// </summary>
public class TrackerManager : MonoBehaviour
{
    private Dictionary<string, TrackerBehavior> trackers = new Dictionary<string, TrackerBehavior>();
    private Dictionary<string, List<TrackedObject>> trackedObjects = new Dictionary<string, List<TrackedObject>>();

    public void UpdateTrackers(string realName, string arName)
    {
        // only allow trackers in the VR scene
        foreach (TrackerBehavior tracker in Object.FindObjectsOfType(typeof(TrackerBehavior)))
        {
            if(tracker.gameObject?.scene.name?.CompareTo(realName) == 0){
                trackers.Add(tracker.trackerName, tracker);
            }
        }
        
        // only allow tracked objects in the AR scene
        foreach (TrackedObject tracked in Object.FindObjectsOfType(typeof(TrackedObject)))
        {
            if (tracked.gameObject?.scene.name?.CompareTo(arName) == 0)
            {
                if (!trackedObjects.ContainsKey(tracked.trackerName))
                {
                    trackedObjects[tracked.trackerName] = new List<TrackedObject>();
                }

                trackedObjects[tracked.trackerName].Add(tracked);
            }
        }
    }

    public void ForceUpdateTrackedObject()
    {
        foreach (List<TrackedObject> tracked in trackedObjects.Values)
        {
            foreach (TrackedObject t in tracked)
            { 
                if (t.isActiveAndEnabled)
                {
                    t.PerformUpdate();
                }
            }
        }
    }

    public bool GetTracker(string trackerName, out TrackerBehavior tracker)
    {
        return trackers.TryGetValue(trackerName, out tracker);
    }
}