// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.Analytics
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityAnalyticsCommon/Public/UnityAnalyticsCommon.h")]
    public static partial class AnalyticsCommon
    {

        [StaticAccessor("GetUnityAnalyticsCommon()", StaticAccessorType.Dot)]
        private extern static bool ugsAnalyticsEnabledInternal
        {
            [NativeMethod("UGSAnalyticsUserOptStatus")]
            get;
            [NativeMethod("SetUGSAnalyticsUserOptStatus")]
            set;
        }
    }
}
