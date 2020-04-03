using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class CustomRotationEvent : UnityEvent<System.Object>
{
}

[System.Serializable]
public class RotationCallback
{
    public enum Type
    {
        PassThrough,
        Constant,
        SineWave,
        Curve,
        Custom
    };

    public Type type;

    [ConditionalHide("type", (int) Type.Constant, true)]
    public Vector3 constantOffset = Vector3.zero;

    [ConditionalHide("type", (int) Type.SineWave, true)]
    public Vector3 amplitude = Vector3.one;

    [ConditionalHide("type", (int) Type.SineWave, true)]
    public Vector3 frequency = Vector3.one;

    [ConditionalHide("type", (int) Type.Curve, true)]
    public AnimationCurve curveX = new AnimationCurve();

    [ConditionalHide("type", (int) Type.Curve, true)]
    public AnimationCurve curveY = new AnimationCurve();

    [ConditionalHide("type", (int) Type.Curve, true)]
    public AnimationCurve curveZ = new AnimationCurve();

    [ConditionalHide("type", (int)Type.Custom, true)]
    public int markerId;

    [ConditionalHide("type", (int) Type.Custom, true)]
    public CustomTranslationEvent customEvent;

    [HideInInspector] public Quaternion result;
    private Quaternion q0;
    private Quaternion q1;
    private float t0 = -1;
    private float t1 = -1;

    private Quaternion vel = Quaternion.identity;

    public Quaternion GetRotation(Quaternion orig)
    {
        result = orig;
        float t = Time.fixedTime;
        switch (type)
        {
            case Type.PassThrough:
                break;
            case Type.Constant:
                result *= Quaternion.Euler(constantOffset);
                break;
            case Type.SineWave:
                Vector3 offset = Vector3.Scale(amplitude,
                    new Vector3(Mathf.Sin(2 * Mathf.PI * frequency.x * t), 
                        Mathf.Sin(2 * Mathf.PI * frequency.y * t),
                        Mathf.Sin(2 * Mathf.PI * frequency.z * t)));
                result *= Quaternion.Euler(offset);
                break;
            case Type.Curve:
                result *= Quaternion.Euler(curveX.Evaluate(t), curveY.Evaluate(t), curveZ.Evaluate(t));
                break;
            case Type.Custom:
                customEvent.Invoke(this);
                break;
        }

        // Update prev
        q0 = q1;
        q1 = orig;
        t0 = t1;
        t1 = t;

        return result;
    }
}