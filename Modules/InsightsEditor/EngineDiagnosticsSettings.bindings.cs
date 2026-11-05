// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor.Build.Profile;
using UnityEditor.InsightsEditor;
using UnityEditor.Mono.BuildPipeline;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.EngineDiagnostics
{
    [VisibleToOtherModules]
    [Serializable]
    [NativeHeader("Modules/UnityConnect/Insights/InsightsSettings.h")]
    internal enum BuildProfileEngineDiagnosticsState
    {
        /// <summary>
        /// Engine Diagnostics are disabled
        /// </summary>
        Disabled = 0,
        /// <summary>
        /// Engine Diagnostics are enabled
        /// </summary>
        Enabled = 1,
        /// <summary>
        /// The user never changed the setting. Engine Diagnostics are enabled.
        /// </summary>
        Default = 2
    }

    [NativeHeader("Modules/UnityConnect/Insights/InsightsSettings.h")]
    [NativeHeader("Modules/Insights/CollectionRequirement.h")]
    [StaticAccessor("GetInsightsSettings()")]
    public static class EngineDiagnosticsSettings
    {
        public static bool enabled
        {
            get => GetActiveBuildProfileState() != BuildProfileEngineDiagnosticsState.Disabled;
            set
            {
                var state = value ? BuildProfileEngineDiagnosticsState.Enabled : BuildProfileEngineDiagnosticsState.Disabled;
                if (!SetActiveBuildProfileState(state))
                    Debug.LogWarning($"EngineDiagnosticsSettings.enabled couldn't save Diagnostic Data Collection as {state} because no build profile is available.");
            }
        }

        [VisibleToOtherModules]
        [NativeMethod("GetEngineDiagnosticsEnabled")]
        internal static extern bool IsEnabled();

        // For UI that shows a build profile that may not be the active one
        internal static bool IsEnabled(BuildProfile profile) =>
            !string.IsNullOrEmpty(CloudProjectSettings.projectId) &&
            GetState(profile.buildTarget, profile) != BuildProfileEngineDiagnosticsState.Disabled;

        internal static extern void NotifyActiveBuildProfileChanged();

        // Called by InsightsSettings to resolve the active build profile's state.
        [RequiredByNativeCode]
        internal static BuildProfileEngineDiagnosticsState GetActiveBuildProfileState()
        {
            var profile = ActiveOrClassicProfile();
            var buildTarget = profile != null ? profile.buildTarget : EditorUserBuildSettings.activeBuildTarget;
            return GetState(buildTarget, profile);
        }

        static BuildProfile ActiveOrClassicProfile() =>
            BuildProfileContext.activeProfile ?? BuildProfileContext.instance.GetForClassicPlatform(EditorUserBuildSettings.activePlatformGuid);

        internal static BuildProfileEngineDiagnosticsState GetState(BuildTarget buildTarget, BuildProfile profile)
        {
            // The Build Profiles window doesn't offer the setting on these platforms, so they never collect
            if (!IsFeatureSupported(buildTarget))
                return BuildProfileEngineDiagnosticsState.Disabled;

            if (profile == null)
                return BuildProfileEngineDiagnosticsState.Default;

            // No platform settings means the platform's support module isn't installed
            return profile.platformBuildProfile?.insightsSettingsContainer.buildProfileEngineDiagnosticsState ?? BuildProfileEngineDiagnosticsState.Disabled;
        }

        // Also used by tests and automation that cannot use the Build Profiles window
        internal static bool SetActiveBuildProfileState(BuildProfileEngineDiagnosticsState state)
        {
            var profile = ActiveOrClassicProfile();
            if (profile?.platformBuildProfile == null)
                return false;

            var container = profile.platformBuildProfile.insightsSettingsContainer;
            container.buildProfileEngineDiagnosticsState = state;
            profile.platformBuildProfile.insightsSettingsContainer = container;

            if (BuildProfileContext.IsClassicPlatformProfile(profile))
                BuildProfileContext.SaveBuildProfileInProject(profile);
            else
            {
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssetIfDirty(profile);
            }

            InvalidatePlayerDataCache(profile.buildTarget);
            NotifyActiveBuildProfileChanged();
            return true;
        }

        // Platform profiles aren't among the files DataBuildDirtyTracker checks, so without this the next build reuses the old setting
        internal static void InvalidatePlayerDataCache(BuildTarget buildTarget)
        {
            var dirtyInfoPath = BuildPipeline.GetBuildDirtyInfoForBuildTarget(buildTarget, 0);
            var platformCache = Path.GetDirectoryName(dirtyInfoPath);
            var cacheRoot = Path.GetDirectoryName(platformCache);
            if (!Directory.Exists(cacheRoot))
                return;

            // Each subtarget has its own cache folder named after the platform
            foreach (var directory in Directory.GetDirectories(cacheRoot, Path.GetFileName(platformCache) + "*"))
            {
                DataBuildDirtyTracker.ForceDirty(Path.Combine(directory, Path.GetFileName(dirtyInfoPath)));
            }
        }

        [VisibleToOtherModules]
        internal static extern bool IsFeatureSupported(BuildTarget target);
        [VisibleToOtherModules]
        internal static extern bool GetInsightsModuleEnabled();
        [VisibleToOtherModules]
        internal static extern void SetCustomEventUrl(string url);
        [VisibleToOtherModules]
        internal static extern CollectionRequirement[] GetCollectionRequirements();
        [VisibleToOtherModules]
        internal static extern void SetCollectionRequirements(CollectionRequirement[] requirements);
        [VisibleToOtherModules]
        internal static extern CollectionRequirement[] GetProjectPackages();
        [VisibleToOtherModules]
        internal static extern void SetProjectPackages(CollectionRequirement[] packages);

        // Called by InsightsSettings::Transfer when serializing for a build.
        [RequiredByNativeCode]
        internal static void SyncRequirements() =>
            InsightsRequirementsResolver.SyncRequirements();
    }
}
