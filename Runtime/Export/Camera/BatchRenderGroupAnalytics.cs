// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    class BatchRendererGroupRuntimeAnalytic : Analytics.AnalyticsEventBase
    {
        BatchRendererGroupRuntimeAnalytic() : base("brgPlayerUsage", 1) { }

        [RequiredByNativeCode]
        public static BatchRendererGroupRuntimeAnalytic CreateBatchRendererGroupRuntimeAnalytic() { return new BatchRendererGroupRuntimeAnalytic(); }

        int brgRuntimeStatus;
    }
}
