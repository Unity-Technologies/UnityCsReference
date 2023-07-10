// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;


namespace UnityEngine.Analytics
{

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    public class VRDeviceAnalyticBase : UnityEngine.Analytics.AnalyticsEventBase
    {
        public VRDeviceAnalyticBase() : base("deviceStatus", 1) { }
    }

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    public class VRDeviceAnalyticAspect : VRDeviceAnalyticBase
    {
        [UsedByNativeCode]
        public static VRDeviceAnalyticAspect CreateVRDeviceAnalyticAspect() { return new VRDeviceAnalyticAspect(); }

        public float vr_aspect_ratio;
    }

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    public class VRDeviceMirrorAnalytic : VRDeviceAnalyticBase
    {
        [UsedByNativeCode]
        public static VRDeviceMirrorAnalytic CreateVRDeviceMirrorAnalytic() { return new VRDeviceMirrorAnalytic(); }

        public bool vr_device_mirror_mode;
    }

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    public class VRDeviceUserAnalytic : VRDeviceAnalyticBase
    {
        [UsedByNativeCode]
        public static VRDeviceUserAnalytic CreateVRDeviceUserAnalytic() { return new VRDeviceUserAnalytic(); }

        public int vr_user_presence;
    }

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    public class VRDeviceActiveControllersAnalytic : VRDeviceAnalyticBase
    {
        [UsedByNativeCode]
        public static VRDeviceActiveControllersAnalytic CreateVRDeviceActiveControllersAnalytic() { return new VRDeviceActiveControllersAnalytic(); }

        public string[] vr_active_controllers;
    }
}
