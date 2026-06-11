// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor
{
    internal struct BuildPlayerDataOptions
    {
        public string[] scenes { get; set; }
        public BuildTargetGroup targetGroup { get; set; }
        public BuildTarget target { get; set; }
        public int subtarget { get; set; }
        public BuildOptions options { get; set; }
        public string[] extraScriptingDefines { get; set; }
        public string[] previousBuildReportDirectories { get; set; }
    }
}
