// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using UnityEditor.Modules;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Events;
using UnityEngine.Profiling;

namespace UnityEditor.Build.Profile
{
    public sealed partial class BuildProfile
    {
        [UsedImplicitly]
        internal static event Action<BuildProfile> onBuildProfileEnable;
        [VisibleToOtherModules]
        internal static void AddOnBuildProfileEnable(Action<BuildProfile> action) => onBuildProfileEnable += action;
        [VisibleToOtherModules]
        internal static void RemoveOnBuildProfileEnable(Action<BuildProfile> action) => onBuildProfileEnable -= action;

        // This callback is of use when a build profile is created via AssetDatabase, and we need to notify the UI
        // and select the newly created profile in the listview.
        [UsedImplicitly]
        internal static event Action<BuildProfile> onBuildProfileCreated;
        [VisibleToOtherModules]
        internal static void AddOnBuildProfileCreated(Action<BuildProfile> action) => onBuildProfileCreated += action;
        [VisibleToOtherModules]
        internal static void RemoveOnBuildProfileCreated(Action<BuildProfile> action) => onBuildProfileCreated -= action;

        internal static BuildProfile CreateInstance(BuildTarget buildTarget, StandaloneBuildSubtarget subtarget)
        {
            var platformGuid = BuildProfileModuleUtil.GetPlatformId(buildTarget, subtarget);
            ValidatePlatformExists(platformGuid);

            var buildProfile = CreateInstance<BuildProfile>();
            buildProfile.buildTarget = buildTarget;
            buildProfile.subtarget = subtarget;
            buildProfile.platformGuid = platformGuid;
            buildProfile.OnEnable();
            return buildProfile;
        }

        internal static BuildProfile CreateInstance(GUID platformId)
        {
            ValidatePlatformExists(platformId);

            var (buildTarget, subtarget) = BuildProfileModuleUtil.GetBuildTargetAndSubtarget(platformId);
            var buildProfile = CreateInstance<BuildProfile>();
            buildProfile.buildTarget = buildTarget;
            buildProfile.subtarget = subtarget;
            buildProfile.platformGuid = platformId;
            buildProfile.OnEnable();
            return buildProfile;
        }

        /// <summary>
        /// Creates a new build profile asset for the specified platform with the provided name. Created asset will be
        /// placed under "Assets/Settings/Build Profiles".
        /// </summary>
        /// <param name="platformId">The GUID of the target platform. Must be a valid installed editor platform.</param>
        /// <param name="profileName">The name for the build profile. Used as the asset filename.</param>
        /// <param name="onProfileReady">Optional callback invoked when the profile has completed
        /// initialization and is ready to use. For the callback to survive domain reloads, it must be
        /// a static method or a method on a serialized UnityEngine.Object.
        /// Non-persistent callbacks will not be invoked if a domain reload occurs during initialization.</param>
        /// <returns>
        /// The newly created <see cref="BuildProfile"/> instance. The profile may require initialization if
        /// platform packages need to be installed.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="platformId"/> is cannot be found
        /// as an installed platform.</exception>
        /// /// <remarks>
        /// <para>
        /// This method automatically installs required platform packages if they are not already installed.
        /// Package installation happens asynchronously.
        /// </para>
        /// <para>
        /// The profile will be created at: Assets/Settings/Build Profiles/{profileName}.asset
        /// If a profile with the same name exists, a unique name will be generated.
        /// </para>
        /// <para>
        /// If required, package installation begins immediately and cannot be cancelled.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// //Static method example - will continue after a domain reload
        /// var buildProfile = BuildProfile.CreateBuildProfile(
        ///     androidGuid,
        ///     "My Profile",
        ///     OnProfileReadyStatic);
        ///
        /// static void OnProfileReadyStatic(BuildProfile profile)
        /// {
        ///     Debug.Log($"Profile {profile.name} ready!");
        /// }
        /// </code>
        /// </example>
        public static BuildProfile CreateBuildProfile(
            GUID platformId,
            string profileName,
            UnityAction<BuildProfile> onProfileReady = null)
        {
            // Ensure the base platform is installed. Derived platforms will
            // be automatically loaded during profile initialization.
            bool isPlatformInstalled = false;
            GUID basePlatformGUID = BuildTargetDiscovery.GetBasePlatformGUID(platformId);
            var installedPlatforms = BuildProfile.GetInstalledPlatformModules();
            foreach (var platform in installedPlatforms)
            {
                if (basePlatformGUID == platform.platformGuid)
                {
                    isPlatformInstalled = true;
                    break;
                }
            }

            if (!isPlatformInstalled)
            {
                var platformName = BuildTargetDiscovery.BuildPlatformDisplayName(platformId);
                throw new ArgumentException(
                    $"Cannot create build profile for '{platformName}' (GUID: {platformId}). "
                    + $"This platform is not installed in the editor. ");
            }

            BuildProfileModuleUtil.EnsureCustomBuildProfileFolderExists();
            string assetPath = BuildProfileModuleUtil.GetProfilePathWithProvidedName(platformId, profileName);
            var packagesToInstall = BuildTargetDiscovery.GetAllMissingRequiredPlatformPackageNames(platformId);

            return CreateInstance(platformId, assetPath, -1, packagesToInstall, onProfileReady);
        }

        /// <summary>
        /// Internal helper function for creating new build profile assets and invoking the onBuildProfileCreated
        /// event after an asset is created by AssetDatabase.CreateAsset.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
        internal static BuildProfile CreateInstance(GUID platformId, string assetPath)
        {
            return CreateInstance(platformId, assetPath, -1, Array.Empty<string>());
        }

        [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
        internal static BuildProfile CreateInstance(
            GUID platformId,
            string assetPath,
            int preconfiguredSettingsVariant,
            string[] packagesToAdd,
            UnityAction<BuildProfile> onProfileReady = null)
        {
            ValidateFileNameLength(assetPath);
            ValidatePlatformExists(platformId);

            var (buildTarget, subtarget) = BuildProfileModuleUtil.GetBuildTargetAndSubtarget(platformId);
            var buildProfile = CreateInstance<BuildProfile>();
            buildProfile.buildTarget = buildTarget;
            buildProfile.subtarget = subtarget;
            buildProfile.platformGuid = platformId;
            AssetDatabase.CreateAsset(
                buildProfile,
                BuildProfileModuleUtil.GetUniqueBuildProfilePath(assetPath));
            BuildProfileContext.instance.RegisterProfileAwaitingInitialization(
                buildProfile, packagesToAdd, preconfiguredSettingsVariant, onProfileReady);

            // OnEnable must be called after CreateAsset so that serialized fields are properly initialized.
            buildProfile.OnEnable();

            if (BuildTargetDiscovery.TryGetSDKPlatformExtension(platformId, out var sdkExtension))
                sdkExtension.OnMultiTargetBuildProfileCreated(buildProfile);

            // Notify the UI of creation so that the new build profile can be selected
            onBuildProfileCreated?.Invoke(buildProfile);
            return buildProfile;
        }

        /// <summary>
        /// Validates that the platform GUID corresponds to a known Unity platform.
        /// Does NOT check if the platform module is installed.
        /// </summary>
        static void ValidatePlatformExists(GUID platformGuid)
        {
            foreach (var guid in BuildTargetDiscovery.GetAllPlatforms())
            {
                if (guid == platformGuid)
                    return;
            }

            throw new ArgumentException(
                $"Platform GUID {platformGuid} is not a valid Unity build platform.");
        }

        /// <summary>
        /// Validates if the provided path name length is supported by the Asset database.
        /// Throws an ArgumentException if the platform is not valid.
        /// </summary>
        /// <param name="assetPath">The path to the build profile to be created.</param>
        static void ValidateFileNameLength(string assetPath)
        {
            var byteCount = System.Text.Encoding.UTF8.GetByteCount(Path.GetFileName(assetPath));
            // File name length is limited by the asset database
            if (byteCount > BuildProfileModuleUtil.k_MaxAssetFileNameLength)
                throw new ArgumentException($"Build profile name is too long ({byteCount}) - max supported is {BuildProfileModuleUtil.k_MaxAssetFileNameLength} bytes.");
        }

        internal void NotifyBuildProfileExtensionOfCreation(int preconfiguredSettingsVariant)
        {
            var buildProfileExtension = BuildProfileModuleUtil.GetBuildProfileExtension(platformGuid);
            if (buildProfileExtension != null)
            {
                buildProfileExtension.OnBuildProfileCreated(this, preconfiguredSettingsVariant);
                SerializePlayerSettings();
                AssetDatabase.SaveAssetIfDirty(this);
            }
        }

        void TryCreatePlatformSettings()
        {
            if (platformBuildProfile != null)
            {
                Debug.LogError("[BuildProfile] Platform settings already created.");
                return;
            }

            if (TryGetSupportedIBuildTargets(out var supportedTargets))
            {
                var platformSettings = new List<AdditionalPlatformSettingsData>();
                foreach (var target in supportedTargets)
                {
                    var guid = target.Guid;
                    IBuildProfileExtension extension = ModuleManager.GetBuildProfileExtension(guid);
                    if (extension == null)
                        continue;

                    platformSettings.Add(new AdditionalPlatformSettingsData
                        { platformGuid = guid, platformSettings = extension.CreateBuildProfilePlatformSettings() });
                }

                if (platformSettings.Count == 0)
                    return;

                additionalPlatformBuildSettings = platformSettings.ToArray();
                platformBuildProfile = additionalPlatformBuildSettings[0].platformSettings;
                EditorUtility.SetDirty(this);
                return;
            }

            IBuildProfileExtension buildProfileExtension = ModuleManager.GetBuildProfileExtension(platformGuid);
            if (buildProfileExtension != null && ModuleManager.IsPlatformSupportLoadedByGuid(platformGuid))
            {
                platformBuildProfile = buildProfileExtension.CreateBuildProfilePlatformSettings();
                EditorUtility.SetDirty(this);
            }
        }

        /// <summary>
        /// For multi-target platform profiles, checks if there are any installed supported targets
        /// that do not have platform settings created yet, and creates them if necessary.
        /// </summary>
        void TryCreateAdditionalPlatformSettings()
        {
            if (!TryGetSupportedIBuildTargets(out var supportedTargets))
                return;

            var newSettings = new List<AdditionalPlatformSettingsData>();
            foreach (var target in supportedTargets)
            {
                var guid = target.Guid;
                if (HasPlatformSettings(guid))
                    continue;

                IBuildProfileExtension extension = ModuleManager.GetBuildProfileExtension(guid);
                if (extension == null)
                    continue;

                newSettings.Add(new AdditionalPlatformSettingsData
                {
                    platformGuid = guid,
                    platformSettings = extension.CreateBuildProfilePlatformSettings()
                });
            }

            if (newSettings.Count == 0)
                return;

            var startIndex = m_AdditionalPlatformBuildSettings.Length;
            Array.Resize(ref m_AdditionalPlatformBuildSettings, startIndex + newSettings.Count);
            for (int i = 0; i < newSettings.Count; i++)
                m_AdditionalPlatformBuildSettings[startIndex + i] = newSettings[i];

            EditorUtility.SetDirty(this);

            bool HasPlatformSettings(GUID guid)
            {
                foreach (var setting in m_AdditionalPlatformBuildSettings)
                    if (setting.platformGuid == guid) return true;
                return false;
            }
        }
    }
}
