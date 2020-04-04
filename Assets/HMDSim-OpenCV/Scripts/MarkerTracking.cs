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
        public Vector3 position = new Vector3(1000,1000,1000);
        public Vector3 posInWorld = new Vector3(1000, 1000, 1000);
        public Vector3 rotation;
        public Quaternion rotInWorld;
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
    public Texture2D debugTexture2D;
    public Renderer debugQuad;
    public Matrix4x4 switchAxis;

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
            float[] rotVecs = new float[9 * expectedMarkerCount];
            int[] markerIds = new int[expectedMarkerCount];

            if (!debugTexture2D || width != debugTexture2D.width || height != debugTexture2D.height)
            {
                debugTexture2D = new Texture2D(width, height, TextureFormat.RGB24, false);
            }

            byte[] debugBuffer = new byte[width * height * 3];

            int count = HMDSimOpenCV.Instance.Aruco_EstimateMarkersPoseWithDetector(rgbBuffer, width, height,
                (int) marker.MarkerDictionary, marker.markerSize, cameraCalibration.chBoard.detectorHandle, expectedMarkerCount, posVecs, rotVecs, markerIds, debugBuffer);

            debugTexture2D.LoadRawTextureData(debugBuffer);
            debugTexture2D.Apply();
            debugQuad.material.mainTexture = debugTexture2D;

            bool found = false;

            for (int i = 0; i < count; i++)
            {
                if (markerIds[i] == marker.markerId)
                {
                    // Found the correct marker
                    pair.Value.position = new Vector3(posVecs[i * 3], posVecs[i * 3 + 1], posVecs[i * 3 + 2]);
                    pair.Value.posInWorld = cameraCalibration.trackableCamera.transform.TransformPoint(pair.Value.position);
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
                    Matrix4x4 fixedMat = cameraCalibration.trackableCamera.transform.localToWorldMatrix * localMat * invertYM;
                    Debug.Log(invertYM);
                    Debug.Log(invertYM * switchAxis);
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
                rcb.result = data.rotInWorld; // 
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

    public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
    {
        return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
    }
}
