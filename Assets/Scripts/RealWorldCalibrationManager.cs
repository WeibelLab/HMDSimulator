using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RealWorldCalibrationManager : MonoBehaviour
{
    
    [Header("Calibration target")]

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
        CubesHologram,      // calibrate cubes by moving a hologram
        Pattern_count
    }

    [Header("6DoF Calibration specifics")]
    [Tooltip("The tracker's coordinate system center")]
    public Transform TrackerBase;

    public SixDofCalibrationApproach sixDoFPattern;

    [Header("Visual feedback")]
    public Transform[] ModalityInstructions;

    protected AugmentedRealityCalibrationManager ARHeadset;
    protected GrabbableCube cubeGrabbing;

    [Header("Survey")]
    [Tooltip("(ReadOnly) Set to true when the user is being presented with a survey")]
    public bool inSurveyMode = false;
    public Transform vrSurvey;
    public Transform leftHandPointer;
    public Transform rightHandPointer;

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


    public virtual void PerformAlignment(Vector3 Real_relativeObjectPosition, Vector3 AR_worldTargetPosition)
    {
       
    }

    public virtual void ResetPattern(AugmentedRealityCalibrationManager.CalibrationModality modality)
    {
        // communicates with the AR side to change what the user sees
        ARHeadset.ConditionChange(modality);

        // resets the location of the cube
        targetObject.localPosition = targetObjectStartPosition;
        targetObject.localRotation = targetObjectStartRotation;

        // applies changes the the local environment
        updateModalityInstructions((int)sixDoFPattern);

        // updates the simulation so that we cannot grab the cube in the second one
        switch (modality)
        {
            // none case is only for instructing the user, so shows target and allow the user to do what they want
            case AugmentedRealityCalibrationManager.CalibrationModality.None:
                cubeGrabbing.CanGrab = true;
                cubeGrabbing.GoesBackToInitialPosition = true;
                cubeGrabbing.SendBackToInitialPose();
                break;

            case AugmentedRealityCalibrationManager.CalibrationModality.Point:
            case AugmentedRealityCalibrationManager.CalibrationModality.Points:
                cubeGrabbing.CanGrab = true;
                cubeGrabbing.GoesBackToInitialPosition = true;
                cubeGrabbing.SendBackToInitialPose();
                break;


            case AugmentedRealityCalibrationManager.CalibrationModality.Hologram:
                cubeGrabbing.CanGrab = false;
                cubeGrabbing.GoesBackToInitialPosition = false;
                currentCubePosition = 0;
                targetObjectLerper.StartLerping(CubePositions[currentCubePosition % CubePositions.Length].position, CubePositions[currentCubePosition % CubePositions.Length].rotation);
                InvisibleCubeLerper.StartLerping(Camera.main.transform.TransformPoint(InvisibleCubeInitialPosition), Quaternion.identity);
                break;
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


    public virtual void Solve()
    {

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
        Debug.Log("[RealWorldCalibrationManager] Reprojection error: " + error);

        // Construct matrix
        Matrix4x4 result = new Matrix4x4();
        for (int i = 0; i < 16; i++)
        {
            result[i] = resultMatrix[i];
        }

        result = result.transpose;
        Debug.Log("[RealWorldCalibrationManager] Result Matrix is: " + result);

        return result;
    }

    public virtual void update()
    {
        // nothing
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            ResetPattern(AugmentedRealityCalibrationManager.CalibrationModality.None);
        }

        // calibration 1
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ResetPattern(AugmentedRealityCalibrationManager.CalibrationModality.Point);
        }

        // calibration 2
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ResetPattern(AugmentedRealityCalibrationManager.CalibrationModality.Points);
        }

        // calibration 3
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ResetPattern(AugmentedRealityCalibrationManager.CalibrationModality.Hologram);
        }

        // calibrates if the user presses enter
        if (Input.GetKeyDown(KeyCode.Return))
        {
            ARHeadset.Calibrate();
        }

        //
        // Saves results if the user presses Enter
        //
        if (Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ARHeadset.calibrationExperiment.SaveExperiment();
        }


        // Skips a point if needed
        if (Input.GetKeyDown(KeyCode.N))
        {
            ARHeadset.NextPoint();

            if (sixDoFPattern == SixDofCalibrationApproach.CubesHologram)
            {
                ++currentCubePosition;
                targetObjectLerper.StartLerping(CubePositions[currentCubePosition % CubePositions.Length].position, CubePositions[currentCubePosition % CubePositions.Length].rotation);
                InvisibleCubeLerper.StartLerping(Camera.main.transform.TransformPoint(InvisibleCubeInitialPosition), Quaternion.identity);
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            PerformAlignment();
        }

    }

    /// <summary>
    /// Sends a message to the AR Headset letting it know that it is time to calibrate
    /// </summary>
    public void PerformAlignment()
    {
        ARHeadset.SaveCurrentAlignment();

        // after performing an alignment, checks if it was calibrated
        if (ARHeadset.calibrated)
        {
            // allow the user to move the object
            cubeGrabbing.CanGrab = true;
            cubeGrabbing.GoesBackToInitialPosition = false;
        }

        // display survey!
    }

    void Update()
    {
        if (!ARHeadset)
        {
            ARHeadset = AugmentedRealityCalibrationManager.Instance;
            ARHeadset.SetSolver(this);
        }

        targetObjectStartPosition = targetObject.localPosition;
        targetObjectStartRotation = targetObject.localRotation;

        update();
    }
}
