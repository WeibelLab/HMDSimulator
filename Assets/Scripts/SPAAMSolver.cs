using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Class that holds matching points in two coordinate systems
/// </summary>
[Serializable]
public class MatchingPoints
{
    [SerializeField] public Vector3 objectPosition;
    [SerializeField] public Vector3 targetPosition;
}

public class SPAAMSolver : MonoBehaviour
{
    public Transform TrackerBase;
    public Matrix4x4 groundTruthEquation;
    public Matrix4x4 manualEquation;
    public bool solved = false;

    public List<MatchingPoints> groundTruthAlignments = new List<MatchingPoints>();
    public List<MatchingPoints> manualAlignments = new List<MatchingPoints>();

    protected SPAAMTargetManager manager;

    public virtual void PerformAlignment(Vector3 objectPosition, Vector3 targetPosition)
    {
        MatchingPoints manualAlignment = new MatchingPoints
        {
            objectPosition = objectPosition,
            targetPosition = targetPosition
        };

        MatchingPoints groundTruthAlignment = new MatchingPoints
        {
            objectPosition = TrackerBase.InverseTransformPoint(targetPosition),
            targetPosition = targetPosition
        };

        manualAlignments.Add(manualAlignment);
        groundTruthAlignments.Add(groundTruthAlignment);
    }


    public virtual void Solve()
    {
        // TODO: Send info to opencv and solve the linear equation
        //Matrix4x4 groundTruth = SolveAlignment(groundTruthAlignments, false);
        groundTruthEquation = SolveAlignment(groundTruthAlignments, true);
        manualEquation = SolveAlignment(manualAlignments, true);
        Debug.Log("[SPAAMSolver] LocalToWorld:" + TrackerBase.localToWorldMatrix);
        groundTruthAlignments.Clear();
        manualAlignments.Clear();
        solved = true;
    }

    protected virtual Matrix4x4 SolveAlignment(List<MatchingPoints> alignments, bool affine = true)
    {
        // input parameters
        int alignmentCount = alignments.Count;
        float[] input = new float[6 * alignmentCount];
        float[] resultMatrix = new float[16];

        // contruct input float array
        for (int i = 0; i < alignmentCount; i++)
        {
            int pairStep = 6 * i;
            MatchingPoints curr = alignments[i];
            input[pairStep] = curr.objectPosition.x;
            input[pairStep + 1] = curr.objectPosition.y;
            input[pairStep + 2] = curr.objectPosition.z;
            input[pairStep + 3] = curr.targetPosition.x;
            input[pairStep + 4] = curr.targetPosition.y;
            input[pairStep + 5] = curr.targetPosition.z;
        }

        // Call opencv function
        float error = HMDSimOpenCV.SPAAM_Solve(input, alignmentCount, resultMatrix, affine, false, true);
        Debug.Log("Reprojection error: " + error);

        // Construct matrix
        Matrix4x4 result = new Matrix4x4();
        for (int i = 0; i < 16; i++)
        {
            result[i] = resultMatrix[i];
        }

        result = result.transpose;
        Debug.Log("Result Matrix is: " + result);

        return result;
    }

    void Update()
    {
        if (!manager)
        {
            manager = SPAAMTargetManager.Instance;
            manager.SetSolver(this);
        }
    }
}
