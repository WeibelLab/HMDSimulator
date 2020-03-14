﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class HMDSimOpenCV : MonoBehaviour
{
    public const string NATIVE_LIBRARY_NAME = "HMDSimulator-OpenCV-Integration";

    private static HMDSimOpenCV _instance;

    public static HMDSimOpenCV Instance
    {
        get { return _instance; }
    }

#if UNITY_EDITOR

    // Handle to the C++ DLL
    public IntPtr libraryHandle;
    public const string LIB_PATH = "/Plugins/x64/HMDSimulator-OpenCV-Integration.dll";
#endif

    public enum ARUCO_PREDEFINED_DICTIONARY
    {
        DICT_4X4_50 = 0,
        DICT_4X4_100,
        DICT_4X4_250,
        DICT_4X4_1000,
        DICT_5X5_50,
        DICT_5X5_100,
        DICT_5X5_250,
        DICT_5X5_1000,
        DICT_6X6_50,
        DICT_6X6_100,
        DICT_6X6_250,
        DICT_6X6_1000,
        DICT_7X7_50,
        DICT_7X7_100,
        DICT_7X7_250,
        DICT_7X7_1000,
        DICT_ARUCO_ORIGINAL,
        DICT_APRILTAG_16h5,

        ///< 4x4 bits, minimum hamming distance between any two codes = 5, 30 codes
        DICT_APRILTAG_25h9,

        ///< 5x5 bits, minimum hamming distance between any two codes = 9, 35 codes
        DICT_APRILTAG_36h10,

        ///< 6x6 bits, minimum hamming distance between any two codes = 10, 2320 codes
        DICT_APRILTAG_36h11 ///< 6x6 bits, minimum hamming distance between any two codes = 11, 587 codes
    };

#if UNITY_EDITOR

    public delegate void DebugCallback(string message);

    public delegate void _RegisterDebugCallback_Type(DebugCallback callback);

    public delegate bool _Aruco_DrawMarker_Type(
        int predefinedDict, int markerId, int markerSize, bool border, byte[] rgbOutput);

    public delegate int _Aruco_EstimateMarkersPose_Type(
        byte[] rgbInput, int width, int height, int predefinedDict, float markerLength, float[] cameraMatrix,
        float[] distCoeffs, int distCoeffLength,
        int expectedMarkerCount,
        float[] outputMarkerPosVec3, float[] outputMarkerRotVec3, int[] outputMarkerIds);

    public delegate int _Aruco_CreateDetector_Type(int predefinedDict, int squareWidth, int squareHeight,
        float squareLength, float markerLength, bool border);

    public delegate bool _Aruco_DrawCharucoBoard_Type(int detectorHandle, byte[] rgbOutput);

    public delegate int _Aruco_CollectCharucoCorners_Type(int detectorHandle, byte[] rgbInput, int width, int height, byte[] rgbOutput);

    public delegate double _Aruco_CalibrateCameraCharuco_Type(int detectorHandle);


    public _RegisterDebugCallback_Type RegisterDebugCallback;
    public _Aruco_DrawMarker_Type Aruco_DrawMarker;
    public _Aruco_EstimateMarkersPose_Type Aruco_EstimateMarkersPose;
    public _Aruco_CreateDetector_Type Aruco_CreateDetector;
    public _Aruco_DrawCharucoBoard_Type Aruco_DrawCharucoBoard;
    public _Aruco_CollectCharucoCorners_Type Aruco_CollectCharucoCorners;
    public _Aruco_CalibrateCameraCharuco_Type Aruco_CalibrateCameraCharuco;

#else

    [DllImport(NATIVE_LIBRARY_NAME, CallingConvention = CallingConvention.StdCall)]
    public static extern void RegisterDebugCallback(DebugCallback callback);
    [DllImport(NATIVE_LIBRARY_NAME, CallingConvention = CallingConvention.StdCall)]
    public static extern bool Aruco_DrawMarker(int predefinedDict, int markerId, int markerSize, bool border, byte[] rgbOutput);
    
    [DllImport(NATIVE_LIBRARY_NAME, CallingConvention = CallingConvention.StdCall)]
    public static extern int Aruco_EstimateMarkersPose(byte[] rgbInput, int width, int height, int predefinedDict, float markerLength, float[] cameraMatrix,
        float[] distCoeffs, int distCoeffLength,
        int expectedMarkerCount,
        float[] outputMarkerPosVec3, float[] outputMarkerRotVec3, int[] outputMarkerIds);

    [DllImport(NATIVE_LIBRARY_NAME, CallingConvention = CallingConvention.StdCall)]
    public static extern int Aruco_CreateDetector(int predefinedDict, int squareWidth, int squareHeight,
        float squareLength, float markerLength, bool border);
    
    [DllImport(NATIVE_LIBRARY_NAME, CallingConvention = CallingConvention.StdCall)]
    public static extern bool Aruco_DrawCharucoBoard(int predefinedDict, int squareWidth, int squareHeight, float squareLength, float markerLength, bool border,
        byte[] rgbOutput);
    
    [DllImport(NATIVE_LIBRARY_NAME, CallingConvention = CallingConvention.StdCall)]
    public static extern int Aruco_CollectCharucoCorners(int detectorHandle, byte[] rgbInput, int width, int height, byte[] rgbOutput = null);
    
    [DllImport(NATIVE_LIBRARY_NAME, CallingConvention = CallingConvention.StdCall)]
    public static extern double Aruco_CalibrateCameraCharuco(int detectorHandle);

#endif

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }

#if UNITY_EDITOR

        // Open native library
        libraryHandle = NativeLibraryManager.OpenLibrary(Application.dataPath + LIB_PATH);
        RegisterDebugCallback = NativeLibraryManager.GetDelegate<_RegisterDebugCallback_Type>(
            libraryHandle,
            "RegisterDebugCallback");
        Aruco_DrawMarker = NativeLibraryManager.GetDelegate<_Aruco_DrawMarker_Type>(
            libraryHandle,
            "Aruco_DrawMarker");
        Aruco_EstimateMarkersPose = NativeLibraryManager.GetDelegate<_Aruco_EstimateMarkersPose_Type>(
            libraryHandle,
            "Aruco_EstimateMarkersPose");
        Aruco_CreateDetector = NativeLibraryManager.GetDelegate<_Aruco_CreateDetector_Type>(
            libraryHandle,
            "Aruco_CreateDetector");
        Aruco_DrawCharucoBoard = NativeLibraryManager.GetDelegate<_Aruco_DrawCharucoBoard_Type>(
            libraryHandle,
            "Aruco_DrawCharucoBoard");
        Aruco_CollectCharucoCorners = NativeLibraryManager.GetDelegate<_Aruco_CollectCharucoCorners_Type>(
            libraryHandle,
            "Aruco_CollectCharucoCorners");
        Aruco_CalibrateCameraCharuco = NativeLibraryManager.GetDelegate<_Aruco_CalibrateCameraCharuco_Type>(
            libraryHandle,
            "Aruco_CalibrateCameraCharuco");
        RegisterDebugCallback(new DebugCallback(DebugLog));
#endif

        _instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {
        
    }

    private static void DebugLog(string message)
    {
        Debug.Log("HMDSimOpenCV: " + message);
    }

    void OnApplicationQuit()
    {
#if UNITY_EDITOR
        if (libraryHandle != IntPtr.Zero)
        {
            var StartTime = Time.time;
            while (NativeLibraryManager.GetModuleHandle(Application.dataPath + LIB_PATH) != IntPtr.Zero)
            {
                NativeLibraryManager.CloseLibrary(libraryHandle);
                if (Time.time - StartTime > 5.0f)
                {
                    break;
                }
            }

            libraryHandle = IntPtr.Zero;
        }
#endif
    }
}