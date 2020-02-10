using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformUpdater : MonoBehaviour
{

    public GameObject originalGo;

    private TransformHandler th;

    // Start is called before the first frame update
    void Start()
    {
        th = originalGo.GetComponent<TransformHandler>();
    }
    
    void LateUpdate()
    {
        if (th)
        {
            transform.localPosition = th.GetLocalTranslation();
            transform.localRotation = th.GetLocalRotation();
            transform.localScale = th.GetLocalScale();
        }
        else
        {
            transform.localPosition = originalGo.transform.localPosition;
            transform.localRotation = originalGo.transform.localRotation;
            transform.localScale = originalGo.transform.localScale;
        }
    }
}
