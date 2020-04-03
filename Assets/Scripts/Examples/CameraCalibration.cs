using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class CameraCalibration : MonoBehaviour
{
    public CharucoBoard chBoard;
    public Camera trackableCamera;
    public RenderTexture cameraTexture;
    public Texture2D image;
    public Matrix4x4 worldMatrix;
    public Renderer debugQuad;

    public Texture2D debugTexture2D;
    public bool calibrated = false;

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
            RenderTexture.active = cameraTexture;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            RenderTexture.active = null;
            worldMatrix = trackableCamera.worldToCameraMatrix;
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
                    byte[] debugBuffer = new byte[width * height * 3];
                    debugTexture2D = new Texture2D(width, height, TextureFormat.RGB24, false);
                    int result = HMDSimOpenCV.Instance.Aruco_CollectCharucoCorners(handle, rgbBuffer, width, height, debugBuffer);
                    debugTexture2D.LoadRawTextureData(debugBuffer);
                    debugTexture2D.Apply();
                    debugQuad.material.mainTexture = debugTexture2D;
                    Debug.Log("Collect corners result: " + result);
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
                    double result = HMDSimOpenCV.Instance.Aruco_CalibrateCameraCharuco(handle);
                    Debug.Log("Calibration error: " + result);
                    calibrated = true;
                }
            }
        }
        //if (chBoard)
        //{
        //    chBoard.detectorHandle
        //}
    }
}
