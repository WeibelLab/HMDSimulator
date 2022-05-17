using UnityEngine;
using System;

/// <summary>
/// Class that holds matching points in two coordinate systems
/// </summary>
[Serializable]
public class MatchingPoints
{
    [SerializeField] public Vector3 objectPosition;
    [SerializeField] public Vector3 targetPosition;
}