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
    [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
    internal sealed class BuildProfileContext : ScriptableObject
    {
        const string k_BuildProfileProviderAssetPath = "Library/BuildProfileContext.asset";
        const string k_BuildProfilePath = "Library/BuildProfiles";
        const string k_SharedProfilePath = $"{k_BuildProfilePath}/SharedProfile.asset";
        static BuildProfileContext s_Instance;

        [SerializeField]
        BuildProfile m_ActiveProfile;

        /// <summary>
        /// Cached mapping of module name to classic platform build profile.
        /// </summary>
        Dictionary<(string, StandaloneBuildSubtarget), BuildProfile> m_BuildModuleNameToClassicPlatformProfile = new();

        /// <summary>
        /// Specifies the custom build profile used by the build pipeline and editor APIs when getting build settings.
        /// </summary>
        /// <remarks>
        /// Classic platforms, while build profiles in code, are not managed by the AssetDatabase and implement complex
        /// shared setting behaviour that is not compatible with the new build profile workflow. From the end users
        /// perspective Classic Platform and Build Profiles are different concepts.
        /// </remarks>
        [VisibleToOtherModules]
        internal BuildProfile activeProfile
        {
            get
            {
                // Active Build profile may be deleted from the project.
                if (m_ActiveProfile != null)
                    return m_ActiveProfile;

                m_ActiveProfile = null;
                return null;
            }

            set
            {
                if (m_ActiveProfile == value)
                    return;

                var prev = m_ActiveProfile;
                if (value == null || value.platformBuildProfile == null)
                {
                    m_ActiveProfile = null;
                    Save();
                    activeProfileChanged?.Invoke(prev, m_ActiveProfile);
                    return;
                }

                if (m_BuildModuleNameToClassicPlatformProfile.TryGetValue(
                        (value.moduleName, value.subtarget), out var entry) && entry == value)
                {
                    Debug.LogWarning("[BuildProfile] Classic Platforms cannot be set as the active build profile.");
                    return;
                }

                m_ActiveProfile = value;
                Save();
                activeProfileChanged?.Invoke(prev, m_ActiveProfile);
            }
        }

        /// <summary>
        /// Callback invoked when the active build profile has been changed to a new value.
        /// </summary>
        /// <see cref="activeProfile"/>
        /// <remarks>
        /// Callback passes a value to the previously active Build Profile. A <i>null</i> value
        /// indicates there's no active profile (set or unset).
        /// <code>
        /// activeProfileChanged += (BuildProfile prev, BuildProfile cur) => {}
        /// </code>
        /// </remarks>
        public event Action<BuildProfile, BuildProfile> activeProfileChanged;

        /// <summary>
        /// Stores metadata required for Build Profile window and legacy APIs
        /// as part of the Library folder.
        /// </summary>
        [UsedImplicitly]
        internal static BuildProfileContext instance
        {
            [VisibleToOtherModules]
            get
            {
                if (s_Instance == null)
                {
                    CreateOrLoad();
                }

                return s_Instance;
            }
        }

        internal IList<BuildProfile> classicPlatformProfiles
        {
            [VisibleToOtherModules]
            get;
            private set;
        }

        /// <summary>
        /// A hidden build profile for syncing shared settings for backwards compatibility.
        /// </summary>
        internal BuildProfile sharedProfile
        {
            get;
            private set;
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

        [RequiredByNativeCode]
        internal static BuildProfile GetActiveOrClassicBuildProfile(
            BuildTarget target, StandaloneBuildSubtarget subTarget = StandaloneBuildSubtarget.Default, string sharedSetting = null)
        {
            if (ShouldReturnActiveProfile(target, subTarget, sharedSetting))
                return instance.activeProfile;

            // For backwards compatibility, getter will look for
            // the classic platform build profile for the target platform
            // when no suitable active profile is found.
            return IsSharedProfile(target) ? instance.sharedProfile : instance.GetForClassicPlatform(target, subTarget);
        }

        [RequiredByNativeCode]
        internal static BuildProfile GetClassicProfileAndResetActive(
            BuildTarget target, StandaloneBuildSubtarget subTarget = StandaloneBuildSubtarget.Default, string sharedSetting = null)
        {
            if (ShouldReturnActiveProfile(target, subTarget, sharedSetting))
            {
                // When invoking a legacy setter, we unset the active profile to prevent
                // inconsistencies with legacy APIs. That is, all legacy APIs need to write to
                // the classic platform build profile directly. When doing this, the next getter
                // MUST return the classic platform build profile containing the newly set value.
                // The active build profile should be updated directly.
                ResetActiveProfile();
            }

            return IsSharedProfile(target) ? instance.sharedProfile : instance.GetForClassicPlatform(target, subTarget);
        }

        [RequiredByNativeCode, UsedImplicitly]
        internal static void ResetActiveProfile()
        {
            if (instance.activeProfile is null)
                return;

            Debug.LogWarning($"[BuildProfile] Active build profile ({AssetDatabase.GetAssetPath(instance.activeProfile)}) is set when calling a global platform API.");
            instance.activeProfile = null;
        }

        internal static bool TryGetActiveOrClassicPlatformSettingsBase<T>(
            BuildTarget target, StandaloneBuildSubtarget subTarget, out T result) where T : BuildProfilePlatformSettingsBase
        {
            BuildProfile buildProfile = GetActiveOrClassicBuildProfile(target, subTarget);
            if (buildProfile != null && buildProfile.platformBuildProfile is T platformProfile)
            {
                result = platformProfile;
                return true;
            }

            result = null;
            return false;
        }

        internal static bool TryGetClassicPlatformSettingsBaseAndResetActive<T>(
            BuildTarget target, StandaloneBuildSubtarget subTarget, out T result) where T : BuildProfilePlatformSettingsBase
        {
            BuildProfile buildProfile = GetClassicProfileAndResetActive(target, subTarget);
            if (buildProfile != null && buildProfile.platformBuildProfile is T platformProfile)
            {
                result = platformProfile;
                return true;
            }

            result = null;
            return false;
        }

        [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
        internal static bool IsClassicPlatformProfile(BuildProfile profile)
        {
            var key = (profile.moduleName, profile.subtarget);
            if (instance.m_BuildModuleNameToClassicPlatformProfile.TryGetValue(key, out var classicProfile))
            {
                return classicProfile == profile;
            }

            return false;
        }

        /// <summary>
        /// List of missing platforms modules that are not installed, don't have a classic platform profile,
        /// and can be shown in the Build Settings window.
        /// </summary>
        /// <see cref="BuildPlayerWindow.ActiveBuildTargetsGUI"/>
        [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
        internal List<(string, StandaloneBuildSubtarget)> GetMissingKnownPlatformModules()
        {
            var result = new List<(string, StandaloneBuildSubtarget)>();
            var keys = BuildProfileModuleUtil.FindAllViewablePlatforms();
            for (var index = 0; index < keys.Count; index++)
            {
                var key = keys[index];

                if (m_BuildModuleNameToClassicPlatformProfile.ContainsKey(key))
                    continue;

                // Installed flag, as calculated by BuildPlatform
                if (BuildProfileModuleUtil.IsModuleInstalled(key.Item1, key.Item2))
                    continue;

                // Some build targets are only compatible with specific OS,
                // from BuildPlayerWindow.ActiveBuildTargetsGUI()
                var iBuildTarget = ModuleManager.GetIBuildTarget(key.Item1);
                if (iBuildTarget != null
                    && !(iBuildTarget.BuildPlatformProperties?.CanBuildOnCurrentHostPlatform ?? true))
                    continue;

                result.Add(key);
            }

            return result;
        }

        void OnDisable()
        {
            Save();

            // Platform profiles must be manually serialized for changes to persist.
            foreach (var kvp in m_BuildModuleNameToClassicPlatformProfile)
            {
                SaveBuildProfileInProject(kvp.Value);
            }

            if (sharedProfile != null)
                SaveBuildProfileInProject(sharedProfile);
        }

        BuildProfile GetForClassicPlatform(BuildTarget target, StandaloneBuildSubtarget subTarget)
        {
            var key = GetKey(target, subTarget);
            return m_BuildModuleNameToClassicPlatformProfile.GetValueOrDefault(key);
        }

        void OnEnable()
        {
            classicPlatformProfiles = new List<BuildProfile>();

            // Load platform build profiles from ProjectSettings folder.
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
                    GetFilePathForBuildProfile(GetKey(platform.defaultTarget, (StandaloneBuildSubtarget)platformWithSubtarget.subtarget)) :
                    GetFilePathForBuildProfile(GetKey(platform.defaultTarget, StandaloneBuildSubtarget.Default));

                if (!File.Exists(path))
                    continue;

                var profile = InternalEditorUtility.LoadSerializedFileAndForget(path);
                if (profile == null || profile.Length == 0 || profile[0] is not BuildProfile profileObj)
                {
                    Debug.LogWarning($"Failed to load build profile from {path}.");
                    continue;
                }

                m_BuildModuleNameToClassicPlatformProfile.Add((profileObj.moduleName, profileObj.subtarget), profileObj);
                classicPlatformProfiles.Add(profileObj);
            }

            if (!File.Exists(k_SharedProfilePath))
                return;

            var sharedProfile = InternalEditorUtility.LoadSerializedFileAndForget(k_SharedProfilePath);
            if (sharedProfile == null || sharedProfile.Length == 0 || sharedProfile[0] is not BuildProfile sharedProfileObj)
            {
                Debug.LogWarning($"Failed to load shared profile from {k_SharedProfilePath}.");
                return;
            }

            instance.sharedProfile = sharedProfileObj;
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

            GetOrCreateSharedBuildProfile();
        }

        BuildProfile GetOrCreateClassicPlatformBuildProfile(BuildTarget target, StandaloneBuildSubtarget subTarget)
        {
            var key = GetKey(target, subTarget);
            if (m_BuildModuleNameToClassicPlatformProfile.TryGetValue(key, out var profile) && profile != null)
            {
                return profile;
            }

            // Platform profiles are not managed by the AssetDatabase.
            // We will manually handle serialization and deserialization of these objects.
            var buildProfile = BuildProfile.CreateInstance(target, subTarget);
            buildProfile.hideFlags = HideFlags.DontSave;

            m_BuildModuleNameToClassicPlatformProfile.Add(key, buildProfile);
            classicPlatformProfiles.Add(buildProfile);

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

        BuildProfile GetOrCreateSharedBuildProfile()
        {
            if (sharedProfile != null)
                return sharedProfile;

            // Shared profile is stored in the Library folder is not managed by the AssetDatabase.
            // We will manually handle serialization and deserialization of it.
            var buildProfile = ScriptableObject.CreateInstance<BuildProfile>();
            buildProfile.buildTarget = BuildTarget.NoTarget;
            buildProfile.subtarget = StandaloneBuildSubtarget.Default;
            buildProfile.moduleName = string.Empty;
            buildProfile.platformBuildProfile = new SharedPlatformSettings();
            buildProfile.hideFlags = HideFlags.DontSave;

            sharedProfile = buildProfile;

            // Only copy after setting shared profile, so EditorUserBuildSettings can access the shared profile
            // when copying the settings.
            EditorUserBuildSettings.CopyToBuildProfile(buildProfile);

            SaveBuildProfileInProject(buildProfile);

            return buildProfile;
        }

        static (string, StandaloneBuildSubtarget) GetKey(BuildTarget buildTarget, StandaloneBuildSubtarget subtarget)
        {
            if (buildTarget == BuildTarget.NoTarget)
            {
                return (string.Empty, subtarget);
            }

            return (ModuleManager.GetTargetStringFrom(buildTarget), subtarget);
        }

        static void SaveBuildProfileInProject(BuildProfile profile)
        {
            if (!Directory.Exists(k_BuildProfilePath))
            {
                Directory.CreateDirectory(k_BuildProfilePath);
            }

            string path = IsSharedProfile(profile.buildTarget) ? k_SharedProfilePath : GetFilePathForBuildProfile((profile.moduleName, profile.subtarget));
            InternalEditorUtility.SaveToSerializedFileAndForget(new []{ profile }, path, allowTextSerialization: true);
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

        [RequiredByNativeCode, UsedImplicitly]
        static void SetClassicProfileRawPlatformSetting(string settingName, string settingValue, BuildTarget target, StandaloneBuildSubtarget subtarget)
        {
            // If it is a shared setting, we will reset active to its classic counterpart if the specified shared
            // setting is enabled in the active profile, and set the value in the shared profile.
            if (IsSharedProfile(target))
            {
                var profile = GetClassicProfileAndResetActive(target, subtarget, settingName);
                profile?.platformBuildProfile.SetRawPlatformSetting(settingName, settingValue);
                return;
            }

            // If the setting doesn't exist in classic platform, return
            BuildProfile classicProfile = instance.GetForClassicPlatform(target, subtarget);
            if (classicProfile == null || classicProfile.platformBuildProfile == null)
                return;

            if (classicProfile.platformBuildProfile.GetRawPlatformSetting(settingName) == null)
                return;

            // Setting exists, so we should set new value and reset active if needed
            if (TryGetClassicPlatformSettingsBaseAndResetActive(target, subtarget, out BuildProfilePlatformSettingsBase platformProfile))
            {
                platformProfile.SetRawPlatformSetting(settingName, settingValue);
            }
            else if (subtarget != StandaloneBuildSubtarget.Server)
            {
                // We shouldn't log a warning if it's the server, since it's possible to not have the server installed and have
                // the set being called. That's because we double write the setting on native side (for player and server)
                Debug.LogWarning($"Can't set {settingName} in build profile. The platform build profile settings is null. Verify that the module for {target} is installed.");
            }
        }

        [RequiredByNativeCode, UsedImplicitly]
        static string GetActiveOrClassicProfileRawPlatformSetting(string settingName, BuildTarget target, StandaloneBuildSubtarget subtarget)
        {
            // If it is a shared setting, we will return the value from the active profile if the specified shared setting
            // is enabled in the active profile; Otherwise, we will return the value from the shared profile.
            if (IsSharedProfile(target))
            {
                var profile = GetActiveOrClassicBuildProfile(target, subtarget, settingName);
                return profile?.platformBuildProfile.GetRawPlatformSetting(settingName) ?? string.Empty;
            }

            if (TryGetActiveOrClassicPlatformSettingsBase(target, subtarget, out BuildProfilePlatformSettingsBase platformProfile))
            {
                string value = platformProfile.GetRawPlatformSetting(settingName);
                return value != null ? value : string.Empty;
            }

            return string.Empty;
        }

        static bool ShouldReturnActiveProfile(BuildTarget buildTarget, StandaloneBuildSubtarget subtarget, string sharedSetting = null)
        {
            if (!string.IsNullOrEmpty(sharedSetting))
                return IsSharedSettingEnabledInActiveProfile(sharedSetting);

            var activeProfile = instance.m_ActiveProfile;
            if (activeProfile == null || buildTarget == BuildTarget.NoTarget || subtarget != activeProfile.subtarget)
                return false;

            var targetModuleName = ModuleManager.GetTargetStringFrom(buildTarget);
            return targetModuleName == activeProfile.moduleName;
        }

        static bool IsSharedSettingEnabledInActiveProfile(string settingName)
        {
            var activeProfile = instance.activeProfile;
            if (activeProfile == null)
                return false;

            var platformSettingsBase = activeProfile.platformBuildProfile;
            return platformSettingsBase.IsSharedSettingEnabled(settingName);
        }

        static string GetFilePathForBuildProfile((string moduleName, StandaloneBuildSubtarget subtarget) key) =>
            $"{k_BuildProfilePath}/PlatformProfile.{key.moduleName}.{key.subtarget}.asset";

        static void Save() => InternalEditorUtility.SaveToSerializedFileAndForget(new[] { instance },
            k_BuildProfileProviderAssetPath, true);

        static bool IsSharedProfile(BuildTarget target) => target == BuildTarget.NoTarget;
    }
}
