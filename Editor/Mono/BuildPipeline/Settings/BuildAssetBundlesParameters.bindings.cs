// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;

namespace UnityEditor
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BuildAssetBundlesParameters
    {
        public string outputPath { get; set; }
        public AssetBundleBuild[] bundleDefinitions { get; set; }
        public BuildAssetBundleOptions options { get; set; }
        public BuildTarget targetPlatform { get; set; }
        public int subtarget { get; set; }
        public string[] extraScriptingDefines { get; set; }
    }
}
