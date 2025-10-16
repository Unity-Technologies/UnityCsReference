// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine;
using UnityEditorInternal;
using System;
using Application = UnityEngine.Application;

namespace Unity.Multiplayer.PlayMode.Editor
{
    internal class InternalUtilities
    {
        public static bool IsDomainReloadRequested() => InternalEditorUtility.IsScriptReloadRequested();

        // This is the only internal API we should access from the BuildProfile module
        private static void GetBuildProfileInternalData(BuildProfile buildProfile, out StandaloneBuildSubtarget buildSubtarget, out BuildTarget buildTarget, out string platformId)
        {
            buildSubtarget = buildProfile.subtarget;
            buildTarget = buildProfile.buildTarget;
            platformId = buildProfile.platformId;
        }

        // Get build target type for both the local and remote instances by using the build profile as input
        public static string GetBuildTargetType(BuildProfile buildProfile)
        {
            if (buildProfile == null)
            {
                return string.Empty;
            }

            GetBuildProfileInternalData(buildProfile, out var subTarget, out var buildTarget, out _);

            if (subTarget == StandaloneBuildSubtarget.Server)
            {
                return GetBuildTargetName(buildTarget);
            }
            return buildTarget.ToString();
        }

        //  Get build target type for both the main and virtual editors by using the active build target as input
        public static string GetBuildTargetType(BuildTarget buildTarget)
        {
            if (EditorUserBuildSettings.standaloneBuildSubtarget == StandaloneBuildSubtarget.Server)
            {
                return GetBuildTargetName(buildTarget);
            }
            return buildTarget.ToString();
        }

        // Get build target name and ensure it aligns with the naming conventions used in the engine: https://github.cds.internal.unity3d.com/unity/unity/blob/50d46788931dea07266284cb6621ce1493aefcea/Tests/UnityEngine.Common/PlatformConverter.cs
        private static string GetBuildTargetName(BuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case BuildTarget.StandaloneLinux64:
                    return "LinuxServer";
                case BuildTarget.StandaloneOSX:
                    return "OSXServer";
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return "WindowsServer";
                default:
                    return buildTarget.ToString();
            }
        }

        public static bool IsStandalonePlatform(BuildTarget buildTarget)
            => BuildPipeline.GetBuildTargetGroup(buildTarget) == BuildTargetGroup.Standalone;

        public static bool IsServerProfile(BuildProfile buildProfile)
        {
            GetBuildProfileInternalData(buildProfile, out var buildSubtarget, out var buildTarget, out _);
            return buildSubtarget == StandaloneBuildSubtarget.Server && IsStandalonePlatform(buildTarget);
        }

        public static bool IsAndroidBuildTarget(BuildProfile buildProfile)
        {
            if (buildProfile == null)
                return false;
            GetBuildProfileInternalData(buildProfile, out _, out var buildTarget, out _);
            return buildTarget == BuildTarget.Android;
        }

        internal static string AddBuildExtension(string path, BuildProfile profile)
        {
            GetBuildProfileInternalData(profile, out var buildSubtarget, out var buildTarget, out _);

            switch (buildTarget)
            {
                case BuildTarget.StandaloneWindows64:
                    return Path.ChangeExtension(path, ".exe");
                case BuildTarget.StandaloneOSX:
                    if (buildSubtarget == StandaloneBuildSubtarget.Player)
                        return Path.ChangeExtension(path, ".app");
                    return Path.ChangeExtension(path, "");
                case BuildTarget.Android:
                    return Path.ChangeExtension(path, ".apk");
            }

            return path;
        }

        internal static Texture2D GetBuildProfileTypeIcon(BuildProfile buildProfile)
        {
            if (buildProfile == null)
            {
                return Texture2D.grayTexture;
            }

            var iconName = GetBuildPlatformIconName(buildProfile);

            var icon = EditorGUIUtility.LoadIcon(iconName + ".Small");

            return icon == null ? EditorGUIUtility.LoadIcon("BuildSettings.Editor.Small") : icon;
        }

        private static string GetBuildPlatformIconName(BuildProfile buildProfile)
        {
            GetBuildProfileInternalData(buildProfile, out var buildSubtarget, out var buildTarget, out _);
            if (buildSubtarget == StandaloneBuildSubtarget.Server)
            {
                return "BuildSettings.DedicatedServer";
            }

            return buildTarget switch
            {
                BuildTarget.StandaloneWindows64 => "BuildSettings.Windows",
                BuildTarget.StandaloneOSX => "BuildSettings.OSX",
                BuildTarget.StandaloneLinux64 => "BuildSettings.Linux",
                BuildTarget.Android => "BuildSettings.Android",
                BuildTarget.iOS => "BuildSettings.iPhone",
                BuildTarget.WebGL => "BuildSettings.WebGL",
                BuildTarget.WSAPlayer => "BuildSettings.Metro",
                BuildTarget.PS4 => "BuildSettings.PS4",
                BuildTarget.PS5 => "BuildSettings.PS5",
                BuildTarget.XboxOne => "BuildSettings.XboxOne",
                BuildTarget.tvOS => "BuildSettings.tvOS",
                BuildTarget.VisionOS => "BuildSettings.VisionOS",
                BuildTarget.Switch => "BuildSettings.Switch",
                _ => "BuildSettings.Editor"
            };

        }
        internal static bool IsBuildProfileSupported(BuildProfile buildProfile)
        {
            GetBuildProfileInternalData(buildProfile, out _, out var buildTarget, out _);
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            var platformGuid = buildProfile.platformGuid;
            return BuildPipeline.IsBuildTargetSupported(buildTargetGroup, buildTarget) && BuildProfileModuleUtil.IsModuleInstalled(platformGuid);
        }

        internal static bool BuildProfileCanRunOnCurrentPlatform(BuildProfile buildProfile)
        {
            GetBuildProfileInternalData(buildProfile, out _, out var buildTarget, out var platformId);

            if (buildProfile.buildTarget == BuildTarget.Android)
            {
                return true;
            }
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                return buildTarget is BuildTarget.StandaloneWindows64 or BuildTarget.StandaloneWindows;
            }

            if (Application.platform == RuntimePlatform.LinuxEditor)
            {
                return buildTarget is BuildTarget.StandaloneLinux64 or BuildTarget.LinuxHeadlessSimulation or BuildTarget.EmbeddedLinux;
            }

            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                return buildTarget is BuildTarget.StandaloneOSX;
            }

            Debug.LogError($"Unknown platform: {Application.platform}"); // This should never happen
            return false;
        }

        [Serializable]
        internal struct BuildProfileState
        {
            [SerializeField] private BuildProfile m_ActiveProfile;
            [SerializeField] private BuildTarget m_ActiveBuildTarget;
            [SerializeField] private StandaloneBuildSubtarget m_ActiveSubtarget;

            public static BuildProfileState FromActiveSettings()
            {
                var profile = BuildProfile.GetActiveBuildProfile();
                if (profile != null)
                    return new BuildProfileState { m_ActiveProfile = profile };

                return new BuildProfileState
                {
                    m_ActiveBuildTarget = EditorUserBuildSettings.activeBuildTarget,
                    m_ActiveSubtarget = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget) == BuildTargetGroup.Standalone
                        ? EditorUserBuildSettings.standaloneBuildSubtarget
                        : StandaloneBuildSubtarget.Default
                };
            }

            public static void Restore(BuildProfileState state)
            {
                if (AreEqual(state, FromActiveSettings()))
                    return;

                if (state.m_ActiveProfile != null)
                    BuildProfile.SetActiveBuildProfile(state.m_ActiveProfile);
                else
                {
                    if (BuildPipeline.GetBuildTargetGroup(state.m_ActiveBuildTarget) == BuildTargetGroup.Standalone)
                        EditorUserBuildSettings.standaloneBuildSubtarget = state.m_ActiveSubtarget;

                    EditorUserBuildSettings.activeBuildProfile = null;

                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildPipeline.GetBuildTargetGroup(state.m_ActiveBuildTarget), state.m_ActiveBuildTarget);
                }
            }

            private static bool AreEqual(BuildProfileState a, BuildProfileState b)
            {
                return a.m_ActiveProfile == b.m_ActiveProfile &&
                       a.m_ActiveBuildTarget == b.m_ActiveBuildTarget &&
                       a.m_ActiveSubtarget == b.m_ActiveSubtarget;
            }
        }
    }
}
