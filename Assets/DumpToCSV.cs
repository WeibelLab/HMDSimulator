using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DumpToCSV : MonoBehaviour
{

    // Headset -- object tracked (Ground Truth for the headset center)
    public Transform headsetTransform;

    // Tiaras -- object tracked (Displaced Headset Center as we will track in the real world)
    public Transform[] tiarasTransform;

    // Coordinate system centers that we care about, make sure to include one that represents the headset center at the start of the application
    public Transform[] coordinateSystemCenters;

    // how many points should we record?
    public int pointsToRecord = 1000;
    public int pointsRecorded = 0;

    private bool isRecording = false;
    private CordinateSystemDataset[] datasets;

    // for each coordinate system center, we will track both the headsetTransform (the ground truth) as well as the tiaras
    public class CordinateSystemDataset
    {
        public string coordinateSystemName;
        public Matrix4x4 worldToLocal;
        public Matrix4x4 localToWorld;
        public TrackedObject[] trackedObjects;
    }

    public class TrackedObject
    {
        public bool isHeadset; // if not, it is a tiara
        public string trackedObjectName;
        public DataPoint[] trackedPoints;
    }

    public struct DataPoint
    {
        public float timestamp;
        public Vector3 position;
        public Quaternion rotation;
    }

    // Start is called before the first frame update
    void Start()
    {
        // let's get started
        int totalObjectsTracked = 1 +                     // hololens
                                  tiarasTransform.Length; // each tiara


        // allocate all the memory for each object first
        datasets = new CordinateSystemDataset[coordinateSystemCenters.Length];
        Debug.Log("[DumpToCSV] - Allocating memory...");
        // the order of the coordinate systems here gives us the order in which we navigate the datasets array
        for (int datasetId = 0; datasetId < coordinateSystemCenters.Length; ++datasetId)
        {
            datasets[datasetId] = new CordinateSystemDataset();
            var dataset = datasets[datasetId];
            dataset.coordinateSystemName = coordinateSystemCenters[datasetId].name;
            dataset.worldToLocal = coordinateSystemCenters[datasetId].worldToLocalMatrix;
            dataset.localToWorld = coordinateSystemCenters[datasetId].localToWorldMatrix;

            // allocate tracked objects and their memory
            dataset.trackedObjects = new TrackedObject[totalObjectsTracked];

            // allocate memory for each tracked point for this coordinate system
            
            for (int i = 0; i < dataset.trackedObjects.Length; ++i)
            {
                dataset.trackedObjects[i] = new TrackedObject();
                var trackedObj = dataset.trackedObjects[i];

                if (i == 0)
                {
                    trackedObj.trackedObjectName = headsetTransform.name;
                    trackedObj.isHeadset = true;
                } else
                {
                    trackedObj.trackedObjectName = tiarasTransform[i - 1].name;
                }

                trackedObj.trackedPoints = new DataPoint[pointsToRecord];
                
            }

            Debug.Log(string.Format("[DumpToCSV] - Memory allocated for {0} and {1} transforms",dataset.coordinateSystemName, totalObjectsTracked));
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // stops recording
            if (isRecording)
            {
                isRecording = false;
                SaveDataRecorded();
            } else
            {
                // starts recording
                pointsRecorded = 0;
                isRecording = true;
            }

        }

        // are we recording?

    }

    void SaveDataRecorded()
    {

    }



}
