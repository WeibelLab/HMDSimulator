using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SPAAM2DSolver : SPAAMSolver
{
    public enum Pattern
    {
        Static = 0,
        Sequential,
        Magic
    }

    public Transform targetObject;

    public Pattern pattern;
    public float minDist = 1.28f;
    public float step = 0.1f;
    public float currentDist = 2.0f;
    public Transform position;

    public Vector4 resultOffsetGT;
    public float conditionNumberGT;
    public Vector4 resultOffset;
    public float conditionNumber;

    public List<int> magicDistLookupList = new List<int>();

    private int lastIndex = -1;

    public Camera cameraSpace;
    public Transform cameraTracker;
    public Transform display;
    public Vector3 offsetVector3;

    private Vector3 debug1;
    private Vector3 debug2;

    void Start()
    {
        //manager = (SPAAMEvalTargetManager)SPAAMTargetManager.Instance;
        //manager.SetSolver(this);
        //cameraSpace = manager.displayCanvas.worldCamera;
    }

    // Start is called before the first frame update
    public override void PerformAlignment(Vector3 objectPosition, Vector3 targetPosition)
    {
        MatchingPoints manualAlignment = new MatchingPoints
        {
            objectPosition = objectPosition,
            targetPosition = targetPosition
        };
        
        Vector3 direction = new Vector3(targetPosition.x * 0.025f, targetPosition.y * 0.015f, 0);
        
        direction = display.TransformPoint(direction) - Camera.main.transform.TransformPoint(offsetVector3);
        direction = direction.normalized;
        //Debug.Log(camera.InverseTransformDirection(direction));
        Vector3 diff = objectPosition - Camera.main.transform.TransformPoint(offsetVector3);//camera.position;
        //debug1 = direction;
        //debug2 = diff.normalized;
        //Debug.Log(camera.InverseTransformDirection(diff.normalized));
        float dist = diff.magnitude;
        

        MatchingPoints groundTruthAlignment = new MatchingPoints
        {
            objectPosition = Camera.main.transform.TransformPoint(offsetVector3) + direction * dist,
            targetPosition = targetPosition
        };

        manualAlignment.objectPosition = cameraTracker.InverseTransformPoint(manualAlignment.objectPosition);
        groundTruthAlignment.objectPosition = cameraTracker.InverseTransformPoint(groundTruthAlignment.objectPosition);

        manualAlignments.Add(manualAlignment);
        groundTruthAlignments.Add(groundTruthAlignment);
        //Debug.Log(groundTruthAlignment.objectPosition);
    }


    void Update()
    {
        if (!manager)
        {
            manager = (SPAAMEvalTargetManager)SPAAMTargetManager.Instance;
            manager.SetSolver(this);
            cameraSpace = ((SPAAMEvalTargetManager)manager).displayCanvas.worldCamera;
        }
        offsetVector3 = ((SPAAMEvalTargetManager)manager).dp.localPos;
        //Debug.DrawRay(camera.position, debug1, Color.blue);
        //Debug.DrawRay(camera.position, debug2, Color.green);

        if (manager && manager.initialized)
        {
            if (lastIndex != manager.index)
            {
                // Update position
                switch (pattern)
                {
                    case Pattern.Static:
                        currentDist = 2.0f;
                        break;
                    case Pattern.Sequential:
                        currentDist = minDist + manager.index * step;
                        break;
                    case Pattern.Magic:
                        currentDist = minDist + magicDistLookupList[manager.index] * step;
                        break;
                }

                lastIndex = manager.index;

                Vector3 newPosition = targetObject.position;
                newPosition.z -= currentDist;
                position.position = newPosition;
            }
        }
    }

    public override void Solve()
    {
        // TODO: Send info to opencv and solve the linear equation
        //Matrix4x4 groundTruth = SolveAlignment(groundTruthAlignments, false);
        Debug.Log("Ground Truth");
        groundTruthEquation = SolveAlignment(groundTruthAlignments, true, true);
        Debug.Log("Manual");
        manualEquation = SolveAlignment(manualAlignments, true, false);
        //Debug.Log("LocalToWorld:" + TrackerBase.localToWorldMatrix);
        groundTruthAlignments.Clear();
        manualAlignments.Clear();
        solved = true;
    }

    protected Matrix4x4 SolveAlignment(List<MatchingPoints> alignments, bool affine = true, bool groundTruth = false)
    {
        // input parameters
        int alignmentCount = alignments.Count;
        float[] input = new float[5 * alignmentCount];
        float[] resultMatrix = new float[18];

        // contruct input float array
        for (int i = 0; i < alignmentCount; i++)
        {
            int pairStep = 5 * i;
            MatchingPoints curr = alignments[i];
            input[pairStep] = curr.objectPosition.x;
            input[pairStep + 1] = curr.objectPosition.y;
            input[pairStep + 2] = curr.objectPosition.z;
            input[pairStep + 3] = curr.targetPosition.x;
            input[pairStep + 4] = curr.targetPosition.y;
        }

        // Call opencv function
        float error = HMDSimOpenCV.SPAAM_Solve(input, alignmentCount, resultMatrix, affine, true, true);
        Debug.Log("Reprojection error: " + error);

        // Construct matrix
        Matrix4x4 result = Matrix4x4.zero;
        for (int i = 0; i < 12; i++)
        {
            result[i] = resultMatrix[i];
        }

        result = result.transpose;
        if (!groundTruth)
        {
            conditionNumber = (resultMatrix[12] / resultMatrix[13]);
            resultOffset.x = resultMatrix[14];
            resultOffset.y = resultMatrix[15];
            resultOffset.z = resultMatrix[16];
            resultOffset.w = resultMatrix[17];


            Debug.Log("Condition number is: " + conditionNumber);
            Debug.Log("Offset is: " + resultOffset);
            Debug.Log("Result Matrix is: " + result);
        }
        else
        {
            conditionNumberGT = (resultMatrix[12] / resultMatrix[13]);
            resultOffsetGT.x = resultMatrix[14];
            resultOffsetGT.y = resultMatrix[15];
            resultOffsetGT.z = resultMatrix[16];
            resultOffsetGT.w = resultMatrix[17];


            Debug.Log("Condition number is: " + conditionNumberGT);
            Debug.Log("Offset is: " + resultOffsetGT);
            Debug.Log("Result Matrix is: " + result);
        }


        return result;
    }
}
