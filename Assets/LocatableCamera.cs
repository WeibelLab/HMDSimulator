using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Locatable cameras are that can be located with respect to the virtual world
/// They represent physical cameras on the headset, and they provide a coordinate system transformation between the camera
/// internal coordinate system to the headset's internal coordinate system.
/// 
/// They are usually used along with computer vision approaches to track objects within the user space.
/// We borrow the terminology Locatable Camera from Microsoft
/// (See https://docs.microsoft.com/en-us/windows/mixed-reality/develop/platform-capabilities-and-apis/locatable-camera)
/// 
/// 
/// This helper script turns a Unity camera into a locatable camera and exports an event
/// for each frame generated - each frame is accompained by a number (unique to the simulation), a coordinate frame, camera intrinsics,
/// and an array of bytes with the image capture.
/// 
/// Belongs to: the "real" scene
/// </summary>
public class LocatableCamera : MonoBehaviour
{

    [Tooltip("Reference to Unity's camera representing the locatable camera")]
    public Camera trackableCamera;

    [Tooltip("Camera's texture")]
    public RenderTexture cameraTexture;

    [HideInInspector]
    public Texture2D lastRenderedFrame;
    [HideInInspector]
    public Matrix4x4 lastFrameWorldToLocal;
    [HideInInspector]
    public Matrix4x4 lastFrameLocalToWorld;
    [HideInInspector]
    int lastFrameNumber;

    // Private variables
    private int width;
    private int height;

    // Start is called before the first frame update
    void Start()
    {
        if (trackableCamera == null)
        {
            trackableCamera = GetComponent<Camera>();
        }

        if (!trackableCamera)
        {
            // if that happens, you might want to make sure that this script is added to you the locatable camera in "Real" scene
            Debug.LogWarning("[LocatableCamera] Locatable camera is not set to any camera, so no locatable camera frames will be available in this simulation");
        }

        // todo: create locatable camera texture?

        if (trackableCamera != null)
        {
            width = cameraTexture.width;
            height = cameraTexture.height;
            lastRenderedFrame = new Texture2D(width, height, TextureFormat.RGB24, false);
            Debug.Log(string.Format("[LocatableCamera] Locatable camera is set to render at a resolution of: {0}x{1}", width,height));
            StartCoroutine(UpdateImage());
        }
    }

    IEnumerator UpdateImage()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();

            // if, for some reason, the camera resolution changed during the simulation, recreate a new internal texture
            if (width != cameraTexture.width || height != cameraTexture.height)
            {

                width = cameraTexture.width;
                height = cameraTexture.height;
                lastRenderedFrame = new Texture2D(width, height, TextureFormat.RGB24, false);
                Debug.Log(string.Format("[LocatableCamera] Locatable camera is set to render for a resolution of: {0}x{1}", width, height));
            }

            // temporary render to render texture
            //var oldTargetTexture = Camera.main.targetTexture;
            //Camera.main.targetTexture = cameraTexture;
            //Camera.main.Render();
            //Camera.main.targetTexture = oldTargetTexture;
            var old = RenderTexture.active;

            // renders the locatable camera
            RenderTexture.active = cameraTexture;
            lastRenderedFrame.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            lastRenderedFrame.Apply();
            RenderTexture.active = null;

            // saves frame number
            lastFrameNumber = Time.frameCount;

            // updates the transforms for the locatable camera
            lastFrameWorldToLocal = trackableCamera.transform.worldToLocalMatrix;
            lastFrameLocalToWorld = trackableCamera.transform.localToWorldMatrix;

            // invokes event
            //(TODO)
        }
    }

}
