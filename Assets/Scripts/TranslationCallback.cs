
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;


[System.Serializable]
public class CustomTranslationEvent : UnityEvent<System.Object>{}

[System.Serializable]
public class TranslationCallback
{
    public enum Type
    {
        PassThrough,
        Constant,
        AccelDrift,
        AxisMagnitude,
        CustomArUcoMarker,
        Custom
    };

    public Type type;
#if UNITY_EDITOR
    [ConditionalHide("type", (int)Type.Constant, true)]
    public Vector3 constantOffset = Vector3.zero;
    [ConditionalHide("type", (int)Type.AccelDrift, true)]
    public float accelDriftFactor = 1.0f;
    [ConditionalHide("type", (int)Type.AxisMagnitude, true)]
    public Vector3 axisMagnitude = Vector3.one;

    // we added this to show how we can use an existing GenericTracker with a custom tracker
    [ConditionalHide("type", (int)Type.CustomArUcoMarker, true)]
    public ArucoMarker marker;

    [ConditionalHide("type", (int)Type.Custom, true)]
    public CustomTranslationEvent customEvent;

    [HideInInspector] public Vector3 result;

#else
    public Vector3 constantOffset = Vector3.zero;
    public float accelDriftFactor = 1.0f;
    public Vector3 axisMagnitude = Vector3.one;
    public ArucoMarker marker;
    public CustomTranslationEvent customEvent;

    public Vector3 result;
#endif
    private Vector3 p0;
    private Vector3 p1;
    private float t0 = -1;
    private float t1 = -1;
    

    private Vector3 vel = Vector3.zero;

    public Vector3 GetTranslation(Vector3 orig)
    {
        
        float t = Time.fixedTime;
        switch (type)
        {
            case Type.PassThrough:
                result = orig;
                break;
            case Type.Constant:
                result = orig;
                result += constantOffset;
                break;
            case Type.AccelDrift:
                
                Vector3 accel = Vector3.zero;
                if (t0 > 0 && t1 > 0)
                {
                    // calculate acceleration
                    float dt0 = t1 - t0;
                    float dt1 = t - t1;

                    // seg0 velocity
                    Vector3 v0 = (p1 - p0) / dt0;

                    // seg1 velocity
                    Vector3 v1 = (orig - p1) / dt1;

                    // Acceleration
                    accel = ((orig - p1) / dt1 - (p1 - p0) / dt0) / ((dt0 + dt1) / 2.0f);
                }
                else
                {
                    result = orig;
                }
                // Update position based on accel (forward)
                vel += (accel * accelDriftFactor) * (t - t1);
                result += vel * (t - t1);
                
                break;
            case Type.AxisMagnitude:
                Vector3 dp = Vector3.zero;
                if (t1 > 0)
                {
                    dp = Vector3.Scale(orig - p1, axisMagnitude);
                    result = result + dp;
                }
                else
                {
                    result = orig;
                }
                break;
            case Type.Custom:
                customEvent.Invoke(this);
                break;
        }

        // Update prev
        p0 = p1;
        p1 = orig;
        t0 = t1;
        t1 = t;

        return result;
    }
}
