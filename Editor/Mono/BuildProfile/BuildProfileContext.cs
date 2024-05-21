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

        [SerializeField]
        string[] m_CachedEditorScriptingDefines = Array.Empty<string>();

        [SerializeField]
        List<string> m_LastRunnableBuildPathKeys = new();
        internal List<string> LastRunnableBuildPathKeys => m_LastRunnableBuildPathKeys;

        /// <summary>
        /// Cached mapping of platform ID to classic platform build profile.
        /// </summary>
        Dictionary<string, BuildProfile> m_PlatformIdToClassicPlatformProfile = new();

        /// <summary>
        /// Cached editor scripting defines for the active profile.
        /// On disk changes to build profile asset, cached value is referenced
        /// when determining if a recompilation is required.
        /// </summary>
        [VisibleToOtherModules]
        internal string[] cachedEditorScriptingDefines
        {
            get => m_CachedEditorScriptingDefines;
            set => m_CachedEditorScriptingDefines = value;
        }

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
                if (m_ActiveProfile != null && m_ActiveProfile.CanBuildLocally())
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
                    m_ActiveProfile.UpdateGlobalManagerPlayerSettings(activeWillBeRemoved: true);
                    m_ActiveProfile = null;
                    Save();
                    EditorUserBuildSettings.SetBuildProfilePath(string.Empty);
                    activeProfileChanged?.Invoke(prev, m_ActiveProfile);
                    BuildProfileModuleUtil.RequestScriptCompilation(null);
                    return;
                }

                if (m_PlatformIdToClassicPlatformProfile.TryGetValue(
                    value.platformId, out var entry) && entry == value)
                {
                    Debug.LogWarning("[BuildProfile] Classic Platforms cannot be set as the active build profile.");
                    return;
                }

                m_ActiveProfile = value;
                Save();
                EditorUserBuildSettings.SetBuildProfilePath(AssetDatabase.GetAssetPath(m_ActiveProfile));
                activeProfileChanged?.Invoke(prev, m_ActiveProfile);
                m_ActiveProfile.UpdateGlobalManagerPlayerSettings();
                BuildProfileModuleUtil.RequestScriptCompilation(m_ActiveProfile);
            }
        }

        internal static void HandlePendingChangesBeforeEnterPlaymode()
        {
            if (!EditorUserBuildSettings.isBuildProfileAvailable)
                return;

            var defines = BuildDefines.GetBuildProfileScriptDefines();
            if (!ArrayUtility.ArrayEquals(defines, instance.cachedEditorScriptingDefines))
            {
                instance.cachedEditorScriptingDefines = defines;
                PlayerSettings.RecompileScripts("Build profile has been modified.");
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
                if (s_Instance == null)
                    CreateOrLoad();
            };
        }

        // On domain reload ScriptableObject objects gets reconstructed from a backup. We therefore set the s_Instance here
        private BuildProfileContext()
        {
            if (s_Instance != null)
            {
                Debug.LogError("BuildProfileContext singleton already exists.");
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

        internal BuildProfile GetForClassicPlatform(BuildTarget target, StandaloneBuildSubtarget subtarget)
        {
            if (!BuildProfileModuleUtil.IsStandalonePlatform(target))
                subtarget = StandaloneBuildSubtarget.Default;
            else if (subtarget == StandaloneBuildSubtarget.Default)
                subtarget = StandaloneBuildSubtarget.Player;

            var platformId = BuildProfileModuleUtil.GetPlatformId(target, subtarget);
            return m_PlatformIdToClassicPlatformProfile.GetValueOrDefault(platformId);
        }

        [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
        internal static bool IsClassicPlatformProfile(BuildProfile profile)
        {
            if (instance.m_PlatformIdToClassicPlatformProfile.TryGetValue(profile.platformId, out var classicProfile))
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
        internal List<string> GetMissingKnownPlatformModules()
        {
            var missingPlatforms = new List<string>();
            var keys = BuildProfileModuleUtil.FindAllViewablePlatforms();
            for (var index = 0; index < keys.Count; index++)
            {
                var key = keys[index];

                if (m_PlatformIdToClassicPlatformProfile.ContainsKey(key))
                    continue;

                // Installed flag, as calculated by BuildPlatform
                if (BuildProfileModuleUtil.IsModuleInstalled(key))
                    continue;

                // Some build targets are only compatible with specific OS
                if (!BuildTargetDiscovery.BuildPlatformIsAvailableOnHostPlatform(new GUID(key), SystemInfo.operatingSystemFamily))
                    continue;

                missingPlatforms.Add(key);
            }

            return missingPlatforms;
        }

        /// <summary>
        /// Check if there's an active build profile with player settings
        /// </summary>
        static internal bool ProjectHasActiveProfileWithPlayerSettings()
        {
            var activeBuildProfile = BuildProfile.GetActiveBuildProfile();
            return activeBuildProfile?.playerSettings != null;
        }

        /// <summary>
        /// Sync the active build profile to EditorUserBuildSettings to ensure they are in a consistent state.
        /// </summary>
        void SyncActiveProfileToFallback()
        {
            if (!EditorUserBuildSettings.isBuildProfileAvailable)
                return;

            var buildProfile = activeProfile ?? GetForClassicPlatform(EditorUserBuildSettings.activeBuildTarget, EditorUserBuildSettings.standaloneBuildSubtarget);

            if (buildProfile == null)
            {
                EditorUserBuildSettings.isBuildProfileAvailable = false;
                return;
            }

            EditorUserBuildSettings.CopyFromBuildProfile(buildProfile);

            string module = BuildTargetDiscovery.GetModuleNameForBuildTarget(buildProfile.buildTarget);
            var extension = ModuleManager.GetBuildProfileExtension(module);
            if (extension != null)
            {
                extension.CopyPlatformSettingsFromBuildProfile(buildProfile.platformBuildProfile);
            }

            EditorUserBuildSettings.isBuildProfileAvailable = false;
        }

        void OnDisable()
        {
            Save();
            EditorUserBuildSettings.SetBuildProfilePath((m_ActiveProfile != null) ?
                AssetDatabase.GetAssetPath(m_ActiveProfile) : string.Empty);

            // Platform profiles must be manually serialized for changes to persist.
            foreach (var kvp in m_PlatformIdToClassicPlatformProfile)
            {
                SaveBuildProfileInProject(kvp.Value);
            }

            if (sharedProfile != null)
                SaveBuildProfileInProject(sharedProfile);

            SyncActiveProfileToFallback();

            EditorApplication.quitting -= SyncActiveProfileToFallback;
        }

        void OnEnable()
        {
            EditorUserBuildSettings.isBuildProfileAvailable = true;
            EditorApplication.quitting -= SyncActiveProfileToFallback;
            EditorApplication.quitting += SyncActiveProfileToFallback;

            classicPlatformProfiles = new List<BuildProfile>();

            // Load platform build profiles from ProjectSettings folder.
            if (!Directory.Exists(k_BuildProfilePath))
                return;

            var viewablePlatformKeys = BuildProfileModuleUtil.FindAllViewablePlatforms();
            for (var index = 0; index < viewablePlatformKeys.Count; index++)
            {
                var key = viewablePlatformKeys[index];
                string path = GetFilePathForBuildProfile(key);

                if (!File.Exists(path) || !BuildProfileModuleUtil.IsModuleInstalled(key))
                    continue;

                var profile = InternalEditorUtility.LoadSerializedFileAndForget(path);
                if (profile == null || profile.Length == 0 || profile[0] is not BuildProfile profileObj)
                {
                    Debug.LogWarning($"Failed to load build profile from {path}.");
                    continue;
                }

                m_PlatformIdToClassicPlatformProfile.Add(profileObj.platformId, profileObj);
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

            var buildProfile = instance.activeProfile ?? GetForClassicPlatform(EditorUserBuildSettings.activeBuildTarget, EditorUserBuildSettings.standaloneBuildSubtarget);

            // profile can be null if we're in the middle of creating classic profiles
            if (buildProfile == null)
                return;

            EditorUserBuildSettings.CopyToBuildProfile(buildProfile);

            string module = BuildTargetDiscovery.GetModuleNameForBuildTarget(buildProfile.buildTarget);
            var extension = ModuleManager.GetBuildProfileExtension(module);
            if (extension != null)
            {
                extension.CopyPlatformSettingsToBuildProfile(buildProfile.platformBuildProfile);
            }
        }

        /// <summary>
        /// Creates platform build profiles for all installed and buildable platforms.
        /// Platforms with sub targets will generate multiple profiles.
        /// </summary>
        void CheckInstalledBuildPlatforms()
        {
            var viewablePlatformKeys = BuildProfileModuleUtil.FindAllViewablePlatforms();
            for (var index = 0; index < viewablePlatformKeys.Count; index++)
            {
                var key = viewablePlatformKeys[index];
                var moduleName = BuildProfileModuleUtil.GetModuleName(key);

                if (!BuildProfileModuleUtil.IsModuleInstalled(key))
                    continue;

                if (ModuleManager.GetBuildProfileExtension(moduleName) == null)
                {
                    // Require platform support and implemented build profile
                    // extension for the target platform.
                    Debug.LogWarning("Platform does not support build profiles targetString=" + moduleName);
                    continue;
                }

                GetOrCreateClassicPlatformBuildProfile(key);
            }

            GetOrCreateSharedBuildProfile();
        }

        BuildProfile GetOrCreateClassicPlatformBuildProfile(string platformId)
        {
            if (m_PlatformIdToClassicPlatformProfile.TryGetValue(platformId, out var profile) && profile != null)
            {
                return profile;
            }

            // Migrate legacy build target indexed profiles to platform ID indexed profiles.
            var migratedProfile = MigrateLegacyIndexedClassicPlatformProfile(platformId);
            if (migratedProfile != null)
                return migratedProfile;

            // Platform profiles are not managed by the AssetDatabase.
            // We will manually handle serialization and deserialization of these objects.
            var buildProfile = BuildProfile.CreateInstance(new GUID(platformId));
            buildProfile.hideFlags = HideFlags.DontSave;

            m_PlatformIdToClassicPlatformProfile.Add(platformId, buildProfile);
            classicPlatformProfiles.Add(buildProfile);

            // Only copy after adding to the build target -> classic profiles dictionary, so EditorUserBuildSettings
            // can access the classic profiles when copying the settings
            EditorUserBuildSettings.CopyToBuildProfile(buildProfile);

            // Classic profile's shared settings should be populated by the existing shared profile
            if (sharedProfile != null)
            {
                var sharedSettings = sharedProfile.platformBuildProfile as SharedPlatformSettings;
                sharedSettings?.CopySharedSettingsToBuildProfile(buildProfile);
            }

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
            buildProfile.platformId = new GUID(string.Empty).ToString();
            buildProfile.platformBuildProfile = new SharedPlatformSettings();
            buildProfile.hideFlags = HideFlags.DontSave;

            sharedProfile = buildProfile;

            // Only copy after setting shared profile, so EditorUserBuildSettings can access the shared profile
            // when copying the settings.
            EditorUserBuildSettings.CopyToBuildProfile(buildProfile);

            SaveBuildProfileInProject(buildProfile);

            return buildProfile;
        }

        // TODO: Remove migration code (https://jira.unity3d.com/browse/PLAT-8909)
        BuildProfile MigrateLegacyIndexedClassicPlatformProfile(string platformId)
        {
            var (buildTarget, subtarget) = BuildProfileModuleUtil.GetBuildTargetAndSubtarget(platformId);
            var legacyKey = GetLegacyKey(buildTarget, subtarget);
            var legacyPath = GetLegacyFilePathForBuildProfile(legacyKey);

            if (!File.Exists(legacyPath))
                return null;

            var profileObj = InternalEditorUtility.LoadSerializedFileAndForget(legacyPath);
            if (profileObj == null || profileObj.Length == 0 || profileObj[0] is not BuildProfile profile)
            {
                Debug.LogWarning($"Failed to load build profile from {legacyPath}.");
                return null;
            }

            profile.platformId = platformId;
            m_PlatformIdToClassicPlatformProfile.Add(profile.platformId, profile);
            classicPlatformProfiles.Add(profile);

            SaveBuildProfileInProject(profile);
            File.Delete(legacyPath);

            return profile;
        }

        static (string, StandaloneBuildSubtarget) GetLegacyKey(BuildTarget buildTarget, StandaloneBuildSubtarget subtarget)
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

            string path = IsSharedProfile(profile.buildTarget) ? k_SharedProfilePath : GetFilePathForBuildProfile(profile.platformId);
            InternalEditorUtility.SaveToSerializedFileAndForget(new []{ profile }, path, allowTextSerialization: true);
        }

        static void CreateOrLoad()
        {
            // LoadSerializedFileAndForget will call non-static BuildProfileContext constructor
            var buildProfileContext = InternalEditorUtility.LoadSerializedFileAndForget(k_BuildProfileProviderAssetPath);
            if (buildProfileContext != null && buildProfileContext.Length > 0 && buildProfileContext[0] != null)
            {
                s_Instance = buildProfileContext[0] as BuildProfileContext;
                if (s_Instance == null)
                    Debug.LogError("BuildProfileContext asset exists but could not be loaded.");
            }
            else if (s_Instance == null)
            {
                s_Instance = CreateInstance<BuildProfileContext>();
                s_Instance.hideFlags = HideFlags.DontSave;
                Save();
            }

            System.Diagnostics.Debug.Assert(s_Instance != null);
            s_Instance.CheckInstalledBuildPlatforms();

            EditorUserBuildSettings.SetBuildProfilePath((s_Instance.m_ActiveProfile != null) ?
                AssetDatabase.GetAssetPath(s_Instance.m_ActiveProfile) : string.Empty);
            s_Instance.cachedEditorScriptingDefines = BuildDefines.GetBuildProfileScriptDefines();

            BuildProfileModuleUtil.DeleteLastRunnableBuildKeyForDeletedProfiles();
        }

        [RequiredByNativeCode, UsedImplicitly]
        static void SetActiveOrClassicProfileRawPlatformSetting(string settingName, string settingValue, BuildTarget target, StandaloneBuildSubtarget subtarget)
        {
            // If it is a shared setting, we will set the value in the active profile if the specified shared setting
            // is enabled in the active profile; Otherwise, we will set the value in the shared profile.
            if (IsSharedProfile(target))
            {
                var profile = GetActiveOrClassicBuildProfile(target, subtarget, settingName);
                profile?.platformBuildProfile.SetRawPlatformSetting(settingName, settingValue);
                return;
            }

            if (TryGetActiveOrClassicPlatformSettingsBase(target, subtarget, out BuildProfilePlatformSettingsBase platformProfile))
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
                string value = platformProfile?.GetRawPlatformSetting(settingName);
                return value != null ? value : string.Empty;
            }

            return string.Empty;
        }

        [RequiredByNativeCode]
        static string GetActiveBuildProfilePath()
        {
            if (instance.activeProfile)
                return AssetDatabase.GetAssetPath(instance.activeProfile);

            return string.Empty;
        }

        static bool ShouldReturnActiveProfile(BuildTarget buildTarget, StandaloneBuildSubtarget subtarget, string sharedSetting = null)
        {
            if (!string.IsNullOrEmpty(sharedSetting))
                return IsSharedSettingEnabledInActiveProfile(sharedSetting);

            var activeProfile = instance.activeProfile;
            if (activeProfile == null || buildTarget == BuildTarget.NoTarget)
                return false;

            var platformId = BuildProfileModuleUtil.GetPlatformId(buildTarget, subtarget);
            return platformId == activeProfile.platformId;
        }

        static bool IsSharedSettingEnabledInActiveProfile(string settingName)
        {
            var activeProfile = instance.activeProfile;
            if (activeProfile == null)
                return false;

            var platformSettingsBase = activeProfile.platformBuildProfile;
            if (platformSettingsBase == null)
                return false;

            return platformSettingsBase.IsSharedSettingEnabled(settingName);
        }

        static string GetFilePathForBuildProfile(string platformId) =>
            $"{k_BuildProfilePath}/PlatformProfile.{platformId}.asset";

        static string GetLegacyFilePathForBuildProfile((string moduleName, StandaloneBuildSubtarget subtarget) key) =>
            $"{k_BuildProfilePath}/PlatformProfile.{key.moduleName}.{key.subtarget}.asset";

        static void Save() => InternalEditorUtility.SaveToSerializedFileAndForget(new[] { instance },
            k_BuildProfileProviderAssetPath, true);

        internal static bool IsSharedProfile(BuildTarget target) => target == BuildTarget.NoTarget;
    }
}
