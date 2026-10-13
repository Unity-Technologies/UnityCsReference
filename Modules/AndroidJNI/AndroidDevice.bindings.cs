// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.Android
{
    ///<summary>AndroidHardwareType describes the type of Android device on which the app is running.</summary>
    public enum AndroidHardwareType
    {
        ///<summary>The Generic category includes all other Android devices.</summary>
        Generic,
        ///<exclude />
        [Obsolete("ChromeOS is no longer supported.")]
        ChromeOS
    }

    ///<summary>Interface into Android specific functionality.</summary>
    public class AndroidDevice
    {
        ///<summary>When running on a ChromeOS device, hardwareType is set to AndroidHardwareType.ChromeOS. It is set to AndroidHardwareType.Generic in all other cases.</summary>
        static public AndroidHardwareType hardwareType => AndroidHardwareType.Generic;
        ///<summary>Set sustained performance mode. When enabled, sustained performance mode is intended to provide a consistent level of performance for a prolonged amount of time.</summary>
        ///<param name="enabled">Set to true to enable sustained performance mode. Set to false to disable it.</param>
        ///<remarks>Internally, this method calls the &lt;a href="https://developer.android.com/reference/android/view/Window#setSustainedPerformanceMode(boolean)"&gt;Android sustained performance API&lt;/a&gt;. It is not reset when the window loses focus.</remarks>
        static public void SetSustainedPerformanceMode(bool enabled) {}
    }
}
