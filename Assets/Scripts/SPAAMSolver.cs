using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.XR.Interaction;
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
    
    [Tooltip("Object that the user will interact with in order to perform calibration")]
    public Transform targetObject;
    public Transform targetCoordinateSystem;
    public Transform sphere1;
    public Transform sphere2;
    public Transform sphere3;
    public Transform sphere4;
    protected Vector3 targetObjectStartPosition;
    protected Quaternion targetObjectStartRotation;
    private PoseInterpolation targetObjectLerper;

    [Tooltip("If checked, calibration will happen automatically after a certain amount of points is collected")]
    public bool calibrateAutomatically = false;

    [Tooltip("Have we solved it?")]
    public bool solved = false;

    public enum SixDofCalibrationApproach
    {
        None,
        CubesHead,          // calibrate cube by moving both
        CubesHandHead,      // calibrate cubes using hand and head
        CubesHologram,       // calibrate cubes by moving a hologram
        Pattern_count
    }

    [Header("6DoF Calibration specifics")]
    [Tooltip("The tracker's coordinate system center")]
    public Transform TrackerBase;

    public SixDofCalibrationApproach sixDoFPattern;

    [Header("Visual feedback")]
    public Transform[] ModalityInstructions;

    // Below we have elements that can help us run a smooth user study
    [Header("Audio feedback")]
    public bool playAudio = true;
    public AudioClip pointCollectedSound;
    public AudioClip calibrationDoneSound;
    public AudioClip newCalibrationApproachSound;
    public AudioSource audioPlayer;

    [Header("Voice feedback (Generic)")]
    public bool playVoiceFeedback = true;
    public AudioClip voiceOverResultsSaved;
    public AudioClip voiceOverCalibrated;
    public AudioClip voiceOverCollectMorePoints;
    public AudioSource audioPlayerForVoiceOver;

    protected SPAAMTargetManager manager;
    protected GrabbableCube cubeGrabbing;

    [Header("Calibration data structures")]
    public ExperimentResult expResult;
    public Matrix4x4 groundTruthEquation;
    public Matrix4x4 manualEquation;
    public List<MatchingPoints> groundTruthAlignments = new List<MatchingPoints>();
    public List<MatchingPoints> manualAlignments = new List<MatchingPoints>();
    protected bool firstPoint = true;


    [Header("Hologram calibration approach")]
    [Tooltip("Where should cube go so that the user can calibrate")]
    public Transform[] CubePositions;
    private int currentCubePosition = 0;

    [Tooltip("Object used to move the hologram with one hand")]
    public Transform InvisibleCube;
    public Vector3 InvisibleCubeInitialPosition = new Vector3(0.0f,0.1f,0.2f);
    private PoseInterpolation InvisibleCubeLerper;
    


    void Start()
    {
        // reset points collected to zero
        groundTruthAlignments = new List<MatchingPoints>();
        manualAlignments = new List<MatchingPoints>();

        targetObjectLerper = targetObject.GetComponentInChildren<PoseInterpolation>();
        cubeGrabbing = targetObject.GetComponentInChildren<GrabbableCube>();
        InvisibleCubeLerper = InvisibleCube.GetComponentInChildren<PoseInterpolation>();
    }


    /// <summary>
    /// Generic method that can be used by any version of this class to toggle transforms
    /// based on an index number (usually the modality being studied)
    /// </summary>
    /// <param name="index"></param>
    public void updateModalityInstructions(int index)
    {
        foreach (Transform t in ModalityInstructions)
        {
            changeVisibility(t, false);
        }

        if (index < ModalityInstructions.Length)
        {
            changeVisibility(ModalityInstructions[index], true);
        }
    }

    string PrintVector(Vector3 v)
    {
        return String.Format("({0,7:000.0000},{1,7:000.0000},{2,7:000.0000})", v.x, v.y, v.z);
    }

    public virtual void PerformAlignment(Vector3 objectPosition, Vector3 targetPosition)
    {
        if (firstPoint)
        {
            expResult.startTime = System.DateTime.Now;
            firstPoint = false;
        }


        MatchingPoints manualAlignment = new MatchingPoints
        {
            objectPosition = objectPosition,
            targetPosition = targetPosition
        };

        Vector3 groundTruthPosition = TrackerBase.InverseTransformPoint(targetPosition - manager.offset); // todo: in the future, offset might include rotation

        MatchingPoints groundTruthAlignment = new MatchingPoints
        {
            objectPosition = groundTruthPosition,
            targetPosition = targetPosition
        };

        manualAlignments.Add(manualAlignment);
        groundTruthAlignments.Add(groundTruthAlignment);

        // saves user performance
        expResult.sixDofObjectPosition.Add(groundTruthPosition);
        expResult.sixDofAlignedPosition.Add(objectPosition);
        expResult.sixDofObjectPositionAR.Add(targetPosition);

        // how bad was that? (yes, we pre-calculate for now)
        expResult.sixDofAlignmentsErrorVect.Add(groundTruthPosition - objectPosition);

        Debug.Log(String.Format("[SPAAMSolver] Aligned {0} with {1} (expected {2}, diff: {3}", PrintVector(objectPosition), PrintVector(targetPosition), PrintVector(groundTruthPosition), PrintVector(groundTruthPosition - objectPosition)));

        // save last data point for the time-to-task measurement
        expResult.endTime = System.DateTime.Now;
        expResult.completionTime = (expResult.endTime - expResult.startTime).TotalSeconds;

        // play sound
        if (playAudio)
        {
            audioPlayer.clip = pointCollectedSound;
            audioPlayer.Play();
        }

        // should we calibrate automatically now that we know what's up?
        if (calibrateAutomatically && manualAlignments.Count >= GetRequiredPointsToCalibrate((int)sixDoFPattern))
        {
            // solvees
            Solve();

            // saves results
            string time = System.DateTime.UtcNow.ToString("HH_mm_ss_dd_MMMM_yyyy");
            ExperimentResult.SaveToDrive(expResult, "result_" + sixDoFPattern.ToString() + "_" + time + ".json");
        }

    }

    public virtual void ResetPattern()
    {
        // each time we reset the projectionPattern, we play a tune to let the user know
        firstPoint = true;
        solved = false;
        expResult = new ExperimentResult();
        expResult.calibrationType = "3D-3D";
        expResult.calibrationModality = (int)sixDoFPattern;
        expResult.calibrationModalityStr = sixDoFPattern.ToString();

        // communicates with the AR side to change what the user sees
        manager.ConditionChange(sixDoFPattern);

        // resets the location of the cube
        targetObject.localPosition = targetObjectStartPosition;
        targetObject.localRotation = targetObjectStartRotation;

        // applies changes the the local environment
        updateModalityInstructions((int)sixDoFPattern);

        // updates the simulation so that we cannot grab the cube in the second one
        switch (sixDoFPattern)
        {
            // none case is only for instructing the user, so shows target and allow the user to do what they want
            case SixDofCalibrationApproach.None:
                cubeGrabbing.CanGrab = true;
                cubeGrabbing.GoesBackToInitialPosition = true;
                cubeGrabbing.SendBackToInitialPose();
                break;

            case SixDofCalibrationApproach.CubesHandHead:
                cubeGrabbing.CanGrab = true;
                cubeGrabbing.GoesBackToInitialPosition = true;
                cubeGrabbing.SendBackToInitialPose();
                break;

            case SixDofCalibrationApproach.CubesHead:
                cubeGrabbing.CanGrab = true;
                cubeGrabbing.GoesBackToInitialPosition = true;
                cubeGrabbing.SendBackToInitialPose();
                break;

            case SixDofCalibrationApproach.CubesHologram:
                cubeGrabbing.CanGrab = false;
                cubeGrabbing.GoesBackToInitialPosition = false;
                currentCubePosition = 0;
                targetObjectLerper.StartLerping(CubePositions[currentCubePosition % CubePositions.Length].position, CubePositions[currentCubePosition % CubePositions.Length].rotation);
                InvisibleCubeLerper.StartLerping(Camera.main.transform.TransformPoint(InvisibleCubeInitialPosition), Quaternion.identity);
                
                break;
        }

        if (sixDoFPattern != SixDofCalibrationApproach.None)
        {
            audioPlayer.clip = newCalibrationApproachSound;
            audioPlayer.Play();
        }
    }

    
    static public void changeVisibility(Transform t, bool visible)
    {
        // show / hide all meshes
        foreach(MeshRenderer me in t.GetComponentsInChildren<MeshRenderer>())
        {
            me.enabled = visible;
        }

        // play / stop playing all instructions
        foreach(AudioSource a in t.GetComponentsInChildren<AudioSource>())
        {
            if (visible)
            {
                a.Play();
            } else
            {
                if (a.isPlaying)
                    a.Stop();
            }
        }
    }

    /// <summary>
    /// Returns the number of points required for calibration
    /// (SPAAMSolver implementations should override this class)
    /// </summary>
    /// <returns></returns>
    public virtual int GetRequiredPointsToCalibrate(int calibrationid)
    {
        switch (calibrationid)
        {
            case (int)SixDofCalibrationApproach.CubesHead:
            case (int)SixDofCalibrationApproach.CubesHandHead:
                return 24;

            case (int) SixDofCalibrationApproach.CubesHologram:
                return 24;
        }

        return 9;
    }

    public virtual void Solve()
    {

        if (sixDoFPattern == SixDofCalibrationApproach.None)
        {
            Debug.LogWarning("[SPAAMSolver] Won't calibrate / solve when no modes are set!");
            return;
        }

        // warns user that more points are needed
        if (manualAlignments.Count < GetRequiredPointsToCalibrate((int)sixDoFPattern))
        {
            audioPlayerForVoiceOver.clip = voiceOverCollectMorePoints;
            audioPlayerForVoiceOver.Play();
            return;
        }

        // TODO: Send info to opencv and solve the linear equation
        //Matrix4x4 groundTruth = SolveAlignment(groundTruthAlignments, false);


        groundTruthEquation = SolveAlignment(groundTruthAlignments, true);
        manualEquation = SolveAlignment(manualAlignments, true);

        // save results
        expResult.sixDoFAutoAlignedMatrix = groundTruthEquation;
        expResult.sixDoFGroundTruthTransformationMatrix = targetCoordinateSystem.localToWorldMatrix;
        expResult.sixDoFTransformationMatrix = manualEquation;

        // points?
        expResult.pointsCollected = manualAlignments.Count;

        // decompose matrices so that we can calculate error
        Vector3 rotationGroundTruth = Matrix4x4Utils.ExtractRotationFromMatrix(ref groundTruthEquation).eulerAngles,
                rotationManual = Matrix4x4Utils.ExtractRotationFromMatrix(ref manualEquation).eulerAngles;

        expResult.sixDofRotationError = rotationGroundTruth - rotationManual;

        Vector3 translationGroundTruth = Matrix4x4Utils.ExtractTranslationFromMatrix(ref groundTruthEquation),
                translationManual = Matrix4x4Utils.ExtractTranslationFromMatrix(ref manualEquation);


        expResult.sixDofTranslationError = translationGroundTruth - translationManual;

        Debug.Log("[SPAAMSolver] Original: " + TrackerBase.localToWorldMatrix);
        Debug.Log("[SPAAMSolver] Manual: " + manualEquation);
        Debug.Log("[SPAAMSolver] Ground Truth: " + groundTruthEquation);
        Debug.Log("[SPAAMSolver] Translation error: " + expResult.sixDofTranslationError);
        Debug.Log("[SPAAMSolver] Rotation error: " + expResult.sixDofRotationError);

        Debug.Log("[SPAAMSolver] Resulting rotation: " + PrintVector(rotationManual));
        Debug.Log("[SPAAMSolver] Resulting rotation (ground truth): " + PrintVector(rotationGroundTruth));

        Debug.Log("[SPAAMSolver] Resulting translation: " + PrintVector(translationManual));
        Debug.Log("[SPAAMSolver] Resulting translation (ground truth): " + PrintVector(translationGroundTruth));



        // cleans lists
        groundTruthAlignments = new List<MatchingPoints>();
        manualAlignments = new List<MatchingPoints>();
        solved = true;

        if (playAudio)
        {
            audioPlayer.clip = calibrationDoneSound;
            audioPlayer.Play();
        }

        if (playVoiceFeedback)
        {
            audioPlayerForVoiceOver.clip = voiceOverCalibrated;
            audioPlayerForVoiceOver.Play();
        }

        // allow the user to move the object
        cubeGrabbing.CanGrab = true;
        cubeGrabbing.GoesBackToInitialPosition = false;

        // change what the user sees in the AR world
        manager.SetCalibrated();

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

        // Call opencv function
        float error = HMDSimOpenCV.SPAAM_Solve(input, alignmentCount, resultMatrix, affine, false, true);
        Debug.Log("[SPAAMSolver] Reprojection error: " + error);

        // Construct matrix
        Matrix4x4 result = new Matrix4x4();
        for (int i = 0; i < 16; i++)
        {
            result[i] = resultMatrix[i];
        }

        result = result.transpose;
        Debug.Log("[SPAAMSolver] Result Matrix is: " + result);

        return result;
    }

    public virtual void update()
    {
        // nothing
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            sixDoFPattern = SixDofCalibrationApproach.None;
            ResetPattern();
        }

        // calibration 1
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            sixDoFPattern = SixDofCalibrationApproach.CubesHead;
            ResetPattern();
        }

        // calibration 2
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            sixDoFPattern = SixDofCalibrationApproach.CubesHandHead;
            ResetPattern();
        }

        // calibration 3
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            sixDoFPattern = SixDofCalibrationApproach.CubesHologram;
            ResetPattern();
        }

        // calibrates if the user presses enter
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Solve();
        }

        //
        // Saves results if the user presses Space
        //
        if (Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            // TODO: Store expResult
            string time = System.DateTime.UtcNow.ToString("HH_mm_ss_dd_MMMM_yyyy");
            ExperimentResult.SaveToDrive(expResult, "result_" + sixDoFPattern.ToString() + "_" + time + ".json");

            audioPlayerForVoiceOver.clip = voiceOverResultsSaved;
            audioPlayerForVoiceOver.Play();
        }



        // Skips a point if needed
        if (Input.GetKeyDown(KeyCode.N))
        {
            manager.NextPoint();

            if (sixDoFPattern == SixDofCalibrationApproach.CubesHologram)
            {
                ++currentCubePosition;
                targetObjectLerper.StartLerping(CubePositions[currentCubePosition % CubePositions.Length].position, CubePositions[currentCubePosition % CubePositions.Length].rotation);
                InvisibleCubeLerper.StartLerping(Camera.main.transform.TransformPoint(InvisibleCubeInitialPosition), Quaternion.identity);
            }
        }


        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (sixDoFPattern == SixDofCalibrationApproach.CubesHologram || sixDoFPattern == SixDofCalibrationApproach.CubesHead)
            {
                Tuple<Vector3,Vector3,Vector3,Vector3> targetPositions = manager.PerformAlignmentSpecial();
                Vector3 objectPosition1 = targetCoordinateSystem.InverseTransformPoint(sphere4.position); // this is the main sphere
                Vector3 objectPosition2 = targetCoordinateSystem.InverseTransformPoint(sphere1.position); // Gets the local position of the tracker (as we can only know the object's location with respect to its own coordinate system)
                Vector3 objectPosition3 = targetCoordinateSystem.InverseTransformPoint(sphere2.position); // Gets the local position of the tracker (as we can only know the object's location with respect to its own coordinate system)
                Vector3 objectPosition4 = targetCoordinateSystem.InverseTransformPoint(sphere3.position); // Gets the local position of the tracker (as we can only know the object's location with respect to its own coordinate system)

                PerformAlignment(objectPosition1, targetPositions.Item1);
                PerformAlignment(objectPosition2, targetPositions.Item2);
                PerformAlignment(objectPosition3, targetPositions.Item3);
                PerformAlignment(objectPosition4, targetPositions.Item4);

                ++currentCubePosition;
                targetObjectLerper.StartLerping(CubePositions[currentCubePosition % CubePositions.Length].position, CubePositions[currentCubePosition % CubePositions.Length].rotation);
                InvisibleCubeLerper.StartLerping(Camera.main.transform.TransformPoint(InvisibleCubeInitialPosition), Quaternion.identity);
            } else
            {
                Vector3 targetPosition = manager.PerformAlignment(); // This also updates the target position on their side
                Vector3 objectPosition = targetCoordinateSystem.InverseTransformPoint(sphere4.position);// targetObject.localPosition; // Gets the local position of the tracker (as we can only know the object's location with respect to its own coordinate system)


                PerformAlignment(objectPosition, targetPosition);
            }

        }

    }

    void Update()
    {
        if (!manager)
        {
            manager = SPAAMTargetManager.Instance;
            manager.SetSolver(this);
        }


        targetObjectStartPosition = targetObject.localPosition;
        targetObjectStartRotation = targetObject.localRotation;

        update();
    }
}
