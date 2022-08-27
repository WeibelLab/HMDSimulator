using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// CharucoCameraCalibration is a helper class used calibrating virtual cameras 
/// through computer vision.
///  
/// For example, in the Virtual-Augmented Reality simulator, we use it for
/// calibrating the headset's "locatable" camera.
/// 
/// If this class is enabled, users can press two buttons to calibrate a locatable camera:
///  -> "colect picture" (key K)
///  -> "calibrate" (key P)
/// 
/// Besides those three mean roles, this class has additonal features that can help one
/// debug the calibration process: 
///  - A debug view that shows what the camera has seen
///  - A live view of the camera
///  
/// TODO:
///  - Add an option to export pictures collected by this helper; therefore allowing
///    developers to compare calibration results with methods implemented outside
///    Unity
///  
/// If you are developinga simulation in Unity using a Virtual-Augmented Reality simulator add-on
/// you can use this class as a template of how to integrate your code with an external library
/// 
/// 
/// Belongs to: the "real" scene
/// </summary>
public class CharucoCameraCalibration : MonoBehaviour
{

    [Tooltip("Camera that will be calibrated")]
    public LocatableCamera locatableCamera;

    [Header("Calibration board")]
    public CharucoBoard chBoard;

    [Header("Calibration board")]
    [Tooltip("If true, applies the custom calibration matrix saved here during start")]
    public bool ApplyCustomCalibrationMatrixOnStart = false;
    public bool ApplyProjectionMatrixOnStart = false;
    public float[] CustomCalibrationMatrix = new float[9];
    public float[] CustomDistCoeffs = new float[5];


    [Header("Calibration results")]
    [Tooltip("If true, the camera has been calibrated")]
    public bool calibrated = false;

    [Header("Camera Intrinsics")]
    float bla;


    [Tooltip("If calibrated, the calibration error gets saved here")]
    double calibrationError = 0.0f;
    
    [Header("Debugging")]
    [Tooltip("If true, and if supported by the underlying calibration interface, debugQuad gets filled with a texture that helps understand calibration")]
    public bool debug = false;

    [Tooltip("If debug is set, the debugQuad is used to show an internal status of a calibration routine")]
    public Renderer debugQuad;
   
    [Tooltip("The matrix transformation from the camera local coordinate system to the world")]
    public Texture2D debugTexture2D;

    // On Start
    private void Start()
    {
        StartCoroutine(ApplyCustomCalibrationMatrix());
    }

    IEnumerator ApplyCustomCalibrationMatrix()
    {
        Debug.Log("[CharucoCameraCalibration] Applying custom calibration matrix in 5...");
        yield return new WaitForSeconds(5);

        if (ApplyCustomCalibrationMatrixOnStart)
        {
            UseCustomCameraIntrinsics();
        } else if (ApplyProjectionMatrixOnStart)
        {
            UseCameraIntrinsics();
        }
    }

    /// <summary>
    /// Applies the Unity camera projection matrix instead as the intrinsic matrix for detecting markers
    ///  //  member variables |      indices
    // ------------------|-----------------
    // m00 m01 m02 m03   |   00  04  08  12
    // m10 m11 m12 m13   |   01  05  09  13
    // m20 m21 m22 m23   |   02  06  10  14
    // m30 m31 m32 m33   |   03  07  11  15
    //M[RowIndex, ColumnIndex] == M[RowIndex + ColumnIndex * 4]
    /// 
    /// </summary>
    public bool UseCameraIntrinsics()
    {
        // uses the camera projection matrix instead
        float[] intrinsicCalibrationMatrix = new float[9];
        float[] distortionCoeff = new float[4];
        intrinsicCalibrationMatrix[0] = locatableCamera.trackableCamera.projectionMatrix.m00;
        intrinsicCalibrationMatrix[1] = locatableCamera.trackableCamera.projectionMatrix.m01;
        intrinsicCalibrationMatrix[2] = locatableCamera.trackableCamera.projectionMatrix.m02;
        intrinsicCalibrationMatrix[3] = locatableCamera.trackableCamera.projectionMatrix.m10;
        intrinsicCalibrationMatrix[4] = locatableCamera.trackableCamera.projectionMatrix.m11;
        intrinsicCalibrationMatrix[5] = locatableCamera.trackableCamera.projectionMatrix.m12;
        intrinsicCalibrationMatrix[6] = locatableCamera.trackableCamera.projectionMatrix.m20;
        intrinsicCalibrationMatrix[7] = locatableCamera.trackableCamera.projectionMatrix.m21;
        intrinsicCalibrationMatrix[8] = -locatableCamera.trackableCamera.projectionMatrix.m22;
        bool result = HMDSimOpenCV.Aruco_SetCameraIntrinsics(chBoard.detectorHandle, intrinsicCalibrationMatrix, distortionCoeff, 0);
        if (result)
        {
            calibrated = true;
            Debug.Log("[CharucoCameraCalibration] Applied projection matrix as the calibration matrix");
        }
        else
        {
            Debug.LogError("[CharucoCameraCalibration] Could not apply projection matrix as the calibration matrix. Check logs!");
        }
        return result;
    }

    /// <summary>
    /// Uses a custom camera matrix to detect markers
    /// </summary>
    public bool UseCustomCameraIntrinsics()
    {
        bool calibrationMatrixIsNotAllZeroes = false;
        foreach (var el in CustomCalibrationMatrix)
        {
            if (el != 0.0f)
            {
                calibrationMatrixIsNotAllZeroes = true;
                break;
            }
        }

        // can't apply a calibration matrix taht is all zeroes
        if (!calibrationMatrixIsNotAllZeroes)
            return false;

        bool result = HMDSimOpenCV.Aruco_SetCameraIntrinsics(chBoard.detectorHandle, CustomCalibrationMatrix, CustomDistCoeffs, CustomDistCoeffs.Length);
        if (result)
        {
            calibrated = true;
            Debug.Log("[CharucoCameraCalibration] Applied custom calibration matrix");
        }
        else
        {
            Debug.LogError("[CharucoCameraCalibration] Could not apply custom calibration matrix. Check logs!");
        }
        return result;

    }

        // Update is called once per frame
        void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (chBoard)
            {
                int handle = chBoard.detectorHandle;
                if (handle >= 0)
                {
                    byte[] rgbBuffer = locatableCamera.lastRenderedFrame.GetRawTextureData();
                    int width = locatableCamera.lastRenderedFrame.width;
                    int height = locatableCamera.lastRenderedFrame.height;


                    int result;
                    if (debug && debugQuad != null)
                    {
                        if (!debugTexture2D || width != debugTexture2D.width || height != debugTexture2D.height)
                        {
                            debugTexture2D = new Texture2D(width, height, TextureFormat.RGB24, false);
                        }

                        byte[] debugBuffer = new byte[width * height * 3];
                        result = HMDSimOpenCV.Aruco_CollectCharucoCorners(handle, rgbBuffer, width, height, debugBuffer);
                        debugTexture2D.LoadRawTextureData(debugBuffer);
                        debugTexture2D.Apply();
                        debugQuad.material.mainTexture = debugTexture2D;
                    } else
                    {
                        result = HMDSimOpenCV.Aruco_CollectCharucoCorners(handle, rgbBuffer, width, height, null); 
                    }

                    Debug.Log("[CharucoCameraCalibration] Collect corners result: " + result);
                }
            }
        }

        
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (chBoard)
            {
                int handle = chBoard.detectorHandle;
                if (handle >= 0)
                {
                    calibrationError = HMDSimOpenCV.Aruco_CalibrateCameraCharuco(handle);
                    Debug.Log("[CharucoCameraCalibration] Calibration error: " + calibrationError);

                    // Todo, collect camera intrinsics
                    calibrated = true;
                }
            }
        }
    }

}
