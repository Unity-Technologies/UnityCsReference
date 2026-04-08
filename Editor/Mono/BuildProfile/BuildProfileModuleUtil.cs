// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor.Modules;
using UnityEditor.PackageManager.UI.Internal;
using UnityEditor.Rendering;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using TargetAttributes = UnityEditor.BuildTargetDiscovery.TargetAttributes;
using PlatformPackageList = UnityEditor.BuildTargetDiscovery.PlatformPackageList;
using InternalEditorUtility = UnityEditorInternal.InternalEditorUtility;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Internal utility class for Build Profile Module.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
    internal class BuildProfileModuleUtil
    {
        const string k_BuyProUrl = "https://store.unity.com/products/unity-pro";
        const string k_ConsoleModuleUrl = "https://unity3d.com/platform-installation";
        const string k_LastRunnableBuildPathSeparator = "_";
        const string k_StyleSheet = "BuildProfile/StyleSheets/BuildProfile.uss";
        const string k_HeroPathPrefix = "BuildProfile/Hero/";
        const string k_HeroPathSuffix = ".Hero";
        // The asset database supports file name length to max. 250 symbols
        // Leave 5 symbols for the GenerateUniqueAssetPath() that adds " (1)"(2,3...) in case
        // an asset with such name already exists.
        public const int k_MaxAssetFileNameLength = 245;
        // For UI cases where the extension `.asset` is not taken into consideration
        public const int k_MaxAssetFileNameLengthWithoutExtension = k_MaxAssetFileNameLength - 6;
        static readonly string k_NoModuleLoaded = L10n.Tr("No {0} module loaded.");
        static readonly string k_DerivedPlatformInactive = L10n.Tr("{0} is currently disabled.");
        static readonly string k_DerivedPlatformDisabled = L10n.Tr("{0} was disabled via command-line arguments.");
        static readonly string k_EditorWillNeedToBeReloaded = L10n.Tr("Note: Editor will need to be restarted to load any newly installed modules");
        static readonly string k_BuildProfileRecompileReason = L10n.Tr("Active build profile scripting defines changes.");
        static readonly GUIContent k_OpenDownloadPage = EditorGUIUtility.TrTextContent("Open Download Page");
        static readonly GUIContent k_InstallModuleWithHub = EditorGUIUtility.TrTextContent("Install with Unity Hub");
        static readonly GUIContent k_ActivateDerivedPlatform = EditorGUIUtility.TrTextContent("Enable Platform");
        static HashSet<string> s_BuildProfileIconModules = new()
        {
            "Switch",
        };

        /// <summary>
        /// Internal callback for BuildProfileModule to be notified of
        /// reset initiated through the inspector context menu.
        /// </summary>
        public static event Action<BuildProfile> OnUpdateActiveEditors;

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
        /// Platform description.
        /// </summary>
        public static string GetPlatformDescription(GUID platformGuid) =>
            BuildTargetDiscovery.BuildPlatformDescription(platformGuid);

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

        /// <summary>
        /// Fetches the Hero image associated with the platform.
        /// If no specific hero image is found, it falls back to the platform icon.
        /// </summary>
        public static Texture2D GetPlatformIconHero(GUID platformId)
        {
            if (LoadBuildProfileIcon(platformId, out Texture2D icon))
                return icon;

            // Attempt to load the hero icon for the platform
            icon = EditorGUIUtility.LoadIcon($"{k_HeroPathPrefix}{GetPlatformIconId(platformId)}{k_HeroPathSuffix}");

            // If no specific hero icon is found, fallback to the platform icon
            if (icon == null)
                icon = GetPlatformIcon(platformId);

            return icon;
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
            return EditorGUIUtility.LoadIcon("d_console.warnicon.sml");
        }

        /// <summary>
        /// Returns true if the module is installed and editor has permissions
        /// for the given platform guid.
        /// </summary>
        public static bool IsModuleInstalled(GUID platformId)
        {
            var (buildTarget, _) = GetBuildTargetAndSubtarget(platformId);

            bool installed = BuildTargetDiscovery.BuildPlatformIsInstalled(platformId);
            return installed
                && BuildPipeline.LicenseCheck(buildTarget)
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
            return ModuleManager.GetBuildProfileExtension(platformId) != null;
        }

        [Obsolete("Do not use internal APIs from packages.")]
        public static bool IsBuildProfileSupported(string platformId)
        {
            return IsBuildProfileSupported(new GUID(platformId));
        }

        /// <summary>
        /// Generate button and label for downloading a platform module.
        /// </summary>
        /// <see cref="BuildPlayerWindow.ShowNoModuleLabel"/>
        public static VisualElement CreateModuleNotInstalledElement(GUID platformId)
        {
            var buildProfileWindowUss = EditorGUIUtility.LoadRequired(k_StyleSheet) as StyleSheet;
            VisualElement container = new VisualElement();

            container.styleSheets.Add(buildProfileWindowUss);
            container.AddClasses("flex-row align-items-center flex-wrap full-width");

            if (BuildTargetDiscovery.BuildPlatformIsInstalled(platformId) && BuildTargetDiscovery.BuildPlatformIsDerivedPlatform(platformId))
            {
                return GetPlatformNotEnabledElement(platformId, container);
            }

            // TODO: this is a workaround for onboarding instructions to fix EmbeddedLinux and QNX
            // needs to be removed when https://jira.unity3d.com/browse/PLAT-7721 is implemented
            if (BuildTargetDiscovery.BuildPlatformTryGetCustomInstallLinkAndText(platformId, out var url, out var text))
            {
                return GetPlatformContactSalesElement(container, text, url);
            }

            if (IsBuildProfileDisabledViaArguments(platformId))
            {
                return GetPlatformDisabledViaArgumentsElement(platformId, container);
            }

            return GetPlatformNotInstalledElement(platformId, container);
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
        /// Exported from <see cref="BuildPlayerWindow"/>, UI code specifically for when current license does not cover
        /// BuildTarget.
        /// </summary>
        /// <returns>null when no license errors, else license check UI</returns>
        public static VisualElement CreateLicenseNotFoundElement(GUID platformId)
        {
            var buildTarget = GetBuildTargetAndSubtarget(platformId).Item1;
            if (BuildPipeline.LicenseCheck(buildTarget))
                return null;

            string displayName = GetModuleDisplayName(platformId);
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

            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Column;
            var label = new Label(string.Format(licenseMsg, displayName));
            label.style.whiteSpace = WhiteSpace.Normal;
            root.Add(label);
            if (!IsStandalonePlatform(buildTarget))
            {
                var button = new Button(() => Application.OpenURL(url));
                button.style.width = 200;
                button.text = buttonMsg;
                root.Add(button);
            }
            return root;
        }

        public static bool IsStandalonePlatform(BuildTarget buildTarget) =>
            BuildTargetDiscovery.PlatformHasFlag(buildTarget, TargetAttributes.IsStandalonePlatform);

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

            return options;
        }

        internal static BuildOptions GetBuildOptions(BuildTarget buildTarget, BuildTargetGroup buildTargetGroup, string buildLocation, BuildOptions options = BuildOptions.None)
        {
            // Check if Lz4 is supported for the current buildtargetgroup and enable it if need be
            if (PostprocessBuildPlayer.SupportsLz4Compression(buildTarget))
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

        public static bool IsPlatformVisibleInPlatformBrowserOnly(GUID platformGuid)
        {
            return BuildTargetDiscovery.BuildPlatformIsVisibleInPlatformBrowserOnly(platformGuid);
        }

        public static bool IsPlatformAvailableOnHostPlatform(GUID platformGuid, OperatingSystemFamily operatingSystemFamily)
        {
            return BuildTargetDiscovery.BuildPlatformIsAvailableOnHostPlatform(platformGuid, SystemInfo.operatingSystemFamily);
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
        /// Recompile scripts if the active build profile scripting defines
        /// differs from the last compilation defines.
        /// </summary>
        static void TryRecompileScripts()
        {
            if (EditorApplication.isCompiling)
                return;

            PlayerSettings.RecompileScripts(k_BuildProfileRecompileReason);
        }

        [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
        internal static void SuppressMissingTypeWarning()
        {
            SerializationUtility.SuppressMissingTypeWarning(nameof(BuildProfile));
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
        /// Attempts to load the Mip mapped icon for the given platformId.
        /// </summary>
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

        public static string GetPlatformColorString(GUID platformGuid)
        {
            return BuildTargetDiscovery.GetPlatformColorString(platformGuid);
        }


        public static string[] GetAllPlatformPackageNames()
        {
            return BuildTargetDiscovery.GetAllPlatformPackageNames();
        }

        public static string BuildPlatformDescription(GUID platformGuid)
        {
            return BuildTargetDiscovery.BuildPlatformDescription(platformGuid);
        }

        public static void OnActiveProfileGraphicsSettingsChanged(bool hasGraphicsSettings)
        {
            EditorGraphicsSettings.activeProfileHasGraphicsSettings = hasGraphicsSettings;
            GraphicsSettingsInspector.OnActiveProfileGraphicsSettingsChanged?.Invoke();
        }

        internal static void RemoveQualityLevelFromAllProfiles(string qualityLevelName)
        {
            var profiles = GetAllBuildProfiles();
            foreach (var profile in profiles)
            {
                if (profile.qualitySettings == null)
                    continue;

                profile.qualitySettings.RemoveQualityLevel(qualityLevelName);
            }
        }

        internal static void RenameQualityLevelInAllProfiles(string oldName, string newName)
        {
            var profiles = GetAllBuildProfiles();
            foreach (var profile in profiles)
            {
                if (profile.qualitySettings == null)
                    continue;

                profile.qualitySettings.RenameQualityLevel(oldName, newName);
            }
        }

        /// <summary>
        /// Get all custom build profiles in the project.
        /// </summary>
        public static List<BuildProfile> GetAllBuildProfiles()
        {
            var alreadyLoadedBuildProfiles = Resources.FindObjectsOfTypeAll<BuildProfile>();

            const string buildProfileAssetSearchString = $"t:{nameof(BuildProfile)}";
            var assetsGuids = AssetDatabase.FindAssets(buildProfileAssetSearchString);
            var result = new List<BuildProfile>(assetsGuids.Length);

            // Suppress missing type warning thrown by serialization. This could happen
            // when the build profile window is opened, then entering play mode and the
            // module for that profile is not installed.
            BuildProfileModuleUtil.SuppressMissingTypeWarning();

            foreach (var guid in assetsGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                BuildProfile profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(path);
                if (profile == null)
                {
                    Debug.LogWarning($"[BuildProfile] Failed to load asset at path: {path}");
                    continue;
                }

                result.Add(profile);
            }

            foreach (var buildProfile in alreadyLoadedBuildProfiles)
            {
                // Asset database will not give us any build profiles that get created in memory
                // and we need to include them in this list as we use it to detect that build profiles
                // have been destroyed and destroy their resources like PlayerSettings afterwards.
                // Skipping the in-memory build profiles will result in us deleting their associated
                // player settings object while it's being used and will lead to a crash (UUM-77423)
                if (buildProfile &&
                    !BuildProfileContext.IsClassicPlatformProfile(buildProfile) &&
                    !BuildProfileContext.IsSharedProfile(buildProfile.platformGuid) &&
                    !EditorUtility.IsPersistent(buildProfile))
                {
                    result.Add(buildProfile);
                }
            }

            return result;
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

        static VisualElement GetPlatformNotInstalledElement(GUID platformGuid, VisualElement container)
        {
            var basePlatformGuid = BuildTargetDiscovery.GetBasePlatformGUID(platformGuid);
            var displayName = BuildTargetDiscovery.BuildPlatformDisplayName(basePlatformGuid);

            var platformNotLoadedLabel = new Label(string.Format(k_NoModuleLoaded, displayName));
            var editorReloadNeededLabel = new Label(k_EditorWillNeedToBeReloaded);
            var labels = new VisualElement();

            labels.AddClasses("flex-column flex-grow-1");
            editorReloadNeededLabel.AddClasses("text-small wrap");

            labels.Add(platformNotLoadedLabel);
            labels.Add(editorReloadNeededLabel);

            var url = string.Empty;
            Button button;

            if (!BuildPlayerWindow.IsEditorInstalledWithHub() || !BuildTargetDiscovery.BuildPlatformCanBeInstalledWithHub(platformGuid))
            {
                button = new Button(() =>
                {
                    url = BuildPlayerWindow.GetPlaybackEngineDownloadURL(platformGuid);
                    Help.BrowseURL(url);
                })
                {
                    text = k_OpenDownloadPage.ToString()
                };
            }
            else
            {
                button = new Button(() =>
                {
                    url = BuildPlayerWindow.GetUnityHubModuleDownloadURL(platformGuid);
                    Help.BrowseURL(url);
                })
                {
                    text = k_InstallModuleWithHub.ToString()
                };
            }

            container.Add(labels);
            container.Add(button);

            return container;
        }

        private static VisualElement GetPlatformDisabledViaArgumentsElement(GUID platformId, VisualElement container)
        {
            container.Add(new Label(string.Format(k_DerivedPlatformDisabled,
                BuildTargetDiscovery.BuildPlatformDisplayName(platformId))));

            return container;
        }

        private static VisualElement GetPlatformContactSalesElement(VisualElement container, string text, string url)
        {
            var label = new Label(text);

            label.AddClasses("flex-grow-1 flex-shrink-1 wrap");

            container.Add(label);
            container.Add(new Button(() =>
            {
                Help.BrowseURL(url);
            })
            {
                text = "Contact Sales"
            });

            return container;
        }

        private static VisualElement GetPlatformNotEnabledElement(GUID platformId, VisualElement container)
        {
            var label = new Label(string.Format(k_DerivedPlatformInactive,
                BuildTargetDiscovery.BuildPlatformDisplayName(platformId)));

            label.AddToClassList("flex-grow-1");

            container.Add(label);
            container.Add(new Button(() =>
            {
                EditorPrefs.SetInt(platformId.ToString(), 1);

                // Store the platformId to reselect this platform after recompilation
                EditorPrefs.SetString("LastEnabledPlatformGUID", platformId.ToString());

                RequestScriptCompilation(BuildProfileContext.activeProfile);
            })
            {
                text = k_ActivateDerivedPlatform.ToString()
            });

            return container;
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
    }
}
