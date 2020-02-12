using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DefaultTransformHandler : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public virtual Vector3 GetLocalTranslation()
    {
        return transform.localPosition;
    }

    public virtual Quaternion GetLocalRotation()
    {
        return transform.localRotation;
    }

    public virtual Vector3 GetLocalScale()
    {
        return transform.localScale;
    }
}
