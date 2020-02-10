using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisplayProjection : MonoBehaviour
{

    public Camera cam;
    public GameObject eye;

    private Matrix4x4 oldProj;
    private Matrix4x4 newProj;

    private float height;
    private float width;

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();
        Debug.Log(name);
        Debug.Log(cam.projectionMatrix);
        oldProj = cam.projectionMatrix;

        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        height = 0.025f;
        width = 0.05f;

        Vector3 dir = eye.transform.position - transform.position;
        //Quaternion rot = Quaternion.FromToRotation(Vector3.forward, dir);
        Matrix4x4 offset = Matrix4x4.Translate(dir);
        
        Vector3 center = transform.InverseTransformPoint(eye.transform.position);

        if (name == "Left")
        {
            Debug.Log(center);
        }

        float left = center.x - width / 2.0f;
        float right = center.x + width / 2.0f;
        float top = center.y + height / 2.0f;
        float bottom = center.y - height / 2.0f;

        float near = 0.01f;
        float far = 1000.0f;
        float dist = Mathf.Abs(center.z);

        float scale = near / dist;
        left *= scale;
        right *= scale;
        top *= scale;
        bottom *= scale;

        Matrix4x4 proj = Matrix4x4.Frustum(left, right, bottom, top, near, far);

        newProj = proj;//oldProj * offset;
        //newProj[0, 0] *= 1.0f;
        //newProj[1, 1] *= 1.0f;
        
        cam.projectionMatrix = newProj;
    }

    static Matrix4x4 PerspectiveOffCenter(float left, float right, float bottom, float top, float near, float far)
    {
        float x = 2.0F * near / (right - left);
        float y = 2.0F * near / (top - bottom);
        float a = (right + left) / (right - left);
        float b = (top + bottom) / (top - bottom);
        float c = -(far + near) / (far - near);
        float d = -(2.0F * far * near) / (far - near);
        float e = -1.0F;
        Matrix4x4 m = new Matrix4x4();
        m[0, 0] = x;
        m[0, 1] = 0;
        m[0, 2] = a;
        m[0, 3] = 0;
        m[1, 0] = 0;
        m[1, 1] = y;
        m[1, 2] = b;
        m[1, 3] = 0;
        m[2, 0] = 0;
        m[2, 1] = 0;
        m[2, 2] = c;
        m[2, 3] = d;
        m[3, 0] = 0;
        m[3, 1] = 0;
        m[3, 2] = e;
        m[3, 3] = 0;
        return m;
    }
}
