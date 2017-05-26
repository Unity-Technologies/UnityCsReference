// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections;

namespace UnityEngine
{


public sealed partial class iPhoneSettings
{
    [System.Obsolete ("verticalOrientation property is deprecated. Please use Screen.orientation == ScreenOrientation.Portrait instead.")]
    public static bool verticalOrientation { get { return false; } }
    [System.Obsolete ("screenCanDarken property is deprecated. Please use (Screen.sleepTimeout != SleepTimeout.NeverSleep) instead.")]
    public static bool screenCanDarken { get { return false; } }
    [System.Obsolete ("locationServiceStatus property is deprecated. Please use Input.location.status instead.")]
    public static LocationServiceStatus locationServiceStatus { get { return Input.location.status; } }
    [System.Obsolete ("locationServiceEnabledByUser property is deprecated. Please use Input.location.isEnabledByUser instead.")]
    public static bool locationServiceEnabledByUser { get { return Input.location.isEnabledByUser; } }
    
    
    [System.Obsolete ("StartLocationServiceUpdates method is deprecated. Please use Input.location.Start instead.")]
public static void StartLocationServiceUpdates(float desiredAccuracyInMeters, float updateDistanceInMeters)
        {
            Input.location.Start(desiredAccuracyInMeters, updateDistanceInMeters);
        }
    
    
    [System.Obsolete ("StartLocationServiceUpdates method is deprecated. Please use Input.location.Start instead.")]
public static void StartLocationServiceUpdates(float desiredAccuracyInMeters)
        {
            Input.location.Start(desiredAccuracyInMeters);
        }
    
    
    [System.Obsolete ("StartLocationServiceUpdates method is deprecated. Please use Input.location.Start instead.")]
public static void StartLocationServiceUpdates()
        {
            Input.location.Start();
        }
    
    
    [System.Obsolete ("StopLocationServiceUpdates method is deprecated. Please use Input.location.Stop instead.")]
public static void StopLocationServiceUpdates()
        {
            Input.location.Stop();
        }
    
    
}


} 
