using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// ExperimentResult is a helper class used to serialize the projection matrix obtained
/// after a calibration experiment.
/// </summary>
[Serializable]
public class ExperimentResult
{

    [SerializeField] public double completionTime = 0.0f; // completion time in seconds
    [SerializeField] public DateTime startTime = new DateTime();
    [SerializeField] public DateTime endTime;

    // custom field that can hold any calibration name (projection, 6DoF, etc..)
    [SerializeField] public string calibrationType = "undefined";

    // Fields used for the projective transformation (display calibration per eye)
    [SerializeField] public Matrix4x4 projectionMatrixLeft;
    [SerializeField] public Matrix4x4 projectionMatrixRight;
    [SerializeField] public Matrix4x4 projectionGroundTruthMatrixLeft;
    [SerializeField] public Matrix4x4 projectionGroundTruthMatrixRight;
    [SerializeField] public Vector3 projectionErrorLeft;
    [SerializeField] public Vector3 projectionErrorRight;

    // Fields used for 3D-3D transformation (AKA 6 DoF docking)
    [SerializeField] public Matrix4x4  sixDoFAutoAlignedMatrix;
    [SerializeField] public Matrix4x4  sixDoFTransformationMatrix;
    [SerializeField] public Matrix4x4  sixDoFGroundTruthTransformationMatrix;
    [SerializeField] public Vector3    sixDofTranslationError;
    [SerializeField] public Vector3    sixDofRotationError;

    /// <summary>
    /// Where the object should be
    /// </summary>
    [SerializeField] public List<Vector3> sixDofObjectPosition = new List<Vector3>();

    /// <summary>
    /// Where the user aligned it
    /// </summary>
    [SerializeField] public List<Vector3> sixDofAlignedPosition = new List<Vector3>();

    /// <summary>
    /// The location in the AR coordinate system
    /// </summary>
    [SerializeField] public List<Vector3> sixDofObjectPositionAR = new List<Vector3>();

    /// <summary>
    /// Error Vector (sixDofObjectPosition - sixDofAlignedPosition)
    /// </summary>
    [SerializeField] public List<Vector3> sixDofAlignmentsErrorVect = new List<Vector3>();


    /// <summary>
    /// Total points collected
    /// </summary>
    [SerializeField] public int pointsCollected = 0;


    /// <summary>
    /// A number identifying the type of calibration performed
    /// </summary>
    [SerializeField] public int calibrationModality = -1;

    
    /// <summary>
    /// A string identifying the type of calibration performed
    /// </summary>
    [SerializeField] public string calibrationModalityStr = "undefined";

    public static ExperimentResult CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<ExperimentResult>(jsonString);
    }

    public static string ConvertToJSON(ExperimentResult input)
    {
        return JsonUtility.ToJson(input, true);
    }

    public static void SaveToDrive(ExperimentResult input, string path)
    {
        string jsonString = ConvertToJSON(input);
        Debug.Log("============= saving results (begin) ===================");
        Debug.Log(path);
        Debug.Log(jsonString);
        Debug.Log("============= saving results (end) ===================");
        File.WriteAllText(path, jsonString);
    }
}