using System;
using System.Collections;
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
     
    [SerializeField] public Matrix4x4 projectionMatrixLeft;
    [SerializeField] public Matrix4x4 projectionMatrixRight;
    [SerializeField] public Matrix4x4 groundTruthProjectionMatrixLeft;
    [SerializeField] public Matrix4x4 groundTruthProjectionMatrixRight;


    [SerializeField] public Vector3 errorLeft;
    [SerializeField] public Vector3 errorRight;
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