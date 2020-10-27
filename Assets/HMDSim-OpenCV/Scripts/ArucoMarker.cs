﻿using System.Collections;
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
    public float markerSize = 1.0f;

    private byte[] textureBuffer;
    Texture2D markerTexture;

    [Header("Marker data")]

    MarkerData trackedMarkerData = new MarkerData();

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
    private HMDSimOpenCV.ARUCO_PREDEFINED_DICTIONARY _oldDictionary;

    private void OnEnable()
    {
        // limit resolution to avoid crashing opencv / unity
        // a minimum resolution is required per Aruco
        if (resolution < 25 || resolution > 4096)
            resolution = 512;


        UpdateMarkerSettings();
    }

    public void OnDisable()
    {
        if (markerTrackingManager != null)
        {
            markerTrackingManager.
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
        {
            _markerId = markerId;
            needToGenerateAgain = true;
        }

        if (MarkerDictionary != _oldDictionary)
        {
            _oldDictionary = MarkerDictionary;
            needToGenerateAgain = true;
        }

        if (needToGenerateAgain)
        {
            // draw the image
            bool generatedWell = HMDSimOpenCV.Aruco_DrawMarker((int)_oldDictionary, _markerId, resolution, true, textureBuffer);

            // update the texture
            markerTexture.LoadRawTextureData(textureBuffer);
            markerTexture.Apply();

            if (generatedWell)
            {
                Debug.Log(string.Format("[ArucoMarker] Generated marker id={0} ({1}) with {2}x{2}", _markerId, _oldDictionary.ToString(), resolution));
            } else
            {
                Debug.LogError(string.Format("[ArucoMarker] Error generating marker id={0} ({1}) with {2}x{2}", _markerId, _oldDictionary.ToString(), resolution));
            }
        }

    }

}
