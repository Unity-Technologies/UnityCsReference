// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    struct PlatformInfo
    {
        public string OperatingSystem;
        public BuildTarget buildTarget;
    }
    static class PlatformUtils
    {

        public static PlatformInfo m_PlatformInfo = GetPlatformNameForCurrentOperatingSystem();

        private static PlatformInfo GetPlatformNameForCurrentOperatingSystem()
        {
            PlatformInfo platformInfo = new PlatformInfo();
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    platformInfo.buildTarget = BuildTarget.StandaloneWindows64;
                    platformInfo.OperatingSystem = "Windows";
                    break;
                case RuntimePlatform.OSXEditor:
                    platformInfo.buildTarget = BuildTarget.StandaloneOSX;
                    platformInfo.OperatingSystem = "OSX";
                    break;
                case RuntimePlatform.LinuxEditor:
                    platformInfo.buildTarget = BuildTarget.StandaloneLinux64;
                    platformInfo.OperatingSystem = "Linux";
                    break;
                default:
                    Debug.LogAssertion("Could not determine the current platform, defaulting to Windows");
                    platformInfo.buildTarget = BuildTarget.StandaloneWindows64;
                    platformInfo.OperatingSystem = "Windows";
                    break;
            }
            return platformInfo;
        }
    }
}
