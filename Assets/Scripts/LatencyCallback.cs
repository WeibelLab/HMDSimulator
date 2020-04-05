using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class CustomLatencyEvent : UnityEvent<System.Object>{}

[System.Serializable]
public class LatencyCallback
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
#if UNITY_EDITOR
    [ConditionalHide("type", (int)Type.Constant, true)]
    public float constantOffset = 0;
    [ConditionalHide("type", (int)Type.SineWave, true)]
    public float amplitude = 1;
    [ConditionalHide("type", (int)Type.SineWave, true)]
    public float frequency = 1;

    [ConditionalHide("type", (int) Type.Curve, true)]
    public AnimationCurve curve = new AnimationCurve();

    [ConditionalHide("type", (int)Type.Custom, true)]
    public CustomLatencyEvent customEvent;

    [HideInInspector] public float result;

#else
    public float constantOffset = 0;
    public float amplitude = 1;
    public float frequency = 1;
    public AnimationCurve curve = new AnimationCurve();
    public CustomLatencyEvent customEvent;
    public float result;
#endif
    public float GetLatency()
    {
        float t = Time.fixedTime;
        switch (type)
        {
            case Type.PassThrough:
                break;
            case Type.Constant:
                result = constantOffset;
                break;
            case Type.SineWave:
                result = amplitude * Mathf.Sin(2 * Mathf.PI * frequency * t);
                break;
            case Type.Curve:
                result = curve.Evaluate(t);
                break;
            case Type.Custom:
                customEvent.Invoke(this);
                break;
        }

        return result;
    }
}
