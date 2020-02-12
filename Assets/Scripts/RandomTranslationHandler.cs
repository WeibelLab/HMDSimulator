using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomTranslationHandler : DefaultTransformHandler
{
    public float radius = 1.0f;
    public Vector3 offset;
    public float frequency = 1.0f;

    private float lastTime = 0;
    private Vector3 lastVector3;
    private float duration;

    void Start()
    {
        lastTime = Time.time;
        lastVector3 = Random.insideUnitSphere * radius + offset;
        duration = 1.0f / frequency;
    }

    public override Vector3 GetLocalTranslation()
    {
        if (Time.time - lastTime > duration)
        {
            lastTime = Time.time;
            lastVector3 = Random.insideUnitSphere * radius + offset;
            duration = 1.0f / frequency;
        }
        return transform.localPosition + lastVector3;
    }
}
