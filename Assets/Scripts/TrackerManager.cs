using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TrackerManager : MonoBehaviour
{
    private Dictionary<string, TrackerBehavior> trackers = new Dictionary<string, TrackerBehavior>();
    private Dictionary<string, TrackedObject> trackedObjects = new Dictionary<string, TrackedObject>();

    public void UpdateTrackers(string realName, string arName)
    {
        foreach (TrackerBehavior tracker in Object.FindObjectsOfType(typeof(TrackerBehavior)))
        {
            if(tracker.gameObject?.scene.name?.CompareTo(realName) == 0){
                trackers.Add(tracker.trackerName, tracker);
            }
        }

        foreach (TrackedObject tracked in Object.FindObjectsOfType(typeof(TrackedObject)))
        {
            if (tracked.gameObject?.scene.name?.CompareTo(arName) == 0)
            {
                trackedObjects.Add(tracked.trackerName, tracked);
            }
        }
    }

    public void ForceUpdateTrackedObject()
    {
        foreach (TrackedObject tracked in trackedObjects.Values)
        {
            if (tracked.isActiveAndEnabled)
            {
                tracked.PerformUpdate();
            }
        }
    }

    public bool GetTracker(string trackerName, out TrackerBehavior tracker)
    {
        return trackers.TryGetValue(trackerName, out tracker);
    }
}