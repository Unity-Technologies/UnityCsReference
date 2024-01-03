// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.Modules;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using TargetAttributes = UnityEditor.BuildTargetDiscovery.TargetAttributes;

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
        const string k_BuildSettingsPlatformIconFormat = "BuildSettings.{0}";
        static readonly string k_NoModuleLoaded = L10n.Tr("No {0} module loaded.");
        static readonly string k_EditorWillNeedToBeReloaded = L10n.Tr("Note: Editor will need to be restarted to load any newly installed modules");
        static readonly GUIContent k_OpenDownloadPage = EditorGUIUtility.TrTextContent("Open Download Page");
        static readonly GUIContent k_InstallModuleWithHub = EditorGUIUtility.TrTextContent("Install with Unity Hub");
        static Dictionary<string, BuildTargetDiscovery.DiscoveredTargetInfo> s_DiscoveredTargetInfos = InitializeDiscoveredTargetDict();
        static HashSet<string> s_BuildProfileIconModules = new()
        {
            "Switch",
            "QNX",
            "PS4",
            "PS5"
        };

        /// <summary>
        /// Classic platform display name for a given build profile. Matching
        /// value in the old BuildSettings window.
        /// </summary>
        /// <see cref="BuildPlayerWindow"/>
        public static string GetClassicPlatformDisplayName(string moduleName, StandaloneBuildSubtarget subtarget) =>
            (moduleName, subtarget) switch
            {
                ("OSXStandalone", StandaloneBuildSubtarget.Server) => "Mac Server",
                ("WindowsStandalone", StandaloneBuildSubtarget.Server) => "Windows Server",
                ("LinuxStandalone", StandaloneBuildSubtarget.Server) => "Linux Server",
                ("OSXStandalone", _) => "Mac",
                ("WindowsStandalone", _) => "Windows",
                ("LinuxStandalone", _) => "Linux",
                _ => GetModuleDisplayName(moduleName)
            };

        /// <summary>
        /// Fetch default editor platform icon texture.
        /// </summary>
        public static Texture2D GetPlatformIcon(string moduleName, StandaloneBuildSubtarget subtarget)
        {
            if (LoadBuildProfileIcon(moduleName, out Texture2D icon))
                return icon;

            return EditorGUIUtility.LoadIcon(GetPlatformIconId(moduleName, subtarget));
        }

        /// <summary>
        /// Fetch small (16x16) editor platform icon texture.
        /// </summary>
        public static Texture2D GetPlatformIconSmall(string moduleName, StandaloneBuildSubtarget subtarget)
        {
            if (LoadBuildProfileIcon(moduleName, out Texture2D icon))
                return icon;

            return EditorGUIUtility.LoadIcon(GetPlatformIconId(moduleName, subtarget) + ".Small");
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
        public static bool IsModuleInstalled(string moduleName, StandaloneBuildSubtarget subtarget)
        {
            // NamedBuildTarget will be deprecated. This code is extracted from
            // NamedBuildTarget.FromActiveSettings. Except instead of taking a dependency
            // on Editor User Build Settings, we use the passed subtarget.
            NamedBuildTarget namedTarget;
            var buildTarget = GetBuildTarget(moduleName);
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            if (buildTargetGroup == BuildTargetGroup.Standalone
                && subtarget == StandaloneBuildSubtarget.Server)
            {
                namedTarget = NamedBuildTarget.Server;
            }
            else
            {
                namedTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            }

            bool installed = BuildPlatforms.instance.BuildPlatformFromNamedBuildTarget(namedTarget).installed;
            return installed
                && BuildPipeline.LicenseCheck(buildTarget)
                && !string.IsNullOrEmpty(moduleName)
                && ModuleManager.GetBuildPostProcessor(moduleName) != null;
        }

        /// <summary>
        /// Returns true if an installed module supports build profiles.
        /// </summary>
        public static bool IsBuildProfileSupported(string moduleName, StandaloneBuildSubtarget subtarget)
        {
            return ModuleManager.GetBuildProfileExtension(moduleName) != null;
        }

        /// <summary>
        /// Generate button and label for downloading a platform module.
        /// </summary>
        /// <see cref="BuildPlayerWindow.ShowNoModuleLabel"/>
        public static VisualElement CreateModuleNotInstalledElement(string moduleName, StandaloneBuildSubtarget subtarget)
        {
            var buildTarget = GetBuildTarget(moduleName);
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
        public static VisualElement CreateLicenseNotFoundElement(string moduleName)
        {
            var buildTarget = GetBuildTarget(moduleName);
            if (BuildPipeline.LicenseCheck(buildTarget))
                return null;

            var targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
            string niceName = BuildPipeline.GetBuildTargetGroupDisplayName(namedBuildTarget.ToBuildTargetGroup());
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
            var label = new Label(string.Format(licenseMsg, niceName));
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

        /// <summary>
        /// Internal method for switching <see cref="EditorUserBuildSettings"/> active build target and subtarget.
        /// </summary>
        public static void SwitchLegacyActiveFromBuildProfile(BuildProfile profile)
        {
            EditorUserBuildSettings.SwitchActiveBuildTargetAndSubtarget(
                profile.buildTarget,
                (int)profile.subtarget);
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

        public static IBuildProfileExtension GetBuildProfileExtension(BuildTarget buildTarget) =>
            ModuleManager.GetBuildProfileExtension(ModuleManager.GetTargetStringFromBuildTarget(buildTarget));

        public static GUIStyle dropDownToggleButton => EditorStyles.dropDownToggleButton;

        /// <summary>
        /// Returns all discovered platform keys that are possible Build Profile targets.
        /// </summary>
        public static List<(string, StandaloneBuildSubtarget)> FindAllViewablePlatforms()
        {
            string windows = ModuleManager.GetTargetStringFrom(BuildTarget.StandaloneWindows64);
            string osx = ModuleManager.GetTargetStringFrom(BuildTarget.StandaloneOSX);
            string linux = ModuleManager.GetTargetStringFrom(BuildTarget.StandaloneLinux64);
            var result = new List<(string, StandaloneBuildSubtarget)>()
            {
                (windows, StandaloneBuildSubtarget.Player),
                (osx, StandaloneBuildSubtarget.Player),
                (linux, StandaloneBuildSubtarget.Player),
                (windows, StandaloneBuildSubtarget.Server),
                (osx, StandaloneBuildSubtarget.Server),
                (linux, StandaloneBuildSubtarget.Server)
            };

            // Swap current editor standalone platform to the top.
            if (Application.platform == RuntimePlatform.OSXEditor)
                result.Reverse(0, 2);
            if (Application.platform == RuntimePlatform.LinuxEditor)
                result.Reverse(0, 3);

            foreach (var buildTargetInfo in BuildTargetDiscovery.GetBuildTargetInfoList())
            {
                if (buildTargetInfo.HasFlag(TargetAttributes.IsStandalonePlatform))
                    continue;

                // installed platform check from BuildPlatforms
                bool installed = BuildPipeline.GetPlaybackEngineDirectory(buildTargetInfo.buildTargetPlatformVal, BuildOptions.None, false) != string.Empty;
                if (!installed && buildTargetInfo.HasFlag(TargetAttributes.HideInUI))
                    continue;

                // buildTargetInfo may be missing module name for some target platforms.
                var moduleName = ModuleManager.GetTargetStringFromBuildTarget(buildTargetInfo.buildTargetPlatformVal);
                result.Add((moduleName, StandaloneBuildSubtarget.Default));
            }

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

        /// Retrieve string of filename invalid characters
        /// </summary>
        /// <returns></returns>
        public static string GetFilenameInvalidCharactersStr()
        {
            return EditorUtility.GetInvalidFilenameChars();
        }

        internal static BuildTarget GetBuildTarget(string moduleName)
        {
            return s_DiscoveredTargetInfos[moduleName].buildTargetPlatformVal;
        }

        static Dictionary<string, BuildTargetDiscovery.DiscoveredTargetInfo> InitializeDiscoveredTargetDict()
        {
            var result = new Dictionary<string, BuildTargetDiscovery.DiscoveredTargetInfo>();
            foreach (var kvp in BuildTargetDiscovery.GetBuildTargetInfoList())
            {
                var targetString = ModuleManager.GetTargetStringFromBuildTarget(kvp.buildTargetPlatformVal);
                result.TryAdd(targetString, kvp);
            }
            return result;
        }

        static bool LoadBuildProfileIcon(string moduleName, out Texture2D icon)
        {
            if (s_BuildProfileIconModules.Contains(moduleName))
            {
                icon = EditorGUIUtility.FindTexture(typeof(BuildProfile));
                return true;
            }

            icon = null;
            return false;
        }

        static string GetPlatformIconId(string moduleName, StandaloneBuildSubtarget subtarget)
        {
            if (subtarget == StandaloneBuildSubtarget.Server)
            {
                return string.Format(k_BuildSettingsPlatformIconFormat, "DedicatedServer");
            }

            if (s_DiscoveredTargetInfos.TryGetValue(moduleName, out var targetInfo))
            {
                return targetInfo.iconName;
            }

            return "BuildSettings.Editor";
        }

        /// <summary>
        /// Module display name as defined on native side in "BuildTargetGroupName.h"
        /// </summary>
        static string GetModuleDisplayName(string moduleName)
        {
            if (!s_DiscoveredTargetInfos.TryGetValue(moduleName, out var gt))
                return moduleName;

            return BuildPipeline.GetBuildTargetGroupDisplayName(BuildPipeline.GetBuildTargetGroup(gt.buildTargetPlatformVal));
        }
    }
}
