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
    public class SubsystemsAnalyticBase : UnityEngine.Analytics.AnalyticsEventBase
    {
        public SubsystemsAnalyticBase(string eventName) : base(eventName, 1) { }

        public string subsystem;
    }

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    public class SubsystemsAnalyticStart :SubsystemsAnalyticBase
    {
        public SubsystemsAnalyticStart() : base("SubsystemStart") { }

        [UsedByNativeCode]
        internal static SubsystemsAnalyticStart CreateSubsystemsAnalyticStart() { return new SubsystemsAnalyticStart(); }

    }

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    public class SubsystemsAnalyticStop : SubsystemsAnalyticBase
    {
        public SubsystemsAnalyticStop() : base("SubsystemStop") { }

        [UsedByNativeCode]
        internal static SubsystemsAnalyticStop CreateSubsystemsAnalyticStop() { return new SubsystemsAnalyticStop(); }

    }

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    public class SubsystemsAnalyticInfo : SubsystemsAnalyticBase
    {
        public SubsystemsAnalyticInfo() : base("SubsystemInfo") { }

        [UsedByNativeCode]
        internal static SubsystemsAnalyticInfo CreateSubsystemsAnalyticInfo() { return new SubsystemsAnalyticInfo(); }

        string id;
        string plugin_name;
        string version;
        string library_name;

    }
}
