using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class is responsible for keeping track of an ongoing calibration experiment.
/// 
/// Developers should interact with this class to save data points collected in their experiment.
/// Experiments are design as Ax=B equations where x is a Pose transform and A and B are sets of
/// matching points
/// </summary>
[Serializable]
public class AugmentedReality3D3DCalibrationExperiment
{
    [Header("What calibration is being stored here")]
    public string CalibrationModalityName;
    public bool FullCalibrationStored = false;

    [Header("Calibration data structures")]
    public ExperimentResult3D3D expResult;
    public Matrix4x4 groundTruthEquation;
    public Matrix4x4 manualEquation;
    public List<MatchingPoints> groundTruthAlignments = new List<MatchingPoints>();
    public List<MatchingPoints> manualAlignments = new List<MatchingPoints>();
    protected bool firstPoint = true;

    /// <summary>
    /// Performs an alignment for the Ax=B solver.
    /// A is the sets of points incoming from the tracker, B is where these points should be (usually set by the user)
    /// 
    /// </summary>
    /// <param name="AR_inputPose">The input coming from a tracker</param>
    /// <param name="AR_outputPose">The input coming from user interaction as they matched the aformentioned tracker</param>
    /// <param name="AR_outputPoseGroundTruth">Where the user should have moved the tracker to match the aformentioned tracker</param>
    public void PerformAlignment(Vector3 AR_inputPose, Vector3 AR_outputPose, Vector3 AR_outputPoseGroundTruth)
    {
        if (firstPoint)
        {
            expResult.startTime = System.DateTime.Now;
            firstPoint = false;
        }

        MatchingPoints manualAlignment = new MatchingPoints
        {
            objectPosition = AR_inputPose,
            targetPosition = AR_outputPose
        };

        MatchingPoints groundTruthAlignment = new MatchingPoints
        {
            objectPosition = AR_inputPose,
            targetPosition = AR_outputPoseGroundTruth
        };

        manualAlignments.Add(manualAlignment);
        groundTruthAlignments.Add(groundTruthAlignment);

        // saves user performance
        expResult.i_sixDofInputPosition.Add(AR_inputPose);                        // where the object should be
        expResult.o_sixDofUserTargetPositionAR.Add(AR_outputPose);                // where it was aligned by the user (in the AR coordinate system)
        expResult.o_sixDofGroudTruthInputPosition.Add(AR_outputPoseGroundTruth);  // where it should have been (ground truth)

        // how bad was that? (yes, we pre-calculate for now)
        expResult.sixDofAlignmentsErrorVect.Add(AR_outputPoseGroundTruth - AR_outputPose);

        Debug.Log(String.Format("[ARCalibrationSolver] Aligned {0}  with {1}  (expected (in AR), diff: {3} - {4} cm", PrintVector(AR_inputPose), PrintVector(AR_outputPose), PrintVector(AR_outputPoseGroundTruth), PrintVector(AR_outputPoseGroundTruth - AR_outputPose), (AR_outputPoseGroundTruth - AR_outputPose).magnitude * 100));

        // save last data point for the time-to-task measurement
        expResult.endTime = System.DateTime.Now;
        expResult.completionTime = (expResult.endTime - expResult.startTime).TotalSeconds;

        // let others know that the current stored calibration is not the most up to date
        FullCalibrationStored = false;
    }


    /// <summary>
    /// Should be called after all points were collected and the calibration matrices were stored
    /// </summary>
    public void Calibrate(Matrix4x4 simulatorsGroundTruthTransformationMatrix)
    {
        if (FullCalibrationStored)
            Debug.LogWarning("[ARCalibrationSolver] Already calibrated!");

        // time to calibrate
        groundTruthEquation = SolveAlignment(groundTruthAlignments, true);
        manualEquation = SolveAlignment(manualAlignments, true);

        // store results
        // save results
        expResult.sixDoFUserAlignedMatrix = manualEquation;
        expResult.sixDoFSimulatorGroundTruthMatrix = simulatorsGroundTruthTransformationMatrix;
        expResult.sixDoFGroundTruthAlignedTransformationMatrix = groundTruthEquation;
        expResult.pointsCollected = manualAlignments.Count;

        // decompose matrices so that we can calculate error
        Vector3 rotationGroundTruth = Matrix4x4Utils.ExtractRotationFromMatrix(ref groundTruthEquation).eulerAngles,
                rotationManual = Matrix4x4Utils.ExtractRotationFromMatrix(ref manualEquation).eulerAngles;

        expResult.sixDofRotationError = rotationGroundTruth - rotationManual;

        Vector3 translationGroundTruth = Matrix4x4Utils.ExtractTranslationFromMatrix(ref groundTruthEquation),
                translationManual = Matrix4x4Utils.ExtractTranslationFromMatrix(ref manualEquation);

        expResult.sixDofTranslationError = translationGroundTruth - translationManual;


        Debug.Log("[ARCalibrationSolver] Original: " + simulatorsGroundTruthTransformationMatrix);
        Debug.Log("[ARCalibrationSolver] Manual: " + manualEquation);
        Debug.Log("[ARCalibrationSolver] Ground Truth: " + groundTruthEquation);
        Debug.Log("[ARCalibrationSolver] Translation error: " + expResult.sixDofTranslationError);
        Debug.Log("[ARCalibrationSolver] Rotation error: " + expResult.sixDofRotationError);

        Debug.Log("[ARCalibrationSolver] Resulting rotation: " + PrintVector(rotationManual));
        Debug.Log("[ARCalibrationSolver] Resulting rotation (ground truth): " + PrintVector(rotationGroundTruth));

        Debug.Log("[ARCalibrationSolver] Resulting translation: " + PrintVector(translationManual));
        Debug.Log("[ARCalibrationSolver] Resulting translation (ground truth): " + PrintVector(translationGroundTruth));

        // cleans lists
        groundTruthAlignments = new List<MatchingPoints>();
        manualAlignments = new List<MatchingPoints>();
        FullCalibrationStored = true;
    }

    /// <summary>
    /// Should be called after all points were collected and the calibration matrices were stored
    /// </summary>
    public void SaveExperiment(bool affine = true)
    {
        if (!FullCalibrationStored)
            Debug.LogWarning("[AugmentedReality3D3DCalibrationExperiment] Saving incomplete calibration results (not calibrated)");

        string time = System.DateTime.UtcNow.ToString("HH_mm_ss_dd_MMMM_yyyy");
        ExperimentResult3D3D.SaveToDrive(expResult, "result_" + CalibrationModalityName + "_" + time + ".json");
    }

    static string PrintVector(Vector3 v)
    {
        return String.Format("({0,7:000.0000},{1,7:000.0000},{2,7:000.0000})", v.x, v.y, v.z);
    }

    public void Reset(string CalibrationModality = "Undefined")
    {
        CalibrationModalityName = CalibrationModality;
        firstPoint = true;
        manualAlignments.Clear();
        groundTruthAlignments.Clear();
        expResult = new ExperimentResult3D3D();
        expResult.calibrationModalityStr = CalibrationModalityName;
        expResult.calibrationType = "3D-3D";
        FullCalibrationStored = false;
        groundTruthEquation = Matrix4x4.identity;
        manualEquation = Matrix4x4.identity;
        
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
            input[pairStep + 0] = curr.objectPosition.x;
            input[pairStep + 1] = curr.objectPosition.y;
            input[pairStep + 2] = curr.objectPosition.z;
            input[pairStep + 3] = curr.targetPosition.x;
            input[pairStep + 4] = curr.targetPosition.y;
            input[pairStep + 5] = curr.targetPosition.z;
        }

        // Call OpenCV function - requires plugin
        float error = HMDSimOpenCV.SPAAM_Solve(input, alignmentCount, resultMatrix, affine, false, true);
        Debug.Log("[ARCalibrationSolver] Reprojection error: " + error);

        // Construct matrix
        Matrix4x4 result = new Matrix4x4();
        for (int i = 0; i < 16; i++)
        {
            result[i] = resultMatrix[i];
        }

        result = result.transpose;
        Debug.Log("[ARCalibrationSolver] Result Matrix is: " + result);

        return result;
    }


}
