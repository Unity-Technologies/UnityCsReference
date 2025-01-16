// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using UnityEditor.Modules;
using UnityEditor.Rendering;
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
        string[] m_CachedEditorScriptingDefines = Array.Empty<string>();

        [SerializeField]
        List<string> m_LastRunnableBuildPathKeys = new();
        internal List<string> LastRunnableBuildPathKeys => m_LastRunnableBuildPathKeys;

        /// <summary>
        /// Cached mapping of platform ID to classic platform build profile.
        /// </summary>
        Dictionary<GUID, BuildProfile> m_PlatformIdToClassicPlatformProfile = new();

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
        internal static BuildProfile activeProfile
        {
            get
            {
                // Active Build profile may be deleted from the project.
                var activeProfile = EditorUserBuildSettings.activeBuildProfile;
                if (activeProfile != null && activeProfile.CanBuildLocally())
                    return activeProfile;

                return null;
            }

            set
            {
                var prev = EditorUserBuildSettings.activeBuildProfile;

                if (value == null || value.platformBuildProfile == null)
                {
                    prev?.UpdateGlobalManagerPlayerSettings(activeWillBeRemoved: true);
                    EditorUserBuildSettings.activeBuildProfile = null;

                    activeProfileChanged?.Invoke(prev, null);
                    OnActiveProfileChangedForSettingExtension(prev, null);
                    EditorGraphicsSettings.activeProfileHasGraphicsSettings = false;
                    BuildProfileModuleUtil.RequestScriptCompilation(null);
                    return;
                }

                // Only compare prev with value after the null check, as
                // EditorUserBuildSettings.activeBuildProfile will return null
                // if the build profile has been destroyed but on native side
                // it's still pointing to a dead pptr.
                if (ReferenceEquals(prev, value))
                    return;

                if (s_Instance != null && s_Instance.m_PlatformIdToClassicPlatformProfile.TryGetValue(
                    value.platformGuid, out var entry) && entry == value)
                {
                    Debug.LogWarning("[BuildProfile] Classic Platforms cannot be set as the active build profile.");
                    return;
                }

                EditorUserBuildSettings.activeBuildProfile = value;

                OnActiveProfileChangedForSettingExtension(prev, value);
                value.UpdateGlobalManagerPlayerSettings();
                activeProfileChanged?.Invoke(prev, value);
                EditorGraphicsSettings.activeProfileHasGraphicsSettings = ActiveProfileHasGraphicsSettings();
                BuildProfileModuleUtil.RequestScriptCompilation(value);
            }
        }

        [SerializeField]
        List<BuildProfilePackageAddInfo> m_PackageAddInfos = new();

        [VisibleToOtherModules]
        internal bool TryGetPackageAddInfo(BuildProfile profile, out BuildProfilePackageAddInfo result)
        {
            var profileGuid = GetProfileGUID(profile);
            foreach (var packageAddInfo in m_PackageAddInfos)
            {
                if (packageAddInfo.profileGuid == profileGuid)
                {
                    result = packageAddInfo;
                    return true;
                }
            }
            result = null;
            return false;
        }

        [VisibleToOtherModules]
        internal void AddPackageAddInfo(BuildProfile profile, string[] packagesToAdd, int preconfiguredSettingsVariant)
        {
            if ((packagesToAdd.Length == 0) && (preconfiguredSettingsVariant == BuildProfilePackageAddInfo.preconfiguredSettingsVariantNotSet))
                return;

            var profileGuid = GetProfileGUID(profile);
            var packageAddInfo = new BuildProfilePackageAddInfo()
            {
                profileGuid = profileGuid,
                packagesToAdd = packagesToAdd,
                preconfiguredSettingsVariant = preconfiguredSettingsVariant
            };
            m_PackageAddInfos.Add(packageAddInfo);
        }

        [VisibleToOtherModules]
        internal void ClearPackageAddInfo(BuildProfile profile)
        {
            if (TryGetPackageAddInfo(profile, out BuildProfilePackageAddInfo packageAddInfo))
            {
                m_PackageAddInfos.Remove(packageAddInfo);
            }
        }

        string GetProfileGUID(BuildProfile profile)
        {
            var profilePath = AssetDatabase.GetAssetPath(profile);
            var profileGuid = AssetDatabase.AssetPathToGUID(profilePath);
            return profileGuid;
        }

        static void OnActiveProfileChangedForSettingExtension(BuildProfile previous, BuildProfile newProfile)
        {
            var settingsExtension = ModuleManager.GetEditorSettingsExtension(EditorUserBuildSettings.activePlatformGuid);
            settingsExtension?.OnActiveProfileChanged(previous, newProfile);
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
        public static event Action<BuildProfile, BuildProfile> activeProfileChanged;

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

        // Note: this has to be a serializable type such as List<T>, so that
        // the references to classic build profiles survive domain reloads
        internal List<BuildProfile> classicPlatformProfiles
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
            var platformGuid = BuildProfileModuleUtil.GetPlatformId(target, subTarget);
            if (ShouldReturnActiveProfile(platformGuid, sharedSetting))
                return activeProfile;

            // For backwards compatibility, getter will look for
            // the classic platform build profile for the target platform
            // when no suitable active profile is found.
            return IsSharedProfile(platformGuid) ? instance.sharedProfile : instance.GetForClassicPlatform(platformGuid);
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

        internal BuildProfile GetForClassicPlatform(GUID platformGuid)
        {
            return m_PlatformIdToClassicPlatformProfile.GetValueOrDefault(platformGuid);
        }

        /// <summary>
        /// Get the classic platform profile for the specified build target and subtarget.
        /// This is needed because the server team decided to use this
        /// internal method in their package.
        /// </summary>
        [Obsolete("Do not use internal APIs from packages.")]
        internal BuildProfile GetForClassicPlatform(BuildTarget target, StandaloneBuildSubtarget subtarget)
        {
            if (!BuildProfileModuleUtil.IsStandalonePlatform(target))
                subtarget = StandaloneBuildSubtarget.Default;
            else if (subtarget == StandaloneBuildSubtarget.Default)
                subtarget = StandaloneBuildSubtarget.Player;

            var platformId = BuildProfileModuleUtil.GetPlatformId(target, subtarget);
            return GetForClassicPlatform(platformId);
        }

        [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
        internal static bool IsClassicPlatformProfile(BuildProfile profile)
        {
            if (instance.m_PlatformIdToClassicPlatformProfile.TryGetValue(profile.platformGuid, out var classicProfile))
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
        internal List<GUID> GetMissingKnownPlatformModules()
        {
            var missingPlatforms = new List<GUID>();
            var keys = BuildProfileModuleUtil.FindAllViewablePlatforms();
            for (var index = 0; index < keys.Count; index++)
            {
                var key = keys[index];

                if (m_PlatformIdToClassicPlatformProfile.ContainsKey(key))
                    continue;

                if (BuildProfileModuleUtil.IsPlatformVisibleInPlatformBrowserOnly(key))
                    continue;

                // Installed flag, as calculated by BuildPlatform
                if (BuildProfileModuleUtil.IsModuleInstalled(key))
                    continue;

                // Some build targets are only compatible with specific OS
                if (!BuildTargetDiscovery.BuildPlatformIsAvailableOnHostPlatform(key, SystemInfo.operatingSystemFamily))
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
        /// Check if the active build profile has graphics settings
        /// </summary>
        internal static bool ActiveProfileHasGraphicsSettings()
        {
            if (activeProfile == null)
                return false;

            return activeProfile.graphicsSettings != null;
        }

        /// <summary>
        /// Check if the active build profile has quality settings
        /// </summary>
        internal static bool ActiveProfileHasQualitySettings()
        {
            if (activeProfile == null)
                return false;

            return activeProfile.qualitySettings != null;
        }

        /// <summary>
        /// Sync the active build profile to EditorUserBuildSettings to ensure they are in a consistent state.
        /// </summary>
        void SyncActiveProfileToFallback()
        {
            if (!EditorUserBuildSettings.isBuildProfileAvailable)
                return;

            var buildProfile = activeProfile ?? GetForClassicPlatform(EditorUserBuildSettings.activePlatformGuid);

            if (buildProfile == null)
            {
                EditorUserBuildSettings.isBuildProfileAvailable = false;
                return;
            }

            EditorUserBuildSettings.CopyFromBuildProfile(buildProfile);

            var extension = ModuleManager.GetBuildProfileExtension(buildProfile.platformGuid);
            if (extension != null)
            {
                extension.CopyPlatformSettingsFromBuildProfile(buildProfile.platformBuildProfile);
            }

            EditorUserBuildSettings.isBuildProfileAvailable = false;
        }

        void OnDisable()
        {
            Save();

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

            if (classicPlatformProfiles == null)
                classicPlatformProfiles = new List<BuildProfile>();

            if (classicPlatformProfiles.Count > 0)
            {
                // classicPlatformProfiles survived the domain reload - just readd them to the classic profile map
                foreach (var profileObj in classicPlatformProfiles)
                    m_PlatformIdToClassicPlatformProfile.Add(profileObj.platformGuid, profileObj);
            }

            // Load platform build profiles from the Library folder.
            if (!Directory.Exists(k_BuildProfilePath))
                return;

            var viewablePlatformKeys = BuildProfileModuleUtil.FindAllViewablePlatforms();
            for (var index = 0; index < viewablePlatformKeys.Count; index++)
            {
                var key = viewablePlatformKeys[index];

                if (m_PlatformIdToClassicPlatformProfile.ContainsKey(key))
                    continue;

                if (!BuildProfileModuleUtil.IsModuleInstalled(key))
                    continue;

                if (BuildProfileModuleUtil.IsPlatformVisibleInPlatformBrowserOnly(key))
                    continue;

                string path = GetFilePathForBuildProfile(key);

                if (!File.Exists(path))
                {
                    GetOrCreateClassicPlatformBuildProfile(key);
                    continue;
                }

                var profile = InternalEditorUtility.LoadSerializedFileAndForget(path);
                if (profile == null || profile.Length == 0 || profile[0] is not BuildProfile profileObj)
                {
                    Debug.LogWarning($"Failed to load build profile from {path}.");
                    continue;
                }

                m_PlatformIdToClassicPlatformProfile.Add(profileObj.platformGuid, profileObj);
                classicPlatformProfiles.Add(profileObj);
            }

            if (sharedProfile == null)
            {
                if (!File.Exists(k_SharedProfilePath))
                    return;

                var sharedProfileArray = InternalEditorUtility.LoadSerializedFileAndForget(k_SharedProfilePath);
                if (sharedProfileArray == null || sharedProfileArray.Length == 0 || sharedProfileArray[0] is not BuildProfile sharedProfileObj)
                {
                    Debug.LogWarning($"Failed to load shared profile from {k_SharedProfilePath}.");
                    return;
                }

                sharedProfile = sharedProfileObj;
            }

            EditorGraphicsSettings.activeProfileHasGraphicsSettings = ActiveProfileHasGraphicsSettings();

            var buildProfile = activeProfile;

            if (buildProfile == null)
            {
                buildProfile = GetForClassicPlatform(EditorUserBuildSettings.activePlatformGuid);

                // profile can be null if we're in the middle of creating classic profiles
                if (buildProfile == null)
                    return;

                // We only copy EditorUserBuildSettings into the build profile for classic platforms as we don't want to modify actual user assets
                EditorUserBuildSettings.CopyToBuildProfile(buildProfile);
            }

            var extension = ModuleManager.GetBuildProfileExtension(buildProfile.platformGuid);
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

                if (BuildProfileModuleUtil.IsPlatformVisibleInPlatformBrowserOnly(key))
                    continue;

                if (!BuildProfileModuleUtil.IsModuleInstalled(key))
                    continue;

                if (ModuleManager.GetBuildProfileExtension(key) == null)
                {
                    // Require platform support and implemented build profile extension for the target platform.
                    var displayName = BuildTargetDiscovery.BuildPlatformDisplayName(key);
                    Debug.LogWarning($"Platform {displayName} does not support build profiles.");
                    continue;
                }

                GetOrCreateClassicPlatformBuildProfile(key);
            }

            GetOrCreateSharedBuildProfile();
        }

        BuildProfile GetOrCreateClassicPlatformBuildProfile(GUID platformId)
        {
            if (m_PlatformIdToClassicPlatformProfile.TryGetValue(platformId, out var profile) && profile != null)
            {
                return profile;
            }

            // Platform profiles are not managed by the AssetDatabase.
            // We will manually handle serialization and deserialization of these objects.
            var buildProfile = BuildProfile.CreateInstance(platformId);
            buildProfile.hideFlags = HideFlags.DontSave;

            m_PlatformIdToClassicPlatformProfile.Add(platformId, buildProfile);
            classicPlatformProfiles.Add(buildProfile);

            // Only copy after adding to the platform guid -> classic profiles dictionary, so EditorUserBuildSettings
            // can access the classic profiles when copying the settings
            EditorUserBuildSettings.CopyToBuildProfile(buildProfile);

            // Classic profile's shared settings should be populated by the existing shared profile
            if (sharedProfile != null)
            {
                var sharedSettings = sharedProfile.platformBuildProfile as SharedPlatformSettings;
                sharedSettings?.CopySharedSettingsToBuildProfile(buildProfile);
            }

            // Created profile can also be populated by settings on the managed side
            var extension = ModuleManager.GetBuildProfileExtension(buildProfile.platformGuid);
            if (extension != null)
            {
                extension.CopyPlatformSettingsToBuildProfile(buildProfile.platformBuildProfile);
            }
            else
            {
                var displayName = BuildTargetDiscovery.BuildPlatformDisplayName(buildProfile.platformGuid);
                Debug.LogError($"Build profile extension is null for platform {displayName} and build profile {buildProfile.name}.");
            }

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
            buildProfile.platformGuid = new GUID(string.Empty);
            buildProfile.platformBuildProfile = new SharedPlatformSettings();
            buildProfile.hideFlags = HideFlags.DontSave;

            sharedProfile = buildProfile;

            // Only copy after setting shared profile, so EditorUserBuildSettings can access the shared profile
            // when copying the settings.
            EditorUserBuildSettings.CopyToBuildProfile(buildProfile);

            return buildProfile;
        }

        static void SaveBuildProfileInProject(BuildProfile profile)
        {
            if (!Directory.Exists(k_BuildProfilePath))
            {
                Directory.CreateDirectory(k_BuildProfilePath);
            }

            string path = IsSharedProfile(profile.platformGuid) ? k_SharedProfilePath : GetFilePathForBuildProfile(profile.platformGuid);
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
            }

            System.Diagnostics.Debug.Assert(s_Instance != null);
            s_Instance.CheckInstalledBuildPlatforms();

            s_Instance.cachedEditorScriptingDefines = BuildDefines.GetBuildProfileScriptDefines();

            BuildProfileModuleUtil.DeleteLastRunnableBuildKeyForDeletedProfiles();

            OnActiveProfileChangedForSettingExtension(null, activeProfile);
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
        static void EnsureInitialized()
        {
            GC.KeepAlive(instance);
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

        [RequiredByNativeCode, UsedImplicitly]
        static bool SetActiveCommonGraphicsSetting(string settingName, int value)
        {
            if (!ActiveProfileHasGraphicsSettings())
                return false;

            activeProfile.graphicsSettings.SetGraphicsSetting(settingName, value);
            return true;
        }

        [RequiredByNativeCode, UsedImplicitly]
        static int GetActiveCommonGraphicsSetting(string settingName)
        {
            if (!ActiveProfileHasGraphicsSettings())
                return BuildProfileGraphicsSettings.k_InvalidGraphicsSetting;

            return activeProfile.graphicsSettings.GetGraphicsSetting(settingName);
        }

        [RequiredByNativeCode, UsedImplicitly]
        static bool SetActiveAlwaysIncludedShaders(Shader[] shaders)
        {
            if (!ActiveProfileHasGraphicsSettings())
                return false;

            activeProfile.graphicsSettings.alwaysIncludedShaders = shaders;
            return true;
        }

        [RequiredByNativeCode, UsedImplicitly]
        static Shader[] GetActiveAlwaysIncludedShaders()
        {
            if (!ActiveProfileHasGraphicsSettings())
                return null;

            return activeProfile.graphicsSettings.alwaysIncludedShaders;
        }

        [RequiredByNativeCode, UsedImplicitly]
        static ShaderVariantCollection[] GetActiveShaderVariantCollections()
        {
            if (!ActiveProfileHasGraphicsSettings())
                return null;

            return activeProfile?.graphicsSettings.preloadedShaders;
        }

        [RequiredByNativeCode, UsedImplicitly]
        static bool SetActiveShaderVariantCollections(ShaderVariantCollection[] collection)
        {
            if (!ActiveProfileHasGraphicsSettings())
                return false;

            activeProfile.graphicsSettings.preloadedShaders = collection;
            return true;
        }

        [RequiredByNativeCode, UsedImplicitly]
        static string[] GetActiveProfileQualityLevels()
        {
            if (!ActiveProfileHasQualitySettings())
                return Array.Empty<string>();

            return activeProfile.qualitySettings.qualityLevels;
        }

        [RequiredByNativeCode, UsedImplicitly]
        static string GetActiveProfileDefaultQualityLevel()
        {
            if (!ActiveProfileHasQualitySettings())
                return string.Empty;

            return activeProfile.qualitySettings.defaultQualityLevel;
        }

        [RequiredByNativeCode]
        static string GetActiveBuildProfilePath()
        {
            if (activeProfile)
                return AssetDatabase.GetAssetPath(activeProfile);

            return string.Empty;
        }

        [RequiredByNativeCode]
        static bool HasActiveProfileWithPlayerSettings(out int instanceID)
        {
            if (activeProfile?.playerSettings != null)
            {
                instanceID = activeProfile.GetInstanceID();
                return true;
            }

            instanceID = 0;
            return false;
        }

        [RequiredByNativeCode]
        static void UpdateActiveProfilePlayerSettingsObjectFromYAML()
        {
            activeProfile?.UpdatePlayerSettingsObjectFromYAML();
        }

        static bool ShouldReturnActiveProfile(GUID platformGuid, string sharedSetting = null)
        {
            if (!string.IsNullOrEmpty(sharedSetting))
                return IsSharedSettingEnabledInActiveProfile(sharedSetting);

            if (activeProfile == null || platformGuid.Empty())
                return false;

            return platformGuid == activeProfile.platformGuid;
        }

        static bool IsSharedSettingEnabledInActiveProfile(string settingName)
        {
            if (activeProfile == null)
                return false;

            var platformSettingsBase = activeProfile.platformBuildProfile;
            if (platformSettingsBase == null)
                return false;

            return platformSettingsBase.IsSharedSettingEnabled(settingName);
        }

        static string GetFilePathForBuildProfile(GUID platformId) =>
            $"{k_BuildProfilePath}/PlatformProfile.{platformId}.asset";

        static void Save() => InternalEditorUtility.SaveToSerializedFileAndForget(new[] { instance },
            k_BuildProfileProviderAssetPath, true);

        [VisibleToOtherModules]
        internal static bool IsSharedProfile(BuildTarget target) => target == BuildTarget.NoTarget;

        [VisibleToOtherModules]
        internal static bool IsSharedProfile(GUID platformGuid) => platformGuid.Empty();
    }
}
