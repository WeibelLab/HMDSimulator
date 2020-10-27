using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

public class MarkerTracking : MonoBehaviour
{
    [Header("Camera calibration")]
    [Tooltip("CharucoCameraCalibration helper class that holds the calibration for the camera used by Marker Tracking")]
    public CharucoCameraCalibration cameraCalibrationHelper;

    [Header("Markers Axis")]
    public Matrix4x4 switchAxis;

    [Header("Debugging")]
    [Tooltip("If checked, a debug texture showing internals of tracking will be displayed on debugQuad")]
    public bool debug = false;
    public Texture2D debugTexture2D;
    public Renderer debugQuad;

    // code responsible for managing markers being tracked (min of two always)
    private int expectedMarkerCount = 2;
    private float[] posVecs = new float[3 * 2], rotVecs = new float[9 * 2];
    private int[] markerIds = new int[2];

    private Dictionary<int, ArucoMarker> _trackedMarkers = new Dictionary<int, ArucoMarker>();
    
    // keeps count of unique aruco families and aruco sizes as each <Family,Size> requires a detection pass
    private Dictionary<int, Dictionary<float, HashSet<ArucoMarker>>> _familyMarkerLength = new Dictionary<int, Dictionary<float, HashSet<ArucoMarker>>>();

    private void UpdateMarkerLengthMap()
    {

    }

    private void UpdateTemporaryLists()
    {
        // finds the first power of two greater than the number of markers being tracked
        int lowestPowerOfTwoGreaterThanMarkerCount = _trackedMarkers.Count + 2;
        if (!((lowestPowerOfTwoGreaterThanMarkerCount & (lowestPowerOfTwoGreaterThanMarkerCount - 1)) == 0))
        {
            int p = 1;
            while (p < lowestPowerOfTwoGreaterThanMarkerCount)
                p <<= 1;
            lowestPowerOfTwoGreaterThanMarkerCount = p;
        }

        // new markers were added
        if (lowestPowerOfTwoGreaterThanMarkerCount != expectedMarkerCount)
        {
            expectedMarkerCount = lowestPowerOfTwoGreaterThanMarkerCount;

            posVecs = new float[3 * expectedMarkerCount];
            rotVecs = new float[9 * expectedMarkerCount];
            markerIds = new int[expectedMarkerCount];
        }

    }

    public bool StartTrackingMarker(ArucoMarker marker)
    {
        if (marker == null) return false;
        if (_trackedMarkers.ContainsKey(marker.markerId))
        {
            // unfortunately we can only track one ID
            Debug.LogWarning(String.Format("[MarkerTracking] Already tracking marker with id {0} ({1})!", marker.markerId, _trackedMarkers[marker.markerId].transform.name));
            return false;
        }

        Debug.Log(String.Format("[MarkerTracking] Tracking marker with id {0} ({1})!", marker.markerId, marker.transform.name));
        _trackedMarkers.Add(marker.markerId, marker);

        // update lists used to request markers dectected in the image
        UpdateTemporaryLists();

        // update the number of times we should look for markers
        Dictionary<float, HashSet<ArucoMarker>> markersPerSize;
        if (!_familyMarkerLength.TryGetValue((int)marker.MarkerDictionary, out markersPerSize))
        {
            markersPerSize = new Dictionary<float, HashSet<ArucoMarker>>();
            _familyMarkerLength.Add((int)marker.MarkerDictionary, markersPerSize);
        }

        HashSet<ArucoMarker> markersWithSameLength;
        if (!markersPerSize.TryGetValue(marker.markerSize, out markersWithSameLength))
        {
            markersWithSameLength = new HashSet<ArucoMarker>();
        }

        // makes sure that this aruco marker is accounted
        markersWithSameLength.Add(marker);

        return true;
    }

    public bool StopTrackingMarker(ArucoMarker marker)
    {
        if (marker == null) return false;
        if (!_trackedMarkers.ContainsKey(marker.markerId))
        {
            // unfortunately we can only track one ID
            Debug.LogWarning(String.Format("[MarkerTracking] Not tracking marker with id {0} ({1})!", marker.markerId, _trackedMarkers[marker.markerId].transform.name));
            return false;
        }

        Debug.Log(String.Format("[MarkerTracking] *Not* tracking marker with id {0} ({1})!", marker.markerId, marker.transform.name));
        _trackedMarkers.Remove(marker.markerId);

        // update lists used to request markers dectected in the image
        UpdateTemporaryLists();

        // update the number of times we should look for markers
        Dictionary<float, HashSet<ArucoMarker>> markersPerSize;
        if (!_familyMarkerLength.TryGetValue((int)marker.MarkerDictionary, out markersPerSize))
        {
            // weird... shouldn't happen
            return true;
        }

        HashSet<ArucoMarker> markersWithSameLength;
        if (!markersPerSize.TryGetValue(marker.markerSize, out markersWithSameLength))
        {
            // weird.. shouldn't happen
            return true;
        }

        // makes sure that this aruco marker is accounted
        markersWithSameLength.Remove(marker);
        if (markersWithSameLength.Count == 0)
        {
            markersPerSize.Remove(marker.markerSize);
            
            if (markersPerSize.Count == 0)
            {
                _familyMarkerLength.Remove((int)marker.MarkerDictionary);
            }
        }

        return true;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (!cameraCalibrationHelper.calibrated)
        {
            return;
        }

        Texture2D image = cameraCalibrationHelper.locatableCamera.lastRenderedFrame;
        byte[] rgbBuffer = image.GetRawTextureData();
        int width = image.width;
        int height = image.height;
        
        foreach (var pair in _trackedMarkers)
        {
            ArucoMarker marker = pair.Value.marker;
            float[] posVecs = new float[3 * expectedMarkerCount];
            float[] rotVecs = new float[9 * expectedMarkerCount];
            int[] markerIds = new int[expectedMarkerCount];

            if (!debugTexture2D || width != debugTexture2D.width || height != debugTexture2D.height)
            {
                debugTexture2D = new Texture2D(width, height, TextureFormat.RGB24, false);
            }


            int count;
            if (debug && debugQuad != null)
            {
                byte[] debugBuffer = new byte[width * height * 3];
                count = HMDSimOpenCV.Aruco_EstimateMarkersPoseWithDetector(rgbBuffer, width, height,
                        (int)marker.MarkerDictionary, marker.markerSize, cameraCalibrationHelper.chBoard.detectorHandle, expectedMarkerCount, posVecs, rotVecs, markerIds, debugBuffer);
                debugTexture2D.LoadRawTextureData(debugBuffer);
                debugTexture2D.Apply();
                debugQuad.material.mainTexture = debugTexture2D;
            }
            else
            {
                count = HMDSimOpenCV.Aruco_EstimateMarkersPoseWithDetector(rgbBuffer, width, height,
                        (int)marker.MarkerDictionary, marker.markerSize, cameraCalibrationHelper.chBoard.detectorHandle, expectedMarkerCount, posVecs, rotVecs, markerIds, null);
            }

            bool found = false;

            for (int i = 0; i < count; i++)
            {
                if (markerIds[i] == marker.markerId)
                {
                    // Found the correct marker in the camera space
                    pair.Value.position = new Vector3(posVecs[i * 3], posVecs[i * 3 + 1], posVecs[i * 3 + 2]);

                    // goes from the camera coordinate system to the global coordinate system
                    pair.Value.posInWorld = cameraCalibrationHelper.locatableCamera.transform.TransformPoint(pair.Value.position);
                    pair.Value.posInWorld = cameraCalibrationHelper.locatableCamera.lastFrameLocalToWorld.MultiplyPoint3x4(pair.Value.position);
                    //pair.Value.rotation = new Vector3();
                    //pair.Value.rotation.x = Mathf.Rad2Deg * rotVecs[i * 3] + 180;
                    //pair.Value.rotation.y = Mathf.Rad2Deg * rotVecs[i * 3 + 2];
                    //pair.Value.rotation.z = -Mathf.Rad2Deg * rotVecs[i * 3 + 1];

                    int offset = i * 9;

                    Matrix4x4 localMat = new Matrix4x4(); // from OpenCV
                    localMat.SetRow(0, new Vector4((float)rotVecs[offset + 0], (float)rotVecs[offset + 1], (float)rotVecs[offset + 2], 0));
                    localMat.SetRow(1, new Vector4((float)rotVecs[offset + 3], (float)rotVecs[offset + 4], (float)rotVecs[offset + 5], 0));
                    localMat.SetRow(2, new Vector4((float)rotVecs[offset + 6], (float)rotVecs[offset + 7], (float)rotVecs[offset + 8], 0));
                    localMat.SetRow(3, new Vector4(0, 0, 0, 1));

                    // = Matrix4x4.zero;
                    //switchAxis.m33 = 1;

                    //switchAxis.m00 = -1;
                    //switchAxis.m12 = 1;
                    //switchAxis.m21 = -1;

                    /*
                     * 1,0,0,0
                     * 0,-1,0,0
                     * 0,0,1,0
                     * 0,0,0,1
                     */

                    //localMat = switchAxis * localMat;
                    //Debug.Log(localMat);

                    //Matrix4x4 localMat = Matrix4x4.Rotate(local);
                    Matrix4x4 invertYM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, -1, 1));

                    //// Handness
                    Matrix4x4 fixedMat = cameraCalibrationHelper.locatableCamera.lastFrameLocalToWorld * localMat * invertYM;
                    //Debug.Log(cameraCalibration.localToWorld);
                    //Debug.Log(cameraCalibration.trackableCamera.transform.localToWorldMatrix);
                    Quaternion local = QuaternionFromMatrix(fixedMat);
                    //cameraCalibration.trackableCamera.transform.localToWorldMatrix * 
                    //Quaternion local = QuaternionFromMatrix(localMat);

                    //pair.Value.rotInWorld = fixedRot;

                    //local = Quaternion.Inverse(local);
                    //cameraCalibration.trackableCamera.transform.rotation * 
                    pair.Value.rotInWorld = local;
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
                tcb.result = data.posInWorld;
            }
            else
            {
                Debug.LogError("[MarkerTracking] Cannot find markerId: " + tcb.markerId);
            }
        }
        else
        {
            Debug.LogError("[MarkerTracking] Cannot cast obj");
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
                rcb.result = data.rotInWorld; // 
            }
            else
            {
                Debug.LogError("[MarkerTracking] Cannot find markerId: " + rcb.markerId);
            }

        }
        else
        {
            Debug.LogError("[MarkerTracking] Cannot cast obj");
        }
    }

    public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
    {
        return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
    }
}
