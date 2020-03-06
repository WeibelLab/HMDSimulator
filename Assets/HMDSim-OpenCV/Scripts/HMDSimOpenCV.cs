using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class HMDSimOpenCV : MonoBehaviour
{
    public const string NATIVE_LIBRARY_NAME = "HMDSimulator-OpenCV-Integration";

    private static HMDSimOpenCV _instance;
    public static HMDSimOpenCV Instance { get { return _instance; } }

#if UNITY_EDITOR

    // Handle to the C++ DLL
    public IntPtr libraryHandle;
    public const string LIB_PATH = "/Plugins/x64/HMDSimulator-OpenCV-Integration.dll";
#endif

    public enum ARUCO_PREDEFINED_DICTIONARY {
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
    	DICT_APRILTAG_16h5,     ///< 4x4 bits, minimum hamming distance between any two codes = 5, 30 codes
    	DICT_APRILTAG_25h9,     ///< 5x5 bits, minimum hamming distance between any two codes = 9, 35 codes
    	DICT_APRILTAG_36h10,    ///< 6x6 bits, minimum hamming distance between any two codes = 10, 2320 codes
    	DICT_APRILTAG_36h11     ///< 6x6 bits, minimum hamming distance between any two codes = 11, 587 codes
    };

#if UNITY_EDITOR
    public delegate bool _Aruco_DrawMarker_Type(
        int predefinedDict, int markerId, int markerSize, bool border, byte[] rgbOutput);
    public delegate bool _Aruco_DrawCharucoBoard_Type(
        int predefinedDict, int squareWidth, int squareHeight, float squareLength, float markerLength, bool border,
        byte[] rgbOutput);

    public _Aruco_DrawMarker_Type Aruco_DrawMarker;
    public _Aruco_DrawCharucoBoard_Type Aruco_DrawCharucoBoard;

#else
    [DllImport(NATIVE_LIBRARY_NAME, CallingConvention = CallingConvention.StdCall)]
    public static extern bool Aruco_DrawMarker(int predefinedDict, int markerId, int markerSize, bool border, byte[] rgbOutput);
    [DllImport(NATIVE_LIBRARY_NAME, CallingConvention = CallingConvention.StdCall)]
    public static extern bool Aruco_DrawCharucoBoard(int predefinedDict, int squareWidth, int squareHeight, float squareLength, float markerLength, bool border,
        byte[] rgbOutput);
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
        Aruco_DrawMarker = NativeLibraryManager.GetDelegate<_Aruco_DrawMarker_Type>(
            libraryHandle,
            "Aruco_DrawMarker");
        Aruco_DrawCharucoBoard = NativeLibraryManager.GetDelegate<_Aruco_DrawCharucoBoard_Type>(
            libraryHandle,
            "Aruco_DrawCharucoBoard");
#endif

        _instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    void OnApplicationQuit()
    {
#if UNITY_EDITOR
        NativeLibraryManager.CloseLibrary(libraryHandle);
        libraryHandle = IntPtr.Zero;
#endif
    }
}
