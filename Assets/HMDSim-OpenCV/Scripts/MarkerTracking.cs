using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

public class MarkerTracking : MonoBehaviour
{
    [Serializable]
    public class MarkerData
    {
        public ArucoMarker marker;
        public Vector3 position = Vector3.positiveInfinity;
        public Vector3 rotation;
        public bool found = false;
    }

    [Serializable]
    public class NamedMarkerData
    {
        public int markerId;
        public MarkerData markerData;
    }

    public CameraCalibration cameraCalibration;


    public List<NamedMarkerData> trackedMarkers;
    public int expectedMarkerCount = 16;
    private Dictionary<int, MarkerData> _trackedMarkers = new Dictionary<int, MarkerData>();

    // Start is called before the first frame update
    void Start()
    {
        // Build dictionary
        foreach (var markers in trackedMarkers)
        {
            _trackedMarkers.Add(markers.markerId, markers.markerData);
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!cameraCalibration.calibrated)
        {
            return;
        }

        Texture2D image = cameraCalibration.image;
        byte[] rgbBuffer = image.GetRawTextureData();
        int width = image.width;
        int height = image.height;
        
        foreach (var pair in _trackedMarkers)
        {
            ArucoMarker marker = pair.Value.marker;
            float[] posVecs = new float[3 * expectedMarkerCount];
            float[] rotVecs = new float[3 * expectedMarkerCount];
            int[] markerIds = new int[expectedMarkerCount];
            int count = HMDSimOpenCV.Instance.Aruco_EstimateMarkersPoseWithDetector(rgbBuffer, width, height,
                (int) marker.MarkerDictionary, marker.markerSize, cameraCalibration.chBoard.detectorHandle, expectedMarkerCount, posVecs, rotVecs, markerIds);

            bool found = false;

            for (int i = 0; i < count; i++)
            {
                if (markerIds[i] == marker.markerId)
                {
                    // Found the correct marker
                    pair.Value.position = new Vector3(posVecs[i * 3], posVecs[i * 3 + 1], posVecs[i * 3 + 2]);
                    pair.Value.rotation = new Vector3(rotVecs[i * 3], rotVecs[i * 3 + 1], rotVecs[i * 3 + 2]);
                    found = true;
                }
            }

            pair.Value.found = found;
            //Debug.Log("MarkerId: " + marker.markerId + ", result: " + found);
        }
    }

    public void GetTranslation(Object obj)
    {
        TranslationCallback tcb = (TranslationCallback) obj;
        if (tcb != null)
        {
            if (_trackedMarkers.ContainsKey(tcb.markerId))
            {
                MarkerData data = _trackedMarkers[tcb.markerId];
                tcb.result = cameraCalibration.trackableCamera.transform.TransformPoint(data.position);
            }
            else
            {
                Debug.Log("Cannot find markerId: " + tcb.markerId);
            }
        }
        else
        {
            Debug.Log("Cannot cast obj");
        }

    }

    public void GetRotation(Object obj)
    {
        RotationCallback rcb = (RotationCallback)obj;
        if (rcb != null)
        {
            if (_trackedMarkers.ContainsKey(rcb.markerId))
            {
                MarkerData data = _trackedMarkers[rcb.markerId];
                rcb.result = cameraCalibration.trackableCamera.transform.rotation * Quaternion.Euler(new Vector3(- Mathf.Rad2Deg * data.rotation.x, - Mathf.Rad2Deg * data.rotation.z + 180, Mathf.Rad2Deg * data.rotation.y)); // 
            }
            else
            {
                Debug.Log("Cannot find markerId: " + rcb.markerId);
            }

        }
        else
        {
            Debug.Log("Cannot cast obj");
        }
    }
}
