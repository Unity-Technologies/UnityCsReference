// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEngine.Analytics
{
    [Flags]
    public enum SendEventOptions
    {
        kAppendNone = 0,
        kAppendBuildGuid = 1 << 0,
        kAppendBuildTarget = 1 << 1
    }

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    public class AnalyticsEventBase
    {
        string eventName;

        int eventVersion;

        string eventPrefix;

       SendEventOptions sendEventOptions;
        
        public string EventName() { return eventName; }
        public int EventVersion() { return eventVersion; }
        
        public string EventPrefix() { return eventPrefix; }

        public AnalyticsEventBase(string eventName, int eventVersion, SendEventOptions sendEventOptions = SendEventOptions.kAppendNone, string eventPrefix = "") {this.eventName = eventName; this.eventVersion = eventVersion; this.sendEventOptions = sendEventOptions; this.eventPrefix = eventPrefix; }

        public AnalyticsEventBase(AnalyticsEventBase e): this(e.eventName, e.eventVersion) {}

        public AnalyticsEventBase(){}
    }

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    class BatchRenderGroupUsageAnalytic : UnityEngine.Analytics.AnalyticsEventBase
    {
        public BatchRenderGroupUsageAnalytic() : base("brgUsageEvent", 1) { }

        [UsedByNativeCode]
        public static BatchRenderGroupUsageAnalytic CreateBatchRenderGroupUsageAnalytic() { return new BatchRenderGroupUsageAnalytic(); }

        public int maxBRGInstance;
        public int maxMeshCount;
        public int maxMaterialCount;
        public int maxDrawCommandBatch;
    }

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    class UaaLApplicationLaunchAnalytic : UnityEngine.Analytics.AnalyticsEventBase
    {
        public UaaLApplicationLaunchAnalytic() : base("UaaLApplicationLaunch", 1) { }

        [UsedByNativeCode]
        public static UaaLApplicationLaunchAnalytic CreateUaaLApplicationLaunchAnalytic() { return new UaaLApplicationLaunchAnalytic(); }

        public int launch_type;
        public int launch_process_type;
    }
}
