// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Modules;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using TargetAttributes = UnityEditor.BuildTargetDiscovery.TargetAttributes;
using UnityEditor.Profiling;

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
        static readonly string k_NoModuleLoaded = L10n.Tr("No {0} module loaded.");
        static readonly string k_EditorWillNeedToBeReloaded = L10n.Tr("Note: Editor will need to be restarted to load any newly installed modules");
        static readonly string k_BuildProfileRecompileReason = L10n.Tr("Active build profile scripting defines changes.");
        static readonly GUIContent k_OpenDownloadPage = EditorGUIUtility.TrTextContent("Open Download Page");
        static readonly GUIContent k_InstallModuleWithHub = EditorGUIUtility.TrTextContent("Install with Unity Hub");
        static HashSet<string> s_BuildProfileIconModules = new()
        {
            "Switch",
        };

        /// <summary>
        /// Classic platform display name for a given build profile.
        /// </summary>
        public static string GetClassicPlatformDisplayName(string platformId) =>
            GetModuleDisplayName(platformId);

        /// <summary>
        /// Fetch default editor platform icon texture.
        /// </summary>
        public static Texture2D GetPlatformIcon(string platformId)
        {
            if (LoadBuildProfileIcon(platformId, out Texture2D icon))
                return icon;

            return EditorGUIUtility.LoadIcon(GetPlatformIconId(platformId));
        }

        /// <summary>
        /// Fetch small (16x16) editor platform icon texture.
        /// </summary>
        public static Texture2D GetPlatformIconSmall(string platformId)
        {
            if (LoadBuildProfileIcon(platformId, out Texture2D icon))
                return icon;

            return EditorGUIUtility.LoadIcon(GetPlatformIconId(platformId) + ".Small");
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
        /// for the given build target.
        /// </summary>
        public static bool IsModuleInstalled(string platformId)
        {
            var (buildTarget, _) = GetBuildTargetAndSubtarget(platformId);
            var moduleName = GetModuleName(buildTarget);

            bool installed = BuildTargetDiscovery.BuildPlatformIsInstalled(new GUID(platformId));
            return installed
                && BuildPipeline.LicenseCheck(buildTarget)
                && !string.IsNullOrEmpty(moduleName)
                && ModuleManager.GetBuildProfileExtension(moduleName) != null;
        }

        /// <summary>
        /// Returns true if an installed module supports build profiles.
        /// </summary>
        public static bool IsBuildProfileSupported(string platformId)
        {
            var moduleName = GetModuleName(platformId);
            return ModuleManager.GetBuildProfileExtension(moduleName) != null;
        }

        /// <summary>
        /// Generate button and label for downloading a platform module.
        /// </summary>
        /// <see cref="BuildPlayerWindow.ShowNoModuleLabel"/>
        public static VisualElement CreateModuleNotInstalledElement(string platformId)
        {
            var (buildTarget, subtarget) = GetBuildTargetAndSubtarget(platformId);
            var moduleName = GetModuleName(buildTarget);
            var targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            var namedBuildTarget = (subtarget == StandaloneBuildSubtarget.Server)
                ? NamedBuildTarget.Server
                : NamedBuildTarget.FromBuildTargetGroup(targetGroup);

            if (namedBuildTarget == NamedBuildTarget.Server)
                moduleName = moduleName.Replace("Standalone", "DedicatedServer");

            return new IMGUIContainer(
                () => BuildPlayerWindow.ShowNoModuleLabel(namedBuildTarget, buildTarget, moduleName,
                    k_NoModuleLoaded, k_OpenDownloadPage, k_InstallModuleWithHub, k_EditorWillNeedToBeReloaded));
        }

        /// <summary>
        /// Exported from <see cref="BuildPlayerWindow"/>, UI code specifically for when current license does not cover
        /// BuildTarget.
        /// </summary>
        /// <returns>null when no license errors, else license check UI</returns>
        public static VisualElement CreateLicenseNotFoundElement(string platformId)
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
            if (!BuildTargetDiscovery.PlatformHasFlag(buildTarget, TargetAttributes.IsStandalonePlatform))
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
        /// Retrieve the respective module name for a build target
        /// </summary>
        public static string GetModuleName(BuildTarget buildTarget)
        {
            return BuildTargetDiscovery.GetModuleNameForBuildTarget(buildTarget);
        }

        public static string GetModuleName(string platformId)
        {
            var buildTarget = GetBuildTargetAndSubtarget(platformId).Item1;
            return GetModuleName(buildTarget);
        }

        /// <summary>
        /// Internal method for switching <see cref="EditorUserBuildSettings"/> active build target and subtarget.
        /// </summary>
        public static void SwitchLegacyActiveFromBuildProfile(BuildProfile profile)
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(profile.buildTarget);

            if (buildTargetGroup == BuildTargetGroup.Standalone)
            {
                EditorUserBuildSettings.SwitchActiveBuildTargetAndSubtarget(
                    profile.buildTarget,
                    (int)profile.subtarget);
                return;
            }

            // Subtarget fetched by EditorUserBuildSettings maps to the active build profile.
            EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, profile.buildTarget);
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

            BuildTarget buildTarget = activeProfile.buildTarget;
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            int subtarget = EditorUserBuildSettings.GetActiveSubtargetFor(buildTarget);

            options.options = GetBuildOptions(buildTarget, buildTargetGroup, buildLocation, customBuildOptions);
            options.target = buildTarget;
            options.subtarget = subtarget;
            options.targetGroup = buildTargetGroup;
            options.locationPathName = buildLocation;
            options.assetBundleManifestPath = assetBundleManifestPath ?? PostprocessBuildPlayer.GetStreamingAssetsBundleManifestPath();
            options.scenes = EditorBuildSettingsScene.GetActiveSceneList(activeProfile.scenes);

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
                if (EditorUserBuildSettings.connectProfiler && (developmentBuild || (iBuildTarget.PlayerConnectionPlatformProperties?.ForceAllowProfilerConnection ?? false)) )
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

        public static IBuildProfileExtension GetBuildProfileExtension(string moduleName) =>
            ModuleManager.GetBuildProfileExtension(moduleName);

        public static GUIStyle dropDownToggleButton => EditorStyles.dropDownToggleButton;

        /// <summary>
        /// Returns all discovered platform keys that are possible Build Profile targets.
        /// </summary>
        public static List<string> FindAllViewablePlatforms()
        {
            var result = new List<string>();

            foreach (var platformGuid in BuildTargetDiscovery.GetAllPlatforms())
            {
                var installed = BuildTargetDiscovery.BuildPlatformIsInstalled(platformGuid);
                if (!installed && BuildTargetDiscovery.BuildPlatformIsHiddenInUI(platformGuid))
                    continue;

                result.Add(platformGuid.ToString());
            }

            // Swap current editor standalone platform to the top.
            if (Application.platform == RuntimePlatform.OSXEditor)
                result.Reverse(0, 2);
            if (Application.platform == RuntimePlatform.LinuxEditor)
                result.Reverse(0, 3);

            return result;
        }

        /// <summary>
        /// Check if the user is able to build his VT-enabled Player for a target platform
        /// </summary>
        public static bool IsVirtualTexturingSettingsValid(BuildTarget buildTarget)
        {
            if (!PlayerSettings.GetVirtualTexturingSupportEnabled())
            {
                return true;
            }

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
            var activeProfile = BuildProfile.GetActiveBuildProfile();
            if (activeProfile != null && activeProfile.buildTarget == buildTarget && activeProfile.subtarget == standaloneBuildSubtarget)
                return activeProfile.GetLastRunnableBuildPathKey();

            var classicProfile = BuildProfileContext.instance.GetForClassicPlatform(buildTarget, standaloneBuildSubtarget);
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

        static bool LoadBuildProfileIcon(string platformId, out Texture2D icon)
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

        static string GetPlatformIconId(string platformId)
        {
            var iconName = BuildTargetDiscovery.BuildPlatformIconName(new GUID(platformId));

            if (string.IsNullOrEmpty(iconName))
                return "BuildSettings.Editor";

            return iconName;
        }

        static string GetModuleDisplayName(string platformId)
        {
            return BuildTargetDiscovery.BuildPlatformDisplayName(new GUID(platformId));
        }

        /// <summary>
        /// Get build profile path from the last runnable build key
        /// </summary>
        static string GetAssetPathFromLastRunnableBuildKey(string key)
        {
            int lastUnderscoreIndex = key.LastIndexOf(k_LastRunnableBuildPathSeparator);
            return lastUnderscoreIndex != -1 ? key[(lastUnderscoreIndex + 1)..] : string.Empty;
        }

        public static string GetPlatformId(BuildTarget buildTarget, StandaloneBuildSubtarget subtarget)
        {
            var platformGuid = subtarget == StandaloneBuildSubtarget.Server && IsStandalonePlatform(buildTarget)
                ? BuildTargetDiscovery.GetGUIDFromBuildTarget(NamedBuildTarget.Server, buildTarget)
                : BuildTargetDiscovery.GetGUIDFromBuildTarget(buildTarget);
            return platformGuid.ToString();
        }

        public static (BuildTarget, StandaloneBuildSubtarget) GetBuildTargetAndSubtarget(string platformId)
        {
            return BuildTargetDiscovery.GetBuildTargetAndSubtargetFromGUID(new GUID(platformId));
        }
    }
}
