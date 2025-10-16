// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine;

namespace UnityEditor
{
    [StructLayout(LayoutKind.Sequential)]
    // UCBP-Backport - temporary internal
    internal struct BuildContentDirectoryParameters
    {
        public string outputPath { get; set; }
        public string[] rootAssetPaths { get; set; }
        public BuildContentOptions options { get; set; }
        public BuildCompression compression { get; set; }
        public BuildTarget targetPlatform { get; set; }
        public int subtarget { get; set; }
        public string[] extraScriptingDefines { get; set; }
    }
}

