﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CameraMovement is a helper script that allows you to move the virtual user around.
/// It is very handy when it comes to testing it in the computer (without a VR headset)
/// 
/// 
/// </summary>
public class CameraMovement : MonoBehaviour
{


    public float speed = 2.0f;
    private float translation;
    private float straffe;
   
    [Header("Mouse control")]
    // based on https://stackoverflow.com/a/8466748/1248712
    [Tooltip("If enable, allows you to control the user camera with the mouse")]
    public bool mouseControl = true;
    public enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
    public RotationAxes axes = RotationAxes.MouseXAndY;
    public float sensitivityX = 5F;
    public float sensitivityY = 5F;

    public float minimumX = -360F;
    public float maximumX = 360F;

    public float minimumY = -60F;
    public float maximumY = 60F;

    float rotationY = 0F;


    // Use this for initialization
    void Start()
    {
        // turn off the cursor
        
    }

    // Update is called once per frame
    void Update()
    {
        // Input.GetAxis() is used to get the user's input
        // You can furthor set it on Unity. (Edit, Project Settings, Input)
        translation = Input.GetAxis("Vertical") * speed * Time.deltaTime;
        straffe = Input.GetAxis("Horizontal") * speed * Time.deltaTime;
        transform.Translate(straffe, 0, translation);

        
        if (mouseControl)
        {
            if (axes == RotationAxes.MouseXAndY)
            {
                float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX;

                rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
                rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

                transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
            }
            else if (axes == RotationAxes.MouseX)
            {
                transform.Rotate(0, Input.GetAxis("Mouse X") * sensitivityX, 0);
            }
            else
            {
                rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
                rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

                transform.localEulerAngles = new Vector3(-rotationY, transform.localEulerAngles.y, 0);
            }
        }

    }
}
