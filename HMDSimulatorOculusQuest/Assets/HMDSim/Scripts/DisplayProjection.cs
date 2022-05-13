using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// DisplayProjection sets up the display projection matrices so that
/// each virtual camera accounts for where user's eyes
/// </summary>
public class DisplayProjection : MonoBehaviour
{

    public Camera cam;
    public GameObject eye;
    public bool isLeft = false;
    public float eyeSeparation = 0.007f;
    public bool forceEyeSeparation = false;
    public bool once = false;

    private bool init = false;

    private Matrix4x4 oldProj;
    public Matrix4x4 newProj;
    public Matrix4x4 newProjWithoutOffset;

    public double height = 0.03;
    public double width = 0.0568;

    public Vector3 eyePosition;
    public Vector3 localPos;
    public Transform eyeEst;
    public Transform len;

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();
        oldProj = cam.projectionMatrix;
    }

    public static bool isXRHeadsetPresent()
    {
        var xrDisplaySubsystems = new List<XRDisplaySubsystem>();
        SubsystemManager.GetInstances<XRDisplaySubsystem>(xrDisplaySubsystems);
        foreach (var xrDisplay in xrDisplaySubsystems)
        {
            if (xrDisplay.running)
            {
                
                return true;
                
            }
        }
        return false;
    }

    // OnPreRender is called once before a camera starts rendering a scene.
    // Our goal here is to find out if a VR headset is connected, and if not,
    // we make sure that the OST-HMD renders for a single camera
    public void UpdateEyePosition()
    {
        // do we have a headset connected
        eyePosition = eye.transform.position;

        if (isXRHeadsetPresent())
        { 

        
            float multiplier = 1;
            if (isLeft)
                multiplier = -1;

            // forced eye separation?
            if (!forceEyeSeparation)
            {
                // calculate eye separation
                Vector3 leftEye = Vector3.zero;
                Vector3 rightEye = Vector3.zero;
                bool foundRight = false, foundLeft = false;

                // tries to find left and right eye
                InputTracking.GetNodeStates(cachedXrNodeStates);
                foreach (XRNodeState xns in cachedXrNodeStates)
                {
                    if (xns.nodeType == XRNode.LeftEye)
                    {
                        foundLeft = true;
                        xns.TryGetPosition(out leftEye);
                    }
                    else if (xns.nodeType == XRNode.RightEye)
                    {
                        foundRight = true;
                        xns.TryGetPosition(out rightEye);
                    }

                    if (foundRight && foundLeft)
                        break;
                }

                Vector3 distance = (leftEye - rightEye);
                eyeSeparation = distance.magnitude;
            }

            localPos = new Vector3(multiplier * eyeSeparation / 2.0f, 0, 0);
            eyePosition = eye.transform.TransformPoint(localPos);


        }




    }

    List<XRNodeState> cachedXrNodeStates = new List<XRNodeState>();

    void OnPreRender()
    {
        // Updates the location of the display before rendering
        MainManager.Instance.trackerManager.ForceUpdateTrackedObject();
        eyePosition = eye.transform.position;
        UpdateEyePosition();

        transform.position = eyePosition;
        transform.rotation = len.rotation;

        Vector3 dir = eyePosition - transform.position;
        //Debug.Log(dir.ToString("G6"));
        //Quaternion rot = Quaternion.FromToRotation(Vector3.forward, dir);
        Matrix4x4 offset = Matrix4x4.Translate(dir);
        
        Vector3 center = transform.InverseTransformPoint(len.position);

        if (eyeEst)
        {
            eyeEst.transform.localPosition = new Vector3(0,0,0);//-center;
        }

        if (name == "Left")
        {
            //Debug.Log(center);
        }

        //center *= 0.985f;

        double left = center.x - width / 2.0;
        double right = center.x + width / 2.0;
        double top = center.y + height / 2.0;
        double bottom = center.y - height / 2.0;

        double near = 0.01;
        double far = 100.0;
        double dist = Mathf.Abs(center.z);

        double scale = near / dist;
        left *= scale;
        right *= scale;
        top *= scale;
        bottom *= scale;

        newProjWithoutOffset = Matrix4x4.Frustum((float)left, (float)right, (float)bottom, (float)top, (float)near, (float)far);
        newProj = newProjWithoutOffset * offset;//oldProj * offset;
        //newProj[0, 0] *= 1.0f;
        //newProj[1, 1] *= 1.0f;

        if (!once)
        {
            cam.projectionMatrix = newProjWithoutOffset;
        }
        else
        {
            if (!init)
            {
                cam.projectionMatrix = newProjWithoutOffset;
                init = true;
            }
        }

        //cam.stereoSeparation
        //cam.SetStereoProjectionMatrix(Camera.StereoscopicEye.Left, );
    }

    
}
