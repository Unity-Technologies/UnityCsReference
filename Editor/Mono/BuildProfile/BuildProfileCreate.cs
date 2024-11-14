// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using JetBrains.Annotations;
using UnityEditor.Modules;
using UnityEngine;
using UnityEngine.Bindings;

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
            string moduleName = ModuleManager.GetTargetStringFrom(buildTarget);
            var buildProfile = CreateInstance<BuildProfile>();
            buildProfile.buildTarget = buildTarget;
            buildProfile.subtarget = subtarget;
            buildProfile.platformGuid = BuildProfileModuleUtil.GetPlatformId(buildTarget, subtarget);
            buildProfile.OnEnable();
            return buildProfile;
        }

        internal static BuildProfile CreateInstance(GUID platformId)
        {
            var (buildTarget, subtarget) = BuildProfileModuleUtil.GetBuildTargetAndSubtarget(platformId);
            var moduleName = BuildProfileModuleUtil.GetModuleName(buildTarget);
            var buildProfile = CreateInstance<BuildProfile>();
            buildProfile.buildTarget = buildTarget;
            buildProfile.subtarget = subtarget;
            buildProfile.platformGuid = platformId;
            buildProfile.OnEnable();
            return buildProfile;
        }

        /// <summary>
        /// Internal helper function for creating new build profile assets and invoking the onBuildProfileCreated
        /// event after an asset is created by AssetDatabase.CreateAsset.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
        internal static void CreateInstance(GUID platformId, string assetPath, int preconfiguredSettingsVariant = -1)
        {
            var (buildTarget, subtarget) = BuildProfileModuleUtil.GetBuildTargetAndSubtarget(platformId);
            var buildProfile = CreateInstance<BuildProfile>();
            buildProfile.buildTarget = buildTarget;
            buildProfile.subtarget = subtarget;
            buildProfile.platformGuid = platformId;
            AssetDatabase.CreateAsset(
                buildProfile,
                AssetDatabase.GenerateUniqueAssetPath(assetPath));
            buildProfile.OnEnable();
            buildProfile.NotifyBuildProfileExtensionOfCreation(preconfiguredSettingsVariant);
            // Notify the UI of creation so that the new build profile can be selected
            onBuildProfileCreated?.Invoke(buildProfile);
        }

        internal void NotifyBuildProfileExtensionOfCreation(int preconfiguredSettingsVariant)
        {
            var buildProfileExtension = BuildProfileModuleUtil.GetBuildProfileExtension(platformGuid);
            if (buildProfileExtension != null)
            {
                buildProfileExtension.OnBuildProfileCreated(this, preconfiguredSettingsVariant);
            }
        }

        void TryCreatePlatformSettings()
        {
            if (platformBuildProfile != null)
            {
                Debug.LogError("[BuildProfile] Platform settings already created.");
                return;
            }

            IBuildProfileExtension buildProfileExtension = ModuleManager.GetBuildProfileExtension(platformGuid);
            if (buildProfileExtension != null && ModuleManager.IsPlatformSupportLoaded(moduleName))
            {
                platformBuildProfile = buildProfileExtension.CreateBuildProfilePlatformSettings();
                EditorUtility.SetDirty(this);
            }
        }

        /// <summary>
        /// Add Graphics Settings overrides to the build profile.
        /// </summary>
        internal void CreateGraphicsSettings()
        {
            if (graphicsSettings != null)
                return;

            graphicsSettings = CreateInstance<BuildProfileGraphicsSettings>();
            graphicsSettings.Instantiate();
            AssetDatabase.AddObjectToAsset(graphicsSettings, this);
            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Remove the Graphics Settings overrides from the build profile.
        /// </summary>
        internal void RemoveGraphicsSettings()
        {
            if (graphicsSettings == null)
                return;

            AssetDatabase.RemoveObjectFromAsset(graphicsSettings);
            graphicsSettings = null;
            EditorUtility.SetDirty(this);

            OnGraphicsSettingsSubAssetRemoved?.Invoke();
        }

        /// <summary>
        /// Add Quality Settings overrides to the build profile.
        /// </summary>
        internal void CreateQualitySettings()
        {
            if (qualitySettings != null)
                return;

            qualitySettings = ScriptableObject.CreateInstance<BuildProfileQualitySettings>();
            qualitySettings.Instantiate();
            AssetDatabase.AddObjectToAsset(qualitySettings, this);
            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Remove the Quality Settings overrides from the build profile.
        /// </summary>
        internal void RemoveQualitySettings()
        {
            if (qualitySettings == null)
                return;

            AssetDatabase.RemoveObjectFromAsset(qualitySettings);
            qualitySettings = null;
            EditorUtility.SetDirty(this);

            OnQualitySettingsSubAssetRemoved?.Invoke();
        }
    }
}
