// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEditor.Modules;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Provides a set of configuration settings you can use to build your application on a particular platform.
    /// </summary>
    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [ExcludeFromObjectFactory]
    [ExcludeFromPreset]
    [HelpURL("build-profiles-reference")]
    public sealed partial class BuildProfile : ScriptableObject
    {
        /// <summary>
        /// Asset Schema Version
        /// </summary>
        [SerializeField]
        uint m_AssetVersion = 1;

        /// <summary>
        /// Build Target used to fetch module and build profile extension.
        /// </summary>
        [SerializeField] BuildTarget m_BuildTarget = BuildTarget.NoTarget;
        [VisibleToOtherModules]
        internal BuildTarget buildTarget
        {
            get => m_BuildTarget;
            set => m_BuildTarget = value;
        }

        /// <summary>
        /// Subtarget, Default for all non-Standalone platforms.
        /// </summary>
        [SerializeField] StandaloneBuildSubtarget m_Subtarget;
        [VisibleToOtherModules]
        internal StandaloneBuildSubtarget subtarget
        {
            get => m_Subtarget;
            set => m_Subtarget = value;
        }

        /// <summary>
        /// Platform ID of the build profile.
        /// Correspond to platform GUID in <see cref="BuildTargetDiscovery"/>
        /// </summary>
        [SerializeField] string m_PlatformId;
        [VisibleToOtherModules]
        internal GUID platformGuid
        {
            get => new GUID(m_PlatformId);
            set => m_PlatformId = value.ToString();
        }

        /// <summary>
        /// Platform ID of the build profile as string.
        /// This is needed because the server team decided to use this
        /// internal getter in their package.
        /// PLEASE DON'T USE, USE platformGuid INSTEAD!
        /// </summary>
        [VisibleToOtherModules]
        internal string platformId => m_PlatformId;

        /// <summary>
        /// Platform module specific build settings; e.g. AndroidBuildSettings.
        /// </summary>
        [SerializeReference] BuildProfilePlatformSettingsBase m_PlatformBuildProfile;
        [VisibleToOtherModules]
        internal BuildProfilePlatformSettingsBase platformBuildProfile
        {
            get => m_PlatformBuildProfile;
            set => m_PlatformBuildProfile = value;
        }

        /// <summary>
        /// When set, this build profiles <see cref="scenes"/> used when building.
        /// </summary>
        /// <seealso cref="EditorBuildSettings"/>
        [SerializeField] private bool m_OverrideGlobalSceneList = false;
        internal bool overrideGlobalSceneList
        {
            get => m_OverrideGlobalSceneList;
            set => m_OverrideGlobalSceneList = value;
        }

        /// <summary>
        /// List of scenes specified in the build profile.
        /// </summary>
        [SerializeField] private EditorBuildSettingsScene[] m_Scenes = Array.Empty<EditorBuildSettingsScene>();
        public EditorBuildSettingsScene[] scenes
        {
            get
            {
                CheckSceneListConsistency();
                return m_Scenes;
            }
            set
            {
                if (m_Scenes == value)
                    return;

                m_Scenes = value;
                CheckSceneListConsistency();

                if (this == BuildProfileContext.activeProfile && m_OverrideGlobalSceneList)
                    EditorBuildSettings.SceneListChanged();
            }
        }

        /// <summary>
        /// Scripting Compilation Defines used during player and editor builds.
        /// </summary>
        [SerializeField] private string[] m_ScriptingDefines = Array.Empty<string>();
        public string[] scriptingDefines
        {
            get => m_ScriptingDefines;
            set => m_ScriptingDefines = value;
        }

        [VisibleToOtherModules]
        internal Action OnPackageAddProgress;

        [SerializeField]
        PlayerSettingsYaml m_PlayerSettingsYaml = new();

        PlayerSettings m_PlayerSettings;
        [VisibleToOtherModules]
        internal PlayerSettings playerSettings
        {
            get { return m_PlayerSettings; }

            set { m_PlayerSettings = value; }
        }

        [VisibleToOtherModules]
        internal Action OnPlayerSettingsUpdatedFromYAML;

        /// <summary>
        /// Cross-pipeline graphics settings overrides in build profile
        /// </summary>
        [VisibleToOtherModules]
        internal BuildProfileGraphicsSettings graphicsSettings;

        [VisibleToOtherModules]
        internal Action OnGraphicsSettingsSubAssetRemoved;

        /// <summary>
        /// Quality settings overrides in build profile
        /// </summary>
        [VisibleToOtherModules]
        internal BuildProfileQualitySettings qualitySettings;

        [VisibleToOtherModules]
        internal Action OnQualitySettingsSubAssetRemoved;

        // TODO: Return server IBuildTargets for server build profiles. (https://jira.unity3d.com/browse/PLAT-6612)
        /// <summary>
        /// Get the IBuildTarget of the build profile.
        /// </summary>
        internal IBuildTarget GetIBuildTarget() => ModuleManager.GetIBuildTarget(platformGuid);

        /// <summary>
        /// Returns true if the given <see cref="BuildProfile"/> is the active profile or a classic
        /// profile for the EditorUserBuildSettings active build target.
        /// </summary>
        [VisibleToOtherModules]
        internal bool IsActiveBuildProfileOrPlatform()
        {
            if (BuildProfileContext.activeProfile == this)
                return true;

            if (BuildProfileContext.activeProfile is not null
                || !BuildProfileContext.IsClassicPlatformProfile(this))
                return false;

            return platformGuid == EditorUserBuildSettings.activePlatformGuid;
        }

        [VisibleToOtherModules]
        internal bool CanBuildLocally()
        {
            // Note: If the build profile is still being configured (package add info is present)
            // we do not want it to be buildable.
            if (BuildProfileContext.instance.TryGetPackageAddInfo(this, out _))
                return false;
            // Note: A platform build profile may have a non-null value even if its module is not installed.
            // This scenario is true for server platform profiles, which are the same type as the standalone one.
            return platformBuildProfile != null && BuildProfileModuleUtil.IsModuleInstalled(platformGuid);
        }

        internal string GetLastRunnableBuildPathKey()
        {
            if (platformBuildProfile == null)
                return string.Empty;

            var key = platformBuildProfile.GetLastRunnableBuildPathKey();
            if (string.IsNullOrEmpty(key) || BuildProfileContext.IsClassicPlatformProfile(this))
                return key;

            string assetPath = AssetDatabase.GetAssetPath(this);
            return BuildProfileModuleUtil.GetLastRunnableBuildKeyFromAssetPath(assetPath, key);
        }

        /// <summary>
        /// Duplicate the build profile. Note this does not create a new asset.
        /// </summary>
        [VisibleToOtherModules]
        internal BuildProfile Duplicate()
        {
            var duplicatedProfile = Instantiate(this);

            if (graphicsSettings != null)
                duplicatedProfile.graphicsSettings = Instantiate(graphicsSettings);

            if (qualitySettings != null)
                duplicatedProfile.qualitySettings = Instantiate(qualitySettings);

            return duplicatedProfile;
        }

        void OnEnable()
        {
            ValidateDataConsistency();

            // Check if the platform support module has been installed,
            // and try to set an uninitialized platform settings.
            if (platformBuildProfile == null)
                TryCreatePlatformSettings();

            onBuildProfileEnable?.Invoke(this);
            LoadPlayerSettings();

            TryLoadGraphicsSettings();
            TryLoadQualitySettings();

            if (BuildProfileContext.instance.TryGetPackageAddInfo(this, out var packageAddInfo))
            {
                packageAddInfo.OnPackageAddProgress = () =>
                {
                    OnPackageAddProgress?.Invoke();
                };
                packageAddInfo.OnPackageAddComplete = () =>
                {
                    NotifyBuildProfileExtensionOfCreation(packageAddInfo.preconfiguredSettingsVariant);
                };
                packageAddInfo.RequestPackageInstallation();
            }

            if (!EditorUserBuildSettings.isBuildProfileAvailable
                || BuildProfileContext.activeProfile != this)
                return;

            // On disk changes invoke OnEnable,
            // Check against the last observed editor defines.
            string[] lastCompiledDefines = BuildProfileContext.instance.cachedEditorScriptingDefines;
            if (ArrayUtility.ArrayEquals(m_ScriptingDefines, lastCompiledDefines))
            {
                return;
            }
            BuildProfileContext.instance.cachedEditorScriptingDefines = m_ScriptingDefines;
            BuildProfileModuleUtil.RequestScriptCompilation(this);
        }

        void TryLoadGraphicsSettings()
        {
            if (graphicsSettings != null)
                return;

            var path = AssetDatabase.GetAssetPath(this);
            var objects = AssetDatabase.LoadAllAssetsAtPath(path);

            var data = Array.Find(objects, obj => obj is BuildProfileGraphicsSettings) as BuildProfileGraphicsSettings;
            if (data == null)
                return;

            graphicsSettings = data;
        }

        void TryLoadQualitySettings()
        {
            if (qualitySettings != null)
                return;

            var path = AssetDatabase.GetAssetPath(this);
            var objects = AssetDatabase.LoadAllAssetsAtPath(path);

            var data = Array.Find(objects, obj => obj is BuildProfileQualitySettings) as BuildProfileQualitySettings;
            if (data == null)
                return;

            qualitySettings = data;
        }

        void OnDisable()
        {
            if (BuildProfileContext.activeProfile == this)
                EditorUserBuildSettings.SetActiveProfileScriptingDefines(m_ScriptingDefines);

            var playerSettingsDirty = EditorUtility.IsDirty(m_PlayerSettings);
            if (playerSettingsDirty)
            {
                BuildProfileModuleUtil.SerializePlayerSettings(this);
                EditorUtility.SetDirty(this);
            }

            // OnDisable is called when entering play mode, during domain reloads, or when the object is destroyed.
            // Avoid removing player settings for the first two cases to prevent slow syncs (e.g., color space) caused by global manager updates.
            if (!EditorApplication.isUpdating)
            {
                RemovePlayerSettings();
            }
        }

        [MenuItem("CONTEXT/BuildProfile/Reset", false)]
        static void ContextMenuReset(MenuCommand menuCommand)
        {
            var targetBuildProfile = (BuildProfile) menuCommand.context;
            if (targetBuildProfile == null)
                return;

            targetBuildProfile.platformBuildProfile = null;
            targetBuildProfile.TryCreatePlatformSettings();
            targetBuildProfile.scenes = Array.Empty<EditorBuildSettingsScene>();
            targetBuildProfile.scriptingDefines = Array.Empty<string>();

            BuildProfileModuleUtil.RemovePlayerSettings(targetBuildProfile);
            targetBuildProfile.RemoveQualitySettings();
            targetBuildProfile.RemoveGraphicsSettings();

            AssetDatabase.SaveAssetIfDirty(targetBuildProfile);
        }

        void ValidateDataConsistency()
        {
            // Keep serialized platform GUID in sync with serialized build target and subtarget.
            if (platformGuid.Empty())
            {
                platformGuid = BuildProfileContext.IsSharedProfile(buildTarget) ?
                    new GUID(string.Empty) : BuildProfileModuleUtil.GetPlatformId(buildTarget, subtarget);
                EditorUtility.SetDirty(this);
            }
            else
            {
                var (curBuildTarget, curSubtarget) = BuildProfileModuleUtil.GetBuildTargetAndSubtarget(platformGuid);
                if (buildTarget != curBuildTarget || subtarget != curSubtarget)
                {
                    buildTarget = curBuildTarget;
                    subtarget = curSubtarget;
                    EditorUtility.SetDirty(this);
                }
            }

            CheckSceneListConsistency();
        }

        /// <summary>
        /// EditorBuildSettingsScene stores both path and GUID. Path can become
        /// invalid when scenes are moved or renamed and must be recalculated.
        /// </summary>
        /// <see cref="EditorBuildSettings"/> native function, EnsureScenesAreValid.
        void CheckSceneListConsistency()
        {
            int length = m_Scenes.Length;
            for (int i = length - 1; i >= 0; i--)
            {
                var scene = m_Scenes[i];

                // EditorBuildSettingScene entry may be null.
                if (scene == null)
                {
                    RemoveAt(i);
                    continue;
                }

                bool isGuidValid = !scene.guid.Empty();
                if (isGuidValid)
                {
                    // Scene may have been moved or renamed.
                    scene.path = AssetDatabase.GUIDToAssetPath(scene.guid);
                }


                // Asset may have been deleted.
                // AssetDatabase may cache GUID to/from path mapping.
                if (string.IsNullOrEmpty(scene.path) || !AssetDatabase.AssetPathExists(scene.path))
                {
                    // Scene Object may have been deleted from disk.
                    RemoveAt(i);
                    continue;
                }

                if (!isGuidValid)
                    scene.guid = AssetDatabase.GUIDFromAssetPath(scene.path);
            }

            if (length == m_Scenes.Length)
                return;

            var result = new EditorBuildSettingsScene[length];
            Array.Copy(m_Scenes, result, length);
            m_Scenes = result;
            EditorUtility.SetDirty(this);
            return;

            void RemoveAt(int index)
            {
                length--;
                Array.Copy(m_Scenes, index + 1, m_Scenes, index, length - index);
            }
        }
    }
}
