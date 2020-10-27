using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// ArucoMarker is a portable version of teh ArucoMarker
/// This class is responsible for showing an ArucoMarker on Unity Editor / Game
/// so that it can be tracked by locatable cameras in the simulator
/// </summary>
[RequireComponent(typeof(Renderer))]
public class ArucoMarker : MonoBehaviour
{

    [Header("Simulator Detection")]
    public MarkerTracking markerTrackingManager;

    [Header("Marker configuration")]
    public int resolution = 512;
    //public bool border = true; (disabling this for now)
    public HMDSimOpenCV.ARUCO_PREDEFINED_DICTIONARY MarkerDictionary = HMDSimOpenCV.ARUCO_PREDEFINED_DICTIONARY.DICT_5X5_250;
    public int markerId = 42;
    [Tooltip("Marker size in meters")]
    public float markerSize = 0.10f; // 10 cm

    private byte[] textureBuffer;
    Texture2D markerTexture;

    [Header("Marker data")]

    public MarkerData trackedMarkerData = new MarkerData();

    [Serializable]
    public class MarkerData
    {
        public Vector3 position = new Vector3(1000, 1000, 1000);
        public Vector3 posInWorld = new Vector3(1000, 1000, 1000);
        public Vector3 rotation;
        public Quaternion rotInWorld;
        public bool found = false;
    }


    private int _markerId = -1;
    private float _markerSize = 0.001f; // 10 cm

    private HMDSimOpenCV.ARUCO_PREDEFINED_DICTIONARY _oldDictionary;
    private bool registeredForTracking = false;

    private void OnEnable()
    {
        // limit resolution to avoid crashing opencv / unity
        // a minimum resolution is required per Aruco
        if (resolution < 25 || resolution > 4096)
            resolution = 512;

        if (markerSize <= 0.0f)
            markerSize = 0.10f;

        UpdateMarkerSettings();

        // register marker if not registered due new marker settings
        if (!registeredForTracking && markerTrackingManager != null)
        {
            registeredForTracking = markerTrackingManager.StartTrackingMarker(this);
        }
    }

    public void OnDisable()
    {
        if (registeredForTracking && markerTrackingManager != null)
        {
            markerTrackingManager.StopTrackingMarker(this);
            registeredForTracking = false;
        }
    }

    public void UpdateMarkerSettings()
    {
        bool needToGenerateAgain = false;

        // do we need to allocate again?
        if (textureBuffer == null || textureBuffer.Length != (resolution * resolution * 3))
        {
            // allocate a new buffer
            textureBuffer = new byte[resolution * resolution * 3];

            // update the texture in the picture
            markerTexture = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);

            // update material on the plane
            Renderer r = this.GetComponent<Renderer>();
            r.material.mainTexture = markerTexture;

            // generate texture again
            needToGenerateAgain = true;
        }

        if (markerId != _markerId)
            needToGenerateAgain = true;

        if (MarkerDictionary != _oldDictionary)
            needToGenerateAgain = true;

        if (needToGenerateAgain)
        {
            // remove old marker from the tracked list
            if (registeredForTracking && markerTrackingManager != null)
            { 
                markerTrackingManager.StopTrackingMarker(this);
                registeredForTracking = false;
            }

            // update values
            _markerSize = markerSize;
            _markerId = markerId;
            _oldDictionary = MarkerDictionary;

            // draw the image
            bool generatedWell = HMDSimOpenCV.Aruco_DrawMarker((int)_oldDictionary, _markerId, resolution, true, textureBuffer);

            // update the texture
            markerTexture.LoadRawTextureData(textureBuffer);
            markerTexture.Apply();


            if (generatedWell)
            {
                Debug.Log(string.Format("[ArucoMarker] Generated marker id={0} ({1}) with {2}x{2}", _markerId, _oldDictionary.ToString(), resolution));

                if (!registeredForTracking && markerTrackingManager != null)
                {
                    markerTrackingManager.StartTrackingMarker(this);
                    registeredForTracking = true;
                }
            } else
            {
                Debug.LogError(string.Format("[ArucoMarker] Error generating marker id={0} ({1}) with {2}x{2}", _markerId, _oldDictionary.ToString(), resolution));
            }
        } else
        {
            // we do not need to generate a new marker, but perhaps we need to update tracking status?
            if (_markerSize != markerSize)
            {
                // updates marker length
                _markerSize = markerSize;

                if (registeredForTracking && markerTrackingManager != null)
                {
                    markerTrackingManager.StopTrackingMarker(this);
                    markerTrackingManager.StartTrackingMarker(this);
                }

            }
        }

    }

}
