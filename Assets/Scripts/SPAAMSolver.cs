using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Alignment
{
    [SerializeField] public Vector3 objectPosition;
    [SerializeField] public Vector3 targetPosition;
}

public class SPAAMSolver : MonoBehaviour
{
    public Transform TrackerBase;
    public Matrix4x4 groundTruthEquation;
    public Matrix4x4 manualEquation;

    public List<Alignment> groundTruthAlignments = new List<Alignment>();
    public List<Alignment> manualAlignments = new List<Alignment>();

    public void PerformAlignment(Vector3 objectPosition, Vector3 targetPosition)
    {
        Alignment manualAlignment = new Alignment
        {
            objectPosition = objectPosition,
            targetPosition = targetPosition
        };

        Alignment groundTruthAlignment = new Alignment
        {
            objectPosition = TrackerBase.InverseTransformPoint(targetPosition),
            targetPosition = targetPosition
        };

        manualAlignments.Add(manualAlignment);
        groundTruthAlignments.Add(groundTruthAlignment);
    }


    public void Solve()
    {
        // TODO: Send info to opencv and solve the linear equation

        groundTruthAlignments.Clear();
        manualAlignments.Clear();
    }
}
