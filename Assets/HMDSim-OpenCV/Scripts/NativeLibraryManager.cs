using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class NativeLibraryManager
{


#if UNITY_EDITOR_WIN

	[DllImport("kernel32")]
	public static extern IntPtr LoadLibrary(
		string path);

	[DllImport("kernel32")]
	public static extern IntPtr GetProcAddress(
		IntPtr libraryHandle,
		string symbolName);

	[DllImport("kernel32")]
	public static extern bool FreeLibrary(
		IntPtr libraryHandle);

	public static IntPtr OpenLibrary(string path)
	{
		IntPtr handle = LoadLibrary(path);
		if (handle == IntPtr.Zero)
		{
			throw new Exception("Couldn't open native library: " + path);
		}
		return handle;
	}

	public static void CloseLibrary(IntPtr libraryHandle)
	{
		FreeLibrary(libraryHandle);
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
