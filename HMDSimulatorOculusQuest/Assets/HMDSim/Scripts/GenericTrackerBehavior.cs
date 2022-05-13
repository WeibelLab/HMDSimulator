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

public class GenericTrackerBehavior : TrackerBehavior
{
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
        Vector3 result = transform.position;
        if (root != null)
        {
            result = root.InverseTransformPoint(result);
        }

        foreach (var cb in translationCallbacks)
        {
            result = cb.GetTranslation(result);
        }

        return result;
    }

    private static Quaternion QuaternionFromMatrix(Matrix4x4 m)
    {
        return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
    }

    private Quaternion _GetRotation()
    {
        Quaternion result = transform.rotation;
        if (root != null)
        {
            result = QuaternionFromMatrix(root.worldToLocalMatrix * Matrix4x4.TRS(Vector3.zero, result, Vector3.one));
        }

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
        Snapshot result = snapshots.FirstOrDefault(snapshot => (snapshot.time < target));
        if (result != null)
        {
            return result;
        }
        else if(snapshots.Count > 0)
        {
            return snapshots.First();
        }
        else
        {
            return new Snapshot();
        }
    }

    public override Vector3 GetTranslation()  
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

    public override Quaternion GetRotation()
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
            snapshots.Insert(0, new Snapshot
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