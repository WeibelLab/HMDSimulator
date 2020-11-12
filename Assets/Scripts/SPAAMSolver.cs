﻿using System;
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
    protected Vector3 targetObjectStartPosition;
    protected Quaternion targetObjectStartRotation;

    [Tooltip("Have we solved it?")]
    public bool solved = false;

    public enum SixDofCalibrationApproach
    {
        None,
        CubesHead,          // calibrate cubes only moving head
        CubesHandHead,      // calibrate cubes using hand and head
        Cubes4Points,       // calibrate cubes using hand and head but with less points
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

    [Header("Calibration data structures")]
    public ExperimentResult expResult;
    public Matrix4x4 groundTruthEquation;
    public Matrix4x4 manualEquation;
    public List<MatchingPoints> groundTruthAlignments = new List<MatchingPoints>();
    public List<MatchingPoints> manualAlignments = new List<MatchingPoints>();
    protected bool firstPoint = true;




    void Start()
    {
        // reset points collected to zero
        groundTruthAlignments = new List<MatchingPoints>();
        manualAlignments = new List<MatchingPoints>();
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

        MatchingPoints groundTruthAlignment = new MatchingPoints
        {
            objectPosition = TrackerBase.InverseTransformPoint(targetPosition),
            targetPosition = targetPosition
        };

        manualAlignments.Add(manualAlignment);
        groundTruthAlignments.Add(groundTruthAlignment);

        // save last data point for the time to task measurement
        expResult.endTime = System.DateTime.Now;
        expResult.completionTime = (expResult.endTime - expResult.startTime).TotalSeconds;

        // play sound
        if (playAudio)
        {
            audioPlayer.clip = pointCollectedSound;
            audioPlayer.Play();
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

    public virtual void Solve()
    {

        if (manualAlignments.Count < 9)
        {
            audioPlayerForVoiceOver.clip = voiceOverCollectMorePoints;
            audioPlayerForVoiceOver.Play();
            return;
        }

        // TODO: Send info to opencv and solve the linear equation
        //Matrix4x4 groundTruth = SolveAlignment(groundTruthAlignments, false);
        groundTruthEquation = SolveAlignment(groundTruthAlignments, true);
        manualEquation = SolveAlignment(manualAlignments, true);
        Debug.Log("[SPAAMSolver] LocalToWorld:" + TrackerBase.localToWorldMatrix);
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
            sixDoFPattern = SixDofCalibrationApproach.Cubes4Points;
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

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Vector3 targetPosition = manager.PerformAlignment();
            Vector3 objectPosition = targetObject.localPosition; //TODO
            PerformAlignment(objectPosition, targetPosition);
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
