using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class Snapshot
{
    public float time;
    public Vector3 position;
    public Quaternion rotation;
}

public class GenericTrackerBehavior : MonoBehaviour
{

    public string trackerName;
    public Transform root;
    public int bufferLimit = 400;

    public List<LatencyCallback> latencyCallbacks;
    public List<TranslationCallback> translationCallbacks;
    public List<RotationCallback> rotationCallbacks;

    private List<Snapshot> snapshots;

    void Awake()
    {
        snapshots = new List<Snapshot>();
    }

    private Vector3 _GetTranslation()
    {
        Vector3 result = transform.localPosition;
        foreach (var cb in translationCallbacks)
        {
            result = cb.GetTranslation(result);
        }

        return result;
    }

    private Quaternion _GetRotation()
    {
        Quaternion result = transform.localRotation;
        foreach (var cb in rotationCallbacks)
        {
            result = cb.GetRotation(result);
        }

        return result;
    }

    private float _GetLatency()
    {
        float result = 0;
        foreach (var cb in latencyCallbacks)
        {
            result += cb.GetLatency();
        }

        return result;
    }

    private Snapshot _FindSnapshot()
    {
        float t = Time.fixedTime;
        float latency = _GetLatency();
        float target = t - latency;
        Snapshot result = snapshots.First(snapshot => (snapshot.time < target));
        return result;
    }

    public Vector3 GetTranslation()
    {
        if (latencyCallbacks.Count == 0)
        {
            return _GetTranslation();
        }
        else
        {
            return _FindSnapshot().position;
        }
    }

    public Quaternion GetRotatioin()
    {
        if (latencyCallbacks.Count == 0)
        {
            return _GetRotation();
        }
        else
        {
            return _FindSnapshot().rotation;
        }
    }

    void FixedUpdate()
    {
        if (latencyCallbacks.Count > 0)
        {
            // Accum snapshot buffer
            snapshots.Prepend(new Snapshot
            {
                time = Time.fixedTime,
                position = _GetTranslation(),
                rotation = _GetRotation()
            });

            if (snapshots.Count > bufferLimit)
            {
                snapshots.RemoveAt(snapshots.Count - 1);
            }
        }
    }
}
