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
    public class VCProviderAnalytics : UnityEngine.Analytics.AnalyticsEventBase
    {
        public VCProviderAnalytics() : base("versioncontrol_ProviderSettings_OnUpdate", 1) { }

        [RequiredByNativeCode]
        internal static VCProviderAnalytics CreateVCProviderAnalytics() { return new VCProviderAnalytics(); }

        public string Mode;
    }
}
