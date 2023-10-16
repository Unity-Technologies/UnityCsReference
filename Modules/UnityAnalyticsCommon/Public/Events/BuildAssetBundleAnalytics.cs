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
    public class BuildAssetBundleAnalytic : UnityEngine.Analytics.AnalyticsEventBase
    {
        public BuildAssetBundleAnalytic() : base("unity5BuildAssetBundles", 1) { }

        [RequiredByNativeCode]
        internal static BuildAssetBundleAnalytic CreateBuildAssetBundleAnalytic() { return new BuildAssetBundleAnalytic(); }

        public bool success;
        public string error;
    }
}
