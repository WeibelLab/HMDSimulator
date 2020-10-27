using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// CameraCalibration is a helper class used calibrating virtual cameras 
/// through computer vision.
///  
/// For example,i n the Virtual-Augmented Reality simulator, we use it for
/// calibrating the headset's "locatable" camera.
/// 
/// This helper class plays two important roles:
///  1) Connecting a Unity virtual camera to an external calibration routine
///  2) Generating a calibration matrix based on the virtual camera's parameters
///  3) Quickly switching from a possibly imperfect calibration to the ground truth calibration
/// 
/// Besides those three mean roles, this class has additonal features that can help one
/// debug the calibration process: 
///  - A debug view that shows what the camera has seen
///  - A live view of the camera
/// 
/// </summary>
public class CameraCalibration : MonoBehaviour
{

    [Tooltip("Camera that will be calibrated")]
    public Camera trackableCamera;

    [Tooltip("Camera's texture")]
    public RenderTexture cameraTexture;

    [HideInInspector]
    public Texture2D image;

    [Header("Calibration approach")]
    // todo: remove this direct dependency
    public CharucoBoard chBoard;

    [Header("Camera extrinsics")]
    [Tooltip("The matrix transformation from the world to the camera local")]
    public Matrix4x4 worldToLocal;

    [Tooltip("The matrix transformation from the camera local coordinate system to the world")]
    public Matrix4x4 localToWorld;

    [Header("Camera Intrinsics")]
    float bla;

    [Header("Calibration results")]
    [Tooltip("If true, the camera has been calibrated")]
    public bool calibrated = false;

    [Tooltip("If calibrated, the calibration error gets saved here")]
    double calibrationError = 0.0f;
    
    [Header("Debugging")]
    [Tooltip("If true, and if supported by the underlying calibration interface, debugQuad gets filled with a texture that helps understand calibration")]
    public bool debug = false;

    [Tooltip("If debug is set, the debugQuad is used to show an internal status of a calibration routine")]
    public Renderer debugQuad;
   
    [Tooltip("The matrix transformation from the camera local coordinate system to the world")]
    public Texture2D debugTexture2D;


    // Private variables
    private int width;
    private int height;

    // Start is called before the first frame update
    void Start()
    {
        width = cameraTexture.width;
        height = cameraTexture.height;
        image = new Texture2D(width, height, TextureFormat.RGB24, false);
        StartCoroutine(UpdateImage());
    }

    IEnumerator UpdateImage()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();

            if (width != cameraTexture.width || height != cameraTexture.height)
            {
                width = cameraTexture.width;
                height = cameraTexture.height;
                image = new Texture2D(width, height, TextureFormat.RGB24, false);
            }

            // temporary render to render texture
            //var oldTargetTexture = Camera.main.targetTexture;
            //Camera.main.targetTexture = cameraTexture;
            //Camera.main.Render();
            //Camera.main.targetTexture = oldTargetTexture;
            var old = RenderTexture.active;
            
            // renders the locatable camera
            RenderTexture.active = cameraTexture;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            RenderTexture.active = null;

            // updates the transforms for the locatable camera
            worldToLocal = trackableCamera.transform.worldToLocalMatrix;
            localToWorld = trackableCamera.transform.localToWorldMatrix;
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
                    byte[] rgbBuffer = image.GetRawTextureData();
                    int width = image.width;
                    int height = image.height;


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

                    Debug.Log("[CameraCalibration] Collect corners result: " + result);
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
                    Debug.Log("[CameraCalibration] Calibration error: " + calibrationError);
                    calibrated = true;
                }
            }
        }
    }

    // applies a custom intrinsic matrix to the camera
    public void ApplyCustomCalibrationMatrix()
    {
        //trackableCamera.projectionMatrix
    }
}
