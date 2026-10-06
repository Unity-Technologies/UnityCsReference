// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using Unity.Collections;
using UnityEngine;

class CLILeakDetectionSwapper
{
    [InitializeOnLoadMethod]
    static void SetLeakDetectionModeFromEnvironment()
    {
        var nativeLeakDetectionMode = Environment.GetEnvironmentVariable("UNITY_JOBS_NATIVE_LEAK_DETECTION_MODE");
        if (!string.IsNullOrEmpty(nativeLeakDetectionMode))
        {
            switch (nativeLeakDetectionMode)
            {
                case "0":
                    NativeLeakDetection.Mode = NativeLeakDetectionMode.Disabled;
                    break;
                case "1":
                    NativeLeakDetection.Mode = NativeLeakDetectionMode.Enabled;
                    break;
                case "2":
                    NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace;
                    break;
                default:
                    Debug.LogWarning("The environment variable UNITY_JOBS_NATIVE_LEAK_DETECTION_MODE has an invalid value. Please use: 0 = Disabled, 1 = Enabled, 2 = EnabledWithStackTrace.");
                    break;
            }
            Debug.Log("Native leak detection mode: " + NativeLeakDetection.Mode);
        }
    }
}
