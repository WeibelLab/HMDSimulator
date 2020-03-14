using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class is responsible for showing a CharucoBoard on Unity Editor / Game
/// so that it can be used for camera calibration in the simulator
/// </summary>

[RequireComponent(typeof(Renderer))]
public class CharucoBoard : MonoBehaviour
{

    public int detectorHandle = -1;
    public Vector2Int squareCount = Vector2Int.one;
    public int squareResolution = 512;
    public int markerResolution = 512;
    //public bool border = true; (disabling this for now)
    public HMDSimOpenCV.ARUCO_PREDEFINED_DICTIONARY MarkerDictionary = HMDSimOpenCV.ARUCO_PREDEFINED_DICTIONARY.DICT_5X5_250;
    
    private byte[] textureBuffer;
    Texture2D boardTexture;

    private Vector2Int _resolution = Vector2Int.zero;
    private HMDSimOpenCV.ARUCO_PREDEFINED_DICTIONARY _oldDictionary;


    private void OnEnable()
    {
        // limit resolution to avoid crashing opencv / unity
        // a minimum resolution is required per Aruco
        if (squareResolution < 25 || squareResolution > 4096)
            squareResolution = 512;

        if (markerResolution < 25 || markerResolution > 4096)
            markerResolution = 512;

        if (markerResolution >= squareResolution)
        {
            //markerResolution = squareResolution - 1;
        }

        if (squareCount.x < 1 || squareCount.x > 10)
        {
            squareCount.x = 6;
        }

        if (squareCount.y < 1 || squareCount.y > 10)
        {
            squareCount.y = 6;
        }

        _resolution = new Vector2Int(squareCount.x * squareResolution, squareCount.y * squareResolution);

        UpdateMarkerSettings();
    }

    public void UpdateMarkerSettings()
    {
        bool needToGenerateAgain = false;

        int size = _resolution.x * _resolution.y * 3;

        // do we need to allocate again?
        if (textureBuffer == null || textureBuffer.Length != (size))
        {
            // allocate a new buffer
            textureBuffer = new byte[size];

            // update the texture in the picture
            boardTexture = new Texture2D(_resolution.x, _resolution.y, TextureFormat.RGB24, false);

            // update material on the plane
            Renderer r = this.GetComponent<Renderer>();
            r.material.mainTexture = boardTexture;

            // generate texture again
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
            detectorHandle = HMDSimOpenCV.Instance.Aruco_CreateDetector((int) _oldDictionary, squareCount.x,
                squareCount.y, squareResolution, markerResolution, true);
            bool generatedWell = HMDSimOpenCV.Instance.Aruco_DrawCharucoBoard(detectorHandle, textureBuffer);

            // update the texture
            boardTexture.LoadRawTextureData(textureBuffer);
            boardTexture.Apply();

            if (generatedWell)
            {
                Debug.Log(string.Format("[ArucoMarker] Generated Charuboard ({0}) with {1}x{2}", _oldDictionary.ToString(), _resolution.x, _resolution.y));
            }
            else
            {
                Debug.LogError(string.Format("[ArucoMarker] Error generating Charuboard ({0}) with {1}x{2}", _oldDictionary.ToString(), _resolution.x, _resolution.y));
            }
        }

    }
}
