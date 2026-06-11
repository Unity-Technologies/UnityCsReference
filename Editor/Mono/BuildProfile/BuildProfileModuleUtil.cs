// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor.Modules;
using UnityEditor.Rendering;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using TargetAttributes = UnityEditor.BuildTargetDiscovery.TargetAttributes;
using PlatformPackageList = UnityEditor.BuildTargetDiscovery.PlatformPackageList;
using InternalEditorUtility = UnityEditorInternal.InternalEditorUtility;
using System.IO;
using UnityEngine.Events;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Internal utility class for Build Profile Module.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.BuildAnalysisModule", "UnityEditor.BuildProfileModule")]
    internal class BuildProfileModuleUtil
    {
        const string k_AssetFolderPath = "Assets/Settings/Build Profiles";
        const string k_BuyProUrl = "https://store.unity.com/products/unity-pro";
        const string k_ConsoleModuleUrl = "https://unity3d.com/platform-installation";
        const string k_LastRunnableBuildPathSeparator = "_";
        const string k_HeroImagesPath = "BuildProfile/Icons/Hero";
        const string k_HeroSuffix = ".Hero.png";
        // The asset database supports file name length to max. 250 symbols
        // Leave 5 symbols for the GenerateUniqueAssetPath() that adds " (1)"(2,3...) in case
        // an asset with such name already exists.
        public const int k_MaxAssetFileNameLength = 245;
        // For UI cases where the extension `.asset` is not taken into consideration
        public const int k_MaxAssetFileNameLengthWithoutExtension = k_MaxAssetFileNameLength - 6;
        public const string platformRequirementWarningHelpboxName = "helpbox-card-warning";
        static readonly string k_NoModuleLoaded = L10n.Tr("No {0} module loaded.");
        static readonly string k_SupportedPlatformStatus = L10n.Tr("{0} module installed, {1} module not loaded.");
        static readonly string k_SDKPlatformPackageNotInstalled = L10n.Tr("{0} SDK Platform package is not installed.");
        static readonly string k_DerivedPlatformInactive = L10n.Tr("{0} is currently disabled.");
        static readonly string k_DerivedPlatformDisabled = L10n.Tr("{0} was disabled via command-line arguments.");
        static readonly string k_EditorWillNeedToBeReloaded = L10n.Tr("Note: Editor will need to be restarted to load any newly installed modules");
        static readonly string k_BuildProfileRecompileReason = L10n.Tr("Active build profile scripting defines changes.");
        static readonly GUIContent k_OpenDownloadPage = EditorGUIUtility.TrTextContent("Open Download Page");
        static readonly GUIContent k_InstallModuleWithHub = EditorGUIUtility.TrTextContent("Install with Unity Hub");
        static readonly string k_ModuleInstalled = L10n.Tr("{0} module has been installed.");
        static readonly string k_RestartNeeded = L10n.Tr("Please restart the Unity Editor to load the module.");
        static readonly string k_RestartEditor = L10n.Tr("Restart Unity Editor");
        static readonly GUIContent k_ActivateDerivedPlatform = EditorGUIUtility.TrTextContent("Enable Platform");
        static HashSet<string> s_BuildProfileIconModules = new()
        {
            "Switch",
        };

        static HashSet<GUID> s_PlatformsPendingRestart = new HashSet<GUID>();

        /// <summary>
        /// Mark a platform as having been installed but pending editor restart.
        /// </summary>
        static void MarkPlatformPendingRestart(GUID platformGuid)
        {
            s_PlatformsPendingRestart.Add(platformGuid);
        }

        /// <summary>
        /// Check if a platform is pending editor restart after module installation.
        /// </summary>
        public static bool IsPlatformPendingRestart(GUID platformGuid)
        {
            return s_PlatformsPendingRestart.Contains(platformGuid);
        }

        /// <summary>
        /// Maps Unity Hub's moduleId to Unity's platform GUID.
        /// Hub sends lowercase download link names (e.g., "android", "ios", "webgl").
        /// </summary>
        static GUID? TryGetPlatformGuidFromModuleId(string hubModuleId)
        {
            var allPlatforms = BuildTargetDiscovery.GetAllPlatforms();
            foreach (var platformGuid in allPlatforms)
            {
                var downloadLinkName = BuildTargetDiscovery.BuildPlatformDownloadLinkName(platformGuid);

                // Case-insensitive comparison since Hub sends lowercase
                if (string.Equals(downloadLinkName, hubModuleId, StringComparison.OrdinalIgnoreCase))
                {
                    return platformGuid;
                }
            }

            return null;
        }

        /// <summary>
        /// Internal callback for BuildProfileModule to be notified of
        /// reset initiated through the inspector context menu.
        /// </summary>
        public static event Action<BuildProfile> OnUpdateActiveEditors;

        /// <summary>
        /// Internal static callback requesting an update of the build profile window when
        /// editor settings change.
        /// </summary>
        public static Action OnEditorSettingsChanged;

        /// <summary>
        /// Internal static callback requesting an update of platform requirements UI
        /// (e.g., when a module is installed).
        /// </summary>
        public static Action<GUID> OnPlatformModuleInstallationChanged;

        public static void UpdateActiveEditors(BuildProfile profile)
        {
            OnUpdateActiveEditors?.Invoke(profile);
        }

        /// <summary>
        /// Classic platform display name for a given build profile.
        /// </summary>
        public static string GetClassicPlatformDisplayName(GUID platformId) =>
            GetModuleDisplayName(platformId);

        /// <summary>
        /// Get Platform list of name and link url pair.
        /// </summary>
        public static List<BuildTargetDiscovery.NameAndLink> GetPlatformNameLinkList(GUID platformGuid) =>
            BuildTargetDiscovery.BuildPlatformNameLinkList(platformGuid);

        /// <summary>
        /// Get platform settings docs link for preconfigured settings.
        /// </summary>
        public static string GetPlatformSettingsDocsLink(GUID platformGuid) =>
            BuildTargetDiscovery.BuildPlatformSettingsDocsLink(platformGuid);

        /// <summary>
        /// Fetch default editor platform icon texture.
        /// </summary>
        public static Texture2D GetHelpIcon()
        {
            return EditorGUIUtility.LoadIcon("_Help");
        }

        /// <summary>
        /// Fetch subtitle used to show under the main platform name.
        /// </summary>
        public static string GetSubtitle(GUID platformGUID)
        {
            return BuildTargetDiscovery.BuildPlatformSubtitle(platformGUID);
        }

        /// <summary>
        /// Fetch default editor platform icon texture.
        /// </summary>
        public static Texture2D GetPlatformIcon(GUID platformId)
        {
            if (LoadBuildProfileIcon(platformId, out Texture2D icon))
                return icon;

            if(LoadPlatformMipIcon(platformId, out icon))
                return icon;

            return EditorGUIUtility.LoadIcon(GetPlatformIconId(platformId));
        }

        /// <summary>
        /// Fetch RawImage icon texture for package thumbnail placeholder.
        /// </summary>
        public static Texture2D GetRawImageIcon()
        {
            return EditorGUIUtility.LoadIcon("RawImage");
        }

        /// <summary>
        /// Fetch small (16x16) editor platform icon texture.
        /// </summary>
        public static Texture2D GetPlatformIconSmall(GUID platformId)
        {
            if (LoadBuildProfileIcon(platformId, out Texture2D icon))
                return icon;

            return EditorGUIUtility.LoadIcon(GetPlatformIconId(platformId) + ".Small");
        }

        [Obsolete("Do not use internal APIs from packages.")]
        public static Texture2D GetPlatformIconSmall(string platformId)
        {
            return GetPlatformIconSmall(new GUID(platformId));
        }

        /// <summary>
        /// Fetch scene list icon for build profiles
        /// </summary>
        public static Texture2D GetSceneListIcon()
        {
            return EditorGUIUtility.LoadIcon("SceneList Icon");
        }

        /// <summary>
        /// Load internal warning icon
        /// </summary>
        public static Texture2D GetWarningIcon()
        {
            return EditorGUIUtility.LoadIcon("console.warnicon.sml");
        }

        /// <summary>
        /// Returns true if the module is installed and editor has permissions
        /// for the given platform guid.
        /// </summary>
        public static bool IsModuleInstalled(GUID platformId)
        {
            bool installed = BuildTargetDiscovery.BuildPlatformIsInstalled(platformId);
            return installed
                && IsBuildProfileLicensed(platformId)
                && ModuleManager.IsPlatformSupportLoadedByGuid(platformId);
        }

        [Obsolete("Do not use internal APIs from packages.")]
        public static bool IsModuleInstalled(string platformId)
        {
            return IsModuleInstalled(new GUID(platformId));
        }

        /// <summary>
        /// Returns true if an installed module supports build profiles.
        /// </summary>
        public static bool IsBuildProfileSupported(GUID platformId)
        {
            if (BuildTargetDiscovery.TryGetSupportedPlatformGuids(platformId, out var supportedPlatformGuids))
            {
                foreach (var supportedGuid in supportedPlatformGuids)
                {
                    if (ModuleManager.GetBuildProfileExtension(supportedGuid) != null)
                        return true;
                }
                return false;
            }

            return ModuleManager.GetBuildProfileExtension(platformId) != null;
        }

        [Obsolete("Do not use internal APIs from packages.")]
        public static bool IsBuildProfileSupported(string platformId)
        {
            return IsBuildProfileSupported(new GUID(platformId));
        }

        /// <summary>
        /// Returns true if the build profile for the specified platform is licensed.
        /// </summary>
        public static bool IsBuildProfileLicensed(GUID platformId)
        {
            if (!BuildTargetDiscovery.TryGetSupportedPlatformGuids(platformId, out var supportedPlatformGuids))
            {
                var buildTarget = GetBuildTargetAndSubtarget(platformId).Item1;
                return BuildPipeline.LicenseCheck(buildTarget);
            }

            foreach (var supportedGuid in supportedPlatformGuids)
            {
                var buildTarget = GetBuildTargetAndSubtarget(supportedGuid).Item1;
                if (!BuildPipeline.LicenseCheck(buildTarget))
                    return false;
            }

            return true;
        }

        public static ISDKPlatformExtension GetSDKPlatformExtension(GUID platformId)
        {
            if (BuildTargetDiscovery.TryGetSDKPlatformExtension(platformId, out var extension))
                return extension;
            return null;
        }

        public static Dictionary<GUID, ISDKPlatformExtension> GetAllSDKPlatformExtensions()
        {
            return BuildTargetDiscovery.GetAllSDKPlatformExtensions();
        }

        /// <summary>
        /// Updates the UI to reflect the platform's state, such as pending restart, disabled settings, or missing installation.
        /// </summary>
        public static void UpdateHelpBoxForModuleNotInstalled(HelpBox helpbox, GUID platformId)
        {
            // Check if module was just installed and needs restart
            if (IsPlatformPendingRestart(platformId))
            {
                UpdateHelpBoxForPlatformPendingRestart(platformId, helpbox);
                return;
            }

            if (BuildTargetDiscovery.BuildPlatformIsInstalled(platformId) && BuildTargetDiscovery.BuildPlatformIsDerivedPlatform(platformId))
            {
                UpdateHelpBoxForPlatformNotEnabled(platformId, helpbox);
                return;
            }

            if (BuildTargetDiscovery.BuildPlatformIsSDKPlatform(platformId))
            {
                if (!BuildTargetDiscovery.BuildPlatformModuleIsInstalled(platformId))
                {
                    UpdateHelpBoxForPlatformNotInstalled(platformId, helpbox);
                    return;
                }

                if (!BuildTargetDiscovery.TryGetSDKPlatformExtension(platformId, out _))
                {
                    if (helpbox.name == platformRequirementWarningHelpboxName)
                        return;

                    UpdateHelpBoxForSDKPlatformPackageNotInstalled(platformId, helpbox);
                    return;
                }
            }

            // TODO: this is a workaround for onboarding instructions to fix EmbeddedLinux and QNX
            // needs to be removed when https://jira.unity3d.com/browse/PLAT-7721 is implemented
            if (BuildTargetDiscovery.BuildPlatformTryGetCustomInstallLinkAndText(platformId, out var url, out var text))
            {
                UpdateHelpBoxForPlatformContactSales(helpbox, text, url);
                return;
            }

            if (IsBuildProfileDisabledViaArguments(platformId))
            {
                UpdateHelpBoxForPlatformDisabledViaArguments(platformId, helpbox);
                return;
            }

            UpdateHelpBoxForPlatformNotInstalled(platformId, helpbox);
        }

        static bool IsBuildProfileDisabledViaArguments(GUID platformId)
        {
            var (buildTarget, _) = BuildTargetDiscovery.GetBuildTargetAndSubtargetFromGUID(platformId);
            var buildTargetInfoList = BuildTargetDiscovery.GetBuildTargetInfoList();

            foreach (var info in buildTargetInfoList)
                if (info.buildTargetPlatformVal.Equals(buildTarget))
                    return InternalEditorUtility.IsPlaybackEngineDisabled(info.dirName);

            return false;
        }

        /// <summary>
        /// Updates the HelpBox with localized text and action buttons specific to a missing platform license.
        /// </summary>
        public static void UpdateHelpBoxForLicenseNotFound(HelpBox helpbox, GUID platformId)
        {
            string displayName = GetModuleDisplayName(platformId);
            if (BuildTargetDiscovery.TryGetSupportedPlatformGuids(platformId, out var supportedPlatformGuids))
            {
                var displayNames = new List<string>();
                foreach (var supportedGuid in supportedPlatformGuids)
                    displayNames.Add(GetModuleDisplayName(supportedGuid));
                displayName += " (" + string.Join(", ", displayNames) + ")";
            }

            var buildTarget = GetBuildTargetAndSubtarget(platformId).Item1;
            string licenseMsg = L10n.Tr("Your license does not cover {0} Publishing.");
            string buttonMsg = L10n.Tr("Go to Our Online Store");
            string url = k_BuyProUrl;
            if (BuildTargetDiscovery.PlatformHasFlag(buildTarget, TargetAttributes.IsConsole))
            {
                licenseMsg += " Please see the {0} section of the Platform Module Installation documentation for more details.";
                buttonMsg = L10n.Tr("Platform Module Installation");
                url = k_ConsoleModuleUrl;
            }
            licenseMsg = L10n.Tr(licenseMsg);

            helpbox.text = string.Format(licenseMsg, displayName);
            if (!IsStandalonePlatform(buildTarget))
            {
                helpbox.buttonText = buttonMsg;
                helpbox.onButtonClicked += () => Application.OpenURL(url);
            }
        }

        public static bool IsStandalonePlatform(BuildTarget buildTarget) =>
            BuildTargetDiscovery.PlatformHasFlag(buildTarget, TargetAttributes.IsStandalonePlatform);

        /// <summary>
        /// Returns true if the build target is supported by coverage
        /// </summary>
        public static bool IsBuildTargetSupportedByCoverage(BuildTarget buildTarget)
        {
            if (!IsStandalonePlatform(buildTarget))
                return false;

            if (buildTarget == BuildTarget.StandaloneWindows)
                return false;

            var namedBuildTarget = NamedBuildTarget.FromActiveSettings(buildTarget);
            var scriptingBackend = PlayerSettings.GetScriptingBackend(namedBuildTarget);

            return scriptingBackend == ScriptingImplementation.Mono2x;
        }

        public static bool IsBuildAutomationSupported(GUID platformGuid)
        {
            if (!BuildTargetDiscovery.TryGetBuildTarget(platformGuid, out var iBuildTarget))
                return false;

            if (!iBuildTarget.TryGetProperties<IBuildPlatformProperties>(out var buildPlatformProperties))
                return false;

            return buildPlatformProperties.SupportBuildAutomation;
        }

        /// <summary>
        /// Retrieve the respective module name for a platform guid.
        /// </summary>
        public static string GetModuleName(GUID platformId)
        {
            var buildTarget = GetBuildTargetAndSubtarget(platformId).Item1;
            return BuildTargetDiscovery.GetModuleNameForBuildTarget(buildTarget);
        }

        /// <summary>
        /// Internal method for switching <see cref="EditorUserBuildSettings"/> active build target and subtarget.
        /// </summary>
        public static void SwitchLegacyActiveFromBuildProfile(BuildProfile profile)
        {
            EditorUserBuildSettings.SwitchActiveBuildTargetGuid(profile);
        }

        public static void SwitchLegacySelectedBuildTargets(BuildProfile profile)
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(profile.buildTarget);
            var buildTargetSubTarget = (int)profile.subtarget;

            if (buildTargetGroup == BuildTargetGroup.Standalone)
                EditorUserBuildSettings.SetSelectedSubtargetFor(profile.buildTarget, buildTargetSubTarget);
            EditorUserBuildSettings.selectedBuildTargetGroup = buildTargetGroup;
        }

        /// <summary>
        /// Internally invoke <see cref="BuildPlayerWindow.CallBuildMethods(bool, BuildOptions)"/>.
        /// </summary>
        public static void CallInternalBuildMethods(bool askForBuildLocation, BuildOptions options)
        {
            BuildPlayerWindow.CallBuildMethods(askForBuildLocation, options);
        }

        /// <summary>
        /// Construct the build player options from the active build profile.
        /// </summary>
        /// <param name="buildLocation">The path where the application will be built.</param>
        /// <param name="assetBundleManifestPath">The path to the asset bundle manifest file.</param>
        /// <param name="customBuildOptions">Custom build options to be applied.</param>
        /// <see cref="BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptionsInternal"/>
        internal static BuildPlayerOptions GetBuildPlayerOptionsFromActiveProfile(string buildLocation, string assetBundleManifestPath, BuildOptions customBuildOptions)
        {
            var options = new BuildPlayerOptions();
            var activeProfile = BuildProfile.GetActiveBuildProfile();

            if (activeProfile == null)
                throw new ArgumentException("Active build profile is null.");

            var buildTarget = activeProfile.buildTarget;
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            int subtarget = IsStandalonePlatform(buildTarget) ?
                (int)activeProfile.subtarget : EditorUserBuildSettings.GetActiveSubtargetFor(buildTarget);

            options.options = GetBuildOptions(buildTarget, buildTargetGroup, buildLocation, customBuildOptions);
            options.target = buildTarget;
            options.subtarget = subtarget;
            options.targetGroup = buildTargetGroup;
            options.locationPathName = buildLocation;
            options.assetBundleManifestPath = assetBundleManifestPath ?? PostprocessBuildPlayer.GetStreamingAssetsBundleManifestPath();
            options.scenes = EditorBuildSettingsScene.GetActiveSceneList(activeProfile.GetScenesForBuild());
            options.previousBuildReportDirectories = PostprocessBuildPlayer.GetPreviousContentBuildReportDirectories();

            return options;
        }

        internal static BuildOptions GetBuildOptions(BuildTarget buildTarget, BuildTargetGroup buildTargetGroup, string buildLocation, BuildOptions options = BuildOptions.None)
        {
            // Check if Lz4 is supported for the current buildtargetgroup and enable it if need be
            if (PostprocessBuildPlayer.SupportsCompression(buildTarget, Compression.Lz4))
            {
                var compression = EditorUserBuildSettings.GetCompressionType(buildTargetGroup);
                if (compression < 0)
                    compression = PostprocessBuildPlayer.GetDefaultCompression(buildTarget);
                if (compression == Compression.Lz4)
                    options |= BuildOptions.CompressWithLz4;
                else if (compression == Compression.Lz4HC)
                    options |= BuildOptions.CompressWithLz4HC;
            }

            bool developmentBuild = EditorUserBuildSettings.development;
            if (developmentBuild)
                options |= BuildOptions.Development;
            if (EditorUserBuildSettings.allowDebugging && developmentBuild)
                options |= BuildOptions.AllowDebugging;
            if (EditorUserBuildSettings.symlinkSources)
                options |= BuildOptions.SymlinkSources;

            if (BuildTargetDiscovery.TryGetBuildTarget(buildTarget, out IBuildTarget iBuildTarget))
            {
                if (EditorUserBuildSettings.connectProfiler && (developmentBuild || (iBuildTarget.PlayerConnectionPlatformProperties?.ForceAllowProfilerConnection ?? false)))
                    options |= BuildOptions.ConnectWithProfiler;
            }

            if (EditorUserBuildSettings.buildWithDeepProfilingSupport && developmentBuild)
                options |= BuildOptions.EnableDeepProfilingSupport;

            if (EditorUserBuildSettings.buildWithCodeCoverage && developmentBuild && IsBuildTargetSupportedByCoverage(buildTarget))
                options |= BuildOptions.EnableCodeCoverage;
            if (EditorUserBuildSettings.buildScriptsOnly)
                options |= BuildOptions.BuildScriptsOnly;
            if (!string.IsNullOrEmpty(ProfilerUserSettings.customConnectionID) && developmentBuild)
                options |= BuildOptions.CustomConnectionID;

            if (EditorUserBuildSettings.installInBuildFolder &&
                PostprocessBuildPlayer.SupportsInstallInBuildFolder(buildTarget) &&
                Unsupported.IsSourceBuild())
            {
                options |= BuildOptions.InstallInBuildFolder;
            }
            else if ((options & BuildOptions.PatchPackage) == 0)
            {
                if (!string.IsNullOrEmpty(buildLocation) && BuildPipeline.BuildCanBeAppended(buildTarget, buildLocation) == CanAppendBuild.Yes)
                    options |= BuildOptions.AcceptExternalModificationsToPlayer;
            }

            return options;
        }

        public static IBuildProfileExtension GetBuildProfileExtension(GUID platformId) =>
            ModuleManager.GetBuildProfileExtension(platformId);

        public static GUIStyle dropDownToggleButton => EditorStyles.dropDownToggleButton;

        /// <summary>
        /// Returns all discovered platform keys that are possible Build Profile targets.
        /// </summary>
        public static List<GUID> FindAllViewablePlatforms()
        {
            var result = new List<GUID>();

            foreach (var platformGuid in BuildTargetDiscovery.GetAllPlatforms())
            {
                var installed = BuildTargetDiscovery.BuildPlatformIsInstalled(platformGuid);
                if (!installed && BuildTargetDiscovery.BuildPlatformIsHiddenInUI(platformGuid))
                    continue;

                result.Add(platformGuid);
            }

            // Swap current editor standalone platform to the top.
            if (Application.platform == RuntimePlatform.OSXEditor)
                result.Reverse(0, 2);
            if (Application.platform == RuntimePlatform.LinuxEditor)
                result.Reverse(0, 3);

            return result;
        }

        public static BuildTargetDiscovery.PlatformGroup[] GetAllPlatformGroups()
        {
            var allGroups = BuildTargetDiscovery.GetPlatformGroups();

            for (int i = 0; i < allGroups.Length; i++)
            {
                var platforms = new List<GUID>();

                foreach (var platformGuid in allGroups[i].platforms)
                {
                    var installed = BuildTargetDiscovery.BuildPlatformIsInstalled(platformGuid);
                    if (!installed && BuildTargetDiscovery.BuildPlatformIsHiddenInUI(platformGuid))
                        continue;

                    platforms.Add(platformGuid);
                }

                allGroups[i].platforms = platforms.ToArray();
            }

            return allGroups;
        }

        public static bool IsPlatformVisibleInPlatformBrowserOnly(GUID platformGuid)
        {
            return BuildTargetDiscovery.BuildPlatformIsVisibleInPlatformBrowserOnly(platformGuid);
        }

        public static bool IsPlatformAvailableOnHostPlatform(GUID platformGuid, OperatingSystemFamily operatingSystemFamily)
        {
            return BuildTargetDiscovery.BuildPlatformIsAvailableOnHostPlatform(platformGuid, operatingSystemFamily);
        }

        /// <summary>
        /// Check if the user is able to build his VT-enabled Player for a target platform
        /// </summary>
        public static bool IsVirtualTexturingSettingsValid(GUID platformGuid)
        {
            if (!PlayerSettings.GetVirtualTexturingSupportEnabled())
            {
                return true;
            }

            var (buildTarget, _) = GetBuildTargetAndSubtarget(platformGuid);
            if (!UnityEngine.Rendering.VirtualTexturingEditor.Building.IsPlatformSupportedForPlayer(buildTarget))
            {
                return false;
            }

            GraphicsDeviceType[] gfxTypes = PlayerSettings.GetGraphicsAPIs(buildTarget);
            bool supportedAPI = true;
            foreach (GraphicsDeviceType api in gfxTypes)
            {
                supportedAPI &= UnityEngine.Rendering.VirtualTexturingEditor.Building.IsRenderAPISupported(api, buildTarget, false);
            }

            return supportedAPI;
        }

        /// <summary>
        /// Remove player settings for deleted build profile assets. For example, when deleting them
        /// in the project folder
        /// </summary>
        public static void CleanUpPlayerSettingsForDeletedBuildProfiles(IList<BuildProfile> currentBuildProfiles)
        {
            BuildProfile.CleanUpPlayerSettingsForDeletedBuildProfiles(currentBuildProfiles);
        }

        /// <summary>
        /// Check if build profile has serialized player settings
        /// </summary>
        public static bool HasSerializedPlayerSettings(BuildProfile buildProfile)
        {
            return buildProfile.HasSerializedPlayerSettings();
        }

        /// <summary>
        /// Serialize build profile player settings
        /// </summary>
        public static void SerializePlayerSettings(BuildProfile buildProfile)
        {
            buildProfile.SerializePlayerSettings();
        }

        /// <summary>
        /// Remove build profile player settings object and clear player settings yaml
        /// </summary>
        public static void RemovePlayerSettings(BuildProfile buildProfile)
        {
            buildProfile.RemovePlayerSettings(clearYaml: true);
        }

        /// <summary>
        /// Create graphics settings override for build profile
        /// </summary>
        public static void CreateGraphicsSettings(BuildProfile buildProfile)
        {
            if (buildProfile.graphicsSettings != null)
                return;

            buildProfile.graphicsSettings = ScriptableObject.CreateInstance<BuildProfileGraphicsSettings>();
            buildProfile.graphicsSettings.Instantiate();
            AssetDatabase.AddObjectToAsset(buildProfile.graphicsSettings, buildProfile);
            EditorUtility.SetDirty(buildProfile);
            UpdateActiveEditors(buildProfile);
        }

        /// <summary>
        /// Remove build profile graphics settings override
        /// </summary>
        public static void RemoveGraphicsSettings(BuildProfile buildProfile)
        {
            if (buildProfile.graphicsSettings == null)
                return;

            AssetDatabase.RemoveObjectFromAsset(buildProfile.graphicsSettings);
            buildProfile.graphicsSettings = null;
            EditorUtility.SetDirty(buildProfile);
            UpdateActiveEditors(buildProfile);
        }

        /// <summary>
        /// Create graphics settings quality for build profile
        /// </summary>
        public static void CreateQualitySettings(BuildProfile buildProfile)
        {
            if (buildProfile.qualitySettings != null)
                return;

            buildProfile.qualitySettings = ScriptableObject.CreateInstance<BuildProfileQualitySettings>();
            buildProfile.qualitySettings.Instantiate();
            AssetDatabase.AddObjectToAsset(buildProfile.qualitySettings, buildProfile);
            EditorUtility.SetDirty(buildProfile);
            UpdateActiveEditors(buildProfile);

            BuildProfileQualitySettingsEditor.RefreshCachedQualitySettingEntities();
        }

        /// <summary>
        /// Remove build profile quality settings override
        /// </summary>
        public static void RemoveQualitySettings(BuildProfile buildProfile)
        {
            if (buildProfile.qualitySettings == null)
                return;

            AssetDatabase.RemoveObjectFromAsset(buildProfile.qualitySettings);
            buildProfile.qualitySettings = null;
            EditorUtility.SetDirty(buildProfile);
            UpdateActiveEditors(buildProfile);

            BuildProfileQualitySettingsEditor.RefreshCachedQualitySettingEntities();
        }

        public static void NotifyBuildProfileExtensionOfCreation(BuildProfile buildProfile, int preconfiguredSettingsVariant)
        {
            buildProfile.NotifyBuildProfileExtensionOfCreation(preconfiguredSettingsVariant);
        }

        /// <summary>
        /// Create player settings for build profile based on global player settings
        /// </summary>
        public static void CreatePlayerSettingsFromGlobal(BuildProfile buildProfile)
        {
            buildProfile.CreatePlayerSettingsFromGlobal();
        }

        /// <summary>
        /// Checks if player settings values are the same as project settings values
        /// </summary>
        public static bool IsDataEqualToProjectSettings(PlayerSettings playerSettings)
        {
            return BuildProfile.IsDataEqualToProjectSettings(playerSettings);
        }

        /// Retrieve string of filename invalid characters
        /// </summary>
        /// <returns></returns>
        public static string GetFilenameInvalidCharactersStr()
        {
            return EditorUtility.GetInvalidFilenameChars();
        }

        /// <summary>
        /// Delete last runnable build key in EditorPrefs for a profile that will be deleted
        /// </summary>
        public static void DeleteLastRunnableBuildKeyForProfile(BuildProfile profile)
        {
            var lastRunnableKey = profile.GetLastRunnableBuildPathKey();
            if (!string.IsNullOrEmpty(lastRunnableKey))
                EditorPrefs.DeleteKey(lastRunnableKey);
        }

        /// <summary>
        /// Delete last runnable build keys in EditorPrefs for deleted profiles
        /// </summary>
        public static void DeleteLastRunnableBuildKeyForDeletedProfiles()
        {
            List<string> lastRunnableBuildPathKeys = BuildProfileContext.instance.LastRunnableBuildPathKeys;
            for (int i = lastRunnableBuildPathKeys.Count - 1; i >= 0; i--)
            {
                string key = lastRunnableBuildPathKeys[i];
                var assetPath = GetAssetPathFromLastRunnableBuildKey(key);
                if (!AssetDatabase.AssetPathExists(assetPath))
                {
                    lastRunnableBuildPathKeys.RemoveAt(i);
                    EditorPrefs.DeleteKey(key);
                }
            }
        }

        /// <summary>
        /// Get last runnable build key from build profile path
        /// </summary>
        public static string GetLastRunnableBuildKeyFromAssetPath(string assetPath, string baseKey)
        {
            return string.IsNullOrEmpty(assetPath) ? string.Empty : $"{baseKey}{k_LastRunnableBuildPathSeparator}{assetPath}";
        }

        /// <summary>
        /// On the next editor update recompile scripts.
        /// </summary>
        public static void RequestScriptCompilation(BuildProfile profile)
        {
            if (profile != null)
                BuildProfileContext.instance.cachedEditorScriptingDefines = profile.scriptingDefines;
            else
                BuildProfileContext.instance.cachedEditorScriptingDefines = Array.Empty<string>();

            EditorApplication.delayCall += TryRecompileScripts;
        }

        /// <summary>
        /// Create a new custom build profile asset with the user provided name.
        /// Ensure that custom build profile folders is created if it doesn't already exist.
        /// </summary>
        public static void CreateNewAssetWithName(
            GUID platformId, string customProfileName, string preconfiguredSettingsVariantName,
            int preconfiguredSettingsVariant, string[] packagesToAdd, UnityAction<BuildProfile> onCreate)
        {
            BuildProfileModuleUtil.EnsureCustomBuildProfileFolderExists();
            BuildProfile.CreateInstance(
                platformId
                , GetProfilePathWithProvidedName(platformId, customProfileName, preconfiguredSettingsVariantName)
                , preconfiguredSettingsVariant
                , packagesToAdd
                , onCreate);
        }

        /// <summary>
        /// Checks and creates the custom build profile folder if it does not exist.
        /// </summary>
        public static void EnsureCustomBuildProfileFolderExists()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                AssetDatabase.CreateFolder("Assets", "Settings");

            if (!AssetDatabase.IsValidFolder(k_AssetFolderPath))
                AssetDatabase.CreateFolder("Assets/Settings", "Build Profiles");
        }

        static string GetDefaultNewProfilePath(string buildProfileName, string variantName = null)
        {
            const int k_PathByteCount = 6;
            var assetFileName = string.IsNullOrEmpty(variantName) ?
                $"{SanitizeFileName(buildProfileName)}" :
                $"{SanitizeFileName(buildProfileName)} - {SanitizeFileName(variantName)}";
            // Truncate the length to max. 250 symbols, as supported by the asset database.
            // Leave 5 symbols for the GetUniqueBuildProfilePath() that adds " (1)"(2,3...) in case
            // an asset with such name already exists.
            if ((System.Text.Encoding.UTF8.GetByteCount(assetFileName) + k_PathByteCount) > BuildProfileModuleUtil.k_MaxAssetFileNameLength)
                assetFileName = BuildProfileModuleUtil.TruncateUtf8StringByBytes(assetFileName, BuildProfileModuleUtil.k_MaxAssetFileNameLength);
            return $"{k_AssetFolderPath}/{assetFileName}.asset";
        }

        /// <summary>
        /// Recompile scripts if the active build profile scripting defines
        /// differs from the last compilation defines.
        /// </summary>
        static void TryRecompileScripts()
        {
            if (EditorApplication.isCompiling)
                return;

            PlayerSettings.RecompileScripts(k_BuildProfileRecompileReason);
        }

        /// <summary>
        /// Native callback invoked when Unity Hub completes a module installation.
        /// </summary>
        [RequiredByNativeCode]
        internal static void OnModuleInstallationCompleted(string moduleId, string editorVersion, string timestamp, string message)
        {
            Debug.Log($"[BuildProfile] Module installation completed: {moduleId} (version: {editorVersion}) - {message}");

            // Check if this message is for the current editor version
            // Hub broadcasts to all open editors, but only the matching version should respond
            if (!string.Equals(Application.unityVersion, editorVersion, StringComparison.Ordinal))
            {
                Debug.Log($"[BuildProfile] Ignoring module installation for different editor version. Current: {Application.unityVersion}, Message: {editorVersion}");
                return;
            }

            // Map Hub's moduleId to Unity's platform GUID
            var platformGuid = TryGetPlatformGuidFromModuleId(moduleId);
            if (platformGuid == null)
            {
                Debug.LogWarning($"[BuildProfile] Could not find platform for Hub moduleId: {moduleId}");
                return;
            }

            Debug.Log($"[BuildProfile] Mapped moduleId '{moduleId}' to platform: {BuildTargetDiscovery.BuildPlatformDisplayName(platformGuid.Value)}");

            // Mark this platform as pending editor restart
            MarkPlatformPendingRestart(platformGuid.Value);

            // Notify subscribers to refresh platform requirements UI
            EditorApplication.delayCall += () =>
            {
                OnPlatformModuleInstallationChanged?.Invoke(platformGuid.Value);
                RepaintProjectSettingsWindow();
            };
        }


        [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
        internal static void SuppressMissingTypeWarning()
        {
            SerializationUtility.SuppressMissingTypeWarning(nameof(BuildProfile));
        }

        public static string GetDefaultNewProfilePath(GUID platformGuid)
        {
            var platformDisplayName = BuildProfileModuleUtil.GetClassicPlatformDisplayName(platformGuid);
            return GetDefaultNewProfilePath(platformDisplayName);
        }

        internal static string GetProfilePathWithProvidedName(GUID platformId, string customProfileName, string variantName = null)
        {
            return GetDefaultNewProfilePath(string.IsNullOrEmpty(customProfileName) ?
                BuildProfileModuleUtil.GetClassicPlatformDisplayName(platformId) :
                customProfileName, variantName);
        }

        internal static string GetBuildProfileLastRunnableBuildPathKey(BuildTarget buildTarget, StandaloneBuildSubtarget standaloneBuildSubtarget)
        {
            var platformGuid = GetPlatformId(buildTarget, standaloneBuildSubtarget);
            var activeProfile = BuildProfile.GetActiveBuildProfile();
            if (activeProfile != null && activeProfile.platformGuid == platformGuid)
                return activeProfile.GetLastRunnableBuildPathKey();

            var classicProfile = BuildProfileContext.instance.GetForClassicPlatform(platformGuid);
            return classicProfile != null ? classicProfile.GetLastRunnableBuildPathKey() : string.Empty;
        }

        internal static void SetBuildProfileLastRunnableBuildPathKey(string key, string value)
        {
            if (BuildProfile.GetActiveBuildProfile() != null &&
                !BuildProfileContext.instance.LastRunnableBuildPathKeys.Contains(key))
            {
                BuildProfileContext.instance.LastRunnableBuildPathKeys.Add(key);
            }
            EditorPrefs.SetString(key, value);
        }

        /// <summary>
        /// Generates a unique file path for a build profile by ensuring the file name does not conflict with existing profiles.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
        internal static string GetUniqueBuildProfilePath(string path)
        {
            var allBuildProfiles = FindAllBuildProfiles();
            string[] existingNames = allBuildProfiles.ConvertAll(profile => profile.name).ToArray();

            string baseFileName = Path.GetFileNameWithoutExtension(path);
            string uniqueName = ObjectNames.GetUniqueName(existingNames, baseFileName);

            string directory = Path.GetDirectoryName(path);
            string extension = Path.GetExtension(path);
            string uniqueFilePath = Path.Combine(directory, uniqueName + extension);

            // Check that the file path doesn't exist on disk
            // (shouldn't be the case, as we check against all BuildProfiles before)
            uniqueFilePath = AssetDatabase.GenerateUniqueAssetPath(uniqueFilePath);

            return uniqueFilePath;
        }

        [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
        internal static List<BuildProfile> FindAllBuildProfiles()
        {
            var profiles = new List<BuildProfile>(BuildProfile.GetAllBuildProfiles());
            profiles.Sort((lhs, rhs) => EditorUtility.NaturalCompare(lhs.name, rhs.name));
            return profiles;
        }

        /// <summary>
        /// Attempts to load the Mip mapped icon for the given platformId.
        /// </summary>
        /// <param name="icon">The loaded icon, or <see langword="null"/> if no icon could be found.</param>
        /// <param name="platformId">The platform GUID to load the icon for.</param>
        /// <returns><see langword="true"/> if a mip map icon exists.</returns>
        static bool LoadPlatformMipIcon(GUID platformId, out Texture2D icon)
        {
            icon = EditorGUIUtility.FindTexture($"{GetModuleName(platformId)} Icon");
            return icon != null;
        }

        static bool LoadBuildProfileIcon(GUID platformId, out Texture2D icon)
        {
            var moduleName = GetModuleName(platformId);
            if (s_BuildProfileIconModules.Contains(moduleName))
            {
                icon = EditorGUIUtility.FindTexture(typeof(BuildProfile));
                return true;
            }

            icon = null;
            return false;
        }

        /// <summary>
        /// Fetches the Hero image associated with the platform.
        /// If no specific hero image is found, it falls back to the platform icon.
        /// </summary>
        public static Texture2D GetPlatformHeroImage(GUID platformId)
        {
            // Early out check for platforms that we don't have icon approval for (i.e. Switch)
            if (LoadBuildProfileIcon(platformId, out Texture2D icon))
                return icon;

            // Attempt to load the hero icon for the platform
            return EditorGUIUtility.LoadIcon($"{k_HeroImagesPath}/{GetPlatformIconId(platformId)}{k_HeroSuffix}") ?? GetPlatformIcon(platformId);
        }

        static string GetPlatformIconId(GUID platformId)
        {
            var iconName = BuildTargetDiscovery.BuildPlatformIconName(platformId);

            if (string.IsNullOrEmpty(iconName))
                return "BuildSettings.Editor";

            return iconName;
        }

        static string GetModuleDisplayName(GUID platformId)
        {
            return BuildTargetDiscovery.BuildPlatformDisplayName(platformId);
        }

        static string SanitizeFileName(string name)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            if (name.IndexOfAny(invalidChars) == -1)
                return name;

            foreach (char c in invalidChars)
                name = name.Replace(c.ToString(), string.Empty);
            return name;
        }

        /// <summary>
        /// Get build profile path from the last runnable build key
        /// </summary>
        static string GetAssetPathFromLastRunnableBuildKey(string key)
        {
            int lastUnderscoreIndex = key.LastIndexOf(k_LastRunnableBuildPathSeparator);
            return lastUnderscoreIndex != -1 ? key[(lastUnderscoreIndex + 1)..] : string.Empty;
        }

        /// <summary>
        /// Get the platform GUID corresponding to the BuildTarget and the StandaloneBuildSubtarget.
        /// </summary>
        /// <param name="buildTarget">The BuildTarget to get the platform GUID for.</param>
        /// <param name="subtarget">The StandaloneBuildSubtarget to get the platform GUID for.</param>
        /// <returns>The platform GUID. Derived platform GUID when the active platform is a derived platform. Base platform GUID otherwise.</returns>
        public static GUID GetPlatformId(BuildTarget buildTarget, StandaloneBuildSubtarget subtarget)
        {
            var platformGuid = subtarget == StandaloneBuildSubtarget.Server && IsStandalonePlatform(buildTarget)
                ? BuildTargetDiscovery.GetGUIDFromBuildTarget(NamedBuildTarget.Server, buildTarget)
                : BuildTargetDiscovery.GetGUIDFromBuildTarget(buildTarget);
            return platformGuid;
        }

        public static (BuildTarget, StandaloneBuildSubtarget) GetBuildTargetAndSubtarget(GUID platformId)
        {
            return BuildTargetDiscovery.GetBuildTargetAndSubtargetFromGUID(platformId);
        }

        public static PlatformPackageList BuildPlatformInternalPackages(GUID platformGuid)
        {
            return BuildTargetDiscovery.BuildPlatformInternalPackages(platformGuid);
        }

        public static PlatformPackageList BuildPlatformPartnerPackages(GUID platformGuid)
        {
            return BuildTargetDiscovery.BuildPlatformPartnerPackages(platformGuid);
        }

        public static string[] GetAllPlatformPackageNames()
        {
            return BuildTargetDiscovery.GetAllPlatformPackageNames();
        }

        public static bool IsFromUnityPackageSource(PackageManager.PackageInfo packageInfo)
        {
            return BuildTargetDiscovery.IsFromUnityPackageSource(packageInfo);
        }

        public static string BuildPlatformDescription(GUID platformGuid)
        {
            return BuildTargetDiscovery.BuildPlatformDescription(platformGuid);
        }

        /// <summary>
        /// Get platform key features text.
        /// </summary>
        public static string BuildPlatformKeyFeatures(GUID platformGuid) =>
            BuildTargetDiscovery.BuildPlatformKeyFeatures(platformGuid);

        /// <summary>
        /// Check if the platform has samples in the package manager.
        /// </summary>
        public static bool HasSamplesInPackageManager(GUID platformGuid) =>
            BuildTargetDiscovery.BuildPlatformHasSamplesFlag(platformGuid);

        /// <summary>
        /// Get platform resources text.
        /// </summary>
        public static string BuildPlatformResources(GUID platformGuid) =>
            BuildTargetDiscovery.BuildPlatformResources(platformGuid);

        public static string GetPlatformColorString(GUID platformGuid)
        {
            return BuildTargetDiscovery.GetPlatformColorString(platformGuid);
        }

        public static void OnActiveProfileGraphicsSettingsChanged(bool hasGraphicsSettings)
        {
            EditorGraphicsSettings.activeProfileHasGraphicsSettings = hasGraphicsSettings;
            GraphicsSettingsInspector.OnActiveProfileGraphicsSettingsChanged?.Invoke();
        }

        internal static void RemoveQualityLevelFromAllProfiles(string qualityLevelName)
        {
            var profiles = BuildProfile.GetAllBuildProfiles();
            foreach (var profile in profiles)
            {
                if (profile.qualitySettings == null)
                    continue;

                profile.qualitySettings.RemoveQualityLevel(qualityLevelName);
            }
        }

        internal static void RenameQualityLevelInAllProfiles(string oldName, string newName)
        {
            var profiles = BuildProfile.GetAllBuildProfiles();
            foreach (var profile in profiles)
            {
                if (profile.qualitySettings == null)
                    continue;

                profile.qualitySettings.RenameQualityLevel(oldName, newName);
            }
        }

        public static PlayerSettings GetGlobalPlayerSettings()
        {
            return BuildProfile.GetGlobalPlayerSettings();
        }

        /// <summary>
        /// Handles change in global player settings object from the build profile workflow.
        /// Checks that the player settings in <see cref="nextBuildProfile"/> can be applied
        /// and/or requests action from the end user.
        /// </summary>
        /// <returns>
        ///     true, if player settings for the next profile have been handled.
        /// </returns>
        public static bool HandlePlayerSettingsChanged(
            BuildProfile currentBuildProfile, BuildProfile nextBuildProfile)
        {
            PlayerSettings projectSettingsPlayerSettings = GetGlobalPlayerSettings();
            PlayerSettings currentPlayerSettings = projectSettingsPlayerSettings;
            PlayerSettings nextPlayerSettings = projectSettingsPlayerSettings;

            var currentBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            var nextBuildTarget = EditorUserBuildSettings.activeBuildTarget;

            if (currentBuildProfile != null)
            {
                // For build platform switch on the active multi-target profile, buildTarget
                // is already mutated, so falling back to the pre-switch active buildTarget.
                if (currentBuildProfile != nextBuildProfile)
                    currentBuildTarget = currentBuildProfile.buildTarget;

                if (currentBuildProfile.playerSettings != null)
                {
                    currentPlayerSettings = currentBuildProfile.playerSettings;
                }
            }

            if (nextBuildProfile != null)
            {
                nextBuildTarget = nextBuildProfile.buildTarget;
                if (nextBuildProfile.playerSettings != null)
                {
                    nextPlayerSettings = nextBuildProfile.playerSettings;
                }
            }

            var settingsRequiringRestart = PlayerSettings.GetSettingsRequiringRestart(currentPlayerSettings,
                nextPlayerSettings, currentBuildTarget, nextBuildTarget);
            // if we've found settings that need restarting..
            if (settingsRequiringRestart.Length > 0)
            {
                // ..we show the restart prompt, if the user restarts, we add a restart call to the editor
                if (ShowRestartEditorDialog(settingsRequiringRestart))
                {
                    if (ContainsPlayerSetting(settingsRequiringRestart, PlayerSettingsRequiringRestart.VirtualTexturing))
                    {
                        PlayerSettings.SyncVirtualTexturingState(nextPlayerSettings);
                    }
                    if (ContainsPlayerSetting(settingsRequiringRestart, PlayerSettingsRequiringRestart.GraphicsAPI))
                    {
                        PlayerSettings.SyncSettingsAndCleanSettings();
                    }

                    EditorApplication.delayCall += EditorApplication.RestartEditorAndRecompileScripts;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            // Handle editor changed requiring background work without an editor prompt.
            PlayerSettingsEditor.HandlePlayerSettingsChanged(
                currentPlayerSettings, nextPlayerSettings,
                currentBuildTarget, nextBuildTarget);
            return true;
        }


        /// <summary>
        /// Show the restart editor dialog with the names of the settings that required the restart to take effect.
        /// </summary>
        static bool ShowRestartEditorDialog(PlayerSettingsRequiringRestart[] settingsRequiringRestart)
        {
            var editorPromptText = new StringBuilder();
            editorPromptText.AppendLine(L10n.Tr("The Unity editor must be restarted for the following settings to take effect:"));
            var playerSettingNames = GetPlayerSettingNamesToEditorRestartPromptText(settingsRequiringRestart);
            editorPromptText.AppendLine(playerSettingNames.ToString());
            return EditorUtility.DisplayDialog(L10n.Tr("Unity editor restart required"),
                editorPromptText.ToString(), L10n.Tr("Apply"), L10n.Tr("Cancel"));
        }


        internal static PlayerSettings GetBuildProfileOrGlobalPlayerSettings(BuildProfile buildProfile)
        {
            if (buildProfile == null || buildProfile.playerSettings == null)
            {
                return BuildProfile.GetGlobalPlayerSettings();
            }
            return buildProfile.playerSettings;
        }

        public static bool IsBasePlatformOfActivePlatform(GUID platformGuid)
        {
            var module = ModuleManager.FindPlatformSupportModule(EditorUserBuildSettings.activePlatformGuid);
            if (module == null)
                return false;
            if (module is IDerivedBuildTargetProvider derivedBuildTargetProvider)
            {
                var basePlatformGuid = derivedBuildTargetProvider.GetBasePlatformGuid();
                return basePlatformGuid == platformGuid;
            }
            return false;
        }

        public static void RepaintProjectSettingsWindow()
        {
            foreach (var window in EditorWindow.activeEditorWindows)
            {
                if (window is ProjectSettingsWindow)
                {
                    window.Repaint();
                }
            }
        }

        /// <summary>
        /// Normalizes and removes invalid scripting defines from the provided array.
        /// </summary>
        public static string[] RemoveInvalidScriptingDefines(string[] defines)
        {
            // Converts to string and back to array to normalize, remove duplicates and empty entries.
            return ScriptingDefinesHelper.ConvertScriptingDefineStringToArray(
              ScriptingDefinesHelper.ConvertScriptingDefineArrayToString(defines));
        }

        /*
        * private helper functions
        */
        private static bool ContainsPlayerSetting(PlayerSettingsRequiringRestart[] playerSettings, PlayerSettingsRequiringRestart targetSetting)
        {
            foreach (PlayerSettingsRequiringRestart setting in playerSettings)
            {
                if (setting == targetSetting)
                {
                    return true;
                }
            }
            return false;
        }

        private static StringBuilder GetPlayerSettingNamesToEditorRestartPromptText(PlayerSettingsRequiringRestart[] settingsRequiringRestart)
        {
            var settingsText = new StringBuilder();
            foreach (PlayerSettingsRequiringRestart setting in settingsRequiringRestart)
            {
                var settingPromptText = setting switch
                {
                    PlayerSettingsRequiringRestart.IncrementalGC => "Incremental GC",
                    PlayerSettingsRequiringRestart.ActiveInputHandling => "Active Input Handling",
                    PlayerSettingsRequiringRestart.GraphicsJobs => "Graphics Jobs",
                    PlayerSettingsRequiringRestart.VirtualTexturing => "Virtual Texturing",
                    PlayerSettingsRequiringRestart.GraphicsAPI => "Graphics API",
                    _ => string.Empty
                };
                settingsText.AppendLine(settingPromptText);
            }
            return settingsText;
        }

        public static void InstallRequiredPackagesForClassicProfileIfRequired(BuildProfile profile, Func<string, string, bool> doesUserApprove)
        {
            if (!BuildProfileContext.IsClassicPlatformProfile(profile))
                return;

            var buildTarget = profile.GetIBuildTarget();
            if (buildTarget == null)
                return;
            if (!buildTarget.TryGetProperties<IBuildPlatformProperties>(out var properties))
                return;

            if (!properties.ShouldInstallRequiredPackagesOnActivationOfClassicPlatform)
                return;

            var packages = BuildTargetDiscovery.BuildPlatformInternalPackages(profile.platformGuid).requiredPackages;
            if (packages.Length == 0)
                return;

            var packageStringBuilder = new StringBuilder();
            var neededPackages = new List<string>();
            foreach (var package in packages)
            {
                var packageName = package.qualifiedName;
                if (!PackageManager.PackageInfo.IsPackageRegistered(packageName))
                {
                    packageStringBuilder.AppendLine().Append(packageName);
                    neededPackages.Add(packageName);
                }
            }

            if (neededPackages.Count == 0)
                return;

            if (doesUserApprove != null && !doesUserApprove.Invoke(buildTarget.DisplayName, packageStringBuilder.ToString()))
                return;

            PackageManager.Client.AddAndRemove(neededPackages.ToArray());
        }

        /// <summary>
        /// Updates the provided helpBox with the supported platform status for the given multi-target platform GUID.
        /// Used in the Platform Browser when not all supported platforms of a multi-target platform are installed to
        /// inform the users which platforms are installed and which are available for installation.
        /// </summary>
        public static bool UpdateHelpBoxForSupportedPlatformStatus(GUID platformGuid, HelpBox helpbox)
        {
            if (!BuildTargetDiscovery.TryGetSupportedPlatformGuids(platformGuid, out GUID[] supportedPlatformGuids))
                return false;

            var displayName = BuildTargetDiscovery.BuildPlatformDisplayName(platformGuid);
            var downloadPlatformGuid = new GUID();

            var installedPlatforms = new List<string>();
            var availablePlatforms = new List<string>();
            foreach (var guid in supportedPlatformGuids)
            {
                var name = GetClassicPlatformDisplayName(guid);
                if (IsModuleInstalled(guid))
                    installedPlatforms.Add(name);
                else
                {
                    if (downloadPlatformGuid.Empty())
                        downloadPlatformGuid = guid;

                    availablePlatforms.Add(name);
                }
            }

            if (availablePlatforms.Count == 0)
                return false;

            var installedPlatformDisplayNames = installedPlatforms.Count > 0
                ? $"{displayName} ({string.Join(", ", installedPlatforms)})" : displayName;
            var availablePlatformDisplayNames = $"({string.Join(", ", availablePlatforms)})";

            helpbox.text = string.Format(k_SupportedPlatformStatus, installedPlatformDisplayNames, availablePlatformDisplayNames) +
                "\n" + k_EditorWillNeedToBeReloaded;

            if (!BuildPlayerWindow.IsEditorInstalledWithHub() || !BuildTargetDiscovery.BuildPlatformCanBeInstalledWithHub(downloadPlatformGuid))
            {
                var url = BuildPlayerWindow.GetPlaybackEngineDownloadURL(downloadPlatformGuid);
                helpbox.buttonText = k_OpenDownloadPage.ToString();
                helpbox.onButtonClicked += () => Help.BrowseURL(url);
            }
            else
            {
                var url = BuildPlayerWindow.GetUnityHubModuleDownloadURL(downloadPlatformGuid);
                helpbox.buttonText = k_InstallModuleWithHub.ToString();
                helpbox.onButtonClicked += () => Help.BrowseURL(url);
            }

            return true;
        }

        static void UpdateHelpBoxForPlatformNotInstalled(GUID platformGuid, HelpBox helpbox)
        {
            var basePlatformGuid = BuildTargetDiscovery.GetBasePlatformGUID(platformGuid);
            var displayName = BuildTargetDiscovery.BuildPlatformDisplayName(basePlatformGuid);
            var downloadPlatformGuid = platformGuid;

            if (BuildTargetDiscovery.TryGetSupportedPlatformGuids(platformGuid, out var supportedPlatformGuids))
            {
                downloadPlatformGuid = supportedPlatformGuids[0];

                var supportedDisplayNames = new List<string>();
                foreach (var supportedGuid in supportedPlatformGuids)
                {
                    var baseSupportedGuid = BuildTargetDiscovery.GetBasePlatformGUID(supportedGuid);
                    var supportedDisplayName = BuildTargetDiscovery.BuildPlatformDisplayName(baseSupportedGuid);
                    if (!supportedDisplayNames.Contains(supportedDisplayName))
                        supportedDisplayNames.Add(supportedDisplayName);
                }
                displayName += " (" + string.Join(", ", supportedDisplayNames) + ")";
            }

            helpbox.text = string.Format(k_NoModuleLoaded, displayName) + "\n" + k_EditorWillNeedToBeReloaded;

            if (!BuildPlayerWindow.IsEditorInstalledWithHub() || !BuildTargetDiscovery.BuildPlatformCanBeInstalledWithHub(downloadPlatformGuid))
            {
                var url = BuildPlayerWindow.GetPlaybackEngineDownloadURL(downloadPlatformGuid);
                helpbox.buttonText = k_OpenDownloadPage.ToString();
                helpbox.onButtonClicked += () => Help.BrowseURL(url);
            }
            else
            {
                var url = BuildPlayerWindow.GetUnityHubModuleDownloadURL(downloadPlatformGuid);
                helpbox.buttonText = k_InstallModuleWithHub.ToString();
                helpbox.onButtonClicked += () => Help.BrowseURL(url);
            }
        }

        private static void UpdateHelpBoxForPlatformDisabledViaArguments(GUID platformId, HelpBox helpbox)
        {
            helpbox.text = string.Format(k_DerivedPlatformDisabled, BuildTargetDiscovery.BuildPlatformDisplayName(platformId));
        }

        private static void UpdateHelpBoxForPlatformPendingRestart(GUID platformId, HelpBox target)
        {
            var displayName = BuildTargetDiscovery.BuildPlatformDisplayName(platformId);
            target.text = string.Format(k_ModuleInstalled, displayName) + "\n" + k_RestartNeeded;

            target.buttonText = k_RestartEditor;
            target.onButtonClicked += EditorApplication.RestartEditorAndRecompileScripts;
        }

        private static void UpdateHelpBoxForPlatformContactSales(HelpBox helpbox, string text, string url)
        {
            helpbox.text = text;

            helpbox.buttonText = "Contact Sales";
            helpbox.onButtonClicked += () => Help.BrowseURL(url);
        }

        private static void UpdateHelpBoxForPlatformNotEnabled(GUID platformId, HelpBox helpbox)
        {
            var displayName = BuildTargetDiscovery.BuildPlatformDisplayName(platformId);
            helpbox.text = string.Format(k_DerivedPlatformInactive, displayName);

            helpbox.buttonText = k_ActivateDerivedPlatform.ToString();
            helpbox.onButtonClicked += () =>
            {
                EditorPrefs.SetInt(platformId.ToString(), 1);

                EditorPrefs.SetString("LastEnabledPlatformGUID", platformId.ToString());

                RequestScriptCompilation(BuildProfileContext.activeProfile);
            };
        }

        static void UpdateHelpBoxForSDKPlatformPackageNotInstalled(GUID platformId, HelpBox helpbox)
        {
            var displayName = BuildTargetDiscovery.BuildPlatformDisplayName(platformId);
            helpbox.text = string.Format(k_SDKPlatformPackageNotInstalled, displayName);
        }

        /// <summary>
        /// Truncates a string to fit within a specified UTF-8 byte count while preserving
        /// grapheme clusters (user-perceived characters).
        /// </summary>
        public static string TruncateUtf8StringByBytes(string input, int maxBytes)
        {
            if (string.IsNullOrEmpty(input) || maxBytes < 0)
                return string.Empty;

            var enumerator = StringInfo.GetTextElementEnumerator(input);
            var stringBuilder = new StringBuilder(input.Length);
            int byteCount = 0;

            while (enumerator.MoveNext())
            {
                var elem = enumerator.GetTextElement();
                var elemBytes = Encoding.UTF8.GetByteCount(elem);

                if (byteCount + elemBytes > maxBytes)
                    break;

                stringBuilder.Append(elem);
                byteCount += elemBytes;
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Removes Non-Required sub-assets from Build Profile.
        /// </summary>
        public static void RemoveNonRequiredSubAssets(BuildProfile profile)
        {
            var path = AssetDatabase.GetAssetPath(profile);
            if (string.IsNullOrEmpty(path))
                return;

            var subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in subAssets)
            {
                if (asset == null || asset == profile)
                    continue;

                if (Array.IndexOf(profile.requiredComponents, asset) > -1)
                    continue;

                AssetDatabase.RemoveObjectFromAsset(asset);
                UnityEngine.Object.DestroyImmediate(asset, true);
            }
        }
    }
}
