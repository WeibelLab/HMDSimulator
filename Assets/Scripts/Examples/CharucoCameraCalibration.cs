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
        yield return new WaitForSeconds(5);

        if (ApplyCustomCalibrationMatrixOnStart)
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

            if (calibrationMatrixIsNotAllZeroes)
            {
                bool result = HMDSimOpenCV.Aruco_SetCameraIntrinsics(chBoard.detectorHandle, CustomCalibrationMatrix, CustomDistCoeffs, CustomDistCoeffs.Length);
                if (result)
                {
                    Debug.Log("[CharucoCameraCalibration] Applied custom calibration matrix");
                }
                else
                {
                    Debug.LogError("[CharucoCameraCalibration] Could not apply custom calibration matrix. Check logs!");
                }
            }
        }
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
