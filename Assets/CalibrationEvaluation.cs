using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CalibrationEvaluation : SPAAMSolver
{
    public enum Pattern
    {
        SPAAM = 0,
        Depth_SPAAM,
        Stereo_SPAAM,
        Stylus_mark,
        Pattern_count
    }

    public Transform targetObject;
    public StartingPostion sp;

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

    public int[] lastIndices = new[] {-1, -1};

    public Camera[] cameraSpaceBoth = new Camera[2];
    public Transform cameraTracker;
    public Transform[] displayBoth = new Transform[2];
    public Vector3[] offsetVector3Both = new Vector3[2];

    private Vector3 debug1;
    private Vector3 debug2;

    public ExperimentResult expResult;

    public Matrix4x4[] groundTruthEquationBoth = new Matrix4x4[2];
    public Matrix4x4[] manualEquationBoth = new Matrix4x4[2];

    public List<Alignment>[] groundTruthAlignmentsBoth = new List<Alignment>[2];
    public List<Alignment>[] manualAlignmentsBoth = new List<Alignment>[2];

    protected CalibrationEvalTargetManager ceManager;

    public int side = 0;

    void Start()
    {
        //manager = (SPAAMEvalTargetManager)SPAAMTargetManager.Instance;
        //manager.SetSolver(this);
        //cameraSpace = manager.displayCanvas.worldCamera;
        groundTruthAlignmentsBoth[0] = new List<Alignment>();
        groundTruthAlignmentsBoth[1] = new List<Alignment>();
        manualAlignmentsBoth[0] = new List<Alignment>();
        manualAlignmentsBoth[1] = new List<Alignment>();
    }

    // Start is called before the first frame update
    public override void PerformAlignment(Vector3 objectPosition, Vector3 targetPosition)
    {

        if (pattern == Pattern.SPAAM || pattern == Pattern.Depth_SPAAM)
        {
            Alignment manualAlignment = new Alignment
            {
                objectPosition = objectPosition,
                targetPosition = targetPosition
            };

            Vector3 direction = new Vector3(targetPosition.x * 0.0568f / 2.0f, targetPosition.y * 0.015f, 0);

            direction = displayBoth[side].TransformPoint(direction) -
                        Camera.main.transform.TransformPoint(offsetVector3Both[side]);
            direction = direction.normalized;
            //Debug.Log(camera.InverseTransformDirection(direction));
            Vector3 diff =
                objectPosition - Camera.main.transform.TransformPoint(offsetVector3Both[side]); //camera.position;
            //debug1 = direction;
            //debug2 = diff.normalized;
            //Debug.Log(camera.InverseTransformDirection(diff.normalized));
            float dist = diff.magnitude;


            Alignment groundTruthAlignment = new Alignment
            {
                objectPosition = Camera.main.transform.TransformPoint(offsetVector3Both[side]) + direction * dist,
                targetPosition = targetPosition
            };

            manualAlignment.objectPosition = cameraTracker.InverseTransformPoint(manualAlignment.objectPosition);
            groundTruthAlignment.objectPosition =
                cameraTracker.InverseTransformPoint(groundTruthAlignment.objectPosition);

            manualAlignmentsBoth[side].Add(manualAlignment);
            groundTruthAlignmentsBoth[side].Add(groundTruthAlignment);

            side = side == 0 ? 1 : 0;
        }
        else
        {
            for (int i = 0; i < 2; i++)
            {
                targetPosition = ceManager.targetPositionsOutput[i];

                Alignment manualAlignment = new Alignment
                {
                    objectPosition = objectPosition,
                    targetPosition = targetPosition
                };

                Vector3 direction = new Vector3(targetPosition.x * 0.0568f / 2.0f, targetPosition.y * 0.015f, 0);

                direction = displayBoth[i].TransformPoint(direction) -
                            Camera.main.transform.TransformPoint(offsetVector3Both[i]);
                direction = direction.normalized;
                //Debug.Log(camera.InverseTransformDirection(direction));
                Vector3 diff =
                    objectPosition - Camera.main.transform.TransformPoint(offsetVector3Both[i]); //camera.position;
                //debug1 = direction;
                //debug2 = diff.normalized;
                //Debug.Log(camera.InverseTransformDirection(diff.normalized));
                float dist = diff.magnitude;


                Alignment groundTruthAlignment = new Alignment
                {
                    objectPosition = Camera.main.transform.TransformPoint(offsetVector3Both[i]) + direction * dist,
                    targetPosition = targetPosition
                };

                manualAlignment.objectPosition = cameraTracker.InverseTransformPoint(manualAlignment.objectPosition);
                groundTruthAlignment.objectPosition =
                    cameraTracker.InverseTransformPoint(groundTruthAlignment.objectPosition);

                manualAlignmentsBoth[i].Add(manualAlignment);
                groundTruthAlignmentsBoth[i].Add(groundTruthAlignment);

            }
        }

        //Debug.Log(groundTruthAlignment.objectPosition);
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            // Switch condition
            pattern = (Pattern)((int)(pattern + 1) % (int)Pattern.Pattern_count);
            if (pattern == Pattern.Depth_SPAAM)
            {
                sp.lockPosition = true;
            }
            else
            {
                sp.lockPosition = false;
            }

            lastIndices[0] = -1;
            lastIndices[1] = -1;
            solved = false;
            side = 0;
            ceManager.ConditionChange(pattern);
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            // Reset condition
            lastIndices[0] = -1;
            lastIndices[1] = -1;
            solved = false;
            side = 0;
            ceManager.ConditionChange(pattern);
        }

        if (!ceManager)
        {
            ceManager = (CalibrationEvalTargetManager)SPAAMTargetManager.Instance;
            ceManager.SetSolver(this);
            cameraSpaceBoth[0] = ceManager.displayCanvas[0].worldCamera;
            cameraSpaceBoth[1] = ceManager.displayCanvas[1].worldCamera;
        }
        offsetVector3Both[0] = ceManager.dp[0].localPos;
        offsetVector3Both[1] = ceManager.dp[1].localPos;
        //Debug.DrawRay(camera.position, debug1, Color.blue);
        //Debug.DrawRay(camera.position, debug2, Color.green);

        if (ceManager && ceManager.initialized)
        {
            if (lastIndices[side] != ceManager.indices[side])
            {
                // Update position
                switch (pattern)
                {
                    case Pattern.SPAAM:
                        break;
                    case Pattern.Depth_SPAAM:
                    case Pattern.Stereo_SPAAM:
                        currentDist = minDist + magicDistLookupList[ceManager.indices[side]] * step;
                        break;
                    case Pattern.Stylus_mark:
                        break;
                }

                lastIndices[side] = ceManager.indices[side];

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
        groundTruthEquationBoth[0] = SolveAlignment(groundTruthAlignmentsBoth[0], true, true);
        groundTruthEquationBoth[1] = SolveAlignment(groundTruthAlignmentsBoth[1], true, true);
        Debug.Log("Manual");
        manualEquationBoth[0] = SolveAlignment(manualAlignmentsBoth[0], true, false);
        manualEquationBoth[1] = SolveAlignment(manualAlignmentsBoth[1], true, false);
        //Debug.Log("LocalToWorld:" + TrackerBase.localToWorldMatrix);
        groundTruthAlignmentsBoth[0].Clear();
        groundTruthAlignmentsBoth[1].Clear();
        manualAlignmentsBoth[0].Clear();
        manualAlignmentsBoth[1].Clear();
        solved = true;
    }

    protected Matrix4x4 SolveAlignment(List<Alignment> alignments, bool affine = true, bool groundTruth = false)
    {
        // input parameters
        int alignmentCount = alignments.Count;
        if (alignmentCount <= 6)
        {
            return Matrix4x4.identity;
        }
        float[] input = new float[5 * alignmentCount];
        float[] resultMatrix = new float[18];

        // contruct input float array
        for (int i = 0; i < alignmentCount; i++)
        {
            int pairStep = 5 * i;
            Alignment curr = alignments[i];
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
