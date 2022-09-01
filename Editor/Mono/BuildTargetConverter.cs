// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using JetBrains.Annotations;
using UnityEngine;

namespace UnityEditor
{
    internal static class BuildTargetConverter
    {
        [UsedImplicitly] // used by com.unity.test-framework package
        public static RuntimePlatform? TryConvertToRuntimePlatform(BuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case BuildTarget.Android:
                    return RuntimePlatform.Android;
                case BuildTarget.PS4:
                    return RuntimePlatform.PS4;
                case BuildTarget.PS5:
                    return RuntimePlatform.PS5;
                case BuildTarget.StandaloneLinux64:
                    return RuntimePlatform.LinuxPlayer;
                case BuildTarget.LinuxHeadlessSimulation:
                    return RuntimePlatform.LinuxPlayer;
                case BuildTarget.StandaloneOSX:
                    return RuntimePlatform.OSXPlayer;
                case BuildTarget.StandaloneWindows:
                    return RuntimePlatform.WindowsPlayer;
                case BuildTarget.StandaloneWindows64:
                    return RuntimePlatform.WindowsPlayer;
                case BuildTarget.Switch:
                    return RuntimePlatform.Switch;
                case BuildTarget.WSAPlayer:
                    return RuntimePlatform.WSAPlayerARM;
                case BuildTarget.XboxOne:
                    return RuntimePlatform.XboxOne;
                case BuildTarget.iOS:
                    return RuntimePlatform.IPhonePlayer;
                case BuildTarget.tvOS:
                    return RuntimePlatform.tvOS;
                case BuildTarget.WebGL:
                    return RuntimePlatform.WebGLPlayer;
                case BuildTarget.Lumin:
                    return RuntimePlatform.Lumin;
                case BuildTarget.GameCoreXboxSeries:
                    return RuntimePlatform.GameCoreXboxSeries;
                case BuildTarget.GameCoreXboxOne:
                    return RuntimePlatform.GameCoreXboxOne;
                case BuildTarget.Stadia:
                    return RuntimePlatform.Stadia;
                case BuildTarget.EmbeddedLinux:
                    return RuntimePlatform.EmbeddedLinuxArm64;
                default:
                    return null;
            }
        }
    }
}
