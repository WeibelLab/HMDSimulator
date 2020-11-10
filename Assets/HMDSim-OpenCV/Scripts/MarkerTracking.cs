using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

public class MarkerTracking : MonoBehaviour
{
    [Header("Performance")]
    [Tooltip("Runs marker detection in a separate thread to prevent FPS drops - might cause latency")]
    public bool MultiThreaded = false;

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
    private byte[] debugBuffer;

    // keeps a list of unique markers tracked per dictionary (we can only track one ID per dictionary)
    private Dictionary<int, Dictionary<int, ArucoMarker>> _trackedMarkers = new Dictionary<int, Dictionary<int, ArucoMarker>>();
    
    // keeps count of unique aruco families and aruco sizes as each <Family,Size> requires a detection pass
    private Dictionary<int, Dictionary<float, HashSet<ArucoMarker>>> _familyMarkerLength = new Dictionary<int, Dictionary<float, HashSet<ArucoMarker>>>();

    private void UpdateTemporaryLists()
    {
        // finds the dictonary of the most markers
        int mostMarkers = 0;
        foreach (Dictionary<int, ArucoMarker> d in _trackedMarkers.Values)
        {
            if (d.Count > mostMarkers)
                mostMarkers = d.Count;
        }

        // then, finds the lowest power of two greater than the number of markers being tracked
        int lowestPowerOfTwoGreaterThanMarkerCount = mostMarkers + 2;
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

        Dictionary<int, ArucoMarker> markerIds;
        // do we already have a dictionary for the marker dictionary family selected
        if (!_trackedMarkers.TryGetValue((int)marker.MarkerDictionary, out markerIds))
        {
            // if not, create one
            markerIds = new Dictionary<int, ArucoMarker>();
            _trackedMarkers[(int)marker.MarkerDictionary] = markerIds;
        } else
        // are we already tracking this marker?
        if (markerIds.ContainsKey(marker.markerId))
        {
            // unfortunately we can only track one ID
            Debug.LogWarning(String.Format("[MarkerTracking] Already tracking marker with id {0} ({1}). Cannot track {2}!", marker.markerId, markerIds[marker.markerId].transform.name, marker.transform.name));
            return false;
        }

        markerIds.Add(marker.markerId, marker);
        Debug.Log(String.Format("[MarkerTracking] Tracking marker with id {0} ({1})!", marker.markerId, marker.transform.name));

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
            markersPerSize.Add(marker.markerSize, markersWithSameLength);
        }

        // makes sure that this aruco marker is accounted
        markersWithSameLength.Add(marker);

        return true;
    }

    public bool StopTrackingMarker(ArucoMarker marker)
    {
        if (marker == null) return false;

        Dictionary<int, ArucoMarker> markerIds;
        if (!_trackedMarkers.TryGetValue((int) marker.MarkerDictionary, out markerIds))
        {
            // unfortunately we can only track one ID
            Debug.LogWarning(String.Format("[MarkerTracking] Not tracking any marker in the marker family {0} ({1} was never tracked)!", marker.MarkerDictionary, marker.transform.name));
            return false;
        }

        Debug.Log(String.Format("[MarkerTracking] *Not* tracking marker with id {0} ({1})!", marker.markerId, marker.transform.name));
        markerIds.Remove(marker.markerId);

        // last marker?
        if (markerIds.Count == 0)
        {
            // remove family of markers from the maps
            _trackedMarkers.Remove((int)marker.MarkerDictionary);
        }

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

        // last marker with that length?
        if (markersWithSameLength.Count == 0)
        {
            // remove list of markers with length markerSize
            markersPerSize.Remove(marker.markerSize);
            
            // empty dictionary of marker size?
            if (markersPerSize.Count == 0)
            {
                // remove family
                _familyMarkerLength.Remove((int)marker.MarkerDictionary);
            }
        }

        return true;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (!cameraCalibrationHelper || !cameraCalibrationHelper.calibrated)
        {
            return;
        }

        // get camera image
        int width = cameraCalibrationHelper.locatableCamera.lastRenderedFrame.width;
        int height = cameraCalibrationHelper.locatableCamera.lastRenderedFrame.height;
        byte[] rgbBuffer = cameraCalibrationHelper.locatableCamera.lastRenderedFrame.GetRawTextureData();

        // create debug texture
        if (debug && (!debugTexture2D || width != debugTexture2D.width || height != debugTexture2D.height))
        {
            debugTexture2D = new Texture2D(width, height, TextureFormat.RGB24, false);
            debugBuffer = new byte[width * height * 3];
        }

        // prepare for detection
        Matrix4x4 invertYM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, -1, 1));


        // for each marker dictionary family
        foreach (var familySizeList in _familyMarkerLength)
        {
            int markerDictionary = familySizeList.Key;
            Dictionary<int, ArucoMarker> markersPerFamily = _trackedMarkers[markerDictionary];

            // for each dictionary size in marker dictionary family, look for those markers
            foreach (var sizeDictionary in familySizeList.Value)
            {
                float markerSize = sizeDictionary.Key;

                int count = 0;
                if (debug && debugQuad != null)
                {
                    
                    count = HMDSimOpenCV.Aruco_EstimateMarkersPoseWithDetector(rgbBuffer, width, height,
                            markerDictionary, markerSize, cameraCalibrationHelper.chBoard.detectorHandle, expectedMarkerCount, posVecs, rotVecs, markerIds, debugBuffer);
                    debugTexture2D.LoadRawTextureData(debugBuffer);
                    debugTexture2D.Apply();
                    debugQuad.material.mainTexture = debugTexture2D;
                }
                else
                {
                    count = HMDSimOpenCV.Aruco_EstimateMarkersPoseWithDetector(rgbBuffer, width, height,
                            markerDictionary, markerSize, cameraCalibrationHelper.chBoard.detectorHandle, expectedMarkerCount, posVecs, rotVecs, markerIds, null);
                }

                // for each marker found in this family, we update the respective marker in the simulation
                if (count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        // do we have this marker?
                        ArucoMarker marker;
                        if (!markersPerFamily.TryGetValue(markerIds[i], out marker))
                        {
                            //Debug.LogWarning("We don't care about "+ markerIds[i]);
                            continue; // found a marker that we don't care about
                        }

                        marker.trackedMarkerData.position = new Vector3(posVecs[i * 3], posVecs[i * 3 + 1], posVecs[i * 3 + 2]);
                        marker.trackedMarkerData.posInWorld = cameraCalibrationHelper.locatableCamera.transform.TransformPoint(marker.trackedMarkerData.position); // todo: use lastFrameLocalToWorld instead
                        marker.trackedMarkerData.posInWorld = cameraCalibrationHelper.locatableCamera.lastFrameLocalToWorld.MultiplyPoint3x4(marker.trackedMarkerData.position);

                        int offset = i * 9;

                        Matrix4x4 localMat = new Matrix4x4(); // from OpenCV
                        localMat.SetRow(0, new Vector4((float)rotVecs[offset + 0], (float)rotVecs[offset + 1], (float)rotVecs[offset + 2], 0));
                        localMat.SetRow(1, new Vector4((float)rotVecs[offset + 3], (float)rotVecs[offset + 4], (float)rotVecs[offset + 5], 0));
                        localMat.SetRow(2, new Vector4((float)rotVecs[offset + 6], (float)rotVecs[offset + 7], (float)rotVecs[offset + 8], 0));
                        localMat.SetRow(3, new Vector4(0, 0, 0, 1));

                        //// Handness
                        Matrix4x4 fixedMat = cameraCalibrationHelper.locatableCamera.lastFrameLocalToWorld * localMat * invertYM;
                        Quaternion localRotation = QuaternionFromMatrix(fixedMat);
                        marker.trackedMarkerData.rotInWorld = localRotation;
                        marker.trackedMarkerData.found = true;
                    }
                } else if (count != 0)
                {
                    if (count == -1)
                    {
                        Debug.LogError("[MarkerTracking] Internal error - check logs!");
                    } else if (count == -2)
                    {
                        Debug.LogError("[MarkerTracking] An exception was thrown while detecting markers - check logs!");
                    }
                }
                
            }
        }
    }

    /// <summary>
    /// Decodes Aruco markers based on frames queued up by Unity
    /// </summary>
    public void ThreadedArucoMarkerDecoder()
    {

    }

    /// <summary>
    /// Use this method with a GenericTracker in order to override its callback list
    /// </summary>
    /// <param name="obj"></param>
    public void GetTranslation(Object obj)
    {
        TranslationCallback tcb = (TranslationCallback) obj;
        if (tcb != null)
        {
            Dictionary<int, ArucoMarker> markersInFamily;
            if (_trackedMarkers.TryGetValue((int)tcb.marker.MarkerDictionary, out markersInFamily) && markersInFamily.ContainsKey(tcb.marker.markerId))
            {
                tcb.result = tcb.marker.trackedMarkerData.posInWorld;

            }
            else
            {
                Debug.LogError("[MarkerTracking] Cannot find markerId: " + tcb.marker.markerId);
            }
        }
        else
        {
            Debug.LogError("[MarkerTracking] Cannot cast obj");
        }

    }

    /// <summary>
    /// Use this method with a GenericTracker in order to override its callback list
    /// </summary>
    /// <param name="obj"></param>
    public void GetRotation(Object obj)
    {
        RotationCallback rcb = (RotationCallback)obj;
        if (rcb != null)
        {
            Dictionary<int, ArucoMarker> markersInFamily;
            if (_trackedMarkers.TryGetValue((int)rcb.marker.MarkerDictionary, out markersInFamily) && markersInFamily.ContainsKey(rcb.marker.markerId))
            {
                rcb.result = rcb.marker.trackedMarkerData.rotInWorld;   
            }
            else
            {
                Debug.LogError("[MarkerTracking] Cannot find markerId: " + rcb.marker.markerId);
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
