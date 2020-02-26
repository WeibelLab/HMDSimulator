using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisplayProjection : MonoBehaviour
{

    public Camera cam;
    public GameObject eye;

    private Matrix4x4 oldProj;
    private Matrix4x4 newProj;

    private double height;
    private double width;

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

        Vector3 dir = eye.transform.position - transform.position;
        //Quaternion rot = Quaternion.FromToRotation(Vector3.forward, dir);
        Matrix4x4 offset = Matrix4x4.Translate(dir);
        
        Vector3 center = -transform.InverseTransformPoint(eye.transform.position);

        if (name == "Left")
        {
            Debug.Log(center);
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
    }

    
}
