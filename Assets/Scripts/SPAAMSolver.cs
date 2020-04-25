using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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
        Matrix4x4 groundTruth = SolveAlignment(groundTruthAlignments, false);
        groundTruth = SolveAlignment(groundTruthAlignments, true);
        //Matrix4x4 manual = SolveAlignment(manualAlignments, true);
        Debug.Log("LocalToWorld:" + TrackerBase.localToWorldMatrix);
        groundTruthAlignments.Clear();
        manualAlignments.Clear();
    }

    Matrix4x4 SolveAlignment(List<Alignment> alignments, bool affine = true)
    {
        // input parameters
        int alignmentCount = alignments.Count;
        float[] input = new float[6 * alignmentCount];
        float[] resultMatrix = new float[16];

        // contruct input float array
        for (int i = 0; i < alignmentCount; i++)
        {
            int pairStep = 6 * i;
            Alignment curr = alignments[i];
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
}
