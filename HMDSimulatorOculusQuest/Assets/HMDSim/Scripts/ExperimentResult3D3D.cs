using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// ExperimentResult3D3D is a helper class used to serialize a transformation matrix
/// after a calibration experiment.
/// </summary>
[Serializable]
public class ExperimentResult3D3D
{

    [SerializeField] public double completionTime = 0.0f; // completion time in seconds
    [SerializeField] public DateTime startTime = new DateTime();
    [SerializeField] public DateTime endTime;

    // custom field that can hold any calibration name (projection, 6DoF, etc..)
    [SerializeField] public string calibrationType = "undefined";

    // Fields used for 3D-3D transformation (AKA 6 DoF docking)
    [SerializeField] public Matrix4x4 sixDoFUserAlignedMatrix;
    [SerializeField] public Matrix4x4 sixDoFSimulatorGroundTruthMatrix;
    [SerializeField] public Matrix4x4 sixDoFGroundTruthAlignedTransformationMatrix;

    [SerializeField] public Vector3 sixDofTranslationError;
    [SerializeField] public Vector3 sixDofRotationError;

    /// <summary>
    /// The input position is the physical object position
    /// </summary>
    [SerializeField] public List<Vector3> i_sixDofInputPosition = new List<Vector3>();

    /// <summary>
    /// The output position is where the object is displayed in AR
    /// o_sixDofUserTargetPositionAR holds the user-defined position
    /// </summary>
    [SerializeField] public List<Vector3> o_sixDofUserTargetPositionAR = new List<Vector3>();

    /// <summary>
    /// The output position is where the object is displayed in AR
    /// o_sixDofGroudTruthInputPosition holds the location where the object should have been at
    /// </summary>
    [SerializeField] public List<Vector3> o_sixDofGroudTruthInputPosition = new List<Vector3>();

    /// <summary>
    /// Error Vector (o_sixDofUserTargetPositionAR - o_sixDofGroudTruthInputPosition)
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

    public static ExperimentResult3D3D CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<ExperimentResult3D3D>(jsonString);
    }

    public static string ConvertToJSON(ExperimentResult3D3D input)
    {
        return JsonUtility.ToJson(input, true);
    }

    public static void SaveToDrive(ExperimentResult3D3D input, string path)
    {
        string jsonString = ConvertToJSON(input);
        Debug.Log("============= (3D-3D) saving results (begin) ===================");
        Debug.Log(path);
        Debug.Log(jsonString);
        Debug.Log("============= (3D-3D) saving results (end) ===================");
        File.WriteAllText(path, jsonString);
    }

}