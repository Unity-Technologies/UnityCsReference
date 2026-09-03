// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEditor.Modules;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("UnityEditor.Rendering.ShaderBuildSettings.Tests")]
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
        uint m_AssetVersion = 3;

        /// <summary>
        /// Build Target used to fetch module and build profile extension.
        /// </summary>
        [SerializeField] BuildTarget m_BuildTarget = BuildTarget.NoTarget;
        [VisibleToOtherModules]
        internal BuildTarget buildTarget
        {
            get
            {
                if (!isMultiTarget)
                    return m_BuildTarget;

                var (buildTargetFromGuid, _) = BuildProfileModuleUtil.GetBuildTargetAndSubtarget(selectedPlatformGuid);
                return buildTargetFromGuid;
            }
            set => m_BuildTarget = value;
        }

        /// <summary>
        /// Subtarget, Default for all non-Standalone platforms.
        /// </summary>
        [SerializeField] StandaloneBuildSubtarget m_Subtarget;
        [VisibleToOtherModules]
        internal StandaloneBuildSubtarget subtarget
        {
            get
            {
                if (!isMultiTarget)
                    return m_Subtarget;

                var (_, subtargetFromGuid) = BuildProfileModuleUtil.GetBuildTargetAndSubtarget(selectedPlatformGuid);
                return subtargetFromGuid;
            }
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
        /// Boolean flag for if this profile is targeting a multi-target platform.
        /// </summary>
        [VisibleToOtherModules]
        internal bool isMultiTarget { get; set; }

        /// <summary>
        /// Selected platform GUID of a multi-target platform profile. Determines which
        /// platform settings to use for building and to show in the inspector.
        /// Set at profile creation time.
        /// </summary>
        [SerializeField] string m_SelectedPlatformGuid;
        [VisibleToOtherModules]
        internal GUID selectedPlatformGuid
        {
            get => new GUID(m_SelectedPlatformGuid);
            set => m_SelectedPlatformGuid = value.ToString();
        }

        /// <summary>
        /// Selected build target of the build profile. This is used for showing
        /// platform settings in the inspector. 
        /// Returns the selected build target for multi-target platform profiles, or
        /// the build target for non-multi-target profiles.
        /// </summary>
        [VisibleToOtherModules]
        internal BuildTarget selectedBuildTarget => buildTarget;

        /// <summary>
        /// Selected subtarget of the build profile. This is used for showing
        /// platform settings in the inspector. 
        /// Returns the selected subtarget for multi-target platform profiles, or
        /// the subtarget for non-multi-target profiles.
        /// </summary>
        [VisibleToOtherModules]
        internal StandaloneBuildSubtarget selectedSubtarget => subtarget;

        /// <summary>
        /// Boolean flag for overriding global scene list.
        /// When true, the scene list <see cref="scenes"/> in the build profile is used
        /// when building. Otherwise, the global scene list is used.
        /// </summary>
        /// <seealso cref="EditorBuildSettings"/>
        [SerializeField] private bool m_OverrideGlobalSceneList = false;
        public bool overrideGlobalScenes
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

                if (this == BuildProfileContext.activeProfile && overrideGlobalScenes)
                    EditorBuildSettings.SceneListChanged();
            }
        }

        [SerializeField] private bool m_HasScriptingDefines = false;
        [VisibleToOtherModules]
        internal bool hasScriptingDefines
        {
            get => m_HasScriptingDefines;
            set => m_HasScriptingDefines = value;
        }

        /// <summary>
        /// Scripting Compilation Defines used during player and editor builds.
        /// </summary>
        [SerializeField] private string[] m_ScriptingDefines = Array.Empty<string>();
        public string[] scriptingDefines
        {
            get => m_ScriptingDefines;
            set => SetAndApplyScriptingDefines(value);
        }

        /// <summary>
        /// Internal method for setting Scripting Defines. Cleans, applies and reloads.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.BuildProfileModule")] 
        internal void SetAndApplyScriptingDefines(string[] defines)
        {
            var cleanedValue = BuildProfileModuleUtil.RemoveInvalidScriptingDefines(defines);

            if (!ArrayUtility.ArrayEquals(m_ScriptingDefines, cleanedValue))
            {
                m_ScriptingDefines = cleanedValue;
                EditorUtility.SetDirty(this);
            }

            if (IsActiveBuildProfileOrPlatform())
            {
                var lastCompiled = BuildProfileContext.instance.cachedEditorScriptingDefines;
                
                // Reload when actual changes happen, after cleaning has occurred
                if (!ArrayUtility.ArrayEquals(m_ScriptingDefines, lastCompiled))
                {
                    BuildProfileModuleUtil.RequestScriptCompilation(this);
                }
            }
        }

        [VisibleToOtherModules]
        internal Action OnPackageAddProgress;
        [VisibleToOtherModules]
        internal Action OnPackageAddComplete;

        [SerializeField]
        PlayerSettingsYaml m_PlayerSettingsYaml = new();

        PlayerSettings m_PlayerSettings;
        [VisibleToOtherModules]
        internal PlayerSettings playerSettings
        {
            get { return m_PlayerSettings; }

            set { m_PlayerSettings = value; }
        }

        /// <summary>
        /// Cross-pipeline graphics settings overrides in build profile
        /// </summary>
        [VisibleToOtherModules]
        internal BuildProfileGraphicsSettings graphicsSettings;

        /// <summary>
        /// Quality settings overrides in build profile
        /// </summary>
        [VisibleToOtherModules]
        internal BuildProfileQualitySettings qualitySettings;

        /// <summary>
        /// Player settings overrides in build profile, stored as a subasset
        /// </summary>
        [VisibleToOtherModules]
        internal BuildProfilePlayerSettings buildProfilePlayerSettings;

        /// <summary>
        /// Required components to appear in the build profile.
        /// </summary>
        [VisibleToOtherModules]
        [HideInInspector]
        [SerializeReference]
        internal ScriptableObject[] requiredComponents = [];

        // TODO: Return server IBuildTargets for server build profiles. (https://jira.unity3d.com/browse/PLAT-6612)
        /// <summary>
        /// Get the IBuildTarget of the build profile.
        /// </summary>
        internal IBuildTarget GetIBuildTarget() => ModuleManager.GetIBuildTarget(platformGuid);

        /// <summary>
        /// Get the list of installed supported IBuildTarget for a multi-target build profile.
        /// </summary>
        [VisibleToOtherModules]
        internal bool TryGetSupportedIBuildTargets(out IBuildTarget[] supportedTargets)
        {
            if (BuildTargetDiscovery.TryGetSDKPlatformExtension(platformGuid, out var sdkExtension) && 
                sdkExtension.sdkPlatformBuildTarget is ConfigurableMultiTargetBuildTarget multiTargetBuildTarget)
            {
                if (multiTargetBuildTarget.availableBuildTargets.Length != 0)
                {
                    supportedTargets = multiTargetBuildTarget.availableBuildTargets;
                    return true;
                }
            }

            supportedTargets = Array.Empty<IBuildTarget>();
            return false;
        }

        /// <summary>
        /// Get the list of scenes that is used when building with the build profile.
        /// </summary>
        /// <returns>
        /// Returns the build profile's scene list when it's overriding global scenes. Otherwise,
        /// returns the global scene list.
        /// </returns>
        public EditorBuildSettingsScene[] GetScenesForBuild()
        {
            if (overrideGlobalScenes)
                return scenes;

            return EditorBuildSettings.globalScenes;
        }

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
            if (BuildProfileContext.instance.TryGetInitializationInfo(this, out _))
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

        [VisibleToOtherModules]
        internal void ResetToGlobalQualitySettingsValues()
        {
            var namedBuildTargetName = NamedBuildTarget.FromTargetAndSubtarget(buildTarget, (int)subtarget).TargetName;
            var globalQualityLevels = QualitySettings.GetActiveQualityLevelsForPlatform(namedBuildTargetName);

            var newBuildProfileQualityLevels = new string[globalQualityLevels.Length];

            // populates newBuildProfileQualityLevels array with global quality levels in their existing order
            for (int i = 0; i < globalQualityLevels.Length; i++)
            {
                newBuildProfileQualityLevels[i] = QualitySettings.names[globalQualityLevels[i]];
            }

            var globalDefaultQualityLevelIndex = QualitySettings.GetDefaultQualityForPlatform(namedBuildTargetName);
            if (globalDefaultQualityLevelIndex != -1)
                qualitySettings.defaultQualityLevel = QualitySettings.names[globalDefaultQualityLevelIndex];
            else
                qualitySettings.defaultQualityLevel = newBuildProfileQualityLevels.Length > 0 ? QualitySettings.names[globalQualityLevels[0]] : string.Empty;

            qualitySettings.qualityLevels = newBuildProfileQualityLevels;

            EditorUtility.SetDirty(qualitySettings);
        }

        void OnEnable()
        {
            isMultiTarget = BuildTargetDiscovery.BuildPlatformIsMultiTargetPlatform(platformGuid);

            ValidateDataConsistency();

            // Check if the platform support module has been installed,
            // and try to set an uninitialized platform settings.
            if (platformBuildProfile == null)
                TryCreatePlatformSettings();

            onBuildProfileEnable?.Invoke(this);
            TryLoadBuildProfilePlayerSettings();
            TryLoadGraphicsSettings();
            TryLoadQualitySettings();

            BuildProfileContext.instance.UpdateBuildProfileInitialization(this);

            if (!EditorUserBuildSettings.isBuildProfileAvailable
                || BuildProfileContext.activeProfile != this)
                return;

            // On disk changes invoke OnEnable,
            // Check against the last observed editor defines.
            string[] lastCompiledDefines = BuildProfileContext.instance.cachedEditorScriptingDefines;
            m_ScriptingDefines = BuildProfileModuleUtil.RemoveInvalidScriptingDefines(m_ScriptingDefines);
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

        internal void AttachBuildProfilePlayerSettingsSubasset()
        {
            if (this == null || buildProfilePlayerSettings == null)
                return;
            if (!EditorUtility.IsPersistent(this) || AssetDatabase.Contains(buildProfilePlayerSettings))
                return;

            AssetDatabase.AddObjectToAsset(buildProfilePlayerSettings, this);
            m_PlayerSettingsYaml.Clear();
            EditorUtility.SetDirty(this);
        }

        void TryLoadBuildProfilePlayerSettings()
        {
            TryLoadProjectSettingsAssetPlayerSettings();

            // For persistent profiles, the on-disk subasset is the source of truth.
            // Always prefer it over any in-memory reference that may be stale.
            if (EditorUtility.IsPersistent(this))
            {
                var path = AssetDatabase.GetAssetPath(this);
                var objects = AssetDatabase.LoadAllAssetsAtPath(path);
                var data = Array.Find(objects, obj => obj is BuildProfilePlayerSettings) as BuildProfilePlayerSettings;
                if (data != null)
                {
                    buildProfilePlayerSettings = data;
                }
                else if (buildProfilePlayerSettings != null)
                {
                    // Profile has just become persistent (e.g. CreateAsset was called after CreatePlayerSettingsFromGlobal).
                    // The in-memory subasset needs to be attached. Defer to avoid mutating AssetDatabase from OnEnable.
                    EditorApplication.delayCall += AttachBuildProfilePlayerSettingsSubasset;
                }
                // No subasset on disk: migrate from legacy YAML if available.
                else if (m_PlayerSettingsYaml.HasSettings())
                {
                    buildProfilePlayerSettings = PlayerSettings.DeserializeBuildProfilePlayerSettingsFromYAMLString(m_PlayerSettingsYaml.GetYamlString());
                    buildProfilePlayerSettings.name = "Player Settings";
                    buildProfilePlayerSettings.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                    // Defer the AssetDatabase mutation to avoid issues if OnEnable runs inside
                    // an AssetDatabase refresh, domain reload, or serialization callback.
                    EditorApplication.delayCall += AttachBuildProfilePlayerSettingsSubasset;
                }
            }

            // Create the runtime PlayerSettings wrapper linked to the subasset.
            // The wrapper's PPtr survives asset reimport via the persistent subasset
            // identifier, so it doesn't need re-linking on subsequent OnEnable calls.
            if (buildProfilePlayerSettings != null)
            {
                if (m_PlayerSettings == null)
                {
                    m_PlayerSettings = PlayerSettings.CreateAsBuildProfileOverride(buildProfilePlayerSettings);
                    s_LoadedPlayerSettings.Add(m_PlayerSettings);
                }

                PlayerSettings.EnsureUnityConnectSettingsEqual(m_PlayerSettings, s_GlobalPlayerSettings);
                UpdateGlobalManagerPlayerSettings();
            }
        }

        void OnDisable()
        {
            if (IsActiveBuildProfileOrPlatform())
            {
                m_ScriptingDefines = BuildProfileModuleUtil.RemoveInvalidScriptingDefines(m_ScriptingDefines);
                EditorUserBuildSettings.SetActiveProfileScriptingDefines(m_ScriptingDefines);
                if (!overrideGlobalScenes)
                    EditorUserBuildSettings.SetCachedActiveProfileScenes(EditorBuildSettings.globalScenes);
                else
                    EditorUserBuildSettings.SetCachedActiveProfileScenes(scenes);
            }

            // OnDisable is called when entering play mode, during domain reloads, or when the object is destroyed.
            // Avoid removing player settings for the first two cases to prevent slow syncs (e.g., color space) caused by global manager updates.
            if (!EditorApplication.isUpdating)
            {
                // Attach the in-memory subasset synchronously here — OnDisable runs in narrower contexts than
                // OnEnable (object destroy, play mode entry, domain reload), and a deferred call would be lost
                // across domain reload or editor quit. The helper is idempotent and self-guards.
                AttachBuildProfilePlayerSettingsSubasset();

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
            targetBuildProfile.overrideGlobalScenes = false;
            targetBuildProfile.scenes = Array.Empty<EditorBuildSettingsScene>();
            targetBuildProfile.hasScriptingDefines = false;
            targetBuildProfile.scriptingDefines = Array.Empty<string>();

            BuildProfileModuleUtil.RemovePlayerSettings(targetBuildProfile);
            BuildProfileModuleUtil.RemoveGraphicsSettings(targetBuildProfile);
            BuildProfileModuleUtil.RemoveQualitySettings(targetBuildProfile);
            BuildProfileModuleUtil.RemoveNonRequiredSubAssets(targetBuildProfile);

            AssetDatabase.SaveAssetIfDirty(targetBuildProfile);
            BuildProfileModuleUtil.UpdateActiveEditors(targetBuildProfile);
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
            else if (isMultiTarget)
            {
                if (TryGetSupportedIBuildTargets(out var supportedTargets))
                {
                    if (selectedPlatformGuid.Empty())
                        selectedPlatformGuid = supportedTargets[0].Guid;

                    var (buildTargetFromGuid, subtargetFromGuid) = BuildProfileModuleUtil.GetBuildTargetAndSubtarget(selectedPlatformGuid);
                    if (buildTargetFromGuid != m_BuildTarget || subtargetFromGuid != m_Subtarget)
                    {
                        m_BuildTarget = buildTargetFromGuid;
                        m_Subtarget = subtargetFromGuid;
                        EditorUtility.SetDirty(this);
                    }
                }
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

            // Scripting define foldout may be visible
            // when no scripting defines have been set.
            if (scriptingDefines.Length > 0 && !m_HasScriptingDefines)
                m_HasScriptingDefines = true;

            var guidToCheck = isMultiTarget ? selectedPlatformGuid : platformGuid;
            if (this == BuildProfileContext.activeProfile && guidToCheck != EditorUserBuildSettings.activePlatformGuid)
            {
                if (EditorUserBuildSettings.GetInternalActivePlatformGuid().Empty())
                {
                    // With an active profile and a GUID mismatch, an empty internal
                    // GUID indicates a legacy API triggered the platform switch.
                    // Deactivate the profile to honor that switch.
                    BuildProfileContext.activeProfile = null;
                }
                else
                {
                    // On disk changes to active profile may change platform guid.
                    // Specifically copying the entire YAML of a valid build profile.
                    EditorUserBuildSettings.SwitchActiveBuildTargetGuid(this);
                }
            }
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
