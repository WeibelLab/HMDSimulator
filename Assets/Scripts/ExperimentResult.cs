using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ExperimentResult
{
    [Serializable]
    public class DataClass
    {
        public string[] strings;
    }

    [SerializeField] public DataClass dataList;

    public static ExperimentResult CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<ExperimentResult>(jsonString);
    }

    public static string ConvertToJSON(ExperimentResult input)
    {
        return JsonUtility.ToJson(input, true);
    }
}