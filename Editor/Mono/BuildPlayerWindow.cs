// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor.Build;
using UnityEditorInternal;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using UnityEditor.Modules;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UnityEditor.Build.Profile;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor
{
    public partial class BuildPlayerWindow : EditorWindow
    {
        [AutoStaticsCleanupOnCodeReload]
        internal static event Action<BuildProfile> drawingMultiplayerBuildOptions;

        [UsedImplicitly, RequiredByNativeCode]
        public static void ShowBuildPlayerWindow()
        {
            BuildPipeline.ShowBuildProfileWindow();
        }

        internal static bool WillDrawMultiplayerBuildOptions() => drawingMultiplayerBuildOptions != null;

        internal static void DrawMultiplayerBuildOption(BuildProfile buildProfile)
        {
            drawingMultiplayerBuildOptions?.Invoke(buildProfile);
        }

        static bool BuildLocationIsValid(string path)
        {
            return path.Length > 0 && Directory.Exists(FileUtil.DeleteLastPathNameComponent(path));
        }

        [RequiredByNativeCode]
        static bool BuildPlayerAndRunEnabled()
        {
            var buildTarget = EditorUserBuildSettingsUtils.CalculateSelectedBuildTarget();
            NamedBuildTarget namedBuildTarget = EditorUserBuildSettingsUtils.CalculateSelectedNamedBuildTarget();
            BuildPlatform platform = BuildPlatforms.instance.BuildPlatformFromNamedBuildTarget(namedBuildTarget);
            string module = ModuleManager.GetTargetStringFrom(buildTarget);
            IBuildWindowExtension buildWindowExtension = ModuleManager.GetBuildWindowExtension(module);

            bool buildPlayerAndRunEnabled = buildWindowExtension != null ? buildWindowExtension.EnabledBuildAndRunButton() && !(EditorUserBuildSettings.installInBuildFolder) : !(EditorUserBuildSettings.installInBuildFolder);
            return buildPlayerAndRunEnabled;
        }

        // This overload is used by the Build & Run menu item & hot key - prompt for location only if the configured
        // output location is not valid.
        [UsedImplicitly, RequiredByNativeCode]
        static void BuildPlayerAndRun()
        {
            // When classic platforms are hidden, an active build profile is required.
            if (EditorSettings.hideBuildProfileClassicPlatforms && BuildProfileContext.activeProfile == null)
            {
                if (EditorUtility.DisplayDialog(
                    L10n.Tr("Active Build Profile Required", null),
                    L10n.Tr("To build players, you must first create and activate a Build Profile.\n\nWould you like to open the Build Profile window now?", null),
                    L10n.Tr("Open Build Profile Window", null),
                    L10n.Tr("Cancel", null)))
                {
                    BuildPipeline.ShowBuildProfileWindowAndRequireActiveProfile();
                    return;
                }
                else
                {
                    return;
                }
            }

            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var lastBuildLocation = EditorUserBuildSettings.GetBuildLocation(buildTarget);
            bool buildLocationIsValid = BuildLocationIsValid(lastBuildLocation);

            if (buildLocationIsValid && BuildTargetDiscovery.TryGetBuildTarget(buildTarget, out var iBuildTarget))
            {
                string buildLocation = iBuildTarget.BuildPlatformProperties?.ValidateBuildLocation();
                if (buildLocation != null)
                    EditorUserBuildSettings.SetBuildLocation(buildTarget, buildLocation);
            }

            BuildPlayerAndRunInternal(!buildLocationIsValid);
        }

        // This overload is used by the default player window, to always prompt for build location
        static void BuildPlayerAndRunInternal(bool askForBuildLocation)
        {
            CallBuildMethods(askForBuildLocation, BuildOptions.AutoRunPlayer | BuildOptions.StrictMode);
        }

        public BuildPlayerWindow()
        {
            minSize = new Vector2(640, 580);
            position = new Rect(50, 50, minSize.x, minSize.y);
            titleContent = L10n.TextContent("Build Settings", null, null, null);
        }

        void OnGUI()
        {
            // Override the 'old' build settings window in favor of the new build profile window.
            BuildPipeline.ShowBuildProfileWindow();
            Close();
        }

        // Major.Minor.Micro followed by one of abxfp followed by an identifier, optionally suffixed with " (revisionhash)"
        static readonly Regex s_VersionPattern = new Regex(@"(?<shortVersion>\d+\.\d+\.\d+(?<suffix>((?<alphabeta>[abx])|[fp])[^\s]*))( \((?<revision>[a-fA-F\d]+)\))?",
            RegexOptions.Compiled);

        internal static string GetPlaybackEngineDownloadURL(GUID platformGuid)
        {
            if (!BuildTargetDiscovery.BuildPlatformCanBeInstalledWithHub(platformGuid))
                return "https://unity3d.com/platform-installation";

            string fullVersion = InternalEditorUtility.GetFullUnityVersion();
            string revision = "";
            string shortVersion = "";
            Match versionMatch = s_VersionPattern.Match(fullVersion);
            if (!versionMatch.Success || !versionMatch.Groups["shortVersion"].Success || !versionMatch.Groups["suffix"].Success)
                Debug.LogWarningFormat("Error parsing version '{0}'", fullVersion);

            if (versionMatch.Groups["shortVersion"].Success)
                shortVersion = versionMatch.Groups["shortVersion"].Value;
            if (versionMatch.Groups["revision"].Success)
                revision = versionMatch.Groups["revision"].Value;

            var downloadLinkName = BuildTargetDiscovery.BuildPlatformDownloadLinkName(platformGuid);

            string prefix = "download";
            string suffix = "download_unity";
            string folder = "Unknown";
            string extension = string.Empty;

            if (versionMatch.Groups["alphabeta"].Success)
            {
                // These releases are hosted on the beta site
                prefix = "beta";
                suffix = "download";
            }

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                folder = "TargetSupportInstaller";
                extension = ".exe";
            }
            else if (Application.platform == RuntimePlatform.OSXEditor)
            {
                folder = "MacEditorTargetInstaller";
                extension = ".pkg";
            }
            else if (Application.platform == RuntimePlatform.LinuxEditor)
            {
                if (downloadLinkName == "Android" || downloadLinkName == "Mac-Mono" || downloadLinkName == "Windows-Mono")
                {
                    folder = "MacEditorTargetInstaller";
                    extension = ".pkg";
                }
                else
                {
                    folder = "LinuxEditorTargetInstaller";
                    extension = ".tar.xz";
                }
            }

            var architectureLabel = "";

            // WebGL uses a different artifact for arm64, so we need a separate url for it.
            // This url should be kept in sync with the one built in Tools/Build/InstallerBuildRecipes/MacEditorInstaller.pm BuildMacEditorInstallerSet()
            if (downloadLinkName == "WebGL"
                && RuntimeInformation.OSArchitecture == Architecture.Arm64
                && Application.platform == RuntimePlatform.OSXEditor)
            {
                architectureLabel = $"Arm64-";
            }

            return string.Format("https://{0}.unity3d.com/{1}/{2}/{3}/UnitySetup-{4}-Support-for-Editor-{5}{6}{7}", prefix, suffix, revision, folder, downloadLinkName, architectureLabel, shortVersion, extension);
        }

        internal static string GetUnityHubModuleDownloadURL(params GUID[] platformGuids)
        {
            string fullVersion = InternalEditorUtility.GetFullUnityVersion();
            string revision = "";
            string shortVersion = "";
            Match versionMatch = s_VersionPattern.Match(fullVersion);
            if (!versionMatch.Success || !versionMatch.Groups["shortVersion"].Success || !versionMatch.Groups["suffix"].Success)
                Debug.LogWarningFormat("Error parsing version '{0}'", fullVersion);

            if (versionMatch.Groups["shortVersion"].Success)
                shortVersion = versionMatch.Groups["shortVersion"].Value;
            if (versionMatch.Groups["revision"].Success)
                revision = versionMatch.Groups["revision"].Value;

            var downloadLinkNames = new List<string>(platformGuids.Length);
            foreach (var platformGuid in platformGuids)
            {
                var downloadLinkName = BuildTargetDiscovery.BuildPlatformDownloadLinkName(platformGuid).ToLower();

                if (string.IsNullOrEmpty(downloadLinkName))
                    continue;

                if (!downloadLinkNames.Contains(downloadLinkName))
                    downloadLinkNames.Add(downloadLinkName);
            }

            return string.Format("unityhub://{0}/{1}/module={2}", shortVersion, revision, string.Join("&module=", downloadLinkNames));
        }

        internal static bool IsEditorInstalledWithHub()
        {
            var applicationFolderPath = Directory.GetParent(EditorApplication.applicationPath).FullName;
            var path = "";

            if (Application.platform == RuntimePlatform.OSXEditor)
                path = Path.Combine(applicationFolderPath, "modules.json");
            else if (Application.platform == RuntimePlatform.WindowsEditor)
                path = Path.Combine(Directory.GetParent(applicationFolderPath).FullName, "modules.json");
            else if (Application.platform == RuntimePlatform.LinuxEditor)
                path = Path.Combine(Directory.GetParent(applicationFolderPath).FullName, "modules.json");

            return File.Exists(path);
        }
    }
}
