// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Android
{
    public enum AndroidHardwareType
    {
        Generic,
        ChromeOS
    }

    public class AndroidDevice
    {
        static public AndroidHardwareType hardwareType => AndroidHardwareType.Generic;
        static public void SetSustainedPerformanceMode(bool enabled) {}
    }
}
