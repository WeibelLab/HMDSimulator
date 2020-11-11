﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Valve.VR;

/// <summary>
/// Implementation of the SPAAMSolver used in Virtual-Augmented Reality Simulator
/// 
/// </summary>
public class CalibrationEvaluation : SPAAMSolver
{
    public enum CalibrationApproach
    {
        None,
        SPAAM,
        Depth_SPAAM,
        Stereo_SPAAM,
        Stylus_mark,
        Pattern_count
    }

    public Transform targetObject;

    /// <summary>
    /// We shall send the user back to the starting position every time a new calibration study starts
    /// </summary>
    public StartingPostion sp;

    public CalibrationApproach pattern;
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
    public Transform calibrationArea;
    public Transform[] displayBoth = new Transform[2];
    public Vector3[] offsetVector3Both = new Vector3[2];

    private Vector3 debug1;
    private Vector3 debug2;

    public ExperimentResult expResult;

    public Matrix4x4[] groundTruthEquationBoth = new Matrix4x4[2];
    public Matrix4x4[] manualEquationBoth = new Matrix4x4[2];

    public List<MatchingPoints>[] groundTruthAlignmentsBoth = new List<MatchingPoints>[2];
    public List<MatchingPoints>[] manualAlignmentsBoth = new List<MatchingPoints>[2];

    protected CalibrationEvalTargetManager ceManager;

    public int side = 0;

    // Below we have elements that can help us run a smooth user study

    public AudioClip pointCollectedSound;
    public AudioClip calibrationDoneSound;
    public AudioClip newCalibrationApproach;
    public AudioSource audioPlayer;

    // Below elements that help us explain to the user what they have to do for each stage
    public Transform WaitForInstructions;
    public Transform DepthSPAAMInstructions;
    public Transform StereoSPAAMInstructions;
    public Transform StylusMarkInstructions;

    void Start()
    {
        //manager = (SPAAMEvalTargetManager)SPAAMTargetManager.Instance;
        //manager.SetSolver(this);
        //cameraSpace = manager.displayCanvas.worldCamera;
        groundTruthAlignmentsBoth[0] = new List<MatchingPoints>();
        groundTruthAlignmentsBoth[1] = new List<MatchingPoints>();
        manualAlignmentsBoth[0] = new List<MatchingPoints>();
        manualAlignmentsBoth[1] = new List<MatchingPoints>();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="objectPosition"></param>
    /// <param name="targetPosition"></param>
    public override void PerformAlignment(Vector3 objectPosition, Vector3 targetPosition)
    {

        if (pattern == CalibrationApproach.SPAAM || pattern == CalibrationApproach.Depth_SPAAM)
        {
            MatchingPoints manualAlignment = new MatchingPoints
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


            MatchingPoints groundTruthAlignment = new MatchingPoints
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
                Vector3 newTargetPosition = targetPosition;
                if (pattern == CalibrationApproach.Stereo_SPAAM)
                {
                    //targetPosition = ceManager.targetPositionsOutput[i];
                }

                Vector3 dir = newTargetPosition - Camera.main.transform.TransformPoint(offsetVector3Both[i]);
                Vector3 localEye = displayBoth[i]
                    .InverseTransformPoint(Camera.main.transform.TransformPoint(offsetVector3Both[i]));
                Vector3 dirToLen = displayBoth[i].transform.InverseTransformDirection(dir.normalized);
                dirToLen /= dirToLen.z;
                dirToLen *= Mathf.Abs(localEye.z);
                //Debug.Log(dirToLen * 1000);
                Vector3 manualResult = dirToLen + localEye;
                manualResult.x /= (0.0568f / 2.0f);
                manualResult.y /= 0.015f;
                manualResult.z = 0;
                newTargetPosition = manualResult;

                //ceManager.displayTemplate[i].gameObject.SetActive(true);
                //ceManager.displayTemplate[i].transform.localPosition = new Vector3(newTargetPosition.x * 1024, newTargetPosition.y * 540, 0);

                //Debug.Log(manualResult * 1000);

                MatchingPoints manualAlignment = new MatchingPoints
                {
                    objectPosition = objectPosition,
                    targetPosition = newTargetPosition
                };

                Vector3 direction = new Vector3(newTargetPosition.x * 0.0568f / 2.0f, newTargetPosition.y * 0.015f, 0);

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


                MatchingPoints groundTruthAlignment = new MatchingPoints
                {
                    objectPosition = Camera.main.transform.TransformPoint(offsetVector3Both[i]) + direction * dist,
                    targetPosition = newTargetPosition
                };

                Debug.Log("[CalibrationEvaluation] ====================");
                Debug.Log("[CalibrationEvaluation] manualObject:" + manualAlignment.objectPosition.ToString("G4"));
                Debug.Log("[CalibrationEvaluation] groundTruthObject:" + groundTruthAlignment.objectPosition.ToString("G4"));
                Debug.Log("[CalibrationEvaluation] target:" + groundTruthAlignment.targetPosition.ToString("G4"));
                Debug.Log("[CalibrationEvaluation] =====");
                manualAlignment.objectPosition = cameraTracker.InverseTransformPoint(manualAlignment.objectPosition);
                groundTruthAlignment.objectPosition =
                    cameraTracker.InverseTransformPoint(groundTruthAlignment.objectPosition);

                manualAlignmentsBoth[i].Add(manualAlignment);
                groundTruthAlignmentsBoth[i].Add(groundTruthAlignment);
                Debug.Log("[CalibrationEvaluation] manualObject:" + manualAlignment.objectPosition.ToString("G4"));
                Debug.Log("[CalibrationEvaluation] groundTruthObject:" + groundTruthAlignment.objectPosition.ToString("G4"));
                Debug.Log("[CalibrationEvaluation] target:" + groundTruthAlignment.targetPosition.ToString("G4"));
            }
        }


        audioPlayer.clip = pointCollectedSound;
        audioPlayer.Play();

        //Debug.Log(groundTruthAlignment.objectPosition);
    }

    void ResetPattern()
    {
        // each time we reset the pattern, we play a tune to let the user know

        expResult = new ExperimentResult();
        expResult.calibrationModality = (int) pattern;
        expResult.calibrationModalityStr = pattern.ToString();

        lastIndices[0] = -1;
        lastIndices[1] = -1;
        solved = false;
        side = 0;

        // communicates with the AR side to change what the user sees
        ceManager.ConditionChange(pattern);

        // applies changes the the local environment
        changeVisibility(DepthSPAAMInstructions, false);
        changeVisibility(StereoSPAAMInstructions, false);
        changeVisibility(StylusMarkInstructions, false);
        changeVisibility(WaitForInstructions, false);

        switch (pattern)
        {
            case CalibrationApproach.Depth_SPAAM:
                changeVisibility(DepthSPAAMInstructions, true);
                break;
            case CalibrationApproach.Stereo_SPAAM:
                changeVisibility(StereoSPAAMInstructions, true);
                break;
            case CalibrationApproach.Stylus_mark:
                changeVisibility(StylusMarkInstructions, true);
                break;
            default:
                changeVisibility(WaitForInstructions, true);
                break;
        }

        if (pattern != CalibrationApproach.None)
        { 
            audioPlayer.clip = newCalibrationApproach;
            audioPlayer.Play();
        }
    }

    void changeVisibility(Transform t, bool visible)
    {
        foreach(MeshRenderer me in t.GetComponentsInChildren<MeshRenderer>())
        {
            me.enabled = visible;
        }
    }

    void FixedUpdate()
    {
        if (pattern == CalibrationApproach.Stereo_SPAAM)
        {
            calibrationArea.position = cameraTracker.transform.position;
            calibrationArea.rotation = cameraTracker.transform.rotation;
        }
    }

    void Update()
    {
        // nothing
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            pattern = CalibrationApproach.None;
            ResetPattern();
        }

        // calibration 1
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            pattern = CalibrationApproach.Depth_SPAAM;
            sp.lockPosition = true;
            ResetPattern();
        }

        // calibration 2
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            pattern = CalibrationApproach.Stereo_SPAAM;
            sp.lockPosition = false;
            ResetPattern();
        }

        // calibration 3
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            pattern = CalibrationApproach.Stylus_mark;
            sp.lockPosition = false;
            ResetPattern();
        }

        // calibration 3
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!ceManager.initialized)
            {
                ceManager.InitializePosition();
            }
            else
            {
                Solve();
            }
        }




        if (Input.GetKeyDown(KeyCode.M))
        {
            // Switch condition
            pattern = (CalibrationApproach)((int)(pattern + 1) % (int)CalibrationApproach.Pattern_count);
            
            if (pattern == CalibrationApproach.Depth_SPAAM)
            {
                sp.lockPosition = true;
            }
            else
            {
                sp.lockPosition = false;
            }

            ResetPattern();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            // Reset condition
            ResetPattern();
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            // TODO: Store expResult
            string time = System.DateTime.UtcNow.ToString("HH_mm_dd_MMMM_yyyy");
            ExperimentResult.SaveToDrive(expResult, "D://test//result_"+ time +".json");
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
                    case CalibrationApproach.SPAAM:
                        break;
                    case CalibrationApproach.Depth_SPAAM:
                        currentDist = minDist + magicDistLookupList[ceManager.indices[side]] * step;
                        break;
                    case CalibrationApproach.Stereo_SPAAM:
                    case CalibrationApproach.Stylus_mark:
                        currentDist = minDist - 1.1f + magicDistLookupList[ceManager.indices[side]] * step * 0.5f;
                        break;
                }

                lastIndices[side] = ceManager.indices[side];

                Vector3 newPosition = targetObject.position;
                newPosition.z -= currentDist;
                position.position = newPosition;
            }
        }
    }

    public void SetError(Vector3 error, int index)
    {
        if (index == 0)
        {
            expResult.errorLeft = error;
        }
        else
        {
            expResult.errorRight = error;
        }
    }

    public override void Solve()
    {
        // TODO: Send info to opencv and solve the linear equation
        //Matrix4x4 groundTruth = SolveAlignment(groundTruthAlignments, false);
        //Debug.Log("Ground Truth");
        groundTruthEquationBoth[0] = SolveAlignment(groundTruthAlignmentsBoth[0], true, true);
        groundTruthEquationBoth[1] = SolveAlignment(groundTruthAlignmentsBoth[1], true, true);
        //Debug.Log("Manual");
        manualEquationBoth[0] = SolveAlignment(manualAlignmentsBoth[0], true, false);
        manualEquationBoth[1] = SolveAlignment(manualAlignmentsBoth[1], true, false);

        expResult.pointsCollected = manualAlignmentsBoth[0].Count;

        expResult.groundTruthProjectionMatrixLeft = groundTruthEquationBoth[0];
        expResult.groundTruthProjectionMatrixRight = groundTruthEquationBoth[1];
        expResult.projectionMatrixLeft = manualEquationBoth[0];
        expResult.projectionMatrixRight = manualEquationBoth[1];


        //Debug.Log("LocalToWorld:" + TrackerBase.localToWorldMatrix);
        groundTruthAlignmentsBoth[0].Clear();
        groundTruthAlignmentsBoth[1].Clear();
        manualAlignmentsBoth[0].Clear();
        manualAlignmentsBoth[1].Clear();
        solved = true;

        audioPlayer.clip = calibrationDoneSound;
        audioPlayer.Play();

    }

    protected Matrix4x4 SolveAlignment(List<MatchingPoints> alignments, bool affine = true, bool groundTruth = false)
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
            MatchingPoints curr = alignments[i];
            input[pairStep] = curr.objectPosition.x;
            input[pairStep + 1] = curr.objectPosition.y;
            input[pairStep + 2] = curr.objectPosition.z;
            input[pairStep + 3] = curr.targetPosition.x;
            input[pairStep + 4] = curr.targetPosition.y;
        }

        // Call opencv function
        float error = HMDSimOpenCV.SPAAM_Solve(input, alignmentCount, resultMatrix, affine, true, true);
        Debug.Log("[CalibrationEvaluation] Reprojection error: " + error);

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


            Debug.Log("[CalibrationEvaluation] Condition number is: " + conditionNumber);
            Debug.Log("[CalibrationEvaluation] Offset is: " + resultOffset);
            Debug.Log("[CalibrationEvaluation] Result Matrix is: " + result);


        }
        else
        {
            conditionNumberGT = (resultMatrix[12] / resultMatrix[13]);
            resultOffsetGT.x = resultMatrix[14];
            resultOffsetGT.y = resultMatrix[15];
            resultOffsetGT.z = resultMatrix[16];
            resultOffsetGT.w = resultMatrix[17];

            Debug.Log("[CalibrationEvaluation] Condition number is: " + conditionNumberGT);
            Debug.Log("[CalibrationEvaluation] Offset is: " + resultOffsetGT);
            Debug.Log("[CalibrationEvaluation] Result Matrix is: " + result);
        }


        return result;
    }
}
