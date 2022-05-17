using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackerBehavior : MonoBehaviour
{
    public string trackerName;
    public Transform root;

    public virtual Vector3 GetTranslation()
    {
        return Vector3.zero;
    }

    public virtual Quaternion GetRotation()
    {
        return Quaternion.identity;
    }
}