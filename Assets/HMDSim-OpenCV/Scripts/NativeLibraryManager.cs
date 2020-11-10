using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;


/// <summary>
/// NativeLibraryManager is a helper library that loads & unloads DLLs dynamically in the editor
/// 
/// This library is very useful in situations when you are constantly changing a library, and you
/// don't want to restart Unity for every single change.
/// </summary>
public class NativeLibraryManager
{


#if UNITY_EDITOR_WIN

	[DllImport("kernel32", EntryPoint = "LoadLibrary", SetLastError = true)]
	public static extern IntPtr LoadLibrary(
		string path);

	[DllImport("kernel32.dll", SetLastError = true)]
	static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, int dwFlags);

	[DllImport("kernel32.dll", SetLastError = true)]
	static extern bool SetDllDirectory(string lpPathName);

	[DllImport("kernel32")]
	public static extern IntPtr GetProcAddress(
		IntPtr libraryHandle,
		string symbolName);

	[DllImport("kernel32")]
	public static extern bool FreeLibrary(
		IntPtr libraryHandle);

    [DllImport("kernel32")]
    public static extern int GetLastError();

    [DllImport("kernel32")]
    public static extern IntPtr GetModuleHandle(string path);

	public static IntPtr OpenLibrary(string path)
	{
        Debug.Log("[NativeLibraryManager] Loading: " + path);
		int error = 0;

		if (!SetDllDirectory(Path.GetFullPath(Path.GetDirectoryName(path))))
        {
			error = Marshal.GetLastWin32Error();
			Debug.LogError(String.Format("[NativeLibraryManager] Unable to add path {0} to the library search path: {1}", Path.GetFullPath(Path.GetDirectoryName(path)), error));
		}

		IntPtr handle = LoadLibrary(path);
		error = Marshal.GetLastWin32Error();
		if (handle == IntPtr.Zero)
		{
			
			Debug.LogError(String.Format("[NativeLibraryManager] Could not load library {0} : {1}",path, error));
			throw new Exception("Couldn't open native library: " + path);
		}
		return handle;
	}

	public static bool CloseLibrary(IntPtr libraryHandle)
	{
        Debug.Log("[NativeLibraryManager] Unloading... ");
		bool flag = FreeLibrary(libraryHandle);
		if (!flag)
			Debug.LogError(String.Format("[NativeLibraryManager] Could not unload library: {0}",Marshal.GetLastWin32Error()));
        return flag;
    }

	public static T GetDelegate<T>(
		IntPtr libraryHandle,
		string functionName) where T : class
	{
		IntPtr symbol = GetProcAddress(libraryHandle, functionName);
		if (symbol == IntPtr.Zero)
		{
			throw new Exception("Couldn't get function: " + functionName);
		}
		return Marshal.GetDelegateForFunctionPointer(
			symbol,
			typeof(T)) as T;
	}

#endif



}
