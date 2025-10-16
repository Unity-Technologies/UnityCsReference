// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine;
using System;

namespace UnityEditor
{
    // NB! Keep in sync with BuildPlayerOptionsManagedStruct in Editor/Src/BuildPipeline/BuildPlayerOptions.h
    [StructLayout(LayoutKind.Sequential)]
    public struct BuildPlayerOptions
    {
        public string[] scenes { get; set; }
        public string locationPathName { get; set; }
        public string assetBundleManifestPath { get; set; }
        public BuildTargetGroup targetGroup { get; set; }
        public BuildTarget target { get; set; }
        public int subtarget { get; set; }
        public BuildOptions options { get; set; }
        public string[] extraScriptingDefines { get; set; }
    }

}
