// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.Modules;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Internal utility class for Build Profile Module.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
    internal class BuildProfileModuleUtil
    {
        const string k_BuildSettingsPlatformIconFormat = "BuildSettings.{0}";
        static readonly string k_NoModuleLoaded = L10n.Tr("No {0} module loaded.");
        static readonly string k_EditorWillNeedToBeReloaded = L10n.Tr("Note: Editor will need to be restarted to load any newly installed modules");
        static readonly GUIContent k_OpenDownloadPage = EditorGUIUtility.TrTextContent("Open Download Page");
        static readonly GUIContent k_InstallModuleWithHub = EditorGUIUtility.TrTextContent("Install with Unity Hub");
        static Dictionary<string, BuildTargetDiscovery.DiscoveredTargetInfo> s_DiscoveredTargetInfos;

        /// <summary>
        /// Classic platform display name for a given build profile. Matching
        /// value in the old BuildSettings window.
        /// </summary>
        public static string GetClassicPlatformDisplayName(BuildProfile profile)
        {
            return ModuleManager.GetTargetStringFromBuildTarget(profile.buildTarget);
        }

        /// <summary>
        /// Fetch editor platform icon texture.
        /// </summary>
        public static Texture2D GetPlatformIcon(BuildTarget buildTarget, StandaloneBuildSubtarget subtarget)
        {
            if (subtarget == StandaloneBuildSubtarget.Server)
            {
                return EditorGUIUtility.LoadIcon(string.Format(k_BuildSettingsPlatformIconFormat, "DedicatedServer"));
            }

            if (s_DiscoveredTargetInfos == null)
            {
                InitializeDiscoveredTargetDict();
            }

            var targetString = ModuleManager.GetTargetStringFromBuildTarget(buildTarget);
            return EditorGUIUtility.LoadIcon(
                s_DiscoveredTargetInfos.TryGetValue(targetString, out var targetInfo)
                ? targetInfo.iconName : "BuildSettings.Editor");
        }

        /// <summary>
        /// Returns true if the module is installed and editor has permissions
        /// for the given build target.
        /// </summary>
        public static bool IsModuleInstalled(BuildTarget buildTarget, StandaloneBuildSubtarget subtarget)
        {
            // NamedBuildTarget will be deprecated. This code is extracted from
            // NamedBuildTarget.FromActiveSettings. Except instead of taking a dependency
            // on Editor User Build Settings, we use the passed subtarget.
            NamedBuildTarget namedTarget;
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

            return !BuildPlayerWindow.IsModuleNotInstalled(namedTarget, buildTarget);
        }

        /// <summary>
        /// Generate button and label for downloading a platform module.
        /// </summary>
        /// <see cref="BuildPlayerWindow.ShowNoModuleLabel"/>
        public static VisualElement CreateModuleNotInstalledElement(BuildTarget buildTarget, StandaloneBuildSubtarget subtarget)
        {
            var targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
            string moduleName = ModuleManager.GetTargetStringFrom(buildTarget);
            if (namedBuildTarget == NamedBuildTarget.Server)
                moduleName = moduleName.Replace("Standalone", "DedicatedServer");

            return new IMGUIContainer(
                () => BuildPlayerWindow.ShowNoModuleLabel(namedBuildTarget, buildTarget, moduleName,
                    k_NoModuleLoaded, k_OpenDownloadPage, k_InstallModuleWithHub, k_EditorWillNeedToBeReloaded));
        }

        public static IBuildProfileExtension GetBuildProfileExtension(BuildTarget buildTarget) =>
            ModuleManager.GetBuildProfileExtension(ModuleManager.GetTargetStringFromBuildTarget(buildTarget));

        static void InitializeDiscoveredTargetDict()
        {
            s_DiscoveredTargetInfos = new();
            foreach (var kvp in BuildTargetDiscovery.GetBuildTargetInfoList())
            {
                var targetString = ModuleManager.GetTargetStringFromBuildTarget(kvp.buildTargetPlatformVal);
                s_DiscoveredTargetInfos.TryAdd(targetString, kvp);
            }
        }
    }
}
