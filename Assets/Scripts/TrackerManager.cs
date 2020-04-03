using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TrackerManager : MonoBehaviour
{
    private Dictionary<string, TrackerBehavior> trackers = new Dictionary<string, TrackerBehavior>();

    public void UpdateTrackers()
    {
        foreach (TrackerBehavior tracker in Object.FindObjectsOfType(typeof(TrackerBehavior)))
        {
            if(tracker.gameObject?.scene.name?.CompareTo("RealWorld") == 0){
                trackers.Add(tracker.trackerName, tracker);
            }
        }
    }

    public bool GetTracker(string trackerName, out TrackerBehavior tracker)
    {
        return trackers.TryGetValue(trackerName, out tracker);
    }
}