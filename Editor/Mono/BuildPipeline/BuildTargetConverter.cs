// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using JetBrains.Annotations;
using UnityEngine;

namespace UnityEditor
{
    internal static class BuildTargetConverter
    {
        [UsedImplicitly] // used by com.unity.test-framework package
        public static RuntimePlatform TryConvertToRuntimePlatform(BuildTarget buildTarget) =>
            buildTarget switch
            {
                BuildTarget.StandaloneLinux64 or BuildTarget.LinuxHeadlessSimulation =>
                    RuntimePlatform.LinuxPlayer,
                BuildTarget.StandaloneOSX => RuntimePlatform.OSXPlayer,
                BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64 =>
                    RuntimePlatform.WindowsPlayer,
                BuildTarget.WSAPlayer => RuntimePlatform.WSAPlayerARM,
                BuildTarget.iOS => RuntimePlatform.IPhonePlayer,
                BuildTarget.WebGL => RuntimePlatform.WebGLPlayer,
                BuildTarget.EmbeddedLinux => RuntimePlatform.EmbeddedLinuxArm64,
                BuildTarget.QNX => RuntimePlatform.QNXArm64,
                BuildTarget.GameCoreXboxSeries => RuntimePlatform.GameCoreXboxSeries,
                _ => Enum.Parse<RuntimePlatform>(buildTarget.ToString()),
            };
    }
}
