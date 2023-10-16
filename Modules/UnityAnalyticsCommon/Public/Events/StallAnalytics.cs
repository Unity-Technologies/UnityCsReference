// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;


namespace UnityEditor.Analytics
{
    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    public class StallSummaryAnalytic : UnityEngine.Analytics.AnalyticsEventBase
    {
        public StallSummaryAnalytic() : base("editorStallSummary", 1) { }

        [RequiredByNativeCode]
        internal static StallSummaryAnalytic CreateStallSummaryAnalytic() { return new StallSummaryAnalytic(); }

        public double Duration;
    }

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    class StallMarkerAnalytic : UnityEngine.Analytics.AnalyticsEventBase
    {
        public StallMarkerAnalytic() : base("editorStallMarker", 1) { }

        [RequiredByNativeCode]
        internal static StallMarkerAnalytic CreateStallMarkerAnalytic() { return new StallMarkerAnalytic(); }

        public string Name;
        public bool HasProgressMarkup;
        public double Duration;
    }
}
