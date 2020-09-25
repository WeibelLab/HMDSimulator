using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class ExperimentResult
{
    //[SerializeField] public float completionTime;
    [SerializeField] public Matrix4x4 projectionMatrixLeft;
    [SerializeField] public Matrix4x4 projectionMatrixRight;
    [SerializeField] public Matrix4x4 groundTruthProjectionMatrixLeft;
    [SerializeField] public Matrix4x4 groundTruthProjectionMatrixRight;
    [SerializeField] public Vector3 errorLeft;
    [SerializeField] public Vector3 errorRight;

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
        File.WriteAllText(path, jsonString);
    }
}