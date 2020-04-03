using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;
using UnityEngine.XR;

public class DisplayProjection : MonoBehaviour
{

    public Camera cam;
    public GameObject eye;
    public bool isLeft = false;
    public float eyeSeperation = 0.007f;
    public bool setEysSeperation = false;

    private Matrix4x4 oldProj;
    private Matrix4x4 newProj;

    private double height;
    private double width;

    private Vector3 eyePosition;

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();
        Debug.Log(name);
        Debug.Log(cam.projectionMatrix);
        oldProj = cam.projectionMatrix;
        
    }

    // Update is called once per frame
    void OnPreRender()
    {
        height = 0.02;
        width = 0.04;
        eyePosition = eye.transform.position;
        if (XRDevice.isPresent)
        {
            Vector3 localPos;
            if (isLeft)
            {
                //localPos = UnityEngine.XR.InputTracking.GetLocalPosition(XRNode.LeftEye);
                if (setEysSeperation)
                {
                    localPos = new Vector3(-eyeSeperation / 2.0f, 0, 0);
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
                if (setEysSeperation)
                {
                    localPos = new Vector3(eyeSeperation / 2.0f, 0, 0);
                }
                else
                {

                    Vector3 leftEye = UnityEngine.XR.InputTracking.GetLocalPosition(XRNode.LeftEye);
                    Vector3 rightEye = UnityEngine.XR.InputTracking.GetLocalPosition(XRNode.RightEye);
                    Vector3 distance = (leftEye - rightEye);
                    float seperation = distance.magnitude;
                    localPos = new Vector3(seperation / 2.0f, 0, 0);
                }
                //UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.RightEye)
                //    .TryGetFeatureValue(CommonUsages.devicePosition, out localPos);
            }
            //Debug.Log(isLeft + ":" + localPos);
            //eyePosition = localPos + new Vector3(100,100,100); // TODO: Fix later //eye.transform.TransformPoint(localPos);
            eyePosition = eye.transform.TransformPoint(localPos);
        }

        Vector3 dir = eyePosition - transform.position;
        //Quaternion rot = Quaternion.FromToRotation(Vector3.forward, dir);
        Matrix4x4 offset = Matrix4x4.Translate(dir);
        
        Vector3 center = -transform.InverseTransformPoint(eyePosition);

        if (name == "Left")
        {
            //Debug.Log(center);
        }

        center *= 0.985f;

        double left = center.x - width / 2.0;
        double right = center.x + width / 2.0;
        double top = center.y + height / 2.0;
        double bottom = center.y - height / 2.0;

        double near = 0.01;
        double far = 500.0;
        double dist = Mathf.Abs(center.z);

        double scale = near / dist;
        left *= scale;
        right *= scale;
        top *= scale;
        bottom *= scale;
        Matrix4x4 proj = Matrix4x4.Frustum((float)left, (float)right, (float)bottom, (float)top, (float)near, (float)far);

        newProj = proj;//oldProj * offset;
        //newProj[0, 0] *= 1.0f;
        //newProj[1, 1] *= 1.0f;
        
        cam.projectionMatrix = newProj;
        //cam.stereoSeparation
        //cam.SetStereoProjectionMatrix(Camera.StereoscopicEye.Left, );
    }

    
}
