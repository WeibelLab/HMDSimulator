using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;
using UnityEngine.XR;

public class DisplayProjection : MonoBehaviour
{

    public Camera cam;
    public GameObject eye;
    public bool isLeftDisplay = false;
    public float forcedEyeSeparation = 0.007f;
    public bool forceEyeSeparation = false;
    public bool once = false;

    private bool init = false;

    private Matrix4x4 previousProjectionMatrix;
    public Matrix4x4 newProj;
    public Matrix4x4 newProjWithoutOffset;

    public double height = 0.03;
    public double width = 0.0568;

    public Vector3 eyePosition;
    public Vector3 localPos;
    public Transform eyeEst;
    public Transform len;

    /// <summary>
    /// Returns the current eye separation
    /// </summary>
    /// <returns></returns>
    public float GetEyeSeparation()
    {
        if (forceEyeSeparation)
        {
            return forcedEyeSeparation;
        }
        else
        {

            Vector3 leftEye = UnityEngine.XR.InputTracking.GetLocalPosition(XRNode.LeftEye);
            Vector3 rightEye = UnityEngine.XR.InputTracking.GetLocalPosition(XRNode.RightEye);
            Vector3 distance = (leftEye - rightEye);
            return distance.magnitude;
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();

        // keeps it here so that we can undo
        previousProjectionMatrix = cam.projectionMatrix;
    }

    // Update is called once per frame
    void OnPreRender()
    {
        MainManager.Instance.trackerManager.ForceUpdateTrackedObject();
        eyePosition = eye.transform.position;

        // if an XR Device is present, we will render considering the user's eye
        if (XRDevice.isPresent)
        {
            
            if (isLeftDisplay)
            {
                //localPos = UnityEngine.XR.InputTracking.GetLocalPosition(XRNode.LeftEye);
                if (forceEyeSeparation)
                {
                    localPos = new Vector3(-forcedEyeSeparation / 2.0f, 0, 0);
                }
                else
                {

                    Vector3 leftEye = UnityEngine.XR.InputTracking.GetLocalPosition(XRNode.LeftEye);
                    Vector3 rightEye = UnityEngine.XR.InputTracking.GetLocalPosition(XRNode.RightEye);
                    Vector3 distance = (leftEye - rightEye);
                    float seperation = distance.magnitude;
                    localPos = new Vector3(-seperation / 2.0f, 0, 0);
                }

                //UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.LeftEye)
                //    .TryGetFeatureValue(CommonUsages.devicePosition, out localPos);
            }
            else
            {
                //localPos = UnityEngine.XR.InputTracking.GetLocalPosition(XRNode.RightEye);
                if (forceEyeSeparation)
                {
                    localPos = new Vector3(forcedEyeSeparation / 2.0f, 0, 0);
                }
                else
                {

                    Vector3 leftEye = UnityEngine.XR.InputTracking.GetLocalPosition(XRNode.LeftEye);
                    Vector3 rightEye = UnityEngine.XR.InputTracking.GetLocalPosition(XRNode.RightEye);
                    Vector3 distance = (leftEye - rightEye);
                    float seperation = distance.magnitude;
                    localPos = new Vector3(seperation / 2.0f, 0, 0);
                }

            }
            //Debug.Log(isLeftDisplay + ":" + localPos);
            //eyePosition = localPos + new Vector3(100,100,100); // TODO: Fix later //eye.transform.TransformPoint(localPos);
            eyePosition = eye.transform.TransformPoint(localPos);
        }

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


        double left = center.x - width / 2.0;
        double right = center.x + width / 2.0;
        double top = center.y + height / 2.0;
        double bottom = center.y - height / 2.0;

        double near = 0.01;
        double far = 1000.0;
        double dist = Mathf.Abs(center.z);

        double scale = near / dist;
        left *= scale;
        right *= scale;
        top *= scale;
        bottom *= scale;

        newProjWithoutOffset = Matrix4x4.Frustum((float)left, (float)right, (float)bottom, (float)top, (float)near, (float)far);
        newProj = newProjWithoutOffset * offset;

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

    }

    
}
