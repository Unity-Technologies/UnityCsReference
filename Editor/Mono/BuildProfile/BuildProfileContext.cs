// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using UnityEditor.Modules;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Caches build workflow metadata for the current project.
    /// Handles management of required platform build profiles and implements
    /// native bindings for mapping migrated settings to backing profile.
    /// </summary>
    [InitializeOnLoad]
    internal sealed class BuildProfileContext : ScriptableObject
    {
        const string k_BuildProfileProviderAssetPath = "Library/BuildProfileContext.asset";
        const string k_BuildProfilePath = "Library/BuildProfiles";
        static BuildProfileContext s_Instance;

        [SerializeField]
        BuildProfile m_ActiveProfile;

        /// <summary>
        /// Cached mapping of BuildTarget to classic platform build profile.
        /// </summary>
        Dictionary<(BuildTarget, StandaloneBuildSubtarget), BuildProfile> m_BuildTargetToClassicPlatformProfile = new();

        /// <summary>
        /// Specifies the custom build profile used by the build pipeline and editor APIs when getting build settings.
        /// </summary>
        /// <remarks>
        /// Classic platforms, while build profiles in code, are not managed by the AssetDatabase and implement complex
        /// shared setting behaviour that is not compatible with the new build profile workflow. From the end users
        /// perspective Classic Platform and Build Profiles are different concepts.
        /// </remarks>
        internal BuildProfile activeProfile
        {
            get => m_ActiveProfile;
            set
            {
                if (m_ActiveProfile == value)
                    return;

                if (value == null || value.platformBuildProfile == null)
                {
                    m_ActiveProfile = null;
                    Save();
                    return;
                }

                if (m_BuildTargetToClassicPlatformProfile.TryGetValue(
                        (value.buildTarget, value.subtarget), out var entry) && entry == value)
                {
                    Debug.LogWarning("[BuildProfile] Classic Platforms cannot be set as the active build profile.");
                    return;
                }

                m_ActiveProfile = value;
                Save();
            }
        }

        /// <summary>
        /// Stores metadata required for Build Profile window and legacy APIs
        /// as part of the Library folder.
        /// </summary>
        [UsedImplicitly]
        internal static BuildProfileContext instance
        {
            get
            {
                if (s_Instance == null)
                {
                    CreateOrLoad();
                }

                return s_Instance;
            }
        }

        static BuildProfileContext()
        {
            // Asset operations such as asset loading should be avoided in InitializeOnLoad methods.
            // We delay the creation of the classic platform profiles until the next editor update.
            EditorApplication.delayCall += () =>
            {
                if(s_Instance == null)
                    CreateOrLoad();
            };
        }

        // On domain reload ScriptableObject objects gets reconstructed from a backup. We therefore set the s_Instance here
        private BuildProfileContext()
        {
            if (s_Instance != null)
            {
                Debug.LogError("BuildProfileProvider singleton already exists.");
            }
            else
            {
                s_Instance = this;

                // this can become null due to DestroyImmediate() or domain reload.
                System.Diagnostics.Debug.Assert(s_Instance != null);
            }
        }

        [RequiredByNativeCode, UsedImplicitly]
        internal static BuildProfile GetBuildProfileForGetter(
            BuildTarget target, StandaloneBuildSubtarget subTarget = StandaloneBuildSubtarget.Default)
        {
            if (ShouldReturnActiveProfile(target, subTarget))
                return instance.activeProfile;

            // For backwards compatibility, getter will look for
            // the classic platform build profile for the target platform
            // when no suitable active profile is found.
            return instance.GetForClassicPlatform(target, subTarget);
        }

        [RequiredByNativeCode, UsedImplicitly]
        internal static BuildProfile GetBuildProfileForSetter(
            BuildTarget target, StandaloneBuildSubtarget subTarget = StandaloneBuildSubtarget.Default)
        {
            if (ShouldReturnActiveProfile(target, subTarget))
            {
                // When invoking a legacy setter, we unset the active profile to prevent
                // inconsistencies with legacy APIs. That is, all legacy APIs need to write to
                // the classic platform build profile directly. When doing this, the next getter
                // MUST return the classic platform build profile containing the newly set value.
                // The active build profile should be updated directly.
                Debug.LogWarning($"[BuildProfile] Active build profile ({AssetDatabase.GetAssetPath(instance.activeProfile)}) is set when calling a legacy setter for the same platform. For backwards compatibility, the active build profile has been unset.");
                instance.activeProfile = null;
            }

            return instance.GetForClassicPlatform(target, subTarget);
        }

        internal static bool TryGetClassicPlatformSettingsBaseForGetter<T>(
            BuildTarget target, StandaloneBuildSubtarget subTarget, out T result) where T : BuildProfilePlatformSettingsBase
        {
            BuildProfile buildProfile = GetBuildProfileForGetter(target, subTarget);
            if (buildProfile != null && buildProfile.platformBuildProfile is T platformProfile)
            {
                result = platformProfile;
                return true;
            }

            result = null;
            return false;
        }

        internal static bool TryGetClassicPlatformSettingsBaseForSetter<T>(
            BuildTarget target, StandaloneBuildSubtarget subTarget, out T result) where T : BuildProfilePlatformSettingsBase
        {
            BuildProfile buildProfile = GetBuildProfileForSetter(target, subTarget);
            if (buildProfile != null && buildProfile.platformBuildProfile is T platformProfile)
            {
                result = platformProfile;
                return true;
            }

            result = null;
            return false;
        }

        void OnDisable()
        {
            Save();

            // Platform profiles must be manually serialized for changes to persist.
            foreach (var kvp in m_BuildTargetToClassicPlatformProfile)
            {
                SaveBuildProfileInProject(kvp.Value);
            }
        }

        BuildProfile GetForClassicPlatform(BuildTarget target, StandaloneBuildSubtarget subTarget)
        {
            var key = (target, subTarget);
            return m_BuildTargetToClassicPlatformProfile.GetValueOrDefault(key);
        }

        void OnEnable()
        {
            // If not library path is not found, we don't have any classic profiles to load.
            if (!Directory.Exists(k_BuildProfilePath))
                return;

            // No need to load classic profiles if build profile is disabled.
            if (!EditorUserBuildSettings.IsBuildProfileWorkflowEnabled())
            {
                return;
            }

            foreach (var platform in BuildPlatforms.instance.GetValidPlatforms())
            {
                string path = (platform is BuildPlatformWithSubtarget platformWithSubtarget) ?
                    GetFilePathForBuildProfile(platform.defaultTarget, (StandaloneBuildSubtarget)platformWithSubtarget.subtarget) :
                    GetFilePathForBuildProfile(platform.defaultTarget, StandaloneBuildSubtarget.Default);

                if (!File.Exists(path))
                    continue;

                var profile = InternalEditorUtility.LoadSerializedFileAndForget(path);
                if (profile == null || profile.Length == 0 || profile[0] is not BuildProfile profileObj)
                {
                    Debug.LogWarning($"Failed to load build profile from {path}.");
                    continue;
                }

                m_BuildTargetToClassicPlatformProfile[(profileObj.buildTarget, profileObj.subtarget)] = profileObj;
            }
        }

        /// <summary>
        /// Creates platform build profiles for all installed and buildable platforms.
        /// Platforms with sub targets will generate multiple profiles.
        /// </summary>
        void CheckInstalledBuildPlatforms()
        {
            foreach (var platform in BuildPlatforms.instance.GetValidPlatforms())
            {
                string targetString = ModuleManager.GetTargetStringFromBuildTarget(platform.defaultTarget);
                if (!ModuleManager.IsPlatformSupportLoaded(targetString))
                {
                    continue;
                }

                if (ModuleManager.GetBuildProfileExtension(targetString) == null)
                {
                    // Require platform support and implemented build profile
                    // extension for the target platform.
                    Debug.LogWarning("Platform does not support build profiles targetString=" + targetString);
                    continue;
                }

                if (platform is BuildPlatformWithSubtarget platformWithSubtarget)
                {
                    GetOrCreateClassicPlatformBuildProfile(platform.defaultTarget,
                        (StandaloneBuildSubtarget)platformWithSubtarget.subtarget);
                }
                else
                {
                    GetOrCreateClassicPlatformBuildProfile(platform.defaultTarget, StandaloneBuildSubtarget.Default);
                }
            }
        }

        BuildProfile GetOrCreateClassicPlatformBuildProfile(BuildTarget target, StandaloneBuildSubtarget subTarget)
        {
            var key = (target, subTarget);
            if (m_BuildTargetToClassicPlatformProfile.TryGetValue(key, out var profile) && profile != null)
            {
                return profile;
            }

            // Platform profiles are stored in the ProjectSettings folder and not managed by the AssetDatabase.
            // We will manually handle serialization and deserialization of these objects.
            var buildProfile = BuildProfile.CreateInstance(target, subTarget);
            buildProfile.hideFlags = HideFlags.DontSave;

            // Created profile is populated by existing EditorUserBuildSettings.
            m_BuildTargetToClassicPlatformProfile.Add(key, buildProfile);

            // Only copy after adding to the build target -> classic profiles dictionary, so EditorUserBuildSettings
            // can access the classic profiles when copying the settings
            EditorUserBuildSettings.CopyToBuildProfile(buildProfile);

            // Created profile can also be populated by settings on the managed side
            string module = BuildTargetDiscovery.GetModuleNameForBuildTarget(buildProfile.buildTarget);
            var extension = ModuleManager.GetBuildProfileExtension(module);
            if (extension != null)
            {
                extension.CopyPlatformSettingsToBuildProfile(buildProfile.platformBuildProfile);
            }
            else
            {
                Debug.LogError($"Build profile extension is null for module {module} and build profile {buildProfile.name}");
            }

            SaveBuildProfileInProject(buildProfile);

            return buildProfile;
        }

        static void SaveBuildProfileInProject(BuildProfile profile)
        {
            if (!Directory.Exists(k_BuildProfilePath))
            {
                Directory.CreateDirectory(k_BuildProfilePath);
            }

            InternalEditorUtility.SaveToSerializedFileAndForget(new []{ profile },
                GetFilePathForBuildProfile(profile.buildTarget, profile.subtarget), true);
        }

        static void CreateOrLoad()
        {
            var buildProfileContext = InternalEditorUtility.LoadSerializedFileAndForget(k_BuildProfileProviderAssetPath);
            if (buildProfileContext != null && buildProfileContext.Length > 0)
            {
                s_Instance = buildProfileContext[0] as BuildProfileContext;
                if (s_Instance == null)
                    Debug.LogError("BuildProfileContext asset exists but could not be loaded.");
            }
            else
            {
                s_Instance = CreateInstance<BuildProfileContext>();
                s_Instance.hideFlags = HideFlags.DontSave;
                Save();
            }

            System.Diagnostics.Debug.Assert(s_Instance != null);

            // Only check installed build platforms to create classic profiles
            // when the build profile flag is enabled. We need to check here
            // because this method can be called from the static constructor.
            // So we should only set the instance but not create profiles
            if (EditorUserBuildSettings.IsBuildProfileWorkflowEnabled())
            {
                s_Instance.CheckInstalledBuildPlatforms();
            }
        }

        [RequiredByNativeCode]
        static void SetClassicProfileRawPlatformSetting(string settingName, string settingValue, BuildTarget target, StandaloneBuildSubtarget subtarget)
        {
            if (TryGetClassicPlatformSettingsBaseForSetter(target, subtarget, out BuildProfilePlatformSettingsBase platformProfile))
            {
                platformProfile.SetRawPlatformSetting(settingName, settingValue);
            }
            else
            {
                Debug.LogWarning($"Can't set {settingName} in build profile. The platform build profile settings is null.");
            }
        }

        [RequiredByNativeCode]
        static string GetClassicProfileRawPlatformSetting(string settingName, BuildTarget target, StandaloneBuildSubtarget subtarget)
        {
            if (TryGetClassicPlatformSettingsBaseForGetter(target, subtarget, out BuildProfilePlatformSettingsBase platformProfile))
            {
                return platformProfile.GetRawPlatformSetting(settingName);
            }
            else
            {
                Debug.LogWarning($"Can't get {settingName} from build profile. The platform build profile settings is null.");
                return string.Empty;
            }
        }

        static bool ShouldReturnActiveProfile(BuildTarget buildTarget, StandaloneBuildSubtarget subtarget)
        {
            var activeProfile = instance.m_ActiveProfile;
            if (activeProfile == null || subtarget != activeProfile.subtarget)
                return false;

            var activeBuildTarget = activeProfile.buildTarget;

            // It's acceptable for windows build target to be either 64 or 32
            if (buildTarget == BuildTarget.StandaloneWindows64 || buildTarget == BuildTarget.StandaloneWindows)
            {
                return activeBuildTarget == BuildTarget.StandaloneWindows64 || activeBuildTarget == BuildTarget.StandaloneWindows;
            }
            else
            {
                return buildTarget == activeBuildTarget;
            }
        }

        static string GetFilePathForBuildProfile(BuildTarget buildTarget, StandaloneBuildSubtarget subtarget) =>
            $"{k_BuildProfilePath}/PlatformProfile.{buildTarget}.{subtarget}.asset";

        static void Save() => InternalEditorUtility.SaveToSerializedFileAndForget(new[] { instance },
            k_BuildProfileProviderAssetPath, true);
    }
}
