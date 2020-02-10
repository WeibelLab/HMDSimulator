﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirtualSceneInitializer : MonoBehaviour
{
    public GameObject Eye;
    public GameObject mirrorEye;
    public GameObject RealWorld;

    public GameObject EyeParent;
    public Camera LeftEye;
    public Camera RightEye;

    // Start is called before the first frame update
    void Start()
    {
        if (RealWorld != null)
        {
            mirrorEye = new GameObject("MirrorEye");
            mirrorEye.transform.parent = transform;
            TransformUpdater tu = mirrorEye.AddComponent<TransformUpdater>();
            tu.originalGo = Eye;

            // For now only one eye;
            LeftEye.GetComponent<DisplayProjection>().eye = mirrorEye;
            RightEye.GetComponent<DisplayProjection>().eye = mirrorEye;

            TraverseHierarchy(RealWorld.transform, transform);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void TraverseHierarchy(Transform parent, Transform mirrorParent) {
        foreach (Transform child in parent.transform)
        {
            // Debug.Log(child.name);

            // Copy current Node
            GameObject go = Instantiate(child.gameObject);

            // Remove unneeded components
            foreach (var c in go.GetComponents(typeof(Component)))
            {
                if (!(c is Transform) && !(c is MeshFilter) && !(c is MeshRenderer))
                {
                    Destroy(c);
                }
            }

            // Attach updater script
            TransformUpdater tu = go.AddComponent<TransformUpdater>();

            // Set updater field
            tu.originalGo = child.gameObject;

            // Set hierarchy
            go.transform.parent = mirrorParent;

            // Remove duplicated children
            foreach (Transform goChild in go.transform)
            {
                Destroy(goChild.gameObject);
            }

            // Attach camera if it's display
            if (child.name == "LeftEye")
            {
                var updater = LeftEye.gameObject.AddComponent<TransformUpdater>();
                updater.originalGo = go;
            }
            if (child.name == "RightEye")
            {
                var updater = RightEye.gameObject.AddComponent<TransformUpdater>();
                updater.originalGo = go;
            }
            if (child.name == "Display")
            {
                EyeParent.transform.parent = go.transform;
                EyeParent.transform.localPosition = Vector3.zero;
                EyeParent.transform.localRotation = Quaternion.identity;
            }

            TraverseHierarchy(child, go.transform);
        }
    }
}
