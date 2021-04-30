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
        public long timestamp;
        public Vector3 position;
        public Quaternion rotation;

        public void RecordPoint(long ts, Vector3 pos, Quaternion rot)
        {
            timestamp = ts;
            position = pos;
            rotation = rot;
        }
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
               // for (int j = 0; j < pointsToRecord; ++j)
               // {
               //     trackedObj.trackedPoints[j] = new DataPoint();
               // }
                
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
        if (isRecording)
        {
            long milliseconds = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;

            for (int datasetId = 0; datasetId < coordinateSystemCenters.Length; ++datasetId)
            {
                var dataset = datasets[datasetId];

                // headset pose
                
                // finds the local position of the tracked object wrt to a different coordinate system
                Matrix4x4 headsetInCurrentDataset = datasets[datasetId].worldToLocal * headsetTransform.localToWorldMatrix; // local ToWorldMatrix changes every frame
                dataset.trackedObjects[0].trackedPoints[pointsRecorded].timestamp = milliseconds;
                dataset.trackedObjects[0].trackedPoints[pointsRecorded].rotation = headsetInCurrentDataset.rotation;
                dataset.trackedObjects[0].trackedPoints[pointsRecorded].position = headsetInCurrentDataset.MultiplyPoint3x4(Vector3.zero);

                // for each tracked tiara
                for (int i = 1; i <= tiarasTransform.Length; ++i)
                {
                    var trackedObj = dataset.trackedObjects[i];

                    Matrix4x4 tiaraTransformInCoordinateSystem = datasets[datasetId].worldToLocal * tiarasTransform[i-1].localToWorldMatrix; // local ToWorldMatrix changes every frame
                    trackedObj.trackedPoints[pointsRecorded].timestamp = milliseconds;
                    trackedObj.trackedPoints[pointsRecorded].rotation = tiaraTransformInCoordinateSystem.rotation;
                    trackedObj.trackedPoints[pointsRecorded].position = tiaraTransformInCoordinateSystem.MultiplyPoint3x4(Vector3.zero);
                }

            }

            // increase the points recorded pointer
            pointsRecorded++;
        }

    }

    void SaveDataRecorded()
    {
        string pathToSave = Application.dataPath;
        foreach (var dataset in datasets)
        {
            foreach (var trackedObj in dataset.trackedObjects)
            {
                string csvFilename = string.Format("{0}\\{1}-{2}.csv", pathToSave, dataset.coordinateSystemName, trackedObj.isHeadset ? "headset" : trackedObj.trackedObjectName);

                Debug.Log(string.Format("[DumpToCSV] - (Fake) Done saving {0} data points to to file {1}",pointsRecorded,csvFilename));
            }
            
        }

        

    }



}
