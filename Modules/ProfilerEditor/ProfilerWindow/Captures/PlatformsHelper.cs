// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

namespace Unity.Profiling.Editor.UI
{
    internal static class PlatformsHelper
    {
        static BuildTarget GetBuildTarget(this RuntimePlatform runtimePlatform)
        {
            var buildTarget = BuildTarget.NoTarget;
            switch (runtimePlatform)
            {
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXServer:
                    buildTarget = BuildTarget.StandaloneOSX;
                    break;
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsServer:
                    buildTarget = BuildTarget.StandaloneWindows;
                    break;
                case RuntimePlatform.IPhonePlayer:
                    buildTarget = BuildTarget.iOS;
                    break;
                case RuntimePlatform.tvOS:
                    buildTarget = BuildTarget.tvOS;
                    break;
                case RuntimePlatform.Android:
                    buildTarget = BuildTarget.Android;
                    break;
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxEditor:
                case RuntimePlatform.LinuxServer:
                    buildTarget = BuildTarget.StandaloneLinux64;
                    break;
                case RuntimePlatform.EmbeddedLinuxArm64:
                case RuntimePlatform.EmbeddedLinuxX64:
                    buildTarget = BuildTarget.EmbeddedLinux;
                    break;
                case RuntimePlatform.WebGLPlayer:
                    buildTarget = BuildTarget.WebGL;
                    break;
                case RuntimePlatform.WSAPlayerX86:
                case RuntimePlatform.WSAPlayerX64:
                case RuntimePlatform.WSAPlayerARM:
                    buildTarget = BuildTarget.WSAPlayer;
                    break;
                case RuntimePlatform.PS4:
                    buildTarget = BuildTarget.PS4;
                    break;
                case RuntimePlatform.PS5:
                    buildTarget = BuildTarget.PS5;
                    break;
                case RuntimePlatform.XboxOne:
                    buildTarget = BuildTarget.XboxOne;
                    break;
                case RuntimePlatform.GameCoreXboxSeries:
                case RuntimePlatform.GameCoreXboxOne:
                    buildTarget = BuildTarget.GameCoreXboxOne;
                    break;
                case RuntimePlatform.Switch:
                    buildTarget = BuildTarget.Switch;
                    break;
                case RuntimePlatform.Switch2:
                    buildTarget = BuildTarget.Switch2;
                    break;
                case RuntimePlatform.QNXArm64:
                case RuntimePlatform.QNXX64:
                    buildTarget = BuildTarget.QNX;
                    break;
                case RuntimePlatform.LinuxHeadlessSimulation:
                    buildTarget = BuildTarget.LinuxHeadlessSimulation;
                    break;
                case RuntimePlatform.VisionOS:
                    buildTarget = BuildTarget.VisionOS;
                    break;
                default:
                    // Unknown target
                    break;
            }
            return buildTarget;
        }

        internal static Texture GetPlatformIcon(RuntimePlatform platform)
        {
            Texture icon = null;

            // Try to use builtin Editor icon.
            var builtinIconName = GetPlatformIconName(platform);
            if (builtinIconName != null)
                icon = IconUtility.LoadBuiltInIconWithName(builtinIconName);

            // Fallback to NoIcon.
            if (icon == null)
                icon = IconUtility.NoIcon;

            return icon;
        }

        static string GetPlatformIconName(RuntimePlatform platform)
        {
            string name;
            switch (platform)
            {
                case RuntimePlatform.LinuxServer:
                case RuntimePlatform.OSXServer:
                case RuntimePlatform.WindowsServer:
                    name = "DedicatedServer";
                    break;
                case RuntimePlatform.LinuxHeadlessSimulation:
                    name = "LinuxHeadlessSimulation";
                    break;
                default:
                {
                    var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(platform.GetBuildTarget());
                    if (buildTargetGroup == BuildTargetGroup.Unknown)
                        return null;

                    switch (buildTargetGroup)
                    {
                        case BuildTargetGroup.WSA:
                            name = "Metro";
                            break;
                        default:
                            name = buildTargetGroup.ToString();
                            break;
                    }
                }
                break;
            }

            return "BuildSettings." + name + " On.png";
        }
    }
}
